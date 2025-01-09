using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace om_svc_order.Models
{
    public class OrderItem
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        [Required]
        public Guid ItemId { get; set; }

        [Required]
        public string? ItemName { get; set; }

        [Required]
        public string? ItemCategory { get; set; }

        [Required]
        public int Qty { get; set; }

        [Required]
        public decimal Price { get; set; }
    }
}