using System.ComponentModel.DataAnnotations;

namespace om_svc_order.Models
{
    public class OrderHistory
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId  { get; set; }

        [Required]
        public required string PropertyName { get; set; }

        [Required]
        public required string ChangedFrom { get; set; }

        [Required]
        public required string ChangedTo { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; }

        [Required]
        public Guid ChangedByUserId { get; set; }
    }
}
