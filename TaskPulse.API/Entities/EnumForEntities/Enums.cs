namespace TaskPulse.API.Entities.EnumForEntities;

public enum UserRole
{
    Admin, 
    Manager, 
    Member
}
public enum ProjectStatus
{
    NotStarted,
    Active,
    Completed,
    Archived
}

public enum PriorityLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum TaskItemStatus
{
    ToDo,
    InProgress,
    Review,
    Done,
    Blocked
}

// Written to ActivityLog.Action via .ToText(). Login rows are also what
// attendance is read back from, so don't rename that one casually.
public enum ActivityAction
{
    Login,
    Logout,
    UserCreated,
    UserProfileUpdated,
    UserRoleChanged,
    UserStatusChanged,
    EmailConfirmed,
    PasswordReset
}