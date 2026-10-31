-- =========================================================================================
-- METROGO SMART TICKETING SYSTEM (TVM) - MSSQL LOCALDB DATABASE SETUP & RICH MOCK DATA
-- Target Server: (localdb)\MSSQLLocalDB
-- Course: Software Engineering Lab Assignment 1 (Sem 1_2627)
-- Group Members: Nguyen Gia Huy (524H0158), Tran Do Quoc Anh (524H0041), Le Anh Tuan (524H0074)
-- =========================================================================================

USE master;
GO

IF DB_ID('MetroGoTVM') IS NULL
BEGIN
    CREATE DATABASE MetroGoTVM;
END
GO

USE MetroGoTVM;
GO

-- 1. DROP EXISTING TABLES IF RE-RUNNING
IF OBJECT_ID('dbo.ReconciliationLogs', 'U') IS NOT NULL DROP TABLE dbo.ReconciliationLogs;
IF OBJECT_ID('dbo.Refunds', 'U') IS NOT NULL DROP TABLE dbo.Refunds;
IF OBJECT_ID('dbo.Tickets', 'U') IS NOT NULL DROP TABLE dbo.Tickets;
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL DROP TABLE dbo.Transactions;
IF OBJECT_ID('dbo.Fares', 'U') IS NOT NULL DROP TABLE dbo.Fares;
IF OBJECT_ID('dbo.Terminals', 'U') IS NOT NULL DROP TABLE dbo.Terminals;
IF OBJECT_ID('dbo.Stations', 'U') IS NOT NULL DROP TABLE dbo.Stations;
GO

-- 2. CREATE TABLES
CREATE TABLE dbo.Stations (
    StationId NVARCHAR(20) PRIMARY KEY,
    StationName NVARCHAR(100) NOT NULL,
    TransportMode NVARCHAR(16) NOT NULL, -- 'MRT' or 'BUS'
    LineName NVARCHAR(64) NOT NULL,
    ZoneNumber INT NOT NULL,
    DistrictInfo NVARCHAR(150) NOT NULL
);

CREATE TABLE dbo.Terminals (
    TerminalId NVARCHAR(24) PRIMARY KEY,
    StationId NVARCHAR(20) NOT NULL FOREIGN KEY REFERENCES dbo.Stations(StationId),
    StationName NVARCHAR(100) NOT NULL,
    TransportMode NVARCHAR(16) NOT NULL,
    Status NVARCHAR(24) NOT NULL,
    PrinterStatus NVARCHAR(24) NOT NULL,
    PaperRemaining INT NOT NULL,
    InkLevel INT NOT NULL,
    LastHeartbeat DATETIME2 NOT NULL
);

CREATE TABLE dbo.Fares (
    FareId INT IDENTITY(1,1) PRIMARY KEY,
    Origin NVARCHAR(100) NOT NULL,
    Destination NVARCHAR(100) NOT NULL,
    LineName NVARCHAR(64) NOT NULL,
    TicketType NVARCHAR(32) NOT NULL,
    Category NVARCHAR(32) NOT NULL,
    AmountVnd INT NOT NULL,
    DiscountRate FLOAT NOT NULL DEFAULT 0.0,
    DistrictInfo NVARCHAR(150) NOT NULL,
    EffectiveFrom DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

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
    ReceiptEmail NVARCHAR(120) NULL,
    CreatedAt DATETIME2 NOT NULL
);

CREATE TABLE dbo.Payments (
    PaymentId UNIQUEIDENTIFIER PRIMARY KEY,
    TransactionId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES dbo.Transactions(TransactionId),
    Method NVARCHAR(32) NOT NULL,
    WalletProvider NVARCHAR(32) NOT NULL,
    AmountVnd INT NOT NULL,
    Currency NVARCHAR(8) NOT NULL DEFAULT 'VND',
    Status NVARCHAR(32) NOT NULL,
    ProviderReference NVARCHAR(128) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);

CREATE TABLE dbo.Tickets (
    TicketNumber NVARCHAR(40) PRIMARY KEY,
    TransactionId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES dbo.Transactions(TransactionId),
    BarcodeCode128 NVARCHAR(100) NOT NULL,
    Origin NVARCHAR(100) NOT NULL,
    Destination NVARCHAR(100) NOT NULL,
    TicketType NVARCHAR(32) NOT NULL,
    Category NVARCHAR(32) NOT NULL,
    PriceVnd INT NOT NULL,
    IssuedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL
);

CREATE TABLE dbo.Refunds (
    RefundId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PaymentId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES dbo.Payments(PaymentId),
    TransactionShortCode NVARCHAR(24) NOT NULL,
    AmountVnd INT NOT NULL,
    Reason NVARCHAR(200) NOT NULL,
    GatewayRefundReference NVARCHAR(100) NOT NULL,
    RefundStatus NVARCHAR(32) NOT NULL,
    RequestedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE dbo.ReconciliationLogs (
    ReconcileId INT IDENTITY(1,1) PRIMARY KEY,
    TransactionCode NVARCHAR(24) NOT NULL,
    TerminalCode NVARCHAR(24) NOT NULL,
    Route NVARCHAR(160) NOT NULL,
    PaymentChannel NVARCHAR(48) NOT NULL,
    TvmAmountVnd INT NOT NULL,
    GatewaySettlementVnd INT NOT NULL,
    TvmStatus NVARCHAR(40) NOT NULL,
    SettlementStatus NVARCHAR(40) NOT NULL,
    ReconciliationNote NVARCHAR(250) NOT NULL,
    RecordedAt DATETIME2 NOT NULL
);
GO

-- 3. SEED STATIONS (HCMC METRO LINE 1 & FEEDER BUS STOPS)
INSERT INTO dbo.Stations (StationId, StationName, TransportMode, LineName, ZoneNumber, DistrictInfo) VALUES
('MRT-01', 'Ben Thanh', 'MRT', 'Metro Line 1', 1, 'District 1 · City center · 6 stops · 14 min'),
('MRT-02', 'Opera House', 'MRT', 'Metro Line 1', 1, 'District 1 · Culture & arts · 5 stops · 11 min'),
('MRT-03', 'Ba Son', 'MRT', 'Metro Line 1', 1, 'District 1 · Riverside · 4 stops · 9 min'),
('MRT-04', 'Van Thanh Park', 'MRT', 'Metro Line 1', 2, 'Binh Thanh · Parkside · 2 stops · 4 min'),
('MRT-05', 'Tan Cang', 'MRT', 'Metro Line 1', 2, 'Binh Thanh · Saigon Bridge · 1 stop · 2 min'),
('MRT-06', 'Thao Dien Station', 'MRT', 'Metro Line 1', 2, 'Thu Duc City · Thao Dien Hub · Origin'),
('MRT-07', 'An Phu', 'MRT', 'Metro Line 1', 2, 'Thu Duc City · Commercial hub · 1 stop · 2 min'),
('MRT-08', 'Rach Chiec', 'MRT', 'Metro Line 1', 3, 'Thu Duc City · Sports complex · 2 stops · 5 min'),
('MRT-09', 'Phuoc Long', 'MRT', 'Metro Line 1', 3, 'Thu Duc City · Residential · 3 stops · 7 min'),
('MRT-10', 'Binh Thai', 'MRT', 'Metro Line 1', 3, 'Thu Duc City · Hanoi Highway · 4 stops · 10 min'),
('MRT-11', 'Thu Duc', 'MRT', 'Metro Line 1', 4, 'Thu Duc City · Civic center · 5 stops · 13 min'),
('MRT-12', 'High Tech Park', 'MRT', 'Metro Line 1', 4, 'Thu Duc City · SHTP Zone · 6 stops · 15 min'),
('MRT-13', 'National University', 'MRT', 'Metro Line 1', 4, 'Thu Duc City · VNU-HCM Campus · 7 stops · 17 min'),
('MRT-14', 'Suoi Tien', 'MRT', 'Metro Line 1', 4, 'Thu Duc City · University area · 7 stops · 18 min'),
('BUS-01', 'Thao Dien Bus Stop', 'BUS', 'Bus Feeder Network', 2, 'Thu Duc City · Thao Dien Feeder Stop'),
('BUS-02', 'Ben Thanh Bus Station', 'BUS', 'Bus Route 56', 1, 'District 1 · Central Hub · 8 stops · 20 min'),
('BUS-03', 'Cho Lon Terminal', 'BUS', 'Bus Route 150', 3, 'District 5 · Chinatown · 14 stops · 32 min'),
('BUS-04', 'Tan Son Nhat Airport', 'BUS', 'Airport Shuttle 109', 4, 'Tan Binh · International Terminal · 9 stops · 35 min');

-- 4. SEED TVM TERMINALS
INSERT INTO dbo.Terminals (TerminalId, StationId, StationName, TransportMode, Status, PrinterStatus, PaperRemaining, InkLevel, LastHeartbeat) VALUES
('BUS-TD-002', 'BUS-01', 'Thao Dien Bus Stop', 'BUS', 'Idle', 'Ready', 150, 92, SYSDATETIME()),
('TD-004', 'MRT-06', 'Thao Dien Station', 'MRT', 'Idle', 'Ready', 192, 95, SYSDATETIME()),
('BT-001', 'MRT-01', 'Ben Thanh Central', 'MRT', 'Idle', 'Ready', 240, 98, SYSDATETIME()),
('BS-003', 'MRT-03', 'Ba Son Station', 'MRT', 'Idle', 'PaperLow', 38, 84, SYSDATETIME()),
('ST-014', 'MRT-14', 'Suoi Tien Terminal', 'MRT', 'Idle', 'Ready', 175, 90, SYSDATETIME()),
('AP-007', 'MRT-07', 'An Phu Station', 'MRT', 'Idle', 'Ready', 210, 96, SYSDATETIME());

-- 5. SEED CENTRAL FARE TABLE (WHOLE VND ROUNDED TO 1,000 VND)
INSERT INTO dbo.Fares (Origin, Destination, LineName, TicketType, Category, AmountVnd, DiscountRate, DistrictInfo) VALUES
('Thao Dien Station', 'Ba Son', 'Metro Line 1', 'SingleJourney', 'Adult', 10000, 0.0, 'District 1 · Riverside · 4 stops · 9 min'),
('Thao Dien Station', 'Opera House', 'Metro Line 1', 'SingleJourney', 'Adult', 10000, 0.0, 'District 1 · Culture & arts · 5 stops · 11 min'),
('Thao Dien Station', 'Ben Thanh', 'Metro Line 1', 'SingleJourney', 'Adult', 12000, 0.0, 'District 1 · City center · 6 stops · 14 min'),
('Thao Dien Station', 'Suoi Tien', 'Metro Line 1', 'SingleJourney', 'Adult', 15000, 0.0, 'Thu Duc City · University area · 7 stops · 18 min'),
('Thao Dien Station', 'Van Thanh Park', 'Metro Line 1', 'SingleJourney', 'Adult', 8000, 0.0, 'Binh Thanh · Parkside · 2 stops · 4 min'),
('Thao Dien Station', 'Tan Cang', 'Metro Line 1', 'SingleJourney', 'Adult', 8000, 0.0, 'Binh Thanh · Saigon Bridge · 1 stop · 2 min'),
('Thao Dien Station', 'An Phu', 'Metro Line 1', 'SingleJourney', 'Adult', 8000, 0.0, 'Thu Duc City · Commercial hub · 1 stop · 2 min'),
('Thao Dien Station', 'Rach Chiec', 'Metro Line 1', 'SingleJourney', 'Adult', 9000, 0.0, 'Thu Duc City · Sports complex · 2 stops · 5 min'),
('Thao Dien Station', 'Phuoc Long', 'Metro Line 1', 'SingleJourney', 'Adult', 10000, 0.0, 'Thu Duc City · Residential · 3 stops · 7 min'),
('Thao Dien Station', 'Binh Thai', 'Metro Line 1', 'SingleJourney', 'Adult', 11000, 0.0, 'Thu Duc City · Hanoi Highway · 4 stops · 10 min'),
('Thao Dien Station', 'Thu Duc', 'Metro Line 1', 'SingleJourney', 'Adult', 12000, 0.0, 'Thu Duc City · Civic center · 5 stops · 13 min'),
('Thao Dien Station', 'High Tech Park', 'Metro Line 1', 'SingleJourney', 'Adult', 14000, 0.0, 'Thu Duc City · SHTP Zone · 6 stops · 15 min'),
('Thao Dien Station', 'National University', 'Metro Line 1', 'SingleJourney', 'Adult', 15000, 0.0, 'Thu Duc City · VNU-HCM Campus · 7 stops · 17 min'),
('Thao Dien Station', 'All Metro & Bus Stops', 'Metro Line 1', 'OneDayPass', 'Adult', 40000, 0.0, 'Unlimited 1-Day Pass across MRT & Bus'),
('Thao Dien Bus Stop', 'Ben Thanh Bus Station', 'Bus Route 56', 'SingleJourney', 'Adult', 7000, 0.0, 'District 1 · Central Hub · 8 stops · 20 min'),
('Thao Dien Bus Stop', 'Cho Lon Terminal', 'Bus Route 150', 'SingleJourney', 'Adult', 7000, 0.0, 'District 5 · Chinatown · 14 stops · 32 min'),
('Thao Dien Bus Stop', 'Saigon Waterbus Bach Dang', 'Bus Feeder 43', 'SingleJourney', 'Adult', 7000, 0.0, 'District 1 · River Pier · 6 stops · 15 min'),
('Thao Dien Bus Stop', 'VNU-HCM Student Village', 'Bus Route 52', 'SingleJourney', 'Adult', 8000, 0.0, 'Thu Duc City · Campus · 11 stops · 26 min');
GO

-- 6. SEED 60 REALISTIC TRANSACTIONS, PAYMENTS, TICKETS & RECONCILIATION RECORDS
DECLARE @i INT = 1;
WHILE @i <= 60
BEGIN
    DECLARE @txId UNIQUEIDENTIFIER = NEWID();
    DECLARE @payId UNIQUEIDENTIFIER = NEWID();
    DECLARE @shortCode NVARCHAR(24) = 'MG-' + UPPER(SUBSTRING(CONVERT(NVARCHAR(36), @txId), 1, 7));
    DECLARE @dest NVARCHAR(100) = CASE (@i % 5)
        WHEN 0 THEN 'Ba Son'
        WHEN 1 THEN 'Ben Thanh'
        WHEN 2 THEN 'Opera House'
        WHEN 3 THEN 'Suoi Tien'
        ELSE 'High Tech Park'
    END;
    DECLARE @unitFare INT = CASE (@i % 5)
        WHEN 0 THEN 10000
        WHEN 1 THEN 12000
        WHEN 2 THEN 10000
        WHEN 3 THEN 15000
        ELSE 14000
    END;
    DECLARE @qty INT = CASE WHEN (@i % 4 = 0) THEN 2 ELSE 1 END;
    DECLARE @total INT = @unitFare * @qty;
    DECLARE @method NVARCHAR(32) = CASE (@i % 3)
        WHEN 0 THEN 'DigitalWallet'
        WHEN 1 THEN 'BankQR'
        ELSE 'CreditCard'
    END;
    DECLARE @wallet NVARCHAR(32) = CASE (@i % 3)
        WHEN 0 THEN (CASE WHEN @i % 2 = 0 THEN 'ZaloPay' ELSE 'MoMo' END)
        ELSE 'None'
    END;
    DECLARE @created DATETIME2 = DATEADD(MINUTE, -(@i * 18), SYSDATETIME());
    DECLARE @txStatus NVARCHAR(32) = 'Completed';
    DECLARE @payStatus NVARCHAR(32) = 'Confirmed';

    IF @i IN (11, 29)
    BEGIN
        SET @txStatus = 'RefundPending';
        SET @payStatus = 'RefundPending';
    END

    INSERT INTO dbo.Transactions (TransactionId, ShortCode, TerminalId, Origin, Destination, TicketType, Category, Quantity, TotalAmountVnd, Status, ReceiptEmail, CreatedAt)
    VALUES (@txId, @shortCode, 'TD-004', 'Thao Dien Station', @dest, 'SingleJourney', 'Adult', @qty, @total, @txStatus, 'passenger@metrogo.vn', @created);

    INSERT INTO dbo.Payments (PaymentId, TransactionId, Method, WalletProvider, AmountVnd, Currency, Status, ProviderReference, CreatedAt)
    VALUES (@payId, @txId, @method, @wallet, @total, 'VND', @payStatus, @method + '-REF-' + @shortCode, @created);

    IF @txStatus = 'Completed'
    BEGIN
        INSERT INTO dbo.Tickets (TicketNumber, TransactionId, BarcodeCode128, Origin, Destination, TicketType, Category, PriceVnd, IssuedAt, ExpiresAt)
        VALUES (@shortCode + '-1', @txId, 'CODE128-' + @shortCode + '-1', 'Thao Dien Station', @dest, 'SingleJourney', 'Adult', @unitFare, @created, DATEADD(MINUTE, 60, @created));
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Refunds (PaymentId, TransactionShortCode, AmountVnd, Reason, GatewayRefundReference, RefundStatus, RequestedAt)
        VALUES (@payId, @shortCode, @total, 'Printer paper jam during ticket ejection (UC08)', 'AUTO-REF-' + @shortCode, 'Refunded', DATEADD(SECOND, 12, @created));
    END

    INSERT INTO dbo.ReconciliationLogs (TransactionCode, TerminalCode, Route, PaymentChannel, TvmAmountVnd, GatewaySettlementVnd, TvmStatus, SettlementStatus, ReconciliationNote, RecordedAt)
    VALUES (
        @shortCode,
        'TD-004',
        'Thao Dien Station -> ' + @dest,
        CASE WHEN @wallet <> 'None' THEN @wallet ELSE @method END,
        @total,
        @total,
        @txStatus,
        CASE WHEN @txStatus = 'Completed' THEN 'Settled' ELSE 'Auto-Refunded' END,
        CASE WHEN @txStatus = 'Completed' THEN 'Matched OK' ELSE 'Printer fault -> Automatic refund initiated within 60s (UC08)' END,
        @created
    );

    SET @i = @i + 1;
END
GO
