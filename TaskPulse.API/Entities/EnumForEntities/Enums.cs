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
    UserDeleted,
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
    RemoveProjectMember,

    // A Member/Manager submitting a task's completed checklist for the
    // Admin to sign off on — always gated, unlike the other three which are
    // only gated when a Manager (not the Admin) is the one acting.
    CompleteTask,

    // Someone not currently on a task asking to be added to it — always
    // gated, same reasoning as CompleteTask.
    AssignTaskRequest,

    // Someone other than the task's creator editing it — resolved by that
    // creator, not by an Admin (see UpdateTask edit-request endpoints).
    UpdateTask,

    // A Manager assigning a user who's marked themselves busy — resolved by
    // that user, or by an Admin (see the assignment-requests endpoints).
    AssignBusyUser,

    // Someone other than the task's creator deleting it — resolved by that
    // creator, not by an Admin. Same creator-resolved gate as UpdateTask,
    // reusing the same edit-request endpoints.
    DeleteTask,

    // A plain Member duplicating a task — resolved by a Manager/Admin of
    // that task's project, not the generic Admin-only queue (see the
    // duplicate-requests endpoints). A Manager/Admin's own duplicate never
    // goes through this — it's created immediately.
    DuplicateTask
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
    ApprovalResolved,

    // A manual, user-composed notification sent from one account to another
    // (e.g. from a profile page) — not raised by the system itself.
    DirectMessage
}