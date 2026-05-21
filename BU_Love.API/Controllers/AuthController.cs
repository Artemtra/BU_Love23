using BU_Love.API.DTO;
using BU_Love.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BU_Love.API.Controllers
{

        [ApiController]
        [Route("api/[controller]")]
        public class AuthController : ControllerBase
        {
            private readonly IAuthService _authService;

            public AuthController(IAuthService authService)
            {
                _authService = authService;
            }

            [HttpPost("login")]
            public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
            {
                try
                {
                    var response = await _authService.Login(loginDto);
                    return Ok(response);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Unauthorized(new { message = ex.Message });
                }
            }
        }
    
}
