using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.HelpDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface IHelpAssistantService
    {
        Task<CommonResponse<HelpAnswerDto>> AskAsync(AskHelpDto dto, int callerId, bool callerIsAdmin, string callerRole);
    }
}
