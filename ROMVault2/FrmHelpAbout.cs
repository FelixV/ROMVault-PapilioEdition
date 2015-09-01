/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Windows.Forms;

namespace ROMVault2
{
    public partial class FrmHelpAbout : Form
    {
        public FrmHelpAbout()
        {

            InitializeComponent();
            Text = "Version "+Program.Version+"." + Program.SubVersion + " : " + Application.StartupPath;
            lblVersion.Text = "Version "+Program.Version+"." + Program.SubVersion;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            string url = "http://www.ROMVault.com/";
            System.Diagnostics.Process.Start(url);
        }

        private void label2_Click(object sender, EventArgs e)
        {
            try
            {
                string url = "mailto:support@ROMVault.com?subject=Support " + Program.Version + "." + Program.SubVersion;
                System.Diagnostics.Process.Start(url);
            }
            catch
            { }
        }

    }
}
