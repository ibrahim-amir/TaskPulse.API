namespace TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
// Example usage:
//
// Registration (Program.cs):
// builder.Services.AddScoped<EntityExistenceChecker>();
//
// Then inject it wherever it's needed, same as CheckValidationRun:
// public class TaskService
// {
//     private readonly EntityExistenceChecker _existenceChecker;
//     public TaskService(EntityExistenceChecker existenceChecker) => _existenceChecker = existenceChecker;
// }
//
// Single entity check, by Id:
// bool exists = await _existenceChecker.UserExistsById(dto.AssignedUserId);
// if (!exists) errors.Add("Assigned user does not exist.");
//
// Single entity check, by a specific field:
// bool taken = await _existenceChecker.UserExistsByName(dto.Name);
// if (taken) errors.Add("That name is already in use.");
//
// Batch check by Id — any mix of entity types, any count, one call:
// List<string> missing = await _existenceChecker.CheckAllByID(
//     (typeof(Project), dto.ProjectId),
//     (typeof(User), dto.AssignedUserId),
//     (typeof(User), dto.CreatedByUserId));
//
// if (missing.Count > 0)
//     errors.AddRange(missing); // empty list = everything exists
//
// Batch check by field (not Id) — any entity, any field name, any value:
// List<string> missing = await _existenceChecker.CheckAllFieldExists(
//     (typeof(User), "Name", dto.Name),
//     (typeof(User), "Email", dto.Email),
//     (typeof(Notification), "Message", dto.Message));
//
// if (missing.Count > 0)
//     errors.AddRange(missing); // empty list = a matching row exists for every field
//
// Duplicate check (the opposite direction) — flags fields that ARE already taken,
// for use before creating a new row (e.g. registration: Name/Email must be free):
// errors.AddRange(await _existenceChecker.CheckFieldsAreUnique(
//     (typeof(User), "Name", obj.Name),
//     (typeof(User), "Email", obj.Email)));

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.Entities;

public class EntityExistenceChecker
{
    private readonly AppDbContext _db;

    public EntityExistenceChecker(AppDbContext db)
    {
        _db = db;
    }

    // --- Individual per-entity checks, by Id ---
    // One function per entity in AppDbContext, each just a thin wrapper
    // around the shared generic lookup below.

    public Task<bool> UserExistsById(object id) => ExistsAsync<User>(id);

    // Field-based check, not by Id — separate from the Id checks above
    // since it queries a specific column instead of the primary key.
    public Task<bool> UserExistsByName(string name) =>
        _db.Users.AnyAsync(u => u.Name == name);

    public Task<bool> ProjectExistsById(object id) => ExistsAsync<Project>(id);
    public Task<bool> TaskExistsById(object id) => ExistsAsync<TaskItem>(id);
    public Task<bool> ProjectMemberExistsById(object id) => ExistsAsync<ProjectMember>(id);
    public Task<bool> CommentExistsById(object id) => ExistsAsync<Comment>(id);
    public Task<bool> TaskAttachmentExistsById(object id) => ExistsAsync<TaskAttachment>(id);
    public Task<bool> NotificationExistsById(object id) => ExistsAsync<Notification>(id);
    public Task<bool> ActivityLogExistsById(object id) => ExistsAsync<ActivityLog>(id);

    // Shared generic lookup by primary key. FindAsync checks the change tracker
    // first (no DB round trip for an entity already loaded this request), then
    // falls back to a real query.
    private async Task<bool> ExistsAsync<TEntity>(object id) where TEntity : class
    {
        return await _db.Set<TEntity>().FindAsync(id) is not null;
    }

    // --- Batch check by Id: any number of entities, any mix of types, one call ---
    //
    // Pass (Type, id) pairs — e.g. (typeof(Project), dto.ProjectId). Add a case
    // to the switch below whenever a new entity type needs to participate here.
    // Returns the list of "not found" messages — empty list means everything exists.
    public async Task<List<string>> CheckAllByID(params (Type EntityType, object Id)[] checks)
    {
        var missing = new List<string>();

        foreach (var (entityType, id) in checks)
        {
            bool exists = entityType.Name switch
            {
                nameof(User) => await UserExistsById(id),
                nameof(Project) => await ProjectExistsById(id),
                nameof(TaskItem) => await TaskExistsById(id),
                nameof(ProjectMember) => await ProjectMemberExistsById(id),
                nameof(Comment) => await CommentExistsById(id),
                nameof(TaskAttachment) => await TaskAttachmentExistsById(id),
                nameof(Notification) => await NotificationExistsById(id),
                nameof(ActivityLog) => await ActivityLogExistsById(id),
                _ => throw new NotSupportedException($"No existence check registered for type '{entityType.Name}'.")
            };

            if (!exists)
                missing.Add($"{entityType.Name} with id '{id}' does not exist.");
        }

        return missing;
    }

    // --- Batch check by field: any entity, any field name, any value, one call ---
    //
    // Unlike CheckAllByID above, this doesn't need a switch/case per entity — it builds
    // the "WHERE FieldName == value" query at runtime via reflection, so it works for
    // a field on any entity without adding new code here first.
    // Returns the list of "not found" messages — empty list means every row was found.
    public async Task<List<string>> CheckAllFieldExists(params (Type EntityType, string FieldName, object Value)[] checks)
    {
        var missing = new List<string>();

        foreach (var (entityType, fieldName, value) in checks)
        {
            bool exists = await FieldExistsAsync(entityType, fieldName, value);
            if (!exists)
                missing.Add($"{entityType.Name}.{fieldName} with value '{value}' does not exist.");
        }

        return missing;
    }

    // --- Duplicate check: opposite of CheckAllFieldExists ---
    //
    // Use this before creating a new row, where a match means "already taken"
    // (an error), not "found" (a good thing). Same call shape as CheckAllFieldExists,
    // reuses the same lookup — just inverts which result counts as a problem.
    // Returns the list of "already in use" messages — empty list means every value is free.
    public async Task<List<string>> CheckFieldsAreUnique(params (Type EntityType, string FieldName, object Value)[] checks)
    {
        var duplicates = new List<string>();

        foreach (var (entityType, fieldName, value) in checks)
        {
            bool exists = await FieldExistsAsync(entityType, fieldName, value);
            if (exists)
                duplicates.Add($"{entityType.Name}.{fieldName} with value '{value}' is already in use.");
        }

        return duplicates;
    }

    // --- Duplicate check that ignores one row: for updates, not creates ---
    //
    // Same as CheckFieldsAreUnique, except the row being edited doesn't count as
    // a duplicate of itself — otherwise saving a user without changing their name
    // would fail on their own name. Relies on every entity inheriting Id.
    // errors.AddRange(await _existenceChecker.CheckFieldsAreUniqueExcluding(
    //     obj.Id, (typeof(User), "Name", obj.Name)));
    public async Task<List<string>> CheckFieldsAreUniqueExcluding(object excludedId, params (Type EntityType, string FieldName, object Value)[] checks)
    {
        var duplicates = new List<string>();

        foreach (var (entityType, fieldName, value) in checks)
        {
            bool exists = await FieldExistsAsync(entityType, fieldName, value, excludedId);
            if (exists)
                duplicates.Add($"{entityType.Name}.{fieldName} with value '{value}' is already in use.");
        }

        return duplicates;
    }

    private async Task<bool> FieldExistsAsync(Type entityType, string fieldName, object value, object? excludedId = null)
    {
        // _db.Set<TEntity>() — but TEntity is only known as a runtime Type here,
        // so call the generic method via reflection instead of Set<T>() directly.
        var setMethod = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), 1, Type.EmptyTypes)!
            .MakeGenericMethod(entityType);
        var dbSet = setMethod.Invoke(_db, null)!;

        // Build the equivalent of: e => e.<FieldName> == value
        var property = entityType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
            throw new NotSupportedException($"'{entityType.Name}' has no property named '{fieldName}'.");

        var parameter = Expression.Parameter(entityType, "e");
        var propertyAccess = Expression.Property(parameter, property);
        var constant = Expression.Constant(value, property.PropertyType);
        Expression predicateBody = Expression.Equal(propertyAccess, constant);

        // On an update, add "&& e.Id != excludedId" so the row being edited
        // isn't counted as a duplicate of itself.
        if (excludedId is not null)
        {
            var idProperty = entityType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProperty is null)
                throw new NotSupportedException($"'{entityType.Name}' has no Id property to exclude on.");

            var idNotEqual = Expression.NotEqual(
                Expression.Property(parameter, idProperty),
                Expression.Constant(excludedId, idProperty.PropertyType));

            predicateBody = Expression.AndAlso(predicateBody, idNotEqual);
        }

        var predicateType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        var lambda = Expression.Lambda(predicateType, predicateBody, parameter);

        // Call the equivalent of: dbSet.AnyAsync(lambda)
        var anyAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync) && m.GetParameters().Length == 3)
            .MakeGenericMethod(entityType);

        var task = (Task<bool>)anyAsyncMethod.Invoke(null, new object[] { dbSet, lambda, CancellationToken.None })!;
        return await task;
    }
}