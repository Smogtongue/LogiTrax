using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LogiTrax.Models
{
    public enum OrderStatus
    {
        Pending,
        Complete,
        Rejected
    }

    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime DatePlaced { get; set; }
        public string? SessionId { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}