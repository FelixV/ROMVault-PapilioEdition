/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Windows.Forms;

namespace ROMVault2
{
    public partial class FrmShowError : Form
    {
        public FrmShowError()
        {
            InitializeComponent();
        }

        public void settype(string s)
        {
            textBox1.Text = s;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
