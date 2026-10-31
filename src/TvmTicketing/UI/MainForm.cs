using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using TvmTicketing.Domain;
using TvmTicketing.Enums;
using TvmTicketing.Services;

namespace TvmTicketing.UI
{
    public class MainForm : Form
    { 
        private readonly TVM _tvm = new TVM();
        private bool _isOperatorConsole = false;
        private int _currentStep = 1; // 1: Ticket & destination, 2: Payment method, 3: Active Payment QR/Card, 4: Print & collect
        private int _operatorTab = 0; // 0: Machine monitoring, 1: Fare management, 2: Daily reconciliation
        private PaymentMethod _selectedPaymentMethod = PaymentMethod.CreditCard;
        private WalletProvider _selectedWallet = WalletProvider.ZaloPay;
        private Domain.Payment? _activePayment;
        private string _activeQrPayload = string.Empty;
        private int _countdownSeconds = 118;
        private int _idleSeconds = 90;
        private string _unfinishedTxCode = "MG-D197A2E7";
        private bool _showUnfinishedBanner = true;
        private string _searchFilter = string.Empty;

        private readonly System.Windows.Forms.Timer _tickTimer = new System.Windows.Forms.Timer { Interval = 1000 };

        // Core Layout Containers
        private Panel _topUtilityBar = null!;
        private Panel _navTabStrip = null!;
        private Panel _bodyContainer = null!;
        private Panel _footerStatusBar = null!;

        // Kiosk View vs Operator Console View
        private Panel _kioskView = null!;
        private Panel _operatorView = null!;

        // Kiosk Header & Step Bar
        private Label _lblKioskBreadcrumb = null!;
        private Label _lblKioskMainTitle = null!;
        private Label _lblKioskSubtitle = null!;
        private ComboBox _cmbCurrentTerminal = null!;
        private Panel _stepIndicatorBar = null!;
        private RoundedCardPanel _unfinishedBanner = null!;

        // Kiosk Left Card & Right Sidebar
        private RoundedCardPanel _leftMainCard = null!;
        private RoundedCardPanel _rightSummaryCard = null!;
        private RoundedCardPanel _rightHealthCard = null!;
        private RoundedCardPanel _rightActivityCard = null!;

        // Right Summary Controls
        private Label _lblSummaryOrigin = null!;
        private Label _lblSummaryDest = null!;
        private Label _lblSummaryMeta = null!;
        private Label _lblSummaryTotal = null!;
        private MetroButton _btnPrimaryAction = null!;
        private Label _lblCancelAction = null!;

        // Right Health Controls
        private Label _lblHealthNetwork = null!;
        private Label _lblHealthPrinter = null!;
        private Label _lblHealthPaper = null!;
        private Label _lblActivityTx = null!;

        // Footer Controls
        private Label _lblFooterState = null!;
        private Label _lblFooterTerminalId = null!;
        private Label _lblTopConnectedStatus = null!;

        private Panel _stationListContainer = null!;
        private Panel _operatorSubContent = null!;
        private Panel _operatorSubTabStrip = null!;

        public MainForm()
        {
            Text = "MetroGo · Ticket Vending Terminal (PROTOTYPE v2.0 - .NET 10)";
            Size = new Size(1340, 940);
            MinimumSize = new Size(1220, 860);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = MetroGoTheme.PageBg;
            Font = MetroGoTheme.FontRegular(9.5f);
            DoubleBuffered = true;

            _tvm.StartSale();
            InitializeShellLayout();
            RefreshFullUi();

            _tickTimer.Tick += OnSecondTick;
            _tickTimer.Start();
        }

        private void OnSecondTick(object? sender, EventArgs e)
        {
            if (_currentStep == 3 && _countdownSeconds > 0)
            {
                _countdownSeconds--;
                _leftMainCard.Invalidate(true);
                if (_countdownSeconds == 0)
                {
                    MessageBox.Show("Payment window expired (120s timeout). Returning to payment method selection.",
                        "Session Timeout (FR13)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _currentStep = 2;
                    RefreshFullUi();
                }
            }
            if (_idleSeconds > 0)
                _idleSeconds--;
        }

        private void InitializeShellLayout()
        {
            _topUtilityBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.White
            };
            _topUtilityBar.Paint += (s, e) =>
            {
                using var p = new Pen(MetroGoTheme.BorderSubtle);
                e.Graphics.DrawLine(p, 0, _topUtilityBar.Height - 1, _topUtilityBar.Width, _topUtilityBar.Height - 1);
            };

            var btnNewTicket = CreateFlatBarButton("+   New ticket", 22, (s, e) => ResetToNewTicket());
            var btnBack = CreateFlatBarButton("←   Back", 150, (s, e) => NavigateBack());
            var btnHelp = CreateFlatBarButton("?   Help", 250, (s, e) => ShowQuickHelp());

            _lblTopConnectedStatus = new Label
            {
                AutoSize = true,
                Text = "●   Terminal connected",
                ForeColor = MetroGoTheme.SuccessGreen,
                Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 260, 14)
            };

            var lblLang = new Label
            {
                AutoSize = true,
                Text = "EN · VND",
                ForeColor = MetroGoTheme.TextMuted,
                Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 100, 14),
                Cursor = Cursors.Hand
            };

            _topUtilityBar.Controls.AddRange(new Control[] { btnNewTicket, btnBack, btnHelp, _lblTopConnectedStatus, lblLang });

            _navTabStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White
            };
            _navTabStrip.Paint += OnPaintNavTabStrip;
            _navTabStrip.MouseClick += OnClickNavTabStrip;

            _footerStatusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.White
            };
            _footerStatusBar.Paint += (s, e) =>
            {
                using var p = new Pen(MetroGoTheme.BorderSubtle);
                e.Graphics.DrawLine(p, 0, 0, _footerStatusBar.Width, 0);
            };

            _lblFooterState = new Label
            {
                AutoSize = true,
                Text = "●  Ready",
                ForeColor = MetroGoTheme.SuccessGreen,
                Font = MetroGoTheme.FontRegular(8.5f, FontStyle.Bold),
                Location = new Point(22, 10)
            };
            _lblFooterTerminalId = new Label
            {
                AutoSize = true,
                Text = "TD-004",
                ForeColor = MetroGoTheme.TextMuted,
                Font = MetroGoTheme.FontRegular(8.5f),
                Location = new Point(110, 10)
            };

            var lblFooterRight = new Label
            {
                AutoSize = true,
                Text = "↻ Refresh status      ⚙ Demo controls      MetroGo v2.0",
                ForeColor = MetroGoTheme.TextMuted,
                Font = MetroGoTheme.FontRegular(8.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 340, 10),
                Cursor = Cursors.Hand
            };
            lblFooterRight.Click += (s, e) => RefreshFullUi();

            _footerStatusBar.Controls.AddRange(new Control[] { _lblFooterState, _lblFooterTerminalId, lblFooterRight });

            _bodyContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = MetroGoTheme.PageBg,
                AutoScroll = true,
                Padding = new Padding(28, 18, 28, 18)
            };

            BuildKioskView();
            BuildOperatorView();

            Controls.Add(_bodyContainer);
            Controls.Add(_navTabStrip);
            Controls.Add(_topUtilityBar);
            Controls.Add(_footerStatusBar);
        }

        private Button CreateFlatBarButton(string text, int x, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 8),
                Size = new Size(115, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = MetroGoTheme.TextMuted,
                Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            btn.Click += onClick;
            return btn;
        }

        private void OnPaintNavTabStrip(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var borderPen = new Pen(MetroGoTheme.BorderSubtle);
            g.DrawLine(borderPen, 0, _navTabStrip.Height - 1, _navTabStrip.Width, _navTabStrip.Height - 1);

            Color c1 = !_isOperatorConsole ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.TextMuted;
            TextRenderer.DrawText(g, "🎫  Passenger kiosk", MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                new Rectangle(24, 10, 145, 32), c1, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            if (!_isOperatorConsole)
            {
                using var activePen = new Pen(MetroGoTheme.PrimaryBlueBright, 3f);
                g.DrawLine(activePen, 24, _navTabStrip.Height - 2, 160, _navTabStrip.Height - 2);
            }

            Color c2 = _isOperatorConsole ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.TextMuted;
            TextRenderer.DrawText(g, "📊  Operator console", MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                new Rectangle(185, 10, 160, 32), c2, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            if (_isOperatorConsole)
            {
                using var activePen = new Pen(MetroGoTheme.PrimaryBlueBright, 3f);
                g.DrawLine(activePen, 185, _navTabStrip.Height - 2, 330, _navTabStrip.Height - 2);
            }

            string dbBadge = _tvm.Repository.IsSqlConnected ? "MSSQL LocalDB · ONLINE" : "IN-MEMORY DB · ONLINE";
            TextRenderer.DrawText(g, $"{_tvm.location}   ({dbBadge})", MetroGoTheme.FontRegular(8f, FontStyle.Bold),
                new Rectangle(_navTabStrip.Width - 340, 12, 315, 28), MetroGoTheme.TextMuted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        private void OnClickNavTabStrip(object? sender, MouseEventArgs e)
        {
            if (e.X >= 20 && e.X <= 170)
            {
                _isOperatorConsole = false;
                RefreshFullUi();
            }
            else if (e.X >= 180 && e.X <= 345)
            {
                _isOperatorConsole = true;
                RefreshFullUi();
            }
        }

        private void BuildKioskView()
        {
            _kioskView = new Panel
            {
                Location = new Point(28, 16),
                Size = new Size(1240, 810),
                BackColor = Color.Transparent
            };

            _lblKioskBreadcrumb = new Label
            {
                Text = "PASSENGER SERVICES  /  TICKET PURCHASE",
                Font = MetroGoTheme.FontRegular(7.8f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(4, 2)
            };

            _lblKioskMainTitle = new Label
            {
                Text = "Purchase a ticket",
                Font = MetroGoTheme.FontRegular(20f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(0, 22)
            };

            _lblKioskSubtitle = new Label
            {
                Text = "Choose your ticket, pay securely, and collect it below.",
                Font = MetroGoTheme.FontRegular(9.2f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(4, 62)
            };

            var lblTermHeader = new Label
            {
                Text = "CURRENT TERMINAL",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(985, 16)
            };

            _cmbCurrentTerminal = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                BackColor = Color.White,
                Location = new Point(985, 36),
                Width = 235
            };
            _cmbCurrentTerminal.Items.Add("Thao Dien Station · MRT");
            _cmbCurrentTerminal.Items.Add("Thao Dien Bus Stop · BUS");
            _cmbCurrentTerminal.SelectedIndex = 0;
            _cmbCurrentTerminal.SelectedIndexChanged += (s, e) =>
            {
                bool isBus = _cmbCurrentTerminal.SelectedIndex == 1;
                _tvm.Origin = isBus ? "Thao Dien Bus Stop" : "Thao Dien Station";
                _tvm.location = isBus ? "BUS-TD-002 · BUS STOP" : "TD-004 · MRT STATION";
                ResetToNewTicket();
            };

            _stepIndicatorBar = new Panel
            {
                Location = new Point(2, 92),
                Size = new Size(1218, 52),
                BackColor = Color.Transparent
            };
            _stepIndicatorBar.Paint += OnPaintStepIndicatorBar;

            _unfinishedBanner = new RoundedCardPanel
            {
                Location = new Point(2, 154),
                Size = new Size(1218, 42),
                CornerRadius = 8,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };
            var lblUnfinished = new Label
            {
                Text = $"Unfinished transaction {_unfinishedTxCode} - Awaiting payment",
                Font = MetroGoTheme.FontRegular(8.8f),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(16, 12)
            };
            var lnkResume = new LinkLabel
            {
                Text = "Resume transaction",
                Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold),
                LinkColor = MetroGoTheme.PrimaryBlueBright,
                AutoSize = true,
                Location = new Point(1080, 12)
            };
            lnkResume.LinkClicked += (s, e) =>
            {
                _currentStep = 2;
                RefreshFullUi();
            };
            _unfinishedBanner.Controls.Add(lblUnfinished);
            _unfinishedBanner.Controls.Add(lnkResume);

            _leftMainCard = new RoundedCardPanel
            {
                Location = new Point(2, 208),
                Size = new Size(825, 555),
                CornerRadius = 10,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };

            _rightSummaryCard = new RoundedCardPanel
            {
                Location = new Point(845, 208),
                Size = new Size(375, 365),
                CornerRadius = 10,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };
            BuildRightSummaryCard();

            _rightHealthCard = new RoundedCardPanel
            {
                Location = new Point(845, 585),
                Size = new Size(375, 125),
                CornerRadius = 10,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };
            BuildRightHealthCard();

            _rightActivityCard = new RoundedCardPanel
            {
                Location = new Point(845, 720),
                Size = new Size(375, 42),
                CornerRadius = 8,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };
            _lblActivityTx = new Label
            {
                Text = $"▸  Transaction activity                                     {_tvm.Controller.CurrentTransaction.ShortCode}",
                Font = MetroGoTheme.FontRegular(8.3f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(14, 12)
            };
            _rightActivityCard.Controls.Add(_lblActivityTx);

            var lblSessionNote = new Label
            {
                Text = "🕒  Session resets after 90 seconds of inactivity.",
                Font = MetroGoTheme.FontRegular(8.2f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(6, 772)
            };

            _kioskView.Controls.AddRange(new Control[]
            {
                _lblKioskBreadcrumb, _lblKioskMainTitle, _lblKioskSubtitle,
                lblTermHeader, _cmbCurrentTerminal,
                _stepIndicatorBar, _unfinishedBanner,
                _leftMainCard, _rightSummaryCard, _rightHealthCard, _rightActivityCard,
                lblSessionNote
            });

            _bodyContainer.Controls.Add(_kioskView);
        }

        private void OnPaintStepIndicatorBar(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, _stepIndicatorBar.Width - 3, _stepIndicatorBar.Height - 3);
            MetroGoTheme.DrawRoundedBox(g, rect, 9, Color.White, MetroGoTheme.BorderSubtle, 1f);

            int activeGroup = _currentStep == 1 ? 1 : (_currentStep <= 3 ? 2 : 3);

            DrawStepBadge(g, 20, 12, "01", "Ticket & destination", activeGroup == 1, activeGroup > 1);
            using (var linePen = new Pen(MetroGoTheme.BorderSubtle, 1.5f))
            {
                g.DrawLine(linePen, 195, 26, 360, 26);
                g.DrawLine(linePen, 485, 26, 665, 26);
            }
            DrawStepBadge(g, 375, 12, "02", "Payment", activeGroup == 2, activeGroup > 2);
            DrawStepBadge(g, 680, 12, "03", "Print & collect", activeGroup == 3, false);

            TextRenderer.DrawText(g, "🛡  No card details stored", MetroGoTheme.FontRegular(8f),
                new Rectangle(_stepIndicatorBar.Width - 190, 14, 175, 24), MetroGoTheme.TextMuted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        private void DrawStepBadge(Graphics g, int x, int y, string num, string label, bool isCurrent, bool isDone)
        {
            var box = new Rectangle(x, y, 28, 28);
            if (isDone)
            {
                MetroGoTheme.DrawRoundedBox(g, box, 6, MetroGoTheme.SuccessBg, MetroGoTheme.SuccessGreen, 1.2f);
                TextRenderer.DrawText(g, "✓", MetroGoTheme.FontRegular(9.5f, FontStyle.Bold), box,
                    MetroGoTheme.SuccessGreen, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else if (isCurrent)
            {
                MetroGoTheme.DrawRoundedBox(g, box, 6, MetroGoTheme.PrimaryBlue, MetroGoTheme.PrimaryBlue, 0f);
                TextRenderer.DrawText(g, num, MetroGoTheme.FontRegular(8.5f, FontStyle.Bold), box,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else
            {
                MetroGoTheme.DrawRoundedBox(g, box, 6, Color.White, MetroGoTheme.BorderSubtle, 1f);
                TextRenderer.DrawText(g, num, MetroGoTheme.FontRegular(8.5f, FontStyle.Bold), box,
                    MetroGoTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            Color textCol = isDone ? MetroGoTheme.SuccessGreen : (isCurrent ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.TextMuted);
            TextRenderer.DrawText(g, label, MetroGoTheme.FontRegular(9f, FontStyle.Bold),
                new Rectangle(x + 36, y, 145, 28), textCol, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private void BuildRightSummaryCard()
        {
            var lblTitle = new Label
            {
                Text = "Journey summary",
                Font = MetroGoTheme.FontRegular(10.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(22, 20)
            };

            var badgeLine = new Label
            {
                Text = "METRO LINE 1",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.PrimaryBlueBright,
                BackColor = MetroGoTheme.SelectedCardBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(92, 22),
                Location = new Point(260, 19)
            };

            var lblFromCap = new Label
            {
                Text = "FROM",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(36, 62)
            };
            _lblSummaryOrigin = new Label
            {
                Text = "Thao Dien Station",
                Font = MetroGoTheme.FontRegular(10.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(36, 80)
            };

            var lblToCap = new Label
            {
                Text = "TO",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(36, 114)
            };
            _lblSummaryDest = new Label
            {
                Text = "Ba Son",
                Font = MetroGoTheme.FontRegular(10.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(36, 132)
            };

            _lblSummaryMeta = new Label
            {
                Text = "🎫  Single journey                                       1 passenger",
                Font = MetroGoTheme.FontRegular(8.5f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(22, 174)
            };

            var lblTotalCap = new Label
            {
                Text = "TOTAL FARE",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(22, 210)
            };

            _lblSummaryTotal = new Label
            {
                Text = "10,000 VND",
                Font = MetroGoTheme.FontRegular(18f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(20, 226)
            };

            var lblTaxes = new Label
            {
                Text = "All taxes included",
                Font = MetroGoTheme.FontRegular(7.5f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(268, 236)
            };

            _btnPrimaryAction = new MetroButton
            {
                Text = "Choose payment method   →",
                Location = new Point(22, 268),
                Size = new Size(330, 40)
            };
            _btnPrimaryAction.Click += OnPrimarySummaryActionClick;

            _lblCancelAction = new Label
            {
                Text = "Cancel purchase",
                Font = MetroGoTheme.FontRegular(8.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(330, 20),
                Location = new Point(22, 316),
                Cursor = Cursors.Hand
            };
            _lblCancelAction.Click += (s, e) => ResetToNewTicket();

            var lblDemoFoot = new Label
            {
                Text = "🛡  DEMO ONLY · NO REAL CHARGE",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(330, 18),
                Location = new Point(22, 340)
            };

            _rightSummaryCard.Paint += (s, e) =>
            {
                using var p = new Pen(MetroGoTheme.BorderSubtle, 1f) { DashStyle = DashStyle.Dot };
                e.Graphics.DrawLine(p, 24, 66, 24, 152);
                using var solidPen = new Pen(MetroGoTheme.BorderSubtle, 1f);
                e.Graphics.DrawLine(solidPen, 22, 165, 352, 165);
                e.Graphics.DrawLine(solidPen, 22, 200, 352, 200);
            };

            _rightSummaryCard.Controls.AddRange(new Control[]
            {
                lblTitle, badgeLine, lblFromCap, _lblSummaryOrigin,
                lblToCap, _lblSummaryDest, _lblSummaryMeta,
                lblTotalCap, _lblSummaryTotal, lblTaxes,
                _btnPrimaryAction, _lblCancelAction, lblDemoFoot
            });
        }

        private void BuildRightHealthCard()
        {
            var lblTitle = new Label
            {
                Text = "⚙  Terminal health",
                Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(18, 14)
            };

            var lblNet = new Label { Text = "Network", ForeColor = MetroGoTheme.TextMuted, Font = MetroGoTheme.FontRegular(8.5f), AutoSize = true, Location = new Point(18, 42) };
            _lblHealthNetwork = new Label { Text = "●  Connected", ForeColor = MetroGoTheme.SuccessGreen, Font = MetroGoTheme.FontRegular(8.5f, FontStyle.Bold), AutoSize = true, Location = new Point(268, 42) };

            var lblPrn = new Label { Text = "Thermal printer", ForeColor = MetroGoTheme.TextMuted, Font = MetroGoTheme.FontRegular(8.5f), AutoSize = true, Location = new Point(18, 68) };
            _lblHealthPrinter = new Label { Text = "●  Ready", ForeColor = MetroGoTheme.SuccessGreen, Font = MetroGoTheme.FontRegular(8.5f, FontStyle.Bold), AutoSize = true, Location = new Point(290, 68) };

            var lblPap = new Label { Text = "Paper remaining", ForeColor = MetroGoTheme.TextMuted, Font = MetroGoTheme.FontRegular(8.5f), AutoSize = true, Location = new Point(18, 94) };
            _lblHealthPaper = new Label { Text = "192 tickets", ForeColor = MetroGoTheme.TextMuted, Font = MetroGoTheme.FontRegular(8.5f, FontStyle.Bold), AutoSize = true, Location = new Point(290, 94) };

            _rightHealthCard.Controls.AddRange(new Control[] { lblTitle, lblNet, _lblHealthNetwork, lblPrn, _lblHealthPrinter, lblPap, _lblHealthPaper });
        }

        private void OnPrimarySummaryActionClick(object? sender, EventArgs e)
        {
            if (_currentStep == 1)
            {
                _currentStep = 2;
                RefreshFullUi();
            }
            else if (_currentStep == 2)
            {
                var res = _tvm.Controller.BeginPayment(_selectedPaymentMethod, _selectedWallet);
                _activePayment = res.payment;
                _activeQrPayload = res.payload;
                _countdownSeconds = _selectedPaymentMethod == PaymentMethod.CreditCard ? 30 : 118;
                _currentStep = 3;
                RefreshFullUi();
            }
            else if (_currentStep == 3)
            {
                ConfirmSimulatedPayment();
            }
            else if (_currentStep == 4)
            {
                ResetToNewTicket();
            }
        }

        private void ConfirmSimulatedPayment()
        {
            if (_activePayment == null) return;
            var (ok, msg) = _tvm.Controller.CompleteSale(_activePayment);
            if (!ok)
            {
                MessageBox.Show(msg, "Hardware Fault · Automatic Refund (UC08)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshFullUi();
                return;
            }
            _showUnfinishedBanner = false;
            _currentStep = 4;
            RefreshFullUi();
        }

        private void PopulateLeftCardStep1()
        {
            _leftMainCard.Controls.Clear();
            var tx = _tvm.Controller.CurrentTransaction;

            var lblHeader = new Label
            {
                Text = "Choose your ticket",
                Font = MetroGoTheme.FontRegular(13f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(26, 22)
            };
            var lblSub = new Label
            {
                Text = "Select a ticket type and your destination.",
                Font = MetroGoTheme.FontRegular(8.8f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(26, 48)
            };
            var badgeStep = new Label
            {
                Text = "STEP 01 / 03",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(78, 22),
                Location = new Point(718, 24)
            };

            var cardSingle = CreateOptionCard(new Rectangle(26, 80, 375, 68),
                "🎫   Single journey", "One trip · Fare by destination",
                tx.Fare.TicketType == TicketType.SingleJourney,
                () => { tx.Fare.TicketType = TicketType.SingleJourney; _tvm.Controller.CalculateFare(); RefreshFullUi(); });

            var cardPass = CreateOptionCard(new Rectangle(418, 80, 378, 68),
                "🕒   1-day pass", "40,000 VND · Unlimited today",
                tx.Fare.TicketType == TicketType.OneDayPass,
                () => { tx.Fare.TicketType = TicketType.OneDayPass; _tvm.Controller.CalculateFare(); RefreshFullUi(); });

            var lblFromInfo = new Label
            {
                Text = $"🚆   FROM   {_tvm.Origin}",
                Font = MetroGoTheme.FontRegular(8.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(26, 162)
            };
            var lblLineRight = new Label
            {
                Text = tx.Fare.LineName,
                Font = MetroGoTheme.FontRegular(8.2f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(720, 162)
            };

            var searchWrap = new RoundedCardPanel
            {
                Location = new Point(26, 188),
                Size = new Size(770, 38),
                CornerRadius = 7,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };
            var lblSearchIcon = new Label
            {
                Text = "🔍",
                AutoSize = true,
                Location = new Point(12, 9),
                ForeColor = MetroGoTheme.TextMuted
            };
            var txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = MetroGoTheme.FontRegular(9.2f),
                PlaceholderText = "Search destination station...",
                Text = _searchFilter,
                Location = new Point(38, 10),
                Width = 710
            };
            txtSearch.TextChanged += (s, e) =>
            {
                _searchFilter = txtSearch.Text;
                PopulateStationListPanel();
            };
            searchWrap.Controls.Add(lblSearchIcon);
            searchWrap.Controls.Add(txtSearch);

            var lblDestCol = new Label
            {
                Text = "DESTINATION",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(26, 236)
            };
            var lblFareCol = new Label
            {
                Text = "STANDARD FARE",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(712, 236)
            };

            _stationListContainer = new Panel
            {
                Location = new Point(26, 254),
                Size = new Size(770, 224),
                AutoScroll = true,
                BackColor = Color.White
            };
            PopulateStationListPanel();

            var lblPassTitle = new Label
            {
                Text = "Passengers",
                Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(26, 492)
            };
            var lblPassSub = new Label
            {
                Text = $"{tx.Fare.Category} fare · Up to 5 tickets",
                Font = MetroGoTheme.FontRegular(7.8f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(26, 512)
            };

            int catX = 240;
            foreach (FareCategory cat in Enum.GetValues(typeof(FareCategory)))
            {
                bool activeCat = tx.Fare.Category == cat;
                var btnCat = new MetroButton
                {
                    Text = cat == FareCategory.Adult ? "Adult (Standard)" : $"{cat} (-50%)",
                    Location = new Point(catX, 494),
                    Size = new Size(122, 32),
                    CornerRadius = 6,
                    Font = MetroGoTheme.FontRegular(8.2f, FontStyle.Bold),
                    FillColor = activeCat ? MetroGoTheme.SelectedCardBg : Color.White,
                    HoverFillColor = MetroGoTheme.SelectedCardBg,
                    BorderColor = activeCat ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.BorderSubtle,
                    TextColor = activeCat ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.TextMuted
                };
                var capturedCat = cat;
                btnCat.Click += (s, e) =>
                {
                    tx.Fare.Category = capturedCat;
                    _tvm.Controller.CalculateFare();
                    RefreshFullUi();
                };
                _leftMainCard.Controls.Add(btnCat);
                catX += 130;
            }

            var btnMinus = new MetroButton
            {
                Text = "−",
                Location = new Point(682, 492),
                Size = new Size(34, 34),
                FillColor = Color.White,
                HoverFillColor = Color.FromArgb(241, 245, 249),
                BorderColor = MetroGoTheme.BorderSubtle,
                TextColor = MetroGoTheme.TextDark
            };
            btnMinus.Click += (s, e) =>
            {
                if (tx.Quantity > 1) { tx.Quantity--; _tvm.Controller.CalculateFare(); RefreshFullUi(); }
            };

            var lblQty = new Label
            {
                Text = tx.Quantity.ToString(),
                Font = MetroGoTheme.FontRegular(10f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(40, 34),
                Location = new Point(718, 492)
            };

            var btnPlus = new MetroButton
            {
                Text = "+",
                Location = new Point(760, 492),
                Size = new Size(34, 34),
                FillColor = Color.White,
                HoverFillColor = Color.FromArgb(241, 245, 249),
                BorderColor = MetroGoTheme.BorderSubtle,
                TextColor = MetroGoTheme.TextDark
            };
            btnPlus.Click += (s, e) =>
            {
                if (tx.Quantity < 5) { tx.Quantity++; _tvm.Controller.CalculateFare(); RefreshFullUi(); }
            };

            _leftMainCard.Controls.AddRange(new Control[]
            {
                lblHeader, lblSub, badgeStep, cardSingle, cardPass,
                lblFromInfo, lblLineRight, searchWrap, lblDestCol, lblFareCol,
                _stationListContainer, lblPassTitle, lblPassSub, btnMinus, lblQty, btnPlus
            });
        }

        private void PopulateStationListPanel()
        {
            if (_stationListContainer == null) return;
            _stationListContainer.Controls.Clear();

            var tx = _tvm.Controller.CurrentTransaction;
            var fares = _tvm.Repository.CatalogFares
                .Where(f => f.Origin == _tvm.Origin &&
                            (string.IsNullOrWhiteSpace(_searchFilter) ||
                             f.Destination.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                             f.DistrictInfo.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            int y = 0;
            foreach (var f in fares)
            {
                bool isSelected = tx.Fare.Destination == f.Destination;
                var row = new Panel
                {
                    Location = new Point(0, y),
                    Size = new Size(748, 54),
                    Cursor = Cursors.Hand,
                    BackColor = isSelected ? MetroGoTheme.SelectedCardBg : Color.White
                };

                var capturedFare = f;
                row.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using var borderPen = new Pen(MetroGoTheme.BorderSubtle);
                    g.DrawRectangle(borderPen, 0, 0, row.Width - 1, row.Height - 1);
                    if (isSelected)
                    {
                        using var leftBar = new SolidBrush(MetroGoTheme.PrimaryBlueBright);
                        g.FillRectangle(leftBar, 0, 0, 4, row.Height);
                    }

                    TextRenderer.DrawText(g, "🚆", MetroGoTheme.FontRegular(10f), new Rectangle(16, 15, 28, 24), MetroGoTheme.TextMuted);
                    TextRenderer.DrawText(g, capturedFare.Destination, MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                        new Rectangle(46, 8, 360, 22), MetroGoTheme.TextDark, TextFormatFlags.Left);
                    TextRenderer.DrawText(g, capturedFare.DistrictInfo, MetroGoTheme.FontRegular(7.8f),
                        new Rectangle(46, 29, 440, 18), MetroGoTheme.TextMuted, TextFormatFlags.Left);

                    TextRenderer.DrawText(g, $"{capturedFare.AmountVnd:N0} VND", MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                        new Rectangle(565, 16, 130, 22), MetroGoTheme.PrimaryBlue, TextFormatFlags.Right);

                    MetroGoTheme.DrawRadioCircle(g, new Point(720, 27), isSelected);
                };

                row.Click += (s, e) =>
                {
                    tx.Fare.Destination = capturedFare.Destination;
                    tx.Fare.AmountVnd = capturedFare.AmountVnd;
                    tx.Fare.DistrictInfo = capturedFare.DistrictInfo;
                    tx.Fare.LineName = capturedFare.LineName;
                    _tvm.Controller.CalculateFare();
                    RefreshFullUi();
                };

                _stationListContainer.Controls.Add(row);
                y += 54;
            }
        }

        private Panel CreateOptionCard(Rectangle bounds, string title, string subtitle, bool selected, Action onSelect)
        {
            var card = new Panel
            {
                Bounds = bounds,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var r = new Rectangle(1, 1, card.Width - 3, card.Height - 3);
                Color fill = selected ? MetroGoTheme.SelectedCardBg : Color.White;
                Color border = selected ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.BorderSubtle;
                MetroGoTheme.DrawRoundedBox(g, r, 8, fill, border, selected ? 1.6f : 1f);

                TextRenderer.DrawText(g, title, MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                    new Rectangle(18, 12, card.Width - 65, 22), MetroGoTheme.TextDark, TextFormatFlags.Left);
                TextRenderer.DrawText(g, subtitle, MetroGoTheme.FontRegular(8f),
                    new Rectangle(42, 36, card.Width - 80, 20), MetroGoTheme.TextMuted, TextFormatFlags.Left);

                MetroGoTheme.DrawRadioCircle(g, new Point(card.Width - 26, card.Height / 2), selected);
            };
            card.Click += (s, e) => onSelect();
            return card;
        }

        private void PopulateLeftCardStep2()
        {
            _leftMainCard.Controls.Clear();

            var lblHeader = new Label
            {
                Text = "How would you like to pay?",
                Font = MetroGoTheme.FontRegular(13f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(28, 24)
            };
            var lblSub = new Label
            {
                Text = "Choose your payment method before proceeding.",
                Font = MetroGoTheme.FontRegular(8.8f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(28, 50)
            };
            var badgeStep = new Label
            {
                Text = "STEP 02 / 03",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(78, 22),
                Location = new Point(718, 26)
            };

            var cardCredit = CreateOptionCard(new Rectangle(28, 86, 768, 76),
                "💳   Credit card", "Insert or tap · Visa, Mastercard, JCB",
                _selectedPaymentMethod == PaymentMethod.CreditCard,
                () => { _selectedPaymentMethod = PaymentMethod.CreditCard; RefreshFullUi(); });

            var cardBankQr = CreateOptionCard(new Rectangle(28, 176, 768, 76),
                "🔳   Bank QR code", "VietQR · Scan using your banking app",
                _selectedPaymentMethod == PaymentMethod.BankQR,
                () => { _selectedPaymentMethod = PaymentMethod.BankQR; RefreshFullUi(); });

            var cardWallet = CreateOptionCard(new Rectangle(28, 266, 768, 76),
                "👛   Digital wallet", "MoMo, ZaloPay, or VNPay",
                _selectedPaymentMethod == PaymentMethod.DigitalWallet,
                () => { _selectedPaymentMethod = PaymentMethod.DigitalWallet; RefreshFullUi(); });

            _leftMainCard.Controls.AddRange(new Control[] { lblHeader, lblSub, badgeStep, cardCredit, cardBankQr, cardWallet });

            int securityBoxY = 362;
            if (_selectedPaymentMethod == PaymentMethod.DigitalWallet)
            {
                var lblChooseWallet = new Label
                {
                    Text = "CHOOSE WALLET",
                    Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                    ForeColor = MetroGoTheme.TextMuted,
                    AutoSize = true,
                    Location = new Point(28, 358)
                };
                _leftMainCard.Controls.Add(lblChooseWallet);

                WalletProvider[] wallets = { WalletProvider.MoMo, WalletProvider.ZaloPay, WalletProvider.VNPay };
                int wx = 28;
                foreach (var w in wallets)
                {
                    bool isSel = _selectedWallet == w;
                    var btnW = new MetroButton
                    {
                        Text = w.ToString(),
                        Location = new Point(wx, 380),
                        Size = new Size(246, 40),
                        CornerRadius = 6,
                        FillColor = isSel ? MetroGoTheme.SelectedCardBg : Color.White,
                        HoverFillColor = MetroGoTheme.SelectedCardBg,
                        BorderColor = isSel ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.BorderSubtle,
                        TextColor = isSel ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.TextMuted
                    };
                    var capturedW = w;
                    btnW.Click += (s, e) => { _selectedWallet = capturedW; RefreshFullUi(); };
                    _leftMainCard.Controls.Add(btnW);
                    wx += 261;
                }
                securityBoxY = 438;
            }

            var secNotice = new RoundedCardPanel
            {
                Location = new Point(28, securityBoxY),
                Size = new Size(768, 48),
                CornerRadius = 7,
                FillColor = Color.FromArgb(248, 250, 252),
                BorderColor = MetroGoTheme.BorderSubtle
            };
            var lblSec = new Label
            {
                Text = "🛡   No card numbers, PINs, CVVs, banking credentials, or wallet credentials are requested or stored. This prototype uses simulated payment confirmation.",
                Font = MetroGoTheme.FontRegular(8f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(16, 15)
            };
            secNotice.Controls.Add(lblSec);
            _leftMainCard.Controls.Add(secNotice);
        }

        private void PopulateLeftCardStep3()
        {
            _leftMainCard.Controls.Clear();
            var tx = _tvm.Controller.CurrentTransaction;

            string providerTitle = _selectedPaymentMethod switch
            {
                PaymentMethod.CreditCard => "Credit Card (EMV Reader)",
                PaymentMethod.BankQR => "VietQR Banking App",
                _ => _selectedWallet.ToString()
            };

            var badgeAwait = new Label
            {
                Text = "AWAITING PAYMENT",
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = MetroGoTheme.PrimaryBlueBright,
                BackColor = MetroGoTheme.SelectedCardBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(125, 24),
                Location = new Point(28, 24)
            };

            var lblCountdown = new Label
            {
                Text = $"🕒  {_countdownSeconds / 60}:{_countdownSeconds % 60:D2}",
                Font = MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(730, 26)
            };

            var lblPayHeading = new Label
            {
                Text = _selectedPaymentMethod == PaymentMethod.CreditCard
                    ? "Insert or tap your card"
                    : $"Pay with {providerTitle}",
                Font = MetroGoTheme.FontRegular(14f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(760, 28),
                Location = new Point(32, 62)
            };

            var lblPaySub = new Label
            {
                Text = _selectedPaymentMethod == PaymentMethod.CreditCard
                    ? $"Present Visa, Mastercard, JCB or NAPAS card on the EMV terminal (Attempt {_tvm.Controller.CardAttemptCount + 1}/3)."
                    : "Scan the code shown by this machine using your phone.",
                Font = MetroGoTheme.FontRegular(8.8f),
                ForeColor = MetroGoTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(760, 20),
                Location = new Point(32, 92)
            };

            var qrCard = new RoundedCardPanel
            {
                Location = new Point(302, 122),
                Size = new Size(220, 232),
                CornerRadius = 10,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };
            qrCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (_selectedPaymentMethod == PaymentMethod.CreditCard)
                {
                    TextRenderer.DrawText(g, "EMV CARD TERMINAL", MetroGoTheme.FontRegular(8.5f, FontStyle.Bold),
                        new Rectangle(0, 10, qrCard.Width, 20), MetroGoTheme.TextDark, TextFormatFlags.HorizontalCenter);

                    // Draw an illustrative credit card with chip and contactless icon
                    var cardBox = new Rectangle(24, 38, 172, 110);
                    MetroGoTheme.DrawRoundedBox(g, cardBox, 8, Color.FromArgb(30, 41, 59), Color.FromArgb(71, 85, 105), 1.2f);

                    // Chip
                    var chipBox = new Rectangle(40, 56, 32, 24);
                    MetroGoTheme.DrawRoundedBox(g, chipBox, 4, Color.FromArgb(234, 179, 8), Color.FromArgb(202, 138, 4), 1f);

                    // Contactless waves symbol
                    TextRenderer.DrawText(g, "))))", MetroGoTheme.FontRegular(10f, FontStyle.Bold),
                        new Rectangle(80, 56, 40, 24), Color.FromArgb(226, 232, 240), TextFormatFlags.Left);

                    // Card network branding
                    TextRenderer.DrawText(g, "VISA · MC · NAPAS", MetroGoTheme.FontRegular(7.5f, FontStyle.Bold),
                        new Rectangle(28, 118, 164, 20), Color.White, TextFormatFlags.HorizontalCenter);

                    TextRenderer.DrawText(g, "↓  INSERT CHIP OR TAP  ↑", MetroGoTheme.FontRegular(8f, FontStyle.Bold),
                        new Rectangle(0, 162, qrCard.Width, 20), MetroGoTheme.PrimaryBlueBright, TextFormatFlags.HorizontalCenter);

                    TextRenderer.DrawText(g, "SECURE PCI-PTS ENCRYPTED", MetroGoTheme.FontRegular(6.8f, FontStyle.Bold),
                        new Rectangle(0, 204, qrCard.Width, 18), MetroGoTheme.TextMuted, TextFormatFlags.HorizontalCenter);
                }
                else
                {
                    TextRenderer.DrawText(g, providerTitle, MetroGoTheme.FontRegular(8.5f, FontStyle.Bold),
                        new Rectangle(0, 10, qrCard.Width, 20), MetroGoTheme.TextDark, TextFormatFlags.HorizontalCenter);

                    MetroGoTheme.DrawQrMatrix(g, new Rectangle(28, 34, 164, 164), _activeQrPayload + tx.ShortCode);

                    TextRenderer.DrawText(g, "DEMO QR · SCAN WITH APP", MetroGoTheme.FontRegular(6.8f, FontStyle.Bold),
                        new Rectangle(0, 204, qrCard.Width, 18), MetroGoTheme.TextMuted, TextFormatFlags.HorizontalCenter);
                }
            };

            var lblAmtCenter = new Label
            {
                Text = $"{tx.TotalAmountVnd:N0} VND",
                Font = MetroGoTheme.FontRegular(15f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(300, 28),
                Location = new Point(262, 366)
            };

            var lblTxCode = new Label
            {
                Text = tx.ShortCode,
                Font = MetroGoTheme.FontRegular(8f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(300, 18),
                Location = new Point(262, 396)
            };

            var btnSimConfirm = new MetroButton
            {
                Text = "Simulate payment confirmation   ✓",
                Location = new Point(282, 424),
                Size = new Size(260, 40)
            };
            btnSimConfirm.Click += (s, e) => ConfirmSimulatedPayment();

            var btnSimDecline = new MetroButton
            {
                Text = "Simulate Decline",
                Location = new Point(282, 472),
                Size = new Size(125, 28),
                Font = MetroGoTheme.FontRegular(7.8f, FontStyle.Bold),
                FillColor = Color.White,
                HoverFillColor = Color.FromArgb(254, 242, 242),
                BorderColor = MetroGoTheme.BorderSubtle,
                TextColor = MetroGoTheme.DangerRed
            };
            btnSimDecline.Click += (s, e) =>
            {
                _tvm.Controller.CardAttemptCount++;
                if (_tvm.Controller.CardAttemptCount >= 3)
                {
                    MessageBox.Show("Payment declined 3 times (FR14). Transaction automatically cancelled.",
                        "Transaction Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetToNewTicket();
                }
                else
                {
                    MessageBox.Show($"Payment declined by issuer (Attempt {_tvm.Controller.CardAttemptCount}/3). Please retry or switch payment mode.",
                        "Payment Declined (FR14)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    PopulateLeftCardStep3();
                }
            };

            var btnSimJam = new MetroButton
            {
                Text = "Simulate Print Jam (UC08)",
                Location = new Point(417, 472),
                Size = new Size(125, 28),
                Font = MetroGoTheme.FontRegular(7.8f, FontStyle.Bold),
                FillColor = Color.White,
                HoverFillColor = Color.FromArgb(254, 242, 242),
                BorderColor = MetroGoTheme.BorderSubtle,
                TextColor = MetroGoTheme.TextMuted
            };
            btnSimJam.Click += (s, e) =>
            {
                _tvm.Printer.Status = PrinterStatus.Jammed;
                _tvm.SetOutOfService();

                // Synchronize terminal status in repository immediately
                string curCode = _tvm.Controller.CurrentTransaction.TerminalId;
                var curTerm = _tvm.Repository.Terminals.FirstOrDefault(t => t.TerminalCode == curCode)
                              ?? _tvm.Repository.Terminals.FirstOrDefault(t => t.TerminalCode == "TD-004");
                if (curTerm != null)
                {
                    curTerm.PrinterStatusText = "Jammed";
                    curTerm.IsOnline = false;
                }

                ConfirmSimulatedPayment();
            };

            var lblNoRealMoney = new Label
            {
                Text = "NO REAL MONEY WILL BE TAKEN",
                Font = MetroGoTheme.FontRegular(6.8f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(300, 16),
                Location = new Point(262, 508)
            };

            _leftMainCard.Controls.AddRange(new Control[]
            {
                badgeAwait, lblCountdown, lblPayHeading, lblPaySub,
                qrCard, lblAmtCenter, lblTxCode,
                btnSimConfirm, btnSimDecline, btnSimJam, lblNoRealMoney
            });
        }

        private void PopulateLeftCardStep4()
        {
            _leftMainCard.Controls.Clear();
            var tx = _tvm.Controller.CurrentTransaction;
            var firstTicket = tx.Tickets.FirstOrDefault() ?? new Ticket
            {
                TicketNumber = $"{tx.ShortCode}-1",
                Origin = tx.Fare.Origin,
                Destination = tx.Fare.Destination,
                PriceVnd = tx.TotalAmountVnd,
                IssuedAt = DateTime.Now
            };

            var badgeDispensed = new Label
            {
                Text = "✓   TICKET PRINTED & DISPENSED",
                Font = MetroGoTheme.FontRegular(7.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.SuccessGreen,
                BackColor = MetroGoTheme.SuccessBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(195, 24),
                Location = new Point(315, 22)
            };

            var lblTitle = new Label
            {
                Text = "Please collect your ticket.",
                Font = MetroGoTheme.FontRegular(14f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(760, 28),
                Location = new Point(32, 56)
            };

            var lblSub = new Label
            {
                Text = $"{tx.Quantity} paper ticket · Keep it with you for your journey.",
                Font = MetroGoTheme.FontRegular(8.8f),
                ForeColor = MetroGoTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(760, 20),
                Location = new Point(32, 86)
            };

            int cardW = 360;
            int cardH = 340;
            int cardX = (_leftMainCard.Width - cardW) / 2;

            var paperTicketCard = new RoundedCardPanel
            {
                Location = new Point(cardX, 116),
                Size = new Size(cardW, cardH),
                CornerRadius = 10,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };
            paperTicketCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                TextRenderer.DrawText(g, "METROGO SMART TICKET", MetroGoTheme.FontRegular(11.5f, FontStyle.Bold),
                    new Rectangle(24, 18, cardW - 48, 24), MetroGoTheme.TextDark, TextFormatFlags.Left);

                string routeText = $"{firstTicket.Origin}  ➔  {firstTicket.Destination}";
                var routeFont = routeText.Length > 28 ? MetroGoTheme.FontRegular(9f, FontStyle.Bold) : MetroGoTheme.FontRegular(10.5f, FontStyle.Bold);
                TextRenderer.DrawText(g, routeText, routeFont,
                    new Rectangle(24, 50, cardW - 48, 42), MetroGoTheme.TextDark, TextFormatFlags.Left | TextFormatFlags.WordBreak);

                TextRenderer.DrawText(g, $"Category: {firstTicket.Category}   |   Qty: {tx.Quantity}", MetroGoTheme.FontRegular(8.8f),
                    new Rectangle(24, 94, cardW - 48, 20), MetroGoTheme.TextMuted, TextFormatFlags.Left);

                TextRenderer.DrawText(g, $"Total Paid: {tx.TotalAmountVnd:N0} VND", MetroGoTheme.FontRegular(10.5f, FontStyle.Bold),
                    new Rectangle(24, 120, cardW - 48, 22), MetroGoTheme.TextDark, TextFormatFlags.Left);

                TextRenderer.DrawText(g, $"Issued: {firstTicket.IssuedAt:dd/MM/yyyy HH:mm}    Expires: {firstTicket.ExpiresAt:HH:mm}", MetroGoTheme.FontRegular(8.2f),
                    new Rectangle(24, 150, cardW - 48, 18), MetroGoTheme.TextMuted, TextFormatFlags.Left);

                MetroGoTheme.DrawCode128Barcode(g, new Rectangle(24, 178, cardW - 48, 54), firstTicket.BarcodeCode128);

                TextRenderer.DrawText(g, firstTicket.TicketNumber, MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                    new Rectangle(24, 242, cardW - 48, 20), MetroGoTheme.TextDark, TextFormatFlags.HorizontalCenter);

                TextRenderer.DrawText(g, "PROTOTYPE ONLY · NOT VALID FOR TRAVEL", MetroGoTheme.FontRegular(6.5f, FontStyle.Bold),
                    new Rectangle(24, 304, cardW - 48, 16), MetroGoTheme.TextMuted, TextFormatFlags.HorizontalCenter);
            };

            int btnW = 160;
            int btnGap = 16;
            int totalBtnW = btnW * 2 + btnGap;
            int btnStartX = (_leftMainCard.Width - totalBtnW) / 2;

            var btnPrintPaper = new MetroButton
            {
                Text = "🖨   Print paper copy",
                Location = new Point(btnStartX, 468),
                Size = new Size(btnW, 38),
                FillColor = Color.White,
                HoverFillColor = Color.FromArgb(241, 245, 249),
                BorderColor = MetroGoTheme.BorderSubtle,
                TextColor = MetroGoTheme.TextDark
            };
            btnPrintPaper.Click += (s, e) => PrintPaperTicketCopy(firstTicket, tx);

            var btnCollected = new MetroButton
            {
                Text = "Ticket collected   ✓",
                Location = new Point(btnStartX + btnW + btnGap, 468),
                Size = new Size(btnW, 38)
            };
            btnCollected.Click += (s, e) => ResetToNewTicket();

            var lblFootNote = new Label
            {
                Text = "Printer hardware is simulated. 'Print paper copy' opens your system print preview dialog.",
                Font = MetroGoTheme.FontRegular(7.5f),
                ForeColor = MetroGoTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(760, 18),
                Location = new Point(32, 516)
            };

            _leftMainCard.Controls.AddRange(new Control[]
            {
                badgeDispensed, lblTitle, lblSub, paperTicketCard,
                btnPrintPaper, btnCollected, lblFootNote
            });
        }

        private void PrintPaperTicketCopy(Ticket ticket, Transaction tx)
        {
            using var pd = new PrintDocument();
            pd.PaintTicketPage(ticket, tx);
            using var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 800,
                Height = 650,
                StartPosition = FormStartPosition.CenterParent
            };
            preview.ShowDialog(this);
        }

        private void BuildOperatorView()
        {
            _operatorView = new Panel
            {
                Location = new Point(28, 16),
                Size = new Size(1240, 810),
                BackColor = Color.Transparent,
                Visible = false
            };

            var lblBread = new Label
            {
                Text = "BACK-OFFICE  /  OPERATIONS",
                Font = MetroGoTheme.FontRegular(7.8f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(4, 2)
            };

            var badgeStaff = new Label
            {
                Text = "🛡  AUTHORIZED STAFF",
                Font = MetroGoTheme.FontRegular(7.5f, FontStyle.Bold),
                ForeColor = MetroGoTheme.SuccessGreen,
                BackColor = MetroGoTheme.SuccessBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(135, 26),
                Location = new Point(1082, 8)
            };

            var lblTitle = new Label
            {
                Text = "Operator console",
                Font = MetroGoTheme.FontRegular(20f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(0, 22)
            };

            var lblSub = new Label
            {
                Text = "Monitor terminals, maintain fares, and review simulated transactions.",
                Font = MetroGoTheme.FontRegular(9.2f),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(4, 62)
            };

            _operatorSubTabStrip = new Panel
            {
                Location = new Point(2, 98),
                Size = new Size(1218, 44),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _operatorSubTabStrip.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var p = new Pen(MetroGoTheme.BorderSubtle);
                g.DrawLine(p, 0, 42, _operatorSubTabStrip.Width, 42);

                string[] tabs = { "📊  Machine monitoring", "🎫  Fare management", "📄  Daily reconciliation" };
                int[] xs = { 12, 210, 400 };
                for (int i = 0; i < tabs.Length; i++)
                {
                    bool act = _operatorTab == i;
                    Color c = act ? MetroGoTheme.PrimaryBlueBright : MetroGoTheme.TextMuted;
                    TextRenderer.DrawText(g, tabs[i], MetroGoTheme.FontRegular(9.2f, FontStyle.Bold),
                        new Rectangle(xs[i], 6, 180, 30), c, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    if (act)
                    {
                        using var ap = new Pen(MetroGoTheme.PrimaryBlueBright, 2.5f);
                        g.DrawLine(ap, xs[i], 41, xs[i] + 165, 41);
                    }
                }
            };
            _operatorSubTabStrip.MouseClick += (s, e) =>
            {
                if (e.X < 200) _operatorTab = 0;
                else if (e.X < 390) _operatorTab = 1;
                else if (e.X < 600) _operatorTab = 2;
                PopulateOperatorSubContent();
                _operatorSubTabStrip.Invalidate();
            };

            _operatorSubContent = new Panel
            {
                Location = new Point(2, 158),
                Size = new Size(1218, 620),
                BackColor = Color.Transparent
            };

            _operatorView.Controls.AddRange(new Control[]
            {
                lblBread, badgeStaff, lblTitle, lblSub, _operatorSubTabStrip, _operatorSubContent
            });

            _bodyContainer.Controls.Add(_operatorView);
        }

        private void PopulateOperatorSubContent()
        {
            _operatorSubContent.Controls.Clear();

            // Synchronize the active TVM terminal with hardware printer status
            string curCode = _tvm.Controller.CurrentTransaction.TerminalId;
            var curTerm = _tvm.Repository.Terminals.FirstOrDefault(t => t.TerminalCode == curCode)
                          ?? _tvm.Repository.Terminals.FirstOrDefault(t => t.TerminalCode == "TD-004");
            if (curTerm != null)
            {
                curTerm.PaperRemaining = _tvm.Printer.PaperCount;
                if (_tvm.Printer.Status == PrinterStatus.Jammed)
                {
                    curTerm.PrinterStatusText = "Jammed";
                    curTerm.IsOnline = false;
                }
                else if (_tvm.Printer.Status == PrinterStatus.PaperOut || _tvm.Printer.PaperCount <= 0)
                {
                    curTerm.PrinterStatusText = "Paper out";
                    curTerm.IsOnline = false;
                }
                else if (_tvm.Printer.Status == PrinterStatus.PaperLow || _tvm.Printer.PaperCount < 50)
                {
                    curTerm.PrinterStatusText = "Paper low";
                    curTerm.IsOnline = true;
                }
                else
                {
                    curTerm.PrinterStatusText = "Ready";
                    curTerm.IsOnline = true;
                }
            }

            if (_operatorTab == 0)
            {
                int col = 0, row = 0;
                foreach (var term in _tvm.Repository.Terminals.Take(4))
                {
                    int x = col * 614;
                    int y = row * 265;
                    var card = CreateTerminalMonitorCard(term, new Rectangle(x, y, 594, 245));
                    _operatorSubContent.Controls.Add(card);
                    col++;
                    if (col > 1) { col = 0; row++; }
                }
            }
            else if (_operatorTab == 1)
            {
                var card = new RoundedCardPanel
                {
                    Location = new Point(0, 0),
                    Size = new Size(1210, 560),
                    CornerRadius = 10,
                    FillColor = Color.White,
                    BorderColor = MetroGoTheme.BorderSubtle
                };

                var lblInfo = new Label
                {
                    Text = "Central Fare Table (UC11) — Edit fares in whole VND (rounded to 1,000 VND) and distribute to online TVMs:",
                    Font = MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                    ForeColor = MetroGoTheme.TextDark,
                    AutoSize = true,
                    Location = new Point(22, 18)
                };

                var grid = new DataGridView
                {
                    Location = new Point(22, 52),
                    Size = new Size(1165, 435),
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    AutoGenerateColumns = false,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    Font = MetroGoTheme.FontRegular(9f)
                };
                grid.ColumnHeadersHeight = 34;
                grid.RowTemplate.Height = 30;
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Origin Station", DataPropertyName = "Origin", Width = 220 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Destination", DataPropertyName = "Destination", Width = 240 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Line / Mode", DataPropertyName = "LineName", Width = 170 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Standard Fare (VND)", DataPropertyName = "AmountVnd", Width = 160 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "District / Route Info", DataPropertyName = "DistrictInfo", Width = 350 });
                grid.DataSource = _tvm.Repository.CatalogFares.ToList();

                var btnPublish = new MetroButton
                {
                    Text = "Publish Fare Table to 500 TVMs (UC11)",
                    Location = new Point(22, 502),
                    Size = new Size(280, 38)
                };
                btnPublish.Click += (s, e) =>
                {
                    MessageBox.Show("Fare table validated (whole VND) and distributed to all online TVMs within 5 minutes.",
                        "Fare Broadcast Complete (FR20)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                card.Controls.AddRange(new Control[] { lblInfo, grid, btnPublish });
                _operatorSubContent.Controls.Add(card);
            }
            else
            {
                var card = new RoundedCardPanel
                {
                    Location = new Point(0, 0),
                    Size = new Size(1210, 560),
                    CornerRadius = 10,
                    FillColor = Color.White,
                    BorderColor = MetroGoTheme.BorderSubtle
                };

                string dbStatusLabel = _tvm.Repository.IsSqlConnected
                    ? @"MSSQL (localdb)\MSSQLLocalDB"
                    : "In-Memory Buffer";

                var lblInfo = new Label
                {
                    Text = $"Daily Settlement Reconciliation (UC12) — Total records: {_tvm.Repository.ReconciliationLogs.Count}  |  Database: {dbStatusLabel}",
                    Font = MetroGoTheme.FontRegular(9.5f, FontStyle.Bold),
                    ForeColor = MetroGoTheme.TextDark,
                    AutoSize = true,
                    Location = new Point(22, 18)
                };

                var grid = new DataGridView
                {
                    Location = new Point(22, 52),
                    Size = new Size(1165, 490),
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    AutoGenerateColumns = false,
                    AllowUserToAddRows = false,
                    ReadOnly = true,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    Font = MetroGoTheme.FontRegular(8.8f)
                };
                grid.ColumnHeadersHeight = 34;
                grid.RowTemplate.Height = 28;
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tx ID", DataPropertyName = "TransactionCode", Width = 105 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Terminal", DataPropertyName = "TerminalCode", Width = 95 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Route", DataPropertyName = "Route", Width = 220 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Channel", DataPropertyName = "PaymentChannel", Width = 130 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TVM VND", DataPropertyName = "TvmAmountVnd", Width = 90 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Settlement", DataPropertyName = "SettlementStatus", Width = 130 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reconciliation / Refund Note (UC08/UC12)", DataPropertyName = "ReconciliationNote", Width = 370 });
                grid.DataSource = _tvm.Repository.ReconciliationLogs.ToList();

                card.Controls.AddRange(new Control[] { lblInfo, grid });
                _operatorSubContent.Controls.Add(card);
            }
        }

        private RoundedCardPanel CreateTerminalMonitorCard(TerminalNodeInfo term, Rectangle bounds)
        {
            var card = new RoundedCardPanel
            {
                Bounds = bounds,
                CornerRadius = 10,
                FillColor = Color.White,
                BorderColor = MetroGoTheme.BorderSubtle
            };

            var iconBox = new RoundedCardPanel
            {
                Location = new Point(24, 24),
                Size = new Size(42, 42),
                CornerRadius = 8,
                FillColor = MetroGoTheme.SelectedCardBg,
                BorderColor = Color.Transparent
            };
            var lblIcon = new Label
            {
                Text = term.Mode == "BUS" ? "🚌" : "🚆",
                Font = MetroGoTheme.FontRegular(13f),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            iconBox.Controls.Add(lblIcon);

            var lblName = new Label
            {
                Text = term.StationName,
                Font = MetroGoTheme.FontRegular(11f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextDark,
                AutoSize = true,
                Location = new Point(78, 24)
            };
            var lblCode = new Label
            {
                Text = term.TerminalCode,
                Font = MetroGoTheme.FontRegular(8f, FontStyle.Bold),
                ForeColor = MetroGoTheme.TextMuted,
                AutoSize = true,
                Location = new Point(78, 48)
            };

            bool isJammed = term.PrinterStatusText.Equals("Jammed", StringComparison.OrdinalIgnoreCase);
            bool isPaperOut = term.PrinterStatusText.Equals("Paper out", StringComparison.OrdinalIgnoreCase);
            bool isPaperLow = term.PrinterStatusText.Equals("Paper low", StringComparison.OrdinalIgnoreCase);
            bool isFault = isJammed || isPaperOut || !term.IsOnline;

            string badgeText;
            Color badgeFg;
            Color badgeBg;

            if (isJammed)
            {
                badgeText = "FAULT / JAM";
                badgeFg = MetroGoTheme.DangerRed;
                badgeBg = Color.FromArgb(254, 226, 226);
            }
            else if (isPaperOut)
            {
                badgeText = "PAPER OUT";
                badgeFg = MetroGoTheme.DangerRed;
                badgeBg = Color.FromArgb(254, 226, 226);
            }
            else if (!term.IsOnline)
            {
                badgeText = "MAINTENANCE";
                badgeFg = MetroGoTheme.DangerRed;
                badgeBg = Color.FromArgb(254, 226, 226);
            }
            else
            {
                badgeText = "ONLINE";
                badgeFg = MetroGoTheme.SuccessGreen;
                badgeBg = MetroGoTheme.SuccessBg;
            }

            var badgeOnline = new Label
            {
                Text = badgeText,
                Font = MetroGoTheme.FontRegular(7.2f, FontStyle.Bold),
                ForeColor = badgeFg,
                BackColor = badgeBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(92, 24),
                Location = new Point(card.Width - 118, 28)
            };

            var lblPrnCap = new Label { Text = "Printer", Font = MetroGoTheme.FontRegular(8f), ForeColor = MetroGoTheme.TextMuted, AutoSize = true, Location = new Point(24, 98) };

            string prnDisplay = isJammed ? "▲  Jammed" : (isPaperOut ? "▲  Paper out" : (isPaperLow ? "▲  Paper low" : "●  Ready"));
            Color prnColor = (isJammed || isPaperOut) ? MetroGoTheme.DangerRed : (isPaperLow ? Color.FromArgb(217, 119, 6) : MetroGoTheme.SuccessGreen);
            var lblPrnVal = new Label { Text = prnDisplay, Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold), ForeColor = prnColor, AutoSize = true, Location = new Point(24, 120) };

            var lblPapCap = new Label { Text = "Paper remaining", Font = MetroGoTheme.FontRegular(8f), ForeColor = MetroGoTheme.TextMuted, AutoSize = true, Location = new Point(205, 98) };
            var lblPapVal = new Label { Text = $"{term.PaperRemaining} tickets", Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold), ForeColor = MetroGoTheme.TextDark, AutoSize = true, Location = new Point(205, 120) };

            var lblConCap = new Label { Text = "Connection", Font = MetroGoTheme.FontRegular(8f), ForeColor = MetroGoTheme.TextMuted, AutoSize = true, Location = new Point(400, 98) };
            string conDisplay = isFault ? "▲  Degraded / Fault" : "●  Connected";
            Color conColor = isFault ? MetroGoTheme.DangerRed : MetroGoTheme.SuccessGreen;
            var lblConVal = new Label { Text = conDisplay, Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold), ForeColor = conColor, AutoSize = true, Location = new Point(400, 120) };

            var btnRefill = CreateActionOutlineButton("Refill paper", new Point(24, 174), 108, () =>
            {
                term.PaperRemaining = 250;
                term.PrinterStatusText = "Ready";
                term.IsOnline = true;
                if (term.TerminalCode == _tvm.Controller.CurrentTransaction.TerminalId || term.TerminalCode == "TD-004")
                {
                    _tvm.Printer.PaperCount = 250;
                    _tvm.Printer.Status = PrinterStatus.Ready;
                    _tvm.Status = TVMStatus.Active;
                    _tvm.Controller.ResetSystem();
                }
                PopulateOperatorSubContent();
                RefreshRightSidebar();
                MessageBox.Show($"Paper refilled to 250 tickets on {term.TerminalCode}. Machine status Ready.", "Paper Refill (UC09)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            var btnClearJam = CreateActionOutlineButton("Clear printer jam", new Point(142, 174), 136, () =>
            {
                term.PrinterStatusText = "Ready";
                term.IsOnline = true;
                if (term.TerminalCode == _tvm.Controller.CurrentTransaction.TerminalId || term.TerminalCode == "TD-004")
                {
                    _tvm.Printer.Status = PrinterStatus.Ready;
                    _tvm.Status = TVMStatus.Active;
                    _tvm.Controller.ResetSystem();
                }
                PopulateOperatorSubContent();
                RefreshRightSidebar();
                MessageBox.Show($"Printer jam cleared on {term.TerminalCode}. Machine restored to active service.", "Clear Jam (UC09)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            var btnMaint = CreateActionOutlineButton("Maintenance", new Point(288, 174), 116, () =>
            {
                term.IsOnline = !term.IsOnline;
                PopulateOperatorSubContent();
            });

            card.Paint += (s, e) =>
            {
                using var p = new Pen(MetroGoTheme.BorderSubtle);
                e.Graphics.DrawLine(p, 24, 84, card.Width - 24, 84);
                e.Graphics.DrawLine(p, 24, 156, card.Width - 24, 156);
            };

            card.Controls.AddRange(new Control[]
            {
                iconBox, lblName, lblCode, badgeOnline,
                lblPrnCap, lblPrnVal, lblPapCap, lblPapVal, lblConCap, lblConVal,
                btnRefill, btnClearJam, btnMaint
            });
            return card;
        }

        private MetroButton CreateActionOutlineButton(string text, Point loc, int width, Action onClick)
        {
            var btn = new MetroButton
            {
                Text = text,
                Location = loc,
                Size = new Size(width, 40),
                CornerRadius = 6,
                FillColor = Color.White,
                HoverFillColor = Color.FromArgb(241, 245, 249),
                BorderColor = MetroGoTheme.BorderSubtle,
                TextColor = MetroGoTheme.TextDark,
                Font = MetroGoTheme.FontRegular(8.8f, FontStyle.Bold)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void RefreshFullUi()
        {
            _navTabStrip.Invalidate();
            _kioskView.Visible = !_isOperatorConsole;
            _operatorView.Visible = _isOperatorConsole;

            if (_isOperatorConsole)
            {
                PopulateOperatorSubContent();
                return;
            }

            _lblKioskMainTitle.Text = _currentStep == 4 ? "Your ticket is ready." : "Purchase a ticket";
            _unfinishedBanner.Visible = _showUnfinishedBanner && (_currentStep == 1 || _currentStep == 2);

            int topOffset = _unfinishedBanner.Visible ? 208 : 156;
            _leftMainCard.Top = topOffset;
            _rightSummaryCard.Top = topOffset;
            _rightHealthCard.Top = topOffset + 377;
            _rightActivityCard.Top = topOffset + 512;
            _rightActivityCard.Visible = _currentStep >= 3;

            _stepIndicatorBar.Invalidate();

            if (_currentStep == 1) PopulateLeftCardStep1();
            else if (_currentStep == 2) PopulateLeftCardStep2();
            else if (_currentStep == 3) PopulateLeftCardStep3();
            else if (_currentStep == 4) PopulateLeftCardStep4();

            RefreshRightSidebar();
        }

        private void RefreshRightSidebar()
        {
            var tx = _tvm.Controller.CurrentTransaction;
            _lblSummaryOrigin.Text = tx.Fare.Origin;
            _lblSummaryDest.Text = tx.Fare.TicketType == TicketType.OneDayPass ? "All Metro & Bus Stops" : tx.Fare.Destination;
            string typeLabel = tx.Fare.TicketType == TicketType.OneDayPass ? "1-day pass" : "Single journey";
            _lblSummaryMeta.Text = $"🎫  {typeLabel} ({tx.Fare.Category})                   {tx.Quantity} passenger{(tx.Quantity > 1 ? "s" : "")}";
            _lblSummaryTotal.Text = $"{tx.TotalAmountVnd:N0} VND";

            if (_currentStep == 1)
            {
                _btnPrimaryAction.Text = "Choose payment method   →";
                _btnPrimaryAction.FillColor = MetroGoTheme.PrimaryBlue;
                _lblCancelAction.Visible = true;
                _lblCancelAction.Text = "Cancel purchase";
                _lblFooterState.Text = "●  Ready";
            }
            else if (_currentStep == 2)
            {
                _btnPrimaryAction.Text = "Proceed to payment   →";
                _btnPrimaryAction.FillColor = MetroGoTheme.PrimaryBlue;
                _lblCancelAction.Visible = true;
                _lblCancelAction.Text = "Cancel purchase";
                _lblFooterState.Text = "●  Ready";
            }
            else if (_currentStep == 3)
            {
                string prov = _selectedPaymentMethod == PaymentMethod.DigitalWallet ? _selectedWallet.ToString() : _selectedPaymentMethod.ToString();
                _btnPrimaryAction.Text = $"🕒  Waiting for {prov} payment";
                _btnPrimaryAction.FillColor = MetroGoTheme.SelectedCardBg;
                _btnPrimaryAction.TextColor = MetroGoTheme.PrimaryBlueBright;
                _lblCancelAction.Visible = true;
                _lblCancelAction.Text = "Cancel payment";
                _lblFooterState.Text = "●  Awaiting payment";
            }
            else if (_currentStep == 4)
            {
                _btnPrimaryAction.Text = "✓   Ticket Issued";
                _btnPrimaryAction.FillColor = MetroGoTheme.SuccessBg;
                _btnPrimaryAction.TextColor = MetroGoTheme.SuccessGreen;
                _lblCancelAction.Visible = false;
                _lblFooterState.Text = "●  Ticket issued";
            }

            if (_currentStep <= 2)
                _btnPrimaryAction.TextColor = Color.White;

            _btnPrimaryAction.Invalidate();

            bool prnOk = _tvm.Printer.Status == PrinterStatus.Ready || _tvm.Printer.Status == PrinterStatus.PaperLow;
            _lblHealthPrinter.Text = prnOk ? $"●  {_tvm.Printer.Status}" : $"▲  {_tvm.Printer.Status}";
            _lblHealthPrinter.ForeColor = prnOk ? MetroGoTheme.SuccessGreen : MetroGoTheme.DangerRed;
            _lblHealthPaper.Text = $"{_tvm.Printer.PaperCount} tickets";
            _lblActivityTx.Text = $"▸  Transaction activity                                     {tx.ShortCode}";
            _lblFooterTerminalId.Text = tx.TerminalId;

            if (_tvm.Printer.Status == PrinterStatus.Jammed)
            {
                _lblTopConnectedStatus.Text = "▲   Printer Jammed (UC08)";
                _lblTopConnectedStatus.ForeColor = MetroGoTheme.DangerRed;
                _lblFooterState.Text = "▲  Out of service (Printer Jammed)";
                _lblFooterState.ForeColor = MetroGoTheme.DangerRed;
            }
            else if (_tvm.Printer.Status == PrinterStatus.PaperOut || _tvm.Printer.PaperCount <= 0)
            {
                _lblTopConnectedStatus.Text = "▲   Paper Out (UC08)";
                _lblTopConnectedStatus.ForeColor = MetroGoTheme.DangerRed;
                _lblFooterState.Text = "▲  Out of service (Paper Out)";
                _lblFooterState.ForeColor = MetroGoTheme.DangerRed;
            }
            else
            {
                _lblTopConnectedStatus.Text = "●   Terminal connected";
                _lblTopConnectedStatus.ForeColor = MetroGoTheme.SuccessGreen;
            }
        }

        private void ResetToNewTicket()
        {
            _tvm.StartSale();
            _currentStep = 1;
            _idleSeconds = 90;
            RefreshFullUi();
        }

        private void NavigateBack()
        {
            if (_isOperatorConsole)
            {
                _isOperatorConsole = false;
                RefreshFullUi();
                return;
            }
            if (_currentStep > 1 && _currentStep < 4)
            {
                _currentStep--;
                RefreshFullUi();
            }
        }

        private void ShowQuickHelp()
        {
            string helpMsg = string.Join(Environment.NewLine + Environment.NewLine, new[]
            {
                "MetroGo Smart Ticketing System (TVM Prototype)",
                "• Step 1: Select Single Journey or 1-Day Pass, choose your destination, passenger category (Adult/Student/Senior), and quantity (1-5).",
                "• Step 2: Choose Credit Card, Bank QR (VietQR), or Digital Wallet (MoMo, ZaloPay, VNPay).",
                "• Step 3: Click 'Simulate payment confirmation' to print a Code 128 barcode ticket, or test 'Simulate Decline' / 'Simulate Print Jam (UC08)'.",
                "• Operator Console: Switch to the top 'Operator console' tab to monitor terminals, refill paper, edit fares (UC11), or inspect MSSQL daily settlement reconciliation (UC12)."
            });
            MessageBox.Show(helpMsg, "MetroGo Terminal Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public static class TicketPrintExtensions
    {
        public static void PaintTicketPage(this PrintDocument pd, Ticket ticket, Transaction tx)
        {
            pd.PrintPage += (s, e) =>
            {
                if (e.Graphics == null) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var box = new Rectangle(60, 60, 360, 375);
                MetroGoTheme.DrawRoundedBox(g, box, 10, Color.White, MetroGoTheme.BorderSubtle, 1.5f);
                g.DrawString("METROGO SMART TICKET", MetroGoTheme.FontRegular(12f, FontStyle.Bold), Brushes.Black, 85, 82);

                string route = $"{ticket.Origin}  ➔  {ticket.Destination}";
                var routeFont = route.Length > 28 ? MetroGoTheme.FontRegular(9.5f, FontStyle.Bold) : MetroGoTheme.FontRegular(11f, FontStyle.Bold);
                g.DrawString(route, routeFont, Brushes.Black, new RectangleF(85, 118, 310, 42));

                g.DrawString($"Category: {ticket.Category}   |   Qty: {tx.Quantity}", MetroGoTheme.FontRegular(9.5f), Brushes.DimGray, 85, 165);
                g.DrawString($"Total Paid: {tx.TotalAmountVnd:N0} VND", MetroGoTheme.FontRegular(11f, FontStyle.Bold), Brushes.Black, 85, 195);
                g.DrawString($"Issued: {ticket.IssuedAt:dd/MM/yyyy HH:mm}   Expires: {ticket.ExpiresAt:HH:mm}", MetroGoTheme.FontRegular(8.5f), Brushes.DimGray, 85, 225);
                MetroGoTheme.DrawCode128Barcode(g, new Rectangle(85, 258, 310, 55), ticket.BarcodeCode128);
                g.DrawString(ticket.TicketNumber, MetroGoTheme.FontRegular(9.5f, FontStyle.Bold), Brushes.Black, 85 + (310 - 130) / 2, 326);
            };
        }
    }
}
