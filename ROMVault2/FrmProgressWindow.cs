﻿/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ROMVault2.Properties;

namespace ROMVault2
{
    public partial class FrmProgressWindow : Form
    {
        private readonly string _titleRoot;
        private bool _errorOpen;
        private bool _bDone;
        private Form _parentForm;

        public FrmProgressWindow(Form parentForm, string titleRoot, DoWorkEventHandler function)
        {
            _parentForm = parentForm;
            _titleRoot = titleRoot;
            InitializeComponent();

            ClientSize = new Size(511, 131);

            _titleRoot = titleRoot;

            bgWork.DoWork += function;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CP_NOCLOSE_BUTTON = 0x200;
                CreateParams mdiCp = base.CreateParams;
                mdiCp.ClassStyle = mdiCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return mdiCp;
            }
        }



        private void FrmProgressWindowNewShown(object sender, EventArgs e)
        {
            bgWork.ProgressChanged += BgwProgressChanged;
            bgWork.RunWorkerCompleted += BgwRunWorkerCompleted;
            bgWork.RunWorkerAsync(SynchronizationContext.Current);
        }

        private void BgwProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                if (e.ProgressPercentage >= progressBar.Minimum && e.ProgressPercentage <= progressBar.Maximum)
                    progressBar.Value = e.ProgressPercentage;
                UpdateStatusText();
                return;
            }

            bgwText bgwT = e.UserState as bgwText;
            if (bgwT != null)
            {
                label.Text = bgwT.Text;
                return;
            }
            bgwSetRange bgwSR = e.UserState as bgwSetRange;
            if (bgwSR != null)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = bgwSR.MaxVal >= 0 ? bgwSR.MaxVal : 0;
                progressBar.Value = 0;
                UpdateStatusText();
                return;
            }


            bgwText2 bgwT2 = e.UserState as bgwText2;
            if (bgwT2 != null)
            {
                label2.Text = bgwT2.Text;
                return;
            }

            bgwValue2 bgwV2 = e.UserState as bgwValue2;
            if (bgwV2 != null)
            {
                if (bgwV2.Value >= progressBar2.Minimum && bgwV2.Value <= progressBar2.Maximum)
                    progressBar2.Value = bgwV2.Value;
                UpdateStatusText2();
                return;
            }

            bgwSetRange2 bgwSR2 = e.UserState as bgwSetRange2;
            if (bgwSR2 != null)
            {
                progressBar2.Minimum = 0;
                progressBar2.Maximum = bgwSR2.MaxVal >= 0 ? bgwSR2.MaxVal : 0;
                progressBar2.Value = 0;
                UpdateStatusText2();
                return;
            }
            bgwRange2Visible bgwR2V = e.UserState as bgwRange2Visible;
            if (bgwR2V != null)
            {
                label2.Visible = bgwR2V.Visible;
                progressBar2.Visible = bgwR2V.Visible;
                lbl2Prog.Visible = bgwR2V.Visible;
                return;
            }


            bgwText3 bgwT3 = e.UserState as bgwText3;
            if (bgwT3 != null)
            {
                label3.Text = bgwT3.Text;
                return;
            }

            bgwShowCorrupt bgwSC = e.UserState as bgwShowCorrupt;
            if (bgwSC != null)
            {
                if (!_errorOpen)
                {
                    _errorOpen = true;
                    ClientSize = new Size(511, 292);
                    MinimumSize = new Size(511, 292);
                    FormBorderStyle = FormBorderStyle.SizableToolWindow;
                }

                ErrorGrid.Rows.Add();
                int row = ErrorGrid.Rows.Count - 1;

                ErrorGrid.Rows[row].Cells["CError"].Value = bgwSC.zr;
                ErrorGrid.Rows[row].Cells["CError"].Style.ForeColor = Color.FromArgb(255, 0, 0);

                ErrorGrid.Rows[row].Cells["CErrorFile"].Value = bgwSC.filename;
                ErrorGrid.Rows[row].Cells["CErrorFile"].Style.ForeColor = Color.FromArgb(255, 0, 0);

                if (row >= 0) ErrorGrid.FirstDisplayedScrollingRowIndex = row;
            }



            bgwShowError bgwSDE = e.UserState as bgwShowError;
            if (bgwSDE != null)
            {
                if (!_errorOpen)
                {
                    _errorOpen = true;
                    ClientSize = new Size(511, 292);
                    MinimumSize = new Size(511, 292);
                    FormBorderStyle = FormBorderStyle.SizableToolWindow;
                }

                ErrorGrid.Rows.Add();
                int row = ErrorGrid.Rows.Count - 1;

                ErrorGrid.Rows[row].Cells["CError"].Value = bgwSDE.error;
                ErrorGrid.Rows[row].Cells["CError"].Style.ForeColor = Color.FromArgb(255, 0, 0);

                ErrorGrid.Rows[row].Cells["CErrorFile"].Value = bgwSDE.filename;
                ErrorGrid.Rows[row].Cells["CErrorFile"].Style.ForeColor = Color.FromArgb(255, 0, 0);

                if (row >= 0) ErrorGrid.FirstDisplayedScrollingRowIndex = row;

            }



        }
        private void UpdateStatusText()
        {
            int range = progressBar.Maximum - progressBar.Minimum;
            int percent = range > 0 ? (progressBar.Value * 100) / range : 0;

            Text = _titleRoot + String.Format(" - {0}% complete", percent);
        }
        private void UpdateStatusText2()
        {
            lbl2Prog.Text = progressBar2.Maximum > 0 ? string.Format("{0}/{1}", progressBar2.Value, progressBar2.Maximum) : "";
        }

        private void BgwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_errorOpen)
            {
                cancelButton.Text = Resources.ProgressWindowFix_DoDone_Close;
                cancelButton.Enabled = true;
                _bDone = true;
            }
            else
            {
                _parentForm.Show();
                Close();
            }
        }

        private void CancelButtonClick(object sender, EventArgs e)
        {
            if (_bDone)
            {
                if (!_parentForm.Visible) _parentForm.Show();
                Close();
            }
            else
            {
                cancelButton.Text = Resources.ProgressWindowFix_OnClosing_Cancelling;
                cancelButton.Enabled = false;
                bgWork.CancelAsync();
            }
        }

        private void ErrorGridSelectionChanged(object sender, EventArgs e)
        {
            ErrorGrid.ClearSelection();
        }

        private void FrmProgressWindow_Resize(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case FormWindowState.Minimized:
                    if (_parentForm.Visible) _parentForm.Hide();
                    return;
                case FormWindowState.Maximized:
                    if (!_parentForm.Visible) _parentForm.Show();
                    return;
                case FormWindowState.Normal:
                    if (!_parentForm.Visible) _parentForm.Show();
                    return;
            }
        }

    }
}
