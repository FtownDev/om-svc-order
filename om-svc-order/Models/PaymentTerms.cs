namespace om_svc_order.Models
{
    public enum PaymentTerms
    {
        PayInFull = 1,           // Full payment required at booking
        DepositWithBalance = 2,  // Initial deposit with balance due before event
        Net30 = 3,              // Full payment within 30 days of invoice
        Net10 = 4,              // Full payment within 15 days of invoice
        CustomPaymentPlan = 5,  // Special arrangement with multiple payment dates
        CorporateAccount = 6    // Special terms for approved business accounts
    }

}
