using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace om_svc_order.Models
{
    [Table("Orders")]
    public class Order
    {
        public Guid Id { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public Guid EventTypeId { get; set; }

        [Required]
        public Guid BilledToCustomerId { get; set; }

        [Required]
        public Guid BilledToAddressId { get; set; }

        [Required]
        public Guid ShippedToAddressId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal BalanceDue { get; set; }

        public List<List<DateTime>>? DeliveryWindow { get; set; } 

        public List<List<DateTime>>? PickupWindow { get; set; }

        public string? DeliveryPickupNotes { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Deposit { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ItemTotalValue { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(3,2)")]
        public decimal TaxRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TaxValue { get; set; }

        public OrderStatus CurrentStatus { get; set; }

        [Required]
        public PaymentTerms PaymentTerms { get; set; }
    }
}
