using System;
using TvmTicketing.Enums;

namespace TvmTicketing.Domain
{
    public class Payment
    {
        public Guid PaymentId { get; set; } = Guid.NewGuid();
        public Guid TransactionId { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string ProviderReference { get; set; } = string.Empty;
        public string currency { get; set; } = "VND";
        public WalletProvider WalletProvider { get; set; } = WalletProvider.None;
        public int AmountVnd { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public void MarkConfirmed(string reference = "")
        {
            Status = PaymentStatus.Confirmed;
            if (!string.IsNullOrEmpty(reference))
                ProviderReference = reference;
        }

        public void MarkRefundPending(string reason = "")
        {
            Status = PaymentStatus.RefundPending;
            if (!string.IsNullOrEmpty(reason))
                ProviderReference = $"{ProviderReference} [REFUND: {reason}]";
        }
    }
}
