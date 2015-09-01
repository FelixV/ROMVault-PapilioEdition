/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/
using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ROMVault2
{
    static class Program
    {
        //public static UsernamePassword Up;
        public static readonly Encoding Enc = Encoding.GetEncoding(28591);
        public const string Version = "2.2";
        public const int SubVersion = 1;

        public static string ErrorMessage;
        public static string URL;

        public static SynchronizationContext SyncCont;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#if !DEBUG
            Application.ThreadException += ReportError.UnhandledExceptionHandler;
#endif

            FrmSplashScreen progress = new FrmSplashScreen();
            progress.ShowDialog();

            progress.Dispose();

            if (!String.IsNullOrEmpty(ErrorMessage))
            {
                MessageBox.Show(ErrorMessage);
                if (!String.IsNullOrEmpty(URL))
                    System.Diagnostics.Process.Start(URL);
                return;
            }

            Application.Run(new FrmMain());
        }
    }
}
