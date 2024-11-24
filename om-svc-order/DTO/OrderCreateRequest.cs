using om_svc_order.Models;
using System.ComponentModel.DataAnnotations;

namespace om_svc_order.DTO
{ 
    public class OrderCreateRequest
    {
        [Required]
        public Order order { get; set; }

        [Required]
        public List<OrderItem> orderItems { get; set; }
    }
    
}
