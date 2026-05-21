namespace BU_Love.API.DTO
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public int ItemsCount { get; set; }
    }
}
