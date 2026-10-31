using System;
using TvmTicketing.Domain;

namespace TvmTicketing.Payment
{
    public class CardProcessorGateway : IPaymentGateway
    {
        public string terminalId { get; set; } = "EMV-TD004-01";

        public string AuthorizeCard(string maskedPan, int amountVnd)
        {
            return $"EMV-AUTH-{Random.Shared.Next(100000, 999999)}";
        }

        public string Initiate(Transaction tx)
        {
            return $"CARD-PROMPT|{terminalId}|{tx.ShortCode}|{tx.TotalAmountVnd}";
        }

        public bool VerifyNotification(string payload, string signature) => !string.IsNullOrEmpty(signature);

        public string Refund(Domain.Payment payment, string reason)
        {
            return $"VOID-CARD-{DateTime.Now:HHmmss}-{payment.PaymentId.ToString("N").Substring(0, 5).ToUpper()}";
        }

        public void reconnect() { }
    }
}
