Ticket machine
# SE-QT2: Ticket Machine (MetroGo Smart Ticketing System)

**Course:** Software Engineering Lab Group Assignment 1 (Sem 1_2627)  
**Group 69 Members:**
1. 524H0158 : Nguyen Gia Huy (40%) - *Lead Developer & Architect*
2. 524H0041 : Tran Do Quoc Anh (30%) - *Requirements & Domain Modeling*
3. 524H0074 : Le Anh Tuan (30%) - *UI/UX Design & System Modeling*

## About the Project
This repository contains the functional prototype for the **MetroGo Ticket Vendor Machine (TVM)**. Developed in C# 13 and .NET 10 (Windows Forms), this project demonstrates a robust Model-View-Controller (MVC) architecture applied to a public transit ticketing kiosk. 

The application simulates a complete hardware-software ecosystem, combining a touch-first passenger kiosk with a secure back-office operator console. 

###  Key Features
*    **Intuitive Passenger Flow:** A responsive, multi-step UI allowing users to select destinations, ticket types (Single Journey / 1-Day Pass), and fare categories (Adult, Student, Senior).
*    **Multi-Channel Payments:** Integrated adapters for simulated EMV Credit Card readers, dynamic Bank QR codes (VietQR), and Digital Wallets (MoMo, ZaloPay, VNPay).
*   🖨️**Hardware Simulation & GDI+ Graphics:** Custom-rendered UI components, deterministic QR code matrices, and dynamically generated Code 128 barcodes for printed paper tickets.
*    **Operator Console:** Back-office features for terminal monitoring, printer jam clearing, central fare table management (UC11), and daily settlement reconciliation (UC12).
*    **Fault Tolerance (NFR2 & NFR4):** Built-in automated refund handling for simulated hardware failures (e.g., printer jams) and resilient database connections.

##  Project Links
- **GitHub Repository:** https://github.com/Narwhal1412/SE-QT2
- **Figma UI Design:** [MetroGo TVM Figma Mockups](https://www.figma.com/make/cpd1VhfymogiWUdNunS68w/Create-a-New-App?t=ycoktueirSDFqjfx-1)

---

## How to Open & Run in Visual Studio

1. Clone the repository and open `TvmTicketing.sln` in **Visual Studio 2022 / 2026** (ensure the `.NET 10.0 SDK` and `.NET desktop development` workloads are installed).
2. Set `TvmTicketing` as the Startup Project and press **F5** (or click Start) to run the application.
3. *Alternative CLI method:* Open your terminal and run:
   ```bash
   dotnet run --project src/TvmTicketing/TvmTicketing.csproj
