using System;
using System.Collections.Generic;
using TvmTicketing.Enums;

namespace TvmTicketing.Domain
{
    public class Transaction
    {
        public Guid TransactionId { get; set; } = Guid.NewGuid();
        public string ShortCode => "MG-" + TransactionId.ToString("N").Substring(0, 7).ToUpperInvariant();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public TransactionStatus Status { get; set; } = TransactionStatus.Selecting;
        public int Quantity { get; set; } = 1;
        public int TotalAmountVnd { get; set; } = 10000;
        public string receiptEmail { get; set; } = string.Empty;
        public string TerminalId { get; set; } = "TD-004";

        public Fare Fare { get; set; } = new Fare();
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
        public List<Payment> Payments { get; set; } = new List<Payment>();

        public void ConfirmPayment()
        {
            Status = TransactionStatus.Paid;
        }

        public void Cancel()
        {
            Status = TransactionStatus.Cancelled;
        }

        public void MarkCompleted()
        {
            Status = TransactionStatus.Completed;
        }
    }
}
