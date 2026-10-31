# SE-QT2
This repository is dedicated to the solutions of QT2 in software engineering.

================================================================================
METROGO SMART TICKETING SYSTEM (TVM) - VISUAL STUDIO .NET 10 WINFORMS SOLUTION
Course: Software Engineering Lab Group Assignment 1 (Sem 1_2627)
Group Members:
  1. 524H0158 : Nguyen Gia Huy (40%)
  2. 524H0041 : Tran Do Quoc Anh (30%)
  3. 524H0074 : Le Anh Tuan (30%)
================================================================================

1. HOW TO OPEN & RUN IN VISUAL STUDIO
--------------------------------------------------------------------------------
- Open `TvmTicketing.sln` in Visual Studio 2022 / 2026 (with .NET 10 SDK installed).
- Press F5 (or run `dotnet run --project TvmTicketing/TvmTicketing.csproj`).
- To view the UML Class Diagram for Section 5.1 (Forward / Backward Engineering):
  Double-click `TvmTicketing/ClassDiagram1.cd` in Solution Explorer.

2. DATABASE CONFIGURATION ((localdb)\MSSQLLocalDB)
--------------------------------------------------------------------------------
- Automatic Setup: On startup, `TransactionRepository.cs` automatically connects
  to `Server=(localdb)\MSSQLLocalDB`, creates the `MetroGoTVM` database and tables
  if they do not exist, and persists every new transaction, payment, and ticket.
- Full SQL Script: You can also execute `TvmTicketing/Database/MetroGo_MSSQL_Setup.sql`
  in SQL Server Management Studio (SSMS) or Visual Studio SQL Server Object Explorer
  against `(localdb)\MSSQLLocalDB` to pre-populate 60+ historical transactions,
  stations, fares, refunds, and settlement reconciliation logs.
- Resilient Fallback (NFR4): If LocalDB is stopped or unavailable on a lab PC,
  the application seamlessly falls back to its built-in in-memory dataset so the
  UI never crashes during grading.


