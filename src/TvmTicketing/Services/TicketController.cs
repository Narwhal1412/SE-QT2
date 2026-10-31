using System;
using System.Linq;
using TvmTicketing.Domain;
using TvmTicketing.Enums;
using TvmTicketing.Hardware;

namespace TvmTicketing.Services
{
    public class TicketController
    {
        private readonly TicketPrinter _printer;
        private readonly PaymentService _paymentService;
        private readonly TransactionRepository _repository;

        public Transaction CurrentTransaction { get; set; } = new Transaction();
        public int CardAttemptCount { get; set; } = 0;

        public TicketController(TicketPrinter printer, PaymentService paymentService, TransactionRepository repository)
        {
            _printer = printer;
            _paymentService = paymentService;
            _repository = repository;
        }

        public Transaction StartTransaction(string originStation = "Thao Dien Station", string terminalId = "TD-004")
        {
            CardAttemptCount = 0;
            var defaultFare = _repository.CatalogFares.FirstOrDefault(f => f.Origin == originStation)
                              ?? new Fare { Origin = originStation, Destination = "Ba Son", AmountVnd = 10000 };

            CurrentTransaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                Status = TransactionStatus.Selecting,
                Quantity = 1,
                TerminalId = terminalId,
                Fare = new Fare
                {
                    Origin = defaultFare.Origin,
                    Destination = defaultFare.Destination,
                    AmountVnd = defaultFare.AmountVnd,
                    TicketType = TicketType.SingleJourney,
                    Category = FareCategory.Adult,
                    DistrictInfo = defaultFare.DistrictInfo,
                    LineName = defaultFare.LineName
                }
            };
            CalculateFare();
            _repository.SaveTransaction(CurrentTransaction);
            return CurrentTransaction;
        }

        public int CalculateFare()
        {
            int unitFare = CurrentTransaction.Fare.GetFare();
            CurrentTransaction.TotalAmountVnd = unitFare * CurrentTransaction.Quantity;
            return CurrentTransaction.TotalAmountVnd;
        }

        public (Domain.Payment payment, string payload) BeginPayment(PaymentMethod method, WalletProvider wallet = WalletProvider.None)
        {
            var result = _paymentService.InitiatePayment(CurrentTransaction, method, wallet);
            _repository.SaveTransaction(CurrentTransaction);
            _repository.SavePayment(result.payment);
            return result;
        }

        public (bool success, string message) CompleteSale(Domain.Payment payment)
        {
            _paymentService.VerifyCallback(payment);
            CurrentTransaction.ConfirmPayment();
            _repository.SavePayment(payment);

            CurrentTransaction.Status = TransactionStatus.Printing;
            CurrentTransaction.Tickets.Clear();

            int unitPrice = CurrentTransaction.Fare.GetFare();
            for (int i = 1; i <= CurrentTransaction.Quantity; i++)
            {
                var ticket = new Ticket
                {
                    TicketNumber = $"{CurrentTransaction.ShortCode}-{i}",
                    Origin = CurrentTransaction.Fare.Origin,
                    Destination = CurrentTransaction.Fare.TicketType == TicketType.OneDayPass ? "All Metro & Bus Zones" : CurrentTransaction.Fare.Destination,
                    TicketType = CurrentTransaction.Fare.TicketType,
                    Category = CurrentTransaction.Fare.Category,
                    PriceVnd = unitPrice,
                    IssuedAt = DateTime.Now
                };
                ticket.CalculateExpiry();
                ticket.GenerateBarcode();

                if (!_printer.PrintTicket(ticket))
                {
                    string refundRef = HandlePrintFailure(payment);
                    return (false, $"Printer fault ({_printer.Status}). Automatic refund initiated: {refundRef}");
                }

                CurrentTransaction.Tickets.Add(ticket);
            }

            CurrentTransaction.MarkCompleted();
            _repository.SaveTransaction(CurrentTransaction);
            _repository.SaveTickets(CurrentTransaction);

            string chName = payment.Method == PaymentMethod.DigitalWallet
                ? payment.WalletProvider.ToString()
                : (payment.Method == PaymentMethod.BankQR ? "VietQR (NAPAS)" : "Credit Card (Visa)");

            _repository.ReconciliationLogs.Insert(0, new ReconciliationRecord
            {
                TransactionCode = CurrentTransaction.ShortCode,
                TerminalCode = CurrentTransaction.TerminalId,
                Route = $"{CurrentTransaction.Fare.Origin} -> {CurrentTransaction.Fare.Destination}",
                PaymentChannel = chName,
                TvmAmountVnd = CurrentTransaction.TotalAmountVnd,
                GatewaySettlementVnd = CurrentTransaction.TotalAmountVnd,
                TvmStatus = "Completed",
                SettlementStatus = "Settled",
                ReconciliationNote = $"Matched OK ({CurrentTransaction.Tickets.Count} ticket(s) printed)",
                Timestamp = DateTime.Now
            });

            return (true, payment.ProviderReference);
        }

        public string HandlePrintFailure(Domain.Payment payment)
        {
            CurrentTransaction.Status = TransactionStatus.RefundPending;
            string refundRef = _paymentService.RequestRefund(payment, $"Printer fault: {_printer.Status}");
            _repository.SaveTransaction(CurrentTransaction);
            _repository.SavePayment(payment);

            _repository.ReconciliationLogs.Insert(0, new ReconciliationRecord
            {
                TransactionCode = CurrentTransaction.ShortCode,
                TerminalCode = CurrentTransaction.TerminalId,
                Route = $"{CurrentTransaction.Fare.Origin} -> {CurrentTransaction.Fare.Destination}",
                PaymentChannel = payment.Method.ToString(),
                TvmAmountVnd = CurrentTransaction.TotalAmountVnd,
                GatewaySettlementVnd = CurrentTransaction.TotalAmountVnd,
                TvmStatus = "RefundPending (UC08)",
                SettlementStatus = "Auto-Refunded",
                ReconciliationNote = $"Print failure ({_printer.Status}) -> Refund Ref: {refundRef}",
                Timestamp = DateTime.Now
            });

            return refundRef;
        }

        public void ResetSystem()
        {
            _printer.Status = PrinterStatus.Ready;
            if (_printer.PaperCount <= 0)
                _printer.PaperCount = 192;
            CardAttemptCount = 0;
        }
    }
}
