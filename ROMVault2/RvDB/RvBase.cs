/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using ROMVault2.Properties;

namespace ROMVault2.RvDB
{
    public enum DatStatus { InDatCollect, InDatMerged, InDatBad, NotInDat, InUnsorted }
    public enum GotStatus { NotGot, Got, Corrupt, FileLocked }
    public enum FileType { Unknown, Dir, Zip, xSevenZip, xRAR, File, ZipFile, xSevenZipFile }

    public abstract class RvBase
    {
        private readonly FileType _type;

        public string Name;               // The Name of the File or Directory
        public string FileName;           // Found filename if different from Name
        public RvDir Parent;              // A link to the Parent Directory
        public RvDat Dat;
        public long TimeStamp;
        public int ReportIndex;

        public bool SearchFound;

        private DatStatus _datStatus = DatStatus.NotInDat;
        private GotStatus _gotStatus = GotStatus.NotGot;
        private RepStatus _repStatus = RepStatus.UnSet;

        protected RvBase(FileType type)
        {
            _type = type;
        }

        public FileType FileType
        {
            get { return _type; }
        }

        public virtual void Write(BinaryWriter bw)
        {
            bw.Write(DB.Fn(Name));
            bw.Write(DB.Fn(FileName));
            bw.Write(TimeStamp);
            if (Dat == null)
            {
                bw.Write(false);
            }
            else
            {
                //Debug.WriteLine("Dat is not null");
                bw.Write(true);
                //Debug.WriteLine("Dat index is "+ Dat.DatIndex);
                bw.Write(Dat.DatIndex);
            }

            bw.Write((byte)_datStatus);
            bw.Write((byte)_gotStatus);
        }
        public virtual void Read(BinaryReader br, List<RvDat> parentDirDats)
        {
            Name = br.ReadString();
            FileName = br.ReadString();
            TimeStamp = br.ReadInt64();
            bool foundDat = br.ReadBoolean();
            if (foundDat)
            {
                int index = br.ReadInt32();
                if (index == -1)
                    ReportError.SendAndShow(Resources.RvBase_Read_Dat_found_without_an_index);
                else
                    Dat = parentDirDats[index];
            }
            else
            {
                Dat = null;
            }

            _datStatus = (DatStatus)br.ReadByte();
            _gotStatus = (GotStatus)br.ReadByte();
            RepStatusReset();
        }


        public virtual EFile DatRemove()
        {
            Dat = null;
            if (GotStatus == GotStatus.NotGot)
                return EFile.Delete;

            if (!String.IsNullOrEmpty(FileName))
            {
                Name = FileName;
                FileName = null;
            }
            DatStatus = DatStatus.NotInDat;
            return EFile.Keep;
        }
        public virtual void DatAdd(RvBase b)
        {
            // Parent , TimeStamp Should already be correct.

            if (GotStatus == GotStatus.NotGot)
                ReportError.SendAndShow("Error Adding DAT to NotGot File " + b.GotStatus);

            SetStatus(b.DatStatus, GotStatus.Got);

            if (Name == b.Name) // case match so all is good
            {
                FileName = null;
            }
            else
            {
                FileName = Name;
                Name = b.Name;
            }

            Dat = b.Dat;
        }

        public virtual EFile FileRemove()
        {
            TimeStamp = 0;
            FileName = null;

            GotStatus = (Parent.GotStatus == GotStatus.FileLocked) ? GotStatus.FileLocked : GotStatus.NotGot;
            switch (DatStatus)
            {
                case DatStatus.InDatCollect:
                case DatStatus.InDatMerged:
                case DatStatus.InDatBad:
                    return EFile.Keep;

                case DatStatus.NotInDat:
                case DatStatus.InUnsorted:
                    return EFile.Delete; // this item should be removed from the db.
                default:
                    ReportError.SendAndShow(Resources.RvBase_SetGot_Unknown_Set_Got_Status + DatStatus);
                    return EFile.Keep;
            }
        }
        public virtual void FileAdd(RvBase file)
        {
            TimeStamp = file.TimeStamp;
            FileCheckName(file);
            if (file.GotStatus == GotStatus.NotGot)
                ReportError.SendAndShow("Error setting got to a NotGot File");
            GotStatus = file.GotStatus;
        }

        public void FileCheckName(RvBase file)
        {
            // Don't care about bad case if the file is not in a dat.
            if (DatStatus == DatStatus.NotInDat || DatStatus == DatStatus.InUnsorted)
                Name = file.Name;

            FileName = Name == file.Name ? null : file.Name;
        }

        public virtual void CopyTo(RvBase c)
        {
            c.Name = Name;
            c.FileName = FileName;
            //c.Parent = Parent;
            c.Dat = Dat;
            c.TimeStamp = TimeStamp;
            c.ReportIndex = ReportIndex;
            c._datStatus = _datStatus;
            c._gotStatus = _gotStatus;
            c.RepStatus = RepStatus;
        }



        private string Extention
        {
            get
            {
                switch (FileType)
                {
                    case FileType.Zip: return ".zip";
                    //case FileType.SevenZip: return ".7z";
                    //case FileType.RAR: return ".rar";
                }
                return "";
            }
        }
        public string FullName
        {
            get { return DBHelper.GetRealPath(TreeFullName); }
        }
        public string DatFullName
        {
            get { return DBHelper.GetDatPath(TreeFullName); }
        }
        public string TreeFullName
        {
            get
            {
                if (Parent == null) return Name + Extention;
                return IO.Path.Combine(Parent.TreeFullName, Name + Extention);
            }
        }
        public bool IsInUnsorted
        {
            get
            {
                string fullName = TreeFullName;
                return fullName.Substring(0, 6) == "Unsorted";
            }
        }

        public string SuperDatFileName()
        {
            return SuperDatFileName(Dat);
        }

        private string SuperDatFileName(RvDat dat)
        {
            if (dat.AutoAddDirectory)
            {
                if (Parent == null || Parent.Parent == null || Parent.Parent.Dat != dat) return Name;
            }
            else
            {
                if (Parent == null || Parent.Dat != dat) return Name;
            }
            return IO.Path.Combine(Parent.SuperDatFileName(dat), Name);
        }

        public string FileNameInsideGame()
        {
            RvDir d = this as RvDir;
            if (d != null && d.Game != null) return Name;

            return IO.Path.Combine(Parent.FileNameInsideGame(), Name);
        }

        public DatStatus DatStatus
        {
            set
            {
                _datStatus = value;
                RepStatusReset();
            }
            get { return _datStatus; }
        }



        public GotStatus GotStatus
        {
            get { return _gotStatus; }
            set
            {
                _gotStatus = value;
                RepStatusReset();
            }
        }

        public void SetStatus(DatStatus dt, GotStatus flag)
        {
            _datStatus = dt;
            _gotStatus = flag;
            RepStatusReset();
        }


        public RepStatus RepStatus
        {
            get { return _repStatus; }
            set
            {
                if (Parent != null) Parent.UpdateRepStatusUpTree(_repStatus, -1);

                List<RepStatus> rs = RepairStatus.StatusCheck[(int)FileType, (int)_datStatus, (int)_gotStatus];
                if (rs == null || !rs.Contains(value))
                {
                    ReportError.SendAndShow(FullName + " " + Resources.RvBase_Check + FileType + Resources.RvBase_Check + _datStatus + Resources.RvBase_Check + _gotStatus + " from: " + _repStatus + " to: " + value);
                    _repStatus = RepStatus.Error;
                }
                else
                    _repStatus = value;

                if (Parent != null) Parent.UpdateRepStatusUpTree(_repStatus, 1);
            }
        }
        public void RepStatusReset()
        {
            SearchFound = false;
            if ((RepStatus == RepStatus.UnSet || RepStatus == RepStatus.Unknown || RepStatus == RepStatus.Ignore) && FileType == FileType.File && GotStatus == GotStatus.Got && DatStatus==DatStatus.NotInDat)
                foreach (string file in Settings.IgnoreFiles)
                    if (Name == file)
                    {
                        RepStatus = RepStatus.Ignore;
                        return;
                    }

            List<RepStatus> rs = RepairStatus.StatusCheck[(int)FileType, (int)_datStatus, (int)_gotStatus];
            RepStatus = rs == null ? RepStatus.Error : rs[0];
        }
    }
}
