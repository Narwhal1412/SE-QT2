using System;
using TvmTicketing.Domain;

namespace TvmTicketing.Payment
{
    public class BankQRAdapter : IPaymentGateway
    {
        public string GenerateVietQR(Transaction tx)
        {
            return $"00020101021238570010A000000727012700069704360113METROGO{tx.ShortCode}5303704540{tx.TotalAmountVnd}5802VN6304";
        }

        public string Initiate(Transaction tx) => GenerateVietQR(tx);

        public bool VerifyNotification(string payload, string signature) => !string.IsNullOrEmpty(signature);

        public string Refund(Domain.Payment payment, string reason)
        {
            return $"VIETQR-REF-{DateTime.Now:HHmmss}-{payment.PaymentId.ToString("N").Substring(0, 5).ToUpper()}";
        }

        public void RefreshToken() { }

        public void reconnect() { }
    }
}
