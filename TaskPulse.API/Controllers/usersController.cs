using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class usersController : ControllerBase
    {
        private readonly IUserServices _userservice;

        public usersController(IUserServices userservice)
        {
            _userservice = userservice;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {

            var result = await _userservice.GetUserById(id);
            return StatusCode((int)result.StatusCode, result);
        }

    }
}
