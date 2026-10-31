using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using TvmTicketing.Domain;
using TvmTicketing.Enums;

namespace TvmTicketing.Services
{
    public class TerminalNodeInfo
    {
        public string TerminalCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Mode { get; set; } = "MRT";
        public string PrinterStatusText { get; set; } = "Ready";
        public int PaperRemaining { get; set; } = 192;
        public bool IsOnline { get; set; } = true;
    }

    public class ReconciliationRecord
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string TerminalCode { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string PaymentChannel { get; set; } = string.Empty;
        public int TvmAmountVnd { get; set; }
        public int GatewaySettlementVnd { get; set; }
        public string TvmStatus { get; set; } = string.Empty;
        public string SettlementStatus { get; set; } = string.Empty;
        public string ReconciliationNote { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class TransactionRepository
    {
        public string ConnectionString { get; set; } =
            @"Server=(localdb)\MSSQLLocalDB;Database=MetroGoTVM;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;";

        public bool IsSqlConnected { get; private set; }
        public List<Fare> CatalogFares { get; private set; } = new List<Fare>();
        public List<TerminalNodeInfo> Terminals { get; private set; } = new List<TerminalNodeInfo>();
        public List<Transaction> Transactions { get; private set; } = new List<Transaction>();
        public List<ReconciliationRecord> ReconciliationLogs { get; private set; } = new List<ReconciliationRecord>();

        public TransactionRepository()
        {
            SeedRichInMemoryCatalog();
            TryInitializeLocalDb();
        }

        private void SeedRichInMemoryCatalog()
        {
            Terminals = new List<TerminalNodeInfo>
            {
                new TerminalNodeInfo { TerminalCode = "BUS-TD-002", StationName = "Thao Dien Bus Stop", Mode = "BUS", PrinterStatusText = "Ready", PaperRemaining = 150, IsOnline = true },
                new TerminalNodeInfo { TerminalCode = "TD-004", StationName = "Thao Dien Station", Mode = "MRT", PrinterStatusText = "Ready", PaperRemaining = 192, IsOnline = true },
                new TerminalNodeInfo { TerminalCode = "BT-001", StationName = "Ben Thanh Central", Mode = "MRT", PrinterStatusText = "Ready", PaperRemaining = 240, IsOnline = true },
                new TerminalNodeInfo { TerminalCode = "BS-003", StationName = "Ba Son Station", Mode = "MRT", PrinterStatusText = "Paper low", PaperRemaining = 38, IsOnline = true },
                new TerminalNodeInfo { TerminalCode = "ST-014", StationName = "Suoi Tien Terminal", Mode = "MRT", PrinterStatusText = "Ready", PaperRemaining = 175, IsOnline = true },
                new TerminalNodeInfo { TerminalCode = "BUS-Q1-009", StationName = "Me Linh Point Bus Hub", Mode = "BUS", PrinterStatusText = "Ready", PaperRemaining = 128, IsOnline = true }
            };

            CatalogFares = new List<Fare>
            {
                new Fare { Origin = "Thao Dien Station", Destination = "Ba Son", AmountVnd = 10000, LineName = "Metro Line 1", DistrictInfo = "District 1 · Riverside · 4 stops · 9 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Opera House", AmountVnd = 10000, LineName = "Metro Line 1", DistrictInfo = "District 1 · Culture & arts · 5 stops · 11 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Ben Thanh", AmountVnd = 12000, LineName = "Metro Line 1", DistrictInfo = "District 1 · City center · 6 stops · 14 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Suoi Tien", AmountVnd = 15000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · University area · 7 stops · 18 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Van Thanh Park", AmountVnd = 8000, LineName = "Metro Line 1", DistrictInfo = "Binh Thanh · Parkside · 2 stops · 4 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Tan Cang", AmountVnd = 8000, LineName = "Metro Line 1", DistrictInfo = "Binh Thanh · Saigon Bridge · 1 stop · 2 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "An Phu", AmountVnd = 8000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · Commercial hub · 1 stop · 2 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Rach Chiec", AmountVnd = 9000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · Sports complex · 2 stops · 5 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Phuoc Long", AmountVnd = 10000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · Residential · 3 stops · 7 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Binh Thai", AmountVnd = 11000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · Hanoi Highway · 4 stops · 10 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Thu Duc", AmountVnd = 12000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · Civic center · 5 stops · 13 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "High Tech Park", AmountVnd = 14000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · SHTP Zone · 6 stops · 15 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "National University", AmountVnd = 15000, LineName = "Metro Line 1", DistrictInfo = "Thu Duc City · VNU-HCM Campus · 7 stops · 17 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Bus Route 150 · Cho Lon", AmountVnd = 7000, LineName = "Metro Feeder Bus", DistrictInfo = "District 5 · Express Bus · 12 stops · 28 min" },
                new Fare { Origin = "Thao Dien Station", Destination = "Bus Route 109 · Tan Son Nhat", AmountVnd = 15000, LineName = "Airport Shuttle Bus", DistrictInfo = "Tan Binh · Airport Terminal · 9 stops · 35 min" },

                new Fare { Origin = "Thao Dien Bus Stop", Destination = "Ben Thanh Bus Station", AmountVnd = 7000, LineName = "Bus Route 56", DistrictInfo = "District 1 · Central Hub · 8 stops · 20 min" },
                new Fare { Origin = "Thao Dien Bus Stop", Destination = "Cho Lon Terminal", AmountVnd = 7000, LineName = "Bus Route 150", DistrictInfo = "District 5 · Chinatown · 14 stops · 32 min" },
                new Fare { Origin = "Thao Dien Bus Stop", Destination = "Saigon Waterbus Bach Dang", AmountVnd = 7000, LineName = "Bus Feeder 43", DistrictInfo = "District 1 · River Pier · 6 stops · 15 min" },
                new Fare { Origin = "Thao Dien Bus Stop", Destination = "VNU-HCM Student Village", AmountVnd = 8000, LineName = "Bus Route 52", DistrictInfo = "Thu Duc City · Campus · 11 stops · 26 min" }
            };

            string[] sampleRoutes = {
                "Thao Dien Station -> Ba Son",
                "Thao Dien Station -> Ben Thanh",
                "Thao Dien Station -> Opera House",
                "Thao Dien Station -> Suoi Tien",
                "Thao Dien Station -> High Tech Park",
                "Thao Dien Bus Stop -> Ben Thanh Bus Station"
            };
            string[] channels = { "ZaloPay", "MoMo", "VNPay", "VietQR (NAPAS)", "Credit Card (Visa)" };
            int[] fares = { 10000, 12000, 10000, 15000, 14000, 7000 };

            var baseTime = DateTime.Today.AddHours(6).AddMinutes(15);
            for (int i = 1; i <= 32; i++)
            {
                int idx = i % sampleRoutes.Length;
                string ch = channels[i % channels.Length];
                int amt = fares[idx] * ((i % 3 == 0) ? 2 : 1);
                string txCode = $"MG-{104820 + i:X6}";

                string tvmSt = "Completed";
                string gwSt = "Settled";
                string note = "Matched OK";
                int gwAmt = amt;

                if (i == 7)
                {
                    tvmSt = "Refunded (UC08)";
                    gwSt = "Auto-Refunded";
                    note = "Printer paper jam after payment -> Auto refund ref ZALOPAY-REF-0812";
                }
                else if (i == 18)
                {
                    tvmSt = "TimedOut / Cancelled";
                    gwSt = "Late Callback Paid";
                    note = "MISMATCH: Late VietQR callback after 120s expiry -> Auto-refund queued";
                }
                else if (i == 26)
                {
                    tvmSt = "Declined (3x)";
                    gwSt = "Declined";
                    gwAmt = 0;
                    note = "Card declined 3 attempts -> Session terminated";
                }

                ReconciliationLogs.Add(new ReconciliationRecord
                {
                    TransactionCode = txCode,
                    TerminalCode = (i % 5 == 0) ? "BUS-TD-002" : "TD-004",
                    Route = sampleRoutes[idx],
                    PaymentChannel = ch,
                    TvmAmountVnd = amt,
                    GatewaySettlementVnd = gwAmt,
                    TvmStatus = tvmSt,
                    SettlementStatus = gwSt,
                    ReconciliationNote = note,
                    Timestamp = baseTime.AddMinutes(i * 14)
                });
            }
        }

        public void TryInitializeLocalDb()
        {
            try
            {
                var masterBuilder = new SqlConnectionStringBuilder(ConnectionString)
                {
                    InitialCatalog = "master"
                };

                using (var masterConn = new SqlConnection(masterBuilder.ConnectionString))
                {
                    masterConn.Open();
                    using var cmd = masterConn.CreateCommand();
                    cmd.CommandText = @"
                        IF DB_ID('MetroGoTVM') IS NULL
                        BEGIN
                            CREATE DATABASE MetroGoTVM;
                        END";
                    cmd.ExecuteNonQuery();
                }

                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
                        BEGIN
                            CREATE TABLE dbo.Transactions (
                                TransactionId UNIQUEIDENTIFIER PRIMARY KEY,
                                ShortCode NVARCHAR(24) NOT NULL,
                                TerminalId NVARCHAR(24) NOT NULL,
                                Origin NVARCHAR(100) NOT NULL,
                                Destination NVARCHAR(100) NOT NULL,
                                TicketType NVARCHAR(32) NOT NULL,
                                Category NVARCHAR(32) NOT NULL,
                                Quantity INT NOT NULL,
                                TotalAmountVnd INT NOT NULL,
                                Status NVARCHAR(32) NOT NULL,
                                CreatedAt DATETIME2 NOT NULL
                            );

                            CREATE TABLE dbo.Payments (
                                PaymentId UNIQUEIDENTIFIER PRIMARY KEY,
                                TransactionId UNIQUEIDENTIFIER NOT NULL,
                                Method NVARCHAR(32) NOT NULL,
                                WalletProvider NVARCHAR(32) NOT NULL,
                                AmountVnd INT NOT NULL,
                                Status NVARCHAR(32) NOT NULL,
                                ProviderReference NVARCHAR(128) NOT NULL,
                                CreatedAt DATETIME2 NOT NULL
                            );

                            CREATE TABLE dbo.Tickets (
                                TicketNumber NVARCHAR(40) PRIMARY KEY,
                                TransactionId UNIQUEIDENTIFIER NOT NULL,
                                BarcodeCode128 NVARCHAR(100) NOT NULL,
                                Origin NVARCHAR(100) NOT NULL,
                                Destination NVARCHAR(100) NOT NULL,
                                TicketType NVARCHAR(32) NOT NULL,
                                Category NVARCHAR(32) NOT NULL,
                                PriceVnd INT NOT NULL,
                                IssuedAt DATETIME2 NOT NULL,
                                ExpiresAt DATETIME2 NOT NULL
                            );
                        END";
                    cmd.ExecuteNonQuery();
                    IsSqlConnected = true;
                }
            }
            catch
            {
                IsSqlConnected = false;
            }
        }

        public void SaveTransaction(Transaction tx)
        {
            var existing = Transactions.FirstOrDefault(t => t.TransactionId == tx.TransactionId);
            if (existing == null)
                Transactions.Insert(0, tx);

            if (!IsSqlConnected) return;

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    MERGE dbo.Transactions AS target
                    USING (SELECT @Id AS TransactionId) AS src
                    ON target.TransactionId = src.TransactionId
                    WHEN MATCHED THEN
                        UPDATE SET Status = @Status, Quantity = @Qty, TotalAmountVnd = @Total
                    WHEN NOT MATCHED THEN
                        INSERT (TransactionId, ShortCode, TerminalId, Origin, Destination, TicketType, Category, Quantity, TotalAmountVnd, Status, CreatedAt)
                        VALUES (@Id, @Code, @Term, @Orig, @Dest, @Type, @Cat, @Qty, @Total, @Status, @Created);";
                cmd.Parameters.AddWithValue("@Id", tx.TransactionId);
                cmd.Parameters.AddWithValue("@Code", tx.ShortCode);
                cmd.Parameters.AddWithValue("@Term", tx.TerminalId);
                cmd.Parameters.AddWithValue("@Orig", tx.Fare.Origin);
                cmd.Parameters.AddWithValue("@Dest", tx.Fare.Destination);
                cmd.Parameters.AddWithValue("@Type", tx.Fare.TicketType.ToString());
                cmd.Parameters.AddWithValue("@Cat", tx.Fare.Category.ToString());
                cmd.Parameters.AddWithValue("@Qty", tx.Quantity);
                cmd.Parameters.AddWithValue("@Total", tx.TotalAmountVnd);
                cmd.Parameters.AddWithValue("@Status", tx.Status.ToString());
                cmd.Parameters.AddWithValue("@Created", tx.CreatedAt);
                cmd.ExecuteNonQuery();
            }
            catch
            {
            }
        }

        public void SavePayment(Domain.Payment payment)
        {
            if (!IsSqlConnected) return;
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    MERGE dbo.Payments AS target
                    USING (SELECT @Id AS PaymentId) AS src
                    ON target.PaymentId = src.PaymentId
                    WHEN MATCHED THEN
                        UPDATE SET Status = @Status, ProviderReference = @Ref
                    WHEN NOT MATCHED THEN
                        INSERT (PaymentId, TransactionId, Method, WalletProvider, AmountVnd, Status, ProviderReference, CreatedAt)
                        VALUES (@Id, @TxId, @Method, @Wallet, @Amt, @Status, @Ref, @Created);";
                cmd.Parameters.AddWithValue("@Id", payment.PaymentId);
                cmd.Parameters.AddWithValue("@TxId", payment.TransactionId);
                cmd.Parameters.AddWithValue("@Method", payment.Method.ToString());
                cmd.Parameters.AddWithValue("@Wallet", payment.WalletProvider.ToString());
                cmd.Parameters.AddWithValue("@Amt", payment.AmountVnd);
                cmd.Parameters.AddWithValue("@Status", payment.Status.ToString());
                cmd.Parameters.AddWithValue("@Ref", payment.ProviderReference);
                cmd.Parameters.AddWithValue("@Created", payment.CreatedAt);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public void SaveTickets(Transaction tx)
        {
            if (!IsSqlConnected) return;
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                foreach (var t in tx.Tickets)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        IF NOT EXISTS (SELECT 1 FROM dbo.Tickets WHERE TicketNumber = @Num)
                        INSERT INTO dbo.Tickets (TicketNumber, TransactionId, BarcodeCode128, Origin, Destination, TicketType, Category, PriceVnd, IssuedAt, ExpiresAt)
                        VALUES (@Num, @TxId, @Barcode, @Orig, @Dest, @Type, @Cat, @Price, @Issued, @Expires);";
                    cmd.Parameters.AddWithValue("@Num", t.TicketNumber);
                    cmd.Parameters.AddWithValue("@TxId", tx.TransactionId);
                    cmd.Parameters.AddWithValue("@Barcode", t.BarcodeCode128);
                    cmd.Parameters.AddWithValue("@Orig", t.Origin);
                    cmd.Parameters.AddWithValue("@Dest", t.Destination);
                    cmd.Parameters.AddWithValue("@Type", t.TicketType.ToString());
                    cmd.Parameters.AddWithValue("@Cat", t.Category.ToString());
                    cmd.Parameters.AddWithValue("@Price", t.PriceVnd);
                    cmd.Parameters.AddWithValue("@Issued", t.IssuedAt);
                    cmd.Parameters.AddWithValue("@Expires", t.ExpiresAt);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public Transaction? FindById(Guid id) => Transactions.FirstOrDefault(t => t.TransactionId == id);

        public void DeleteOldRecords()
        {
            Transactions.RemoveAll(t => t.CreatedAt < DateTime.Now.AddYears(-5));
        }
    }
}
