using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.AttendanceDto;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Repository.ActivityLogRepo;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class AttendanceService : BaseServiceClass, IAttendanceService
    {
        private readonly UserRepository _userRepository;
        private readonly ActivityLogRepository _activityLogRepository;

        public AttendanceService(
            CustomExceptionHandler customExceptionHandler,
            UserRepository userRepository,
            ActivityLogRepository activityLogRepository)
            : base(customExceptionHandler)
        {
            _userRepository = userRepository;
            _activityLogRepository = activityLogRepository;
        }

        public async Task<CommonResponse<AttendanceSummaryDto>> GetAttendanceAsync(int userId, AttendanceQueryDto query)
        {
            var response = new CommonResponse<AttendanceSummaryDto>();

            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<AttendanceSummaryDto>("User not found.", HttpStatusCode.NotFound);

                var (from, to) = ResolveRange(query);
                if (from > to)
                    return CommonResponseFactory.CreateErrorResponse<AttendanceSummaryDto>("'from' cannot be after 'to'.", HttpStatusCode.BadRequest);

                //Whole days, so a login at any time on the last day still counts.
                var loginTimes = await _activityLogRepository.GetLoginTimesAsync(
                    userId,
                    from.ToDateTime(TimeOnly.MinValue),
                    to.ToDateTime(TimeOnly.MaxValue));

                var firstLoginPerDay = loginTimes
                    .GroupBy(t => DateOnly.FromDateTime(t))
                    .ToDictionary(g => g.Key, g => g.Min());

                var days = BuildDays(from, to, firstLoginPerDay);

                var summary = new AttendanceSummaryDto
                {
                    UserId = userId,
                    From = from,
                    To = to,
                    TotalDays = days.Count,
                    DaysPresent = days.Count(d => d.IsPresent),
                    Days = days
                };

                return CommonResponseFactory.CreateSuccessResponse(summary, "Attendance retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        //No dates given means the current calendar month.
        private static (DateOnly From, DateOnly To) ResolveRange(AttendanceQueryDto query)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = query.From ?? new DateOnly(today.Year, today.Month, 1);
            var to = query.To ?? today;
            return (from, to);
        }

        //Every date in the range, present or not, so the frontend can draw a
        //calendar without filling gaps itself.
        private static List<AttendanceDayDto> BuildDays(DateOnly from, DateOnly to, Dictionary<DateOnly, DateTime> firstLoginPerDay)
        {
            var days = new List<AttendanceDayDto>();

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var present = firstLoginPerDay.TryGetValue(date, out var firstLogin);

                days.Add(new AttendanceDayDto
                {
                    Date = date,
                    IsPresent = present,
                    FirstLoginAt = present ? firstLogin : null
                });
            }

            return days;
        }
    }
}
