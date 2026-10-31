using TvmTicketing.Domain;
using TvmTicketing.Enums;

namespace TvmTicketing.Hardware
{
    public class TicketPrinter
    {
        public PrinterStatus Status { get; set; } = PrinterStatus.Ready;
        public int PaperCount { get; set; } = 192;
        public int inkLevel { get; set; } = 95;

        public bool PrintTicket(Ticket ticket)
        {
            if (Status == PrinterStatus.Jammed || Status == PrinterStatus.PaperOut || PaperCount <= 0)
            {
                Status = PaperCount <= 0 ? PrinterStatus.PaperOut : PrinterStatus.Jammed;
                return false;
            }

            PaperCount--;
            Status = PaperCount < 50 ? PrinterStatus.PaperLow : PrinterStatus.Ready;
            return true;
        }

        public bool CheckEjection()
        {
            return Status == PrinterStatus.Ready || Status == PrinterStatus.PaperLow;
        }
    }
}
