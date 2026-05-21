namespace BU_Love.API.DTO
{
    public class CreateOrderDto
    {
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }
}
