/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ROMVault2.Properties;

namespace ROMVault2
{
    public partial class FrmSettings : Form
    {
        public FrmSettings()
        {
            InitializeComponent();

            cboScanLevel.Items.Clear();
            cboScanLevel.Items.Add("Level1");
            cboScanLevel.Items.Add("Level2");
            cboScanLevel.Items.Add("Level3");

            cboFixLevel.Items.Clear();
            cboFixLevel.Items.Add("TorrentZip Level 1");
            cboFixLevel.Items.Add("TorrentZip Level 2");
            cboFixLevel.Items.Add("TorrentZip Level 3");
            cboFixLevel.Items.Add("Level1");
            cboFixLevel.Items.Add("Level2");
            cboFixLevel.Items.Add("Level3");
        }

        private void FrmConfigLoad(object sender, EventArgs e)
        {
            lblDATRoot.Text = Settings.DATRoot;
            cboScanLevel.SelectedIndex = (int)Settings.ScanLevel;
            cboFixLevel.SelectedIndex = (int)Settings.FixLevel;

            textBox1.Text = "";
            for (int i = 0; i < Settings.IgnoreFiles.Count; i++)
                textBox1.Text += Settings.IgnoreFiles[i] + Environment.NewLine;

            chkDoubleCheckDelete.Checked = Settings.DoubleCheckDelete;
            chkCacheSaveTimer.Checked = Settings.CacheSaveTimerEnabled;
            upTime.Value = Settings.CacheSaveTimePeriod;
            chkDebugLogs.Checked = Settings.DebugLogsEnabled;
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnOkClick(object sender, EventArgs e)
        {
            Settings.DATRoot = lblDATRoot.Text;
            Settings.ScanLevel = (eScanLevel)cboScanLevel.SelectedIndex;
            Settings.FixLevel = (eFixLevel)cboFixLevel.SelectedIndex;

            string strtxt = textBox1.Text;
            strtxt = strtxt.Replace("\r", "");
            string[] strsplit = strtxt.Split('\n');

            Settings.IgnoreFiles = new List<string>(strsplit);
            for (int i = 0; i < Settings.IgnoreFiles.Count; i++)
            {
                Settings.IgnoreFiles[i] = Settings.IgnoreFiles[i].Trim();
                if (string.IsNullOrEmpty(Settings.IgnoreFiles[i]))
                {
                    Settings.IgnoreFiles.RemoveAt(i);
                    i--;
                }
            }

            Settings.DoubleCheckDelete = chkDoubleCheckDelete.Checked;
            Settings.DebugLogsEnabled = chkDebugLogs.Checked;
            Settings.CacheSaveTimerEnabled = chkCacheSaveTimer.Checked;
            Settings.CacheSaveTimePeriod = (int)upTime.Value;

            Settings.WriteConfig();
            Close();
        }

        private void BtnDatClick(object sender, EventArgs e)
        {
            FolderBrowserDialog browse = new FolderBrowserDialog
                                             {
                                                 ShowNewFolderButton = true,
                                                 Description = Resources.FrmSettings_BtnDatClick_Please_select_a_folder_for_DAT_Root,
                                                 RootFolder = (Settings.IsMono ? Environment.SpecialFolder.MyComputer : Environment.SpecialFolder.DesktopDirectory),
                                                 SelectedPath = Settings.DATRoot
                                             };

            if (browse.ShowDialog() != DialogResult.OK) return;

            lblDATRoot.Text = Utils.RelativePath.MakeRelative(AppDomain.CurrentDomain.BaseDirectory, browse.SelectedPath);
        }

    }
}
