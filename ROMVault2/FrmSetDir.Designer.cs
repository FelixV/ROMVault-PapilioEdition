namespace ROMVault2
{
    partial class FrmSetDir
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSetDir));
            this.DataGridGames = new System.Windows.Forms.DataGridView();
            this.CDAT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CROM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnDeleteSelected = new System.Windows.Forms.Button();
            this.grpBoxAddNew = new System.Windows.Forms.GroupBox();
            this.btnSetRomLocation = new System.Windows.Forms.Button();
            this.txtROMLocation = new System.Windows.Forms.Label();
            this.lblROMLocation = new System.Windows.Forms.Label();
            this.txtDATLocation = new System.Windows.Forms.Label();
            this.lblDATLocation = new System.Windows.Forms.Label();
            this.lblDelete = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnResetAll = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridGames)).BeginInit();
            this.grpBoxAddNew.SuspendLayout();
            this.SuspendLayout();
            // 
            // DataGridGames
            // 
            this.DataGridGames.AllowUserToAddRows = false;
            this.DataGridGames.AllowUserToDeleteRows = false;
            this.DataGridGames.AllowUserToResizeRows = false;
            this.DataGridGames.BackgroundColor = System.Drawing.Color.White;
            this.DataGridGames.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGridGames.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.CDAT,
            this.CROM});
            this.DataGridGames.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.DataGridGames.Location = new System.Drawing.Point(12, 135);
            this.DataGridGames.Name = "DataGridGames";
            this.DataGridGames.ReadOnly = true;
            this.DataGridGames.RowHeadersVisible = false;
            this.DataGridGames.RowTemplate.Height = 17;
            this.DataGridGames.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.DataGridGames.ShowCellErrors = false;
            this.DataGridGames.ShowCellToolTips = false;
            this.DataGridGames.ShowEditingIcon = false;
            this.DataGridGames.ShowRowErrors = false;
            this.DataGridGames.Size = new System.Drawing.Size(670, 214);
            this.DataGridGames.TabIndex = 10;
            this.DataGridGames.DoubleClick += new System.EventHandler(this.DataGridGamesDoubleClick);
            // 
            // CDAT
            // 
            this.CDAT.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.CDAT.HeaderText = "DAT Location";
            this.CDAT.Name = "CDAT";
            this.CDAT.ReadOnly = true;
            // 
            // CROM
            // 
            this.CROM.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.CROM.HeaderText = "ROM Location";
            this.CROM.Name = "CROM";
            this.CROM.ReadOnly = true;
            // 
            // btnDeleteSelected
            // 
            this.btnDeleteSelected.Location = new System.Drawing.Point(12, 355);
            this.btnDeleteSelected.Name = "btnDeleteSelected";
            this.btnDeleteSelected.Size = new System.Drawing.Size(96, 25);
            this.btnDeleteSelected.TabIndex = 11;
            this.btnDeleteSelected.Text = "Delete Selected";
            this.btnDeleteSelected.UseVisualStyleBackColor = true;
            this.btnDeleteSelected.Click += new System.EventHandler(this.BtnDeleteSelectedClick);
            // 
            // grpBoxAddNew
            // 
            this.grpBoxAddNew.Controls.Add(this.btnSetRomLocation);
            this.grpBoxAddNew.Controls.Add(this.txtROMLocation);
            this.grpBoxAddNew.Controls.Add(this.lblROMLocation);
            this.grpBoxAddNew.Controls.Add(this.txtDATLocation);
            this.grpBoxAddNew.Controls.Add(this.lblDATLocation);
            this.grpBoxAddNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpBoxAddNew.Location = new System.Drawing.Point(12, 12);
            this.grpBoxAddNew.Name = "grpBoxAddNew";
            this.grpBoxAddNew.Size = new System.Drawing.Size(670, 91);
            this.grpBoxAddNew.TabIndex = 14;
            this.grpBoxAddNew.TabStop = false;
            this.grpBoxAddNew.Text = "Add New Directory Mapping";
            // 
            // btnSetRomLocation
            // 
            this.btnSetRomLocation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSetRomLocation.Location = new System.Drawing.Point(617, 53);
            this.btnSetRomLocation.Name = "btnSetRomLocation";
            this.btnSetRomLocation.Size = new System.Drawing.Size(47, 24);
            this.btnSetRomLocation.TabIndex = 14;
            this.btnSetRomLocation.Text = "Set";
            this.btnSetRomLocation.UseVisualStyleBackColor = true;
            this.btnSetRomLocation.Click += new System.EventHandler(this.BtnSetRomLocationClick);
            // 
            // txtROMLocation
            // 
            this.txtROMLocation.BackColor = System.Drawing.Color.White;
            this.txtROMLocation.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtROMLocation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtROMLocation.Location = new System.Drawing.Point(89, 55);
            this.txtROMLocation.Name = "txtROMLocation";
            this.txtROMLocation.Size = new System.Drawing.Size(515, 22);
            this.txtROMLocation.TabIndex = 13;
            this.txtROMLocation.Text = "label2";
            this.txtROMLocation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblROMLocation
            // 
            this.lblROMLocation.AutoSize = true;
            this.lblROMLocation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblROMLocation.Location = new System.Drawing.Point(12, 60);
            this.lblROMLocation.Name = "lblROMLocation";
            this.lblROMLocation.Size = new System.Drawing.Size(79, 13);
            this.lblROMLocation.TabIndex = 12;
            this.lblROMLocation.Text = "ROM Location:";
            // 
            // txtDATLocation
            // 
            this.txtDATLocation.BackColor = System.Drawing.Color.White;
            this.txtDATLocation.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDATLocation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDATLocation.Location = new System.Drawing.Point(89, 25);
            this.txtDATLocation.Name = "txtDATLocation";
            this.txtDATLocation.Size = new System.Drawing.Size(515, 22);
            this.txtDATLocation.TabIndex = 11;
            this.txtDATLocation.Text = "label2";
            this.txtDATLocation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDATLocation
            // 
            this.lblDATLocation.AutoSize = true;
            this.lblDATLocation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDATLocation.Location = new System.Drawing.Point(12, 30);
            this.lblDATLocation.Name = "lblDATLocation";
            this.lblDATLocation.Size = new System.Drawing.Size(76, 13);
            this.lblDATLocation.TabIndex = 10;
            this.lblDATLocation.Text = "DAT Location:";
            // 
            // lblDelete
            // 
            this.lblDelete.AutoSize = true;
            this.lblDelete.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDelete.Location = new System.Drawing.Point(20, 111);
            this.lblDelete.Name = "lblDelete";
            this.lblDelete.Size = new System.Drawing.Size(144, 13);
            this.lblDelete.TabIndex = 15;
            this.lblDelete.Text = "Delete Existing Mapping";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(586, 355);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(96, 25);
            this.btnClose.TabIndex = 16;
            this.btnClose.Text = "Done";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnCloseClick);
            // 
            // btnResetAll
            // 
            this.btnResetAll.Location = new System.Drawing.Point(138, 355);
            this.btnResetAll.Name = "btnResetAll";
            this.btnResetAll.Size = new System.Drawing.Size(96, 25);
            this.btnResetAll.TabIndex = 17;
            this.btnResetAll.Text = "Reset All";
            this.btnResetAll.UseVisualStyleBackColor = true;
            this.btnResetAll.Click += new System.EventHandler(this.BtnResetAllClick);
            // 
            // FrmSetDir
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(694, 391);
            this.Controls.Add(this.btnResetAll);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.lblDelete);
            this.Controls.Add(this.grpBoxAddNew);
            this.Controls.Add(this.btnDeleteSelected);
            this.Controls.Add(this.DataGridGames);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSetDir";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Set ROM Directories";
            this.Activated += new System.EventHandler(this.FrmSetDirActivated);
            ((System.ComponentModel.ISupportInitialize)(this.DataGridGames)).EndInit();
            this.grpBoxAddNew.ResumeLayout(false);
            this.grpBoxAddNew.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView DataGridGames;
        private System.Windows.Forms.DataGridViewTextBoxColumn CDAT;
        private System.Windows.Forms.DataGridViewTextBoxColumn CROM;
        private System.Windows.Forms.Button btnDeleteSelected;
        private System.Windows.Forms.GroupBox grpBoxAddNew;
        private System.Windows.Forms.Button btnSetRomLocation;
        private System.Windows.Forms.Label txtROMLocation;
        private System.Windows.Forms.Label lblROMLocation;
        private System.Windows.Forms.Label txtDATLocation;
        private System.Windows.Forms.Label lblDATLocation;
        private System.Windows.Forms.Label lblDelete;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnResetAll;
    }
}