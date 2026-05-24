using BU_Love.API.DTO;

namespace BU_Love.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> Login(LoginDto loginDto);
        Task<AuthResponseDto> Register(RegisterDto registerDto);
    }
}
