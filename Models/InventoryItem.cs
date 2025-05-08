using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrax.Models
{
    public class InventoryItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ItemId { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }  // Represents available stock.
        public string Location { get; set; } = string.Empty;

        // Mark as nullable so it’s allowed to be null until set by EF Core.
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
