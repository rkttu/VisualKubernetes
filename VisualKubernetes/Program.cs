using System;
using System.Windows.Forms;

namespace VisualKubernetes
{
    internal static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.OleRequired();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var mainWindow = new MainForm();
            using var appContext = new ApplicationContext(mainWindow);
            Application.Run(appContext);
        }
    }
}
