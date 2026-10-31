using TvmTicketing.Domain;

namespace TvmTicketing.Payment
{
    public interface IPaymentGateway
    {
        string Initiate(Transaction tx);
        bool VerifyNotification(string payload, string signature);
        string Refund(Domain.Payment payment, string reason);
        void reconnect();
    }
}
