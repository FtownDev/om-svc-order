using om_svc_order.Models;
using System.ComponentModel.DataAnnotations;

namespace om_svc_order.DTO
{
    public class OrderRetrieveResponse
    {
        [Required]
        public int pageSize { get; set; }

        [Required]
        public int totalCount { get; set; }

        [Required]
        public List<Order> orders { get; set; }
    }
}
