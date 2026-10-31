using System;
using TvmTicketing.Domain;
using TvmTicketing.Enums;

namespace TvmTicketing.Payment
{
    public class WalletGatewayAdapter : IPaymentGateway
    {
        public WalletProvider Provider { get; set; } = WalletProvider.ZaloPay;
        public string apiVersion { get; set; } = "v2.4";

        public string GenerateWalletQR(Transaction tx)
        {
            return $"{Provider.ToString().ToUpper()}://{apiVersion}/pay?tx={tx.ShortCode}&amount={tx.TotalAmountVnd}&ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        public string Initiate(Transaction tx) => GenerateWalletQR(tx);

        public bool VerifyNotification(string payload, string signature) => !string.IsNullOrEmpty(signature);

        public string Refund(Domain.Payment payment, string reason)
        {
            return $"{Provider.ToString().ToUpper()}-REF-{DateTime.Now:HHmmss}-{payment.PaymentId.ToString("N").Substring(0, 4).ToUpper()}";
        }

        public void reconnect() { }
    }
}
