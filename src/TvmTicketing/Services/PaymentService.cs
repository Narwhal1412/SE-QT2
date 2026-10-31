using System;
using TvmTicketing.Domain;
using TvmTicketing.Enums;
using TvmTicketing.Payment;

namespace TvmTicketing.Services
{
    public class PaymentService
    {
        public IPaymentGateway Gateway { get; set; } = new WalletGatewayAdapter();

        public (Domain.Payment payment, string qrOrPrompt) InitiatePayment(
            Transaction tx,
            PaymentMethod method,
            WalletProvider walletProvider = WalletProvider.None)
        {
            Gateway = method switch
            {
                PaymentMethod.CreditCard => new CardProcessorGateway(),
                PaymentMethod.BankQR => new BankQRAdapter(),
                _ => new WalletGatewayAdapter { Provider = walletProvider == WalletProvider.None ? WalletProvider.ZaloPay : walletProvider }
            };

            var payment = new Domain.Payment
            {
                PaymentId = Guid.NewGuid(),
                TransactionId = tx.TransactionId,
                Method = method,
                WalletProvider = walletProvider,
                AmountVnd = tx.TotalAmountVnd,
                Status = PaymentStatus.Pending,
                ProviderReference = $"{method.ToString().ToUpper()}-{tx.ShortCode}"
            };

            tx.Payments.Add(payment);
            tx.Status = TransactionStatus.AwaitingPayment;
            return (payment, Gateway.Initiate(tx));
        }

        public bool VerifyCallback(Domain.Payment payment, string signature = "HMAC-SHA256-VALID")
        {
            if (!Gateway.VerifyNotification(payment.ProviderReference, signature))
                return false;

            string refPrefix = payment.Method switch
            {
                PaymentMethod.CreditCard => "VISA-AUTH",
                PaymentMethod.BankQR => "NAPAS-VQR",
                _ => payment.WalletProvider.ToString().ToUpper()
            };
            payment.MarkConfirmed($"{refPrefix}-{Random.Shared.Next(100000, 999999)}");
            return true;
        }

        public string RequestRefund(Domain.Payment payment, string reason)
        {
            string refundRef = Gateway.Refund(payment, reason);
            payment.MarkRefundPending($"{reason} -> {refundRef}");
            return refundRef;
        }

        public bool PingGateway() => true;
    }
}
