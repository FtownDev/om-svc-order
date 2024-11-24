using om_svc_order.Models;
using System.ComponentModel.DataAnnotations;

namespace om_svc_order.DTO
{
    public class OrderItemUpdateRequest
    {
        [Required]
        public Guid itemId { get; set; }

        [Required]
        public int qty { get; set; } 

        [Required]
        public OrderItemChangeType changeType { get; set; }
    }
}
