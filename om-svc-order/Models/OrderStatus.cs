namespace om_svc_order.Models
{
    public enum OrderStatus
    {
        Draft = 1,
        Pending = 2,
        Confirmed = 3,
        InProgress = 4,
        ReadyForDelivery = 5,
        OutForDelivery = 6,
        Delivered = 7,
        InUse = 8,
        PendingPickup = 9,
        PickupInProgress = 10,
        Returned = 11,
        Complete = 12,
        Cancelled = 13
    }
}