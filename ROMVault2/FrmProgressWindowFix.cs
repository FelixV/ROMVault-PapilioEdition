/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ROMVault2.Properties;

namespace ROMVault2
{
    public partial class FrmProgressWindowFix : Form
    {

        private bool _bDone;
        private readonly Form _parentForm;

        private readonly Queue<DataGridViewRow> _rowQueue;

        public FrmProgressWindowFix(Form parentForm)
        {
            _rowQueue = new Queue<DataGridViewRow>();
            _parentForm = parentForm;
            InitializeComponent();
            timer1.Interval = 100;
            timer1.Enabled = true;
        }


        private void Timer1Tick(object sender, EventArgs e)
        {
            int rowCount = _rowQueue.Count;
            if (rowCount == 0)
                return;

            DataGridViewRow[] dgvr = new DataGridViewRow[rowCount];
            for (int i = 0; i < rowCount; i++)
                dgvr[i] = _rowQueue.Dequeue();

            dataGridView1.Rows.AddRange(dgvr);
            int iRow = dataGridView1.Rows.Count - 1;
            dataGridView1.FirstDisplayedScrollingRowIndex = iRow;
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



        private void FrmProgressWindowFixShown(object sender, EventArgs e)
        {
            bgWork.DoWork += FixFiles.PerformFixes;
            bgWork.ProgressChanged += BgwProgressChanged;
            bgWork.RunWorkerCompleted += BgwRunWorkerCompleted;
            bgWork.RunWorkerAsync(SynchronizationContext.Current);
        }

        private void BgwProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BgwProgressChanged(e.UserState);
        }

        private void BgwProgressChanged(object e)
        {


            bgwShowFix bgwSf = e as bgwShowFix;
            if (bgwSf != null)
            {
                DataGridViewRow dgrq = (DataGridViewRow)dataGridView1.RowTemplate.Clone();
                dgrq.CreateCells(dataGridView1, bgwSf.FixDir, bgwSf.FixZip, bgwSf.FixFile, bgwSf.Size, bgwSf.Dir, bgwSf.SourceDir, bgwSf.SourceZip, bgwSf.SourceFile);
                _rowQueue.Enqueue(dgrq);
                return;

            }

            bgwShowFixError bgwSFE = e as bgwShowFixError;
            if (bgwSFE != null)
            {
                int iRow = dataGridView1.Rows.Count - 1;
                dataGridView1.Rows[iRow].Cells[4].Style.BackColor = Color.Red;
                dataGridView1.Rows[iRow].Cells[4].Style.ForeColor = Color.Black;
                dataGridView1.Rows[iRow].Cells[4].Value = bgwSFE.FixError;
                return;
            }

            bgwProgress bgwProg = e as bgwProgress;
            if (bgwProg != null)
            {
                if (bgwProg.Progress >= progressBar.Minimum && bgwProg.Progress <= progressBar.Maximum)
                    progressBar.Value = bgwProg.Progress;
                UpdateStatusText();
                return;
            }

            bgwText bgwT = e as bgwText;
            if (bgwT != null)
            {
                label.Text = bgwT.Text;
                return;
            }
            bgwSetRange bgwSR = e as bgwSetRange;
            if (bgwSR != null)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = bgwSR.MaxVal >= 0 ? bgwSR.MaxVal : 0;
                progressBar.Value = 0;
                UpdateStatusText();
            }


        }

        private void BgwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            cancelButton.Text = Resources.ProgressWindowFix_DoDone_Close;
            cancelButton.Enabled = true;
            _bDone = true;
        }

        private void UpdateStatusText()
        {
            int range = progressBar.Maximum - progressBar.Minimum;
            int percent = range > 0 ? (progressBar.Value * 100) / range : 0;

            Text = String.Format("Fixing Files - {0}% complete", percent);
        }

        private void CancelButtonClick(object sender, EventArgs e)
        {
            if (_bDone)
                Close();
            else
            {
                cancelButton.Enabled = false;
                cancelButton.Text = Resources.ProgressWindowFix_OnClosing_Cancelling;
                bgWork.CancelAsync();
            }
        }

        private void DataGridView1SelectionChanged(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
        }

        private void FrmProgressWindowFixResize(object sender, EventArgs e)
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
