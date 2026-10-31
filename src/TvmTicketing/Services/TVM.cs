using TvmTicketing.Enums;
using TvmTicketing.Hardware;

namespace TvmTicketing.Services
{
    public class TVM
    {
        public int TvmId { get; set; } = 4;
        public string Origin { get; set; } = "Thao Dien Station";
        public TVMStatus Status { get; set; } = TVMStatus.Idle;
        public string location { get; set; } = "TD-004 · MRT STATION";

        public TicketPrinter Printer { get; set; }
        public PaymentService PaymentService { get; set; }
        public TransactionRepository Repository { get; set; }
        public TicketController Controller { get; set; }

        public TVM()
        {
            Printer = new TicketPrinter();
            PaymentService = new PaymentService();
            Repository = new TransactionRepository();
            Controller = new TicketController(Printer, PaymentService, Repository);
        }

        public void StartSale()
        {
            if (Printer.Status == PrinterStatus.Jammed || Printer.PaperCount <= 0)
            {
                SetOutOfService();
                return;
            }
            Status = TVMStatus.Active;
            string code = Origin.Contains("Bus") ? "BUS-TD-002" : "TD-004";
            Controller.StartTransaction(Origin, code);
        }

        public void SetOutOfService()
        {
            Status = TVMStatus.OutOfService;
        }
    }
}
