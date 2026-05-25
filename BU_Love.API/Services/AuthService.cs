using BU_Love.API.DB;
using BU_Love.API.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BU_Love.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly BuLoveDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(BuLoveDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> Login(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
                throw new UnauthorizedAccessException("Пользователь не найден");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Неверный пароль");

            return GenerateToken(user);
        }

        public async Task<AuthResponseDto> Register(RegisterDto registerDto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
                throw new InvalidOperationException("Пользователь уже существует");

            var user = new User
            {
                Username = registerDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = "Customer",
                Phone = registerDto.Phone,      
                Address = registerDto.Address,   
                BonusPoints = 100,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return GenerateToken(user);
        }
        private AuthResponseDto GenerateToken(User user)
        {
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, user.Username ?? ""),
        new Claim(ClaimTypes.Role, user.Role ?? "Customer"),
        new Claim("userId", user.Id.ToString()) // Уберите .Value, оставьте просто Id
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "DefaultKey1234567890"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "BU_Love",
                audience: _configuration["Jwt:Audience"] ?? "BU_Love_Client",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.Username ?? "",
                Role = user.Role ?? "Customer",
                Phone = user.Phone ?? "",
                Address = user.Address ?? "",
                BonusPoints = user.BonusPoints ?? 0
            };
        }
    }
    
}
