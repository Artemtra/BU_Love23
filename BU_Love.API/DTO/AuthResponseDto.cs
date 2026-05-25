namespace BU_Love.API.DTO
{
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal BonusPoints { get; set; }
    }
}
