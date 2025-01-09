using System.ComponentModel.DataAnnotations;

namespace om_svc_order.Models
{
    public class OrderItemHistory
    {
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid ItemId { get; set; }

        [Required]
        public string? ItemName { get; set; }

        [Required]
        public string? ItemCategory { get; set; }

        [Required]
        public int OldQuantity { get; set; }

        [Required]
        public int NewQuantity { get; set; }

        public DateTime ChangedDate { get; set; }

        [Required]
        public Guid ChangedByUserId { get; set; }

        [Required]
        public string? ChangedByUserName { get; set; }

        [Required]
        public OrderItemChangeType ChangeType { get; set; }

    }
}
