using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Helper.AuthorizationHelper
{
    // "Can this person see this project, and are they allowed to change it?"
    // Projects and tasks both ask, so the rules live here instead of being
    // written out in each service.
    //
    // Each check returns null when the caller is allowed, or the reason they
    // aren't — so a service can hand that straight back as the error message:
    //
    //     var denied = ProjectAccess.CheckManager(project, callerId, callerIsAdmin);
    //     if (denied is not null)
    //         return CommonResponseFactory.CreateErrorResponse<T>(denied, HttpStatusCode.Forbidden);
    //
    // Expects project.Members to be loaded.
    public static class ProjectAccess
    {
        // Reading: any member of the project, and the Admin.
        public static string? CheckMember(Project project, int callerId, bool callerIsAdmin)
        {
            if (callerIsAdmin)
                return null;

            return project.Members.Any(m => m.UserId == callerId)
                ? null
                : "You are not a member of this project.";
        }

        // Changing: only a manager of that project, and the Admin.
        public static string? CheckManager(Project project, int callerId, bool callerIsAdmin)
        {
            if (callerIsAdmin)
                return null;

            var membership = project.Members.FirstOrDefault(m => m.UserId == callerId);

            if (membership is null)
                return "You are not a member of this project.";

            if (membership.Role != UserRole.Manager)
                return "Only a manager of this project can change it.";

            return null;
        }

        public static bool IsManager(Project project, int callerId, bool callerIsAdmin)
        {
            return CheckManager(project, callerId, callerIsAdmin) is null;
        }
    }
}
