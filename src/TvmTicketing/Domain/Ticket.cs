using System;
using TvmTicketing.Enums;

namespace TvmTicketing.Domain
{
    public class Ticket
    {
        public string TicketNumber { get; set; } = string.Empty;
        public string BarcodeCode128 { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public TicketType TicketType { get; set; }
        public FareCategory Category { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddMinutes(60);
        public int PriceVnd { get; set; }

        public string GenerateBarcode()
        {
            BarcodeCode128 = $"CODE128-{TicketNumber}-{IssuedAt:yyyyMMddHHmm}";
            return BarcodeCode128;
        }

        public DateTime CalculateExpiry()
        {
            ExpiresAt = TicketType == TicketType.OneDayPass
                ? IssuedAt.Date.AddHours(23).AddMinutes(59)
                : IssuedAt.AddMinutes(60);
            return ExpiresAt;
        }

        public void Reprint()
        {
            IssuedAt = DateTime.Now;
            GenerateBarcode();
        }
    }
}
