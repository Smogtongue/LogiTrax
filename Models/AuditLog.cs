using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrax.Models
{
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuditLogId { get; set; }

        // The timestamp when the audit event occurred.
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // The user who performed the action.
        public string User { get; set; } = string.Empty;

        // A brief description of the action (e.g., "Order Created", "Order Status Updated").
        public string Action { get; set; } = string.Empty;

        // Additional details about the action.
        public string Details { get; set; } = string.Empty;
    }
}
