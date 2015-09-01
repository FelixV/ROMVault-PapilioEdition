/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Drawing;
using System.Windows.Forms;
using ROMVault2.Properties;
using ROMVault2.Utils;

namespace ROMVault2
{
    public partial class FrmSetDir : Form
    {
        private string _datLocation;

        private readonly Color _cMagenta = Color.FromArgb(255, 214, 255);
        private readonly Color _cGreen = Color.FromArgb(214, 255, 214);
        private readonly Color _cYellow = Color.FromArgb(255, 255, 214);

        public FrmSetDir()
        {
            InitializeComponent();
        }

        public void SetLocation(string dLocation)
        {
            _datLocation = dLocation;
            txtDATLocation.Text = _datLocation;
            txtROMLocation.Text = DBHelper.GetRealPath(_datLocation);
            UpdateGrid();
        }

        private void UpdateGrid()
        {
            if (Settings.IsMono && DataGridGames.RowCount > 0)
                DataGridGames.CurrentCell = DataGridGames[0,0];

            DataGridGames.Rows.Clear();
            foreach (DirMap t in Settings.DirPathMap)
            {
                DataGridGames.Rows.Add();
                int row = DataGridGames.Rows.Count - 1;
                string key = t.DirKey;
                DataGridGames.Rows[row].Cells["CDAT"].Value = t.DirKey;
                DataGridGames.Rows[row].Cells["CROM"].Value = t.DirPath;

                if (key == "Unsorted")
                {
                    DataGridGames.Rows[row].Cells["CDAT"].Style.BackColor = _cMagenta;
                    DataGridGames.Rows[row].Cells["CROM"].Style.BackColor = _cMagenta;
                }
                else if (key == _datLocation)
                {
                    DataGridGames.Rows[row].Cells["CDAT"].Style.BackColor = _cGreen;
                    DataGridGames.Rows[row].Cells["CROM"].Style.BackColor = _cGreen;
                }
                else if (key.Length >= _datLocation.Length)
                {
                    if (key.Substring(0, _datLocation.Length) == _datLocation)
                    {
                        DataGridGames.Rows[row].Cells["CDAT"].Style.BackColor = _cYellow;
                        DataGridGames.Rows[row].Cells["CROM"].Style.BackColor = _cYellow;
                    }
                }
            }

            for (int j = 0; j < DataGridGames.Rows.Count; j++)
            {
                DataGridGames.Rows[j].Selected = false;
            }
        }


        private void BtnSetRomLocationClick(object sender, EventArgs e)
        {
            FolderBrowserDialog browse = new FolderBrowserDialog
                                             {
                                                 ShowNewFolderButton = true,
                                                 Description = Resources.FrmSetDir_BtnSetRomLocationClick_Please_select_a_folder_for_This_Rom_Set,
                                                 RootFolder = (Settings.IsMono ? Environment.SpecialFolder.MyComputer : Environment.SpecialFolder.DesktopDirectory),
                                                 SelectedPath = DBHelper.GetRealPath(_datLocation)
                                             };
            if (browse.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < Settings.DirPathMap.Count; i++)
                {
                    if (Settings.DirPathMap[i].DirKey == _datLocation)
                    {
                        Settings.DirPathMap.RemoveAt(i);
                        i--;
                    }
                }


                string relPath = RelativePath.MakeRelative(AppDomain.CurrentDomain.BaseDirectory, browse.SelectedPath);

                Settings.DirPathMap.Add(new DirMap(_datLocation, relPath));
                Settings.DirPathMap.Sort();
                SetLocation(_datLocation);
                Settings.WriteConfig();

                //db.CheckNamesToLong();
            }
        }

        private void BtnDeleteSelectedClick(object sender, EventArgs e)
        {
            for (int j = 0; j < DataGridGames.SelectedRows.Count; j++)
            {
                string datLocation = DataGridGames.SelectedRows[j].Cells["CDAT"].Value.ToString();

                if (datLocation == "Unsorted" || datLocation == "ROMVault")
                {
                    ReportError.Show(Resources.FrmSetDir_BtnDeleteSelectedClick_You_cannot_delete_the + datLocation + Resources.FrmSetDir_BtnDeleteSelectedClick_Directory_Settings,
                                    Resources.FrmSetDir_BtnDeleteSelectedClick_ROMVault_Rom_Location);
                }
                else
                {
                    for (int i = 0; i < Settings.DirPathMap.Count; i++)
                    {
                        if (Settings.DirPathMap[i].DirKey == datLocation)
                        {
                            Settings.DirPathMap.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            Settings.WriteConfig();
            UpdateGrid();
            //db.CheckNamesToLong();
        }

        private void BtnCloseClick(object sender, EventArgs e)
        {
            //db.CheckNamesToLong();
            Close();
        }

        private void DataGridGamesDoubleClick(object sender, EventArgs e)
        {
            if (DataGridGames.SelectedRows.Count <= 0) return;

            grpBoxAddNew.Text = Resources.FrmSetDir_DataGridGamesDoubleClick_Edit_Existing_Directory_Mapping;
            SetLocation(DataGridGames.SelectedRows[0].Cells["CDAT"].Value.ToString());
        }

        private void FrmSetDirActivated(object sender, EventArgs e)
        {

            for (int j = 0; j < DataGridGames.Rows.Count; j++)
            {
                DataGridGames.Rows[j].Selected = false;
            }
        }

        private void BtnResetAllClick(object sender, EventArgs e)
        {
            Settings.ResetDirectories();
            Settings.WriteConfig();
            UpdateGrid();
        }



    }
}
