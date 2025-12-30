namespace IntegratedAPI.Models.DTOs
{
    public class PaymentIntentCreateRequest
    {
        public long Amount { get; set; }
        public string? Currency { get; set; }
        public Guid OrderId { get; set; }
    }
}
