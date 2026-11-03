using System;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        bool isFirstInstance;
        using (var mutex = new Mutex(true, @"Local\TaskBarSpeedmon.SingleInstance", out isFirstInstance))
        {
            if (!isFirstInstance) return;   // Already running

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SpeedmonApp());
        }
    }
}
