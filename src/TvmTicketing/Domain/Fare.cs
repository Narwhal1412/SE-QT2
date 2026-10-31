using System;
using TvmTicketing.Enums;

namespace TvmTicketing.Domain
{
    public class Fare
    {
        public string Origin { get; set; } = "Thao Dien Station";
        public string Destination { get; set; } = "Ba Son";
        public TicketType TicketType { get; set; } = TicketType.SingleJourney;
        public FareCategory Category { get; set; } = FareCategory.Adult;
        public int AmountVnd { get; set; } = 10000;
        public float discountRate { get; set; } = 0.0f;

        public string DistrictInfo { get; set; } = "District 1 · Riverside · 4 stops · 9 min";
        public string LineName { get; set; } = "Metro Line 1";

        public int GetFare()
        {
            if (TicketType == TicketType.OneDayPass)
                return 40000;

            float multiplier = Category switch
            {
                FareCategory.Student => 0.5f,
                FareCategory.Senior => 0.5f,
                _ => 1.0f - discountRate
            };
            int raw = (int)Math.Round((AmountVnd * multiplier) / 1000.0) * 1000;
            return Math.Max(1000, raw);
        }
    }
}
