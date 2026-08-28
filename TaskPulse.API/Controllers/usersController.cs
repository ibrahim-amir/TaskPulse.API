using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Repository.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class usersController : ControllerBase
    {
        private readonly IUserServices _userservice;

        public usersController(IUserServices userservice)
        {
            _userservice = userservice;
        }


        // These are where the actual services Start.
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto userdto)
        {
            var result = await _userservice.CreateUserAsync(userdto);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {

            // service to get user by id.
            return Ok();
        }
    }
}
