using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.AttendanceDto;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class usersController : BaseApiController
    {
        private readonly IUserServices _userservice;
        private readonly IAttendanceService _attendanceService;

        public usersController(IUserServices userservice, IAttendanceService attendanceService)
        {
            _userservice = userservice;
            _attendanceService = attendanceService;
        }

        [HttpGet]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> GetUsers([FromQuery] UserQueryDto query)
        {
            return Respond(await _userservice.GetUsersAsync(query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            if (!User.CanAccessUser(id))
                return Forbidden("You can only view your own account.");

            return Respond(await _userservice.GetUserById(id));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, UpdateUserProfileDto dto)
        {
            if (!User.CanAccessUser(id))
                return Forbidden("You can only update your own account.");

            //The id is the route's, never the body's.
            dto.Id = id;
            return Respond(await _userservice.UpdateUserProfileAsync(dto));
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
        {
            dto.Id = id;
            return Respond(await _userservice.UpdateUserRoleAsync(dto));
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatusDto dto)
        {
            dto.Id = id;
            return Respond(await _userservice.SetUserStatusAsync(dto));
        }

        //Disables the account rather than deleting the row, so their tasks,
        //comments and activity history survive.
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> DisableUser(int id)
        {
            var dto = new UpdateUserStatusDto { Id = id, IsDisabled = true };
            return Respond(await _userservice.SetUserStatusAsync(dto));
        }

        [HttpGet("{id}/attendance")]
        public async Task<IActionResult> GetAttendance(int id, [FromQuery] AttendanceQueryDto query)
        {
            if (!User.CanAccessUser(id))
                return Forbidden("You can only view your own attendance.");

            return Respond(await _attendanceService.GetAttendanceAsync(id, query));
        }
    }
}
