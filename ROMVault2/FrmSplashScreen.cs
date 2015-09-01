/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using ROMVault2.Properties;
using ROMVault2.RvDB;

namespace ROMVault2
{
    public partial class FrmSplashScreen : Form
    {
        private double _opacityIncrement = 0.05;

        public FrmSplashScreen()
        {
            InitializeComponent();
            lblVersion.Text = @"Version " + Program.Version + @"." + Program.SubVersion + Resources.FixFiles_FixZip_Colon + Application.StartupPath;
            Opacity = 0;
            timer1.Interval = 50;

            bgWork.DoWork += StartUpCode;
            bgWork.ProgressChanged += BgwProgressChanged;
            bgWork.RunWorkerCompleted += BgwRunWorkerCompleted;
        }

        private void FrmSplashScreenShown(object sender, EventArgs e)
        {
            bgWork.RunWorkerAsync(SynchronizationContext.Current);
            timer1.Start();
        }


        private void StartUpCode(object sender, DoWorkEventArgs e)
        {
            RepairStatus.InitStatusCheck();

            Settings.SetDefaults();

            DB.Read(sender,e);
        }


        private void BgwProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                if (e.ProgressPercentage >= progressBar.Minimum && e.ProgressPercentage <= progressBar.Maximum)
                    progressBar.Value = e.ProgressPercentage;
                return;
            }
            bgwSetRange bgwSr = e.UserState as bgwSetRange;
            if (bgwSr != null)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = bgwSr.MaxVal;
                progressBar.Value = 0;
                return;
            }

            bgwText bgwT = e.UserState as bgwText;
            if (bgwT != null)
            {
                lblStatus.Text = bgwT.Text;
            }
        }

        private void BgwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _opacityIncrement = -0.1;
            timer1.Start();
        }

        private void Timer1Tick(object sender, EventArgs e)
        {

            if (_opacityIncrement > 0)
            {
                if (Opacity < 1)
                    Opacity += _opacityIncrement;
                else
                    timer1.Stop();
            }
            else
            {
                if (Opacity > 0)
                    Opacity += _opacityIncrement;
                else
                {
                    timer1.Stop();
                    Close();
                }
            }
        }

    }
}
