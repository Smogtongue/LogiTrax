// File: DTOs/OrderStatusUpdateDto.cs
namespace LogiTrax.DTOs
{
    public class OrderStatusUpdateDto
    {
        // Valid values should be: "Pending", "Complete", or "Rejected".
        public string Status { get; set; } = string.Empty;
    }
}
