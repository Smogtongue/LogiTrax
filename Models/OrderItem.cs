using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LogiTrax.Models
{
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderItemId { get; set; }
        
        // Foreign key to the Order.
        public int OrderId { get; set; }
        
        [BindNever]
        [ValidateNever]
        [JsonIgnore]
        public Order Order { get; set; } = null!;
        
        // Foreign key to the InventoryItem (product) being ordered.
        public int InventoryItemId { get; set; }
        
        [BindNever]
        [ValidateNever]
        [JsonIgnore]
        public InventoryItem InventoryItem { get; set; } = null!;
        
        // Quantity ordered.
        public int Quantity { get; set; }
    }
}
