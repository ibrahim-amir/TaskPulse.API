namespace TaskPulse.API.Helper.EntityFieldNameEnum;

// Example usage:
//
// string fieldName = Entity.User.Name.ToText(); // "Name"
//
// Drop straight into the existence checker instead of magic strings:
// errors.AddRange(await _existenceChecker.CheckFieldsAreUnique(
//     (typeof(User), Entity.User.Name.ToText(), obj.Name),
//     (typeof(User), Entity.User.Email.ToText(), obj.Email)));
//
// Note: only scalar data fields are listed (plus the inherited Id/CreatedAt) —
// navigation properties (Manager, Project, Comments, etc.) are left out since
// they're not something CheckAllByField/CheckFieldsAreUnique can compare against
// a plain value anyway.

using System;

public static class Entity
{
    public enum User
    {
        Id,
        CreatedAt,
        Name,
        Email,
        PasswordHash,
        Role,
        profileImageUrl,
        IsDisabled
    }

    public enum Project
    {
        Id,
        CreatedAt,
        Name,
        Description,
        Status,
        Priority,
        StartDate,
        EndDate,
        ManagerId
    }

    public enum TaskItem
    {
        Id,
        CreatedAt,
        Title,
        Description,
        Status,
        Priority,
        DueDate,
        EstimatedHours,
        ActualHours,
        ProjectId,
        AssignedUserId,
        CreatedByUserId,
        UpdatedAt
    }

    public enum ProjectMember
    {
        Id,
        CreatedAt,
        ProjectId,
        UserId
    }

    public enum Comment
    {
        Id,
        CreatedAt,
        TaskId,
        UserId,
        Body,
        UpdatedAt
    }

    public enum TaskAttachment
    {
        Id,
        CreatedAt,
        TaskId,
        FileName,
        StoredPath,
        ContentType,
        FileSizeBytes,
        UploadedByUserId
    }

    public enum Notification
    {
        Id,
        CreatedAt,
        UserId,
        Type,
        Message,
        IsRead
    }

    public enum ActivityLog
    {
        Id,
        CreatedAt,
        UserId,
        Action,
        Details,
        ProjectId,
        TaskId
    }
}

public static class EntityFieldExtensions
{
    // Works for any of the enums above — no per-enum overload needed.
    public static string ToText(this Enum field) => field.ToString();
}
