namespace TvmTicketing.Enums
{
    public enum TVMStatus
    {
        Idle,
        Active,
        OutOfService,
        Maintenance
    }

    public enum TransactionStatus
    {
        Selecting,
        AwaitingPayment,
        Paid,
        Printing,
        Completed,
        Cancelled,
        Failed,
        RefundPending,
        Refunded
    }

    public enum TicketType
    {
        SingleJourney,
        OneDayPass
    }

    public enum FareCategory
    {
        Adult,
        Student,
        Senior
    }

    public enum PrinterStatus
    {
        Ready,
        Printing,
        PaperLow,
        PaperOut,
        Jammed
    }

    public enum PaymentMethod
    {
        CreditCard,
        BankQR,
        DigitalWallet
    }

    public enum PaymentStatus
    {
        Pending,
        Confirmed,
        Declined,
        TimedOut,
        RefundPending,
        Refunded
    }

    public enum WalletProvider
    {
        None,
        MoMo,
        ZaloPay,
        VNPay
    }
}
