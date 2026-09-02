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
    PasswordReset,
    ProjectCreated,
    ProjectUpdated,
    ProjectDeleted,
    ProjectMemberAdded,
    ProjectMemberRemoved,
    ApprovalRequested,
    ApprovalApproved,
    ApprovalRejected,
    TaskCreated,
    TaskUpdated,
    TaskStatusChanged,
    TaskDeleted,
    TaskAssigned,
    TaskUnassigned,
    TaskClosed,
    SubtaskAdded,
    SubtaskRemoved,
    FileUploaded,
    CommentAdded,
    CommentUpdated,
    CommentDeleted
}

// What a pending ApprovalRequest is asking permission to do. Every value here
// needs a matching IApprovalActionHandler, or approving it will fail.
public enum ApprovalActionType
{
    UpdateProject,
    DeleteProject,
    RemoveProjectMember
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum NotificationType
{
    TaskAssigned,
    TaskDueSoon,
    TaskDeadlineMissed,
    CommentAdded,
    AddedToProject,
    ApprovalRequired,
    ApprovalResolved
}