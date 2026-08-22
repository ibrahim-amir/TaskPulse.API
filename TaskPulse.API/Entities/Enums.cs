namespace TaskPulse.API.Entities;

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


