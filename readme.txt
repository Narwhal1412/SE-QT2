================================================================================
METROGO SMART TICKETING SYSTEM (TVM)
Course: Software Engineering Lab Group Assignment 1 (Sem 1_2627)
Group 69 Members:
  1. 524H0158 : Nguyen Gia Huy (40%)
  2. 524H0041 : Tran Do Quoc Anh (30%)
  3. 524H0074 : Le Anh Tuan (30%)
================================================================================

1. PROJECT LINKS
--------------------------------------------------------------------------------
- GitHub Repository: https://github.com/Narwhal1412/SE-QT2
- Figma UI Design: https://www.figma.com/make/cpd1VhfymogiWUdNunS68w/Create-a-New-App?t=ycoktueirSDFqjfx-1

2. HOW TO OPEN & RUN IN VISUAL STUDIO
--------------------------------------------------------------------------------
- Open `TvmTicketing.sln` in Visual Studio 2022 / 2026 (with .NET 10 SDK installed).
- Press F5 (or run `dotnet run --project src/TvmTicketing/TvmTicketing.csproj`).
- To view the UML Class Diagram for Section 5.1 (Forward / Backward Engineering):
  Double-click `src/TvmTicketing/ClassDiagram1.cd` in Solution Explorer.

3. DATABASE CONFIGURATION ((localdb)\MSSQLLocalDB)
--------------------------------------------------------------------------------
- Automatic Setup: On startup, `TransactionRepository.cs` automatically connects
  to `Server=(localdb)\MSSQLLocalDB`, creates the `MetroGoTVM` database and tables
  if they do not exist, and persists every new transaction, payment, and ticket.
- Full SQL Script: You can also execute `src/TvmTicketing/Database/MetroGo_MSSQL_Setup.sql`
  in SQL Server Management Studio (SSMS) or Visual Studio SQL Server Object Explorer
  against `(localdb)\MSSQLLocalDB` to pre-populate 60+ historical transactions,
  stations, fares, refunds, and settlement reconciliation logs.
- Resilient Fallback (NFR4): If LocalDB is stopped or unavailable on a lab PC,
  the application seamlessly falls back to its built-in in-memory dataset so the
  UI never crashes during grading.
