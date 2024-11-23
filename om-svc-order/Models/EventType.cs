using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace om_svc_order.Models
{
    [Table("EventTypes")]
    public class EventType
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

    }
}
