using System;
using System.Windows.Forms;
using TvmTicketing.UI;

namespace TvmTicketing
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
