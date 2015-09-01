/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System.Collections.Generic;
using System.IO;
using ROMVault2.Properties;
using ROMVault2.SupportedFiles;
using ROMVault2.Utils;

namespace ROMVault2.RvDB
{
    public class RvDir : RvBase
    {

        public RvTreeRow Tree;
        public RvGame Game;
        private readonly List<RvDat> _dirDats = new List<RvDat>();
        private readonly List<RvBase> _children = new List<RvBase>();
        public readonly ReportStatus DirStatus = new ReportStatus();

        public ZipStatus ZipStatus;

        public RvDir(FileType type)
            : base(type)
        {
            if (type != FileType.Dir && type != FileType.Zip)
                ReportError.SendAndShow("Trying to set Dir type to " + type);
        }



        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);
            if (Tree == null)
                bw.Write(false);
            else
            {
                bw.Write(true);
                Tree.Write(bw);
            }

            if (Game == null)
                bw.Write(false);
            else
            {
                bw.Write(true);
                Game.Write(bw);
            }

            int count = _dirDats.Count;
            bw.Write(count);
            for (int i = 0; i < count; i++)
            {
                _dirDats[i].Write(bw);
            }

            count = _children.Count;
            bw.Write(count);
            for (int i = 0; i < count; i++)
            {
                FileType type = _children[i].FileType;
                bw.Write((byte)type);
                _children[i].Write(bw);
            }

            if (DBTypeGet.isCompressedDir(FileType))
                bw.Write((byte)ZipStatus);

        }
        public override void Read(BinaryReader br, List<RvDat> parentDirDats)
        {
            base.Read(br, parentDirDats);
            bool foundTree = br.ReadBoolean();
            if (foundTree)
            {
                Tree = new RvTreeRow();
                Tree.Read(br);
            }
            else
                Tree = null;

            bool foundGame = br.ReadBoolean();
            if (foundGame)
            {
                Game = new RvGame();
                Game.Read(br);
            }
            else
                Game = null;

            int count = br.ReadInt32();
            _dirDats.Clear();
            for (int i = 0; i < count; i++)
            {
                RvDat dat = new RvDat { DatIndex = i };
                dat.Read(br);
                _dirDats.Add(dat);

                string datname = TreeFullName + @"\" + dat.GetData(RvDat.DatData.DatName);
                if (datname.Length >= 9 && datname.Substring(0, 9) == @"ROMVault\")
                    datname = datname.Substring(9);

                DB.Bgw.ReportProgress(0, new bgwText("Loading: " + datname));
                DB.Bgw.ReportProgress((int)br.BaseStream.Position);

            }
            if (_dirDats.Count > 0)
                parentDirDats = _dirDats;

            count = br.ReadInt32();
            _children.Clear();
            for (int i = 0; i < count; i++)
            {
                RvBase tChild = DBTypeGet.GetRvType((FileType)br.ReadByte());

                tChild.Parent = this;
                tChild.Read(br, parentDirDats);
                _children.Add(tChild);
            }

            if (DBTypeGet.isCompressedDir(FileType))
                ZipStatus = (ZipStatus)br.ReadByte();

        }


        public override EFile DatRemove()
        {
            Tree = null;
            Game = null;
            _dirDats.Clear();
            return base.DatRemove();
        }
        public override void DatAdd(RvBase b)
        {
            Tree = ((RvDir)b).Tree;
            Game = ((RvDir)b).Game;
            if (_dirDats.Count > 0)
                ReportError.SendAndShow(Resources.RvDir_SetDat_Setting_Dir_with_a_dat_list);
            base.DatAdd(b);
        }


        public override EFile FileRemove()
        {
            ZipStatus = ZipStatus.None;
            return base.FileRemove();
        }







        public int DirDatCount
        {
            get
            {
                return _dirDats.Count;
            }
        }
        public RvDat DirDat(int index)
        {
            return _dirDats[index];
        }
        public void DirDatAdd(RvDat dat)
        {
            int index;
            DirDatSearch(dat, out index);
            _dirDats.Insert(index, dat);
            for (int i = 0; i < _dirDats.Count; i++)
                _dirDats[i].DatIndex = i;
        }
        public void DirDatRemove(int index)
        {
            _dirDats.RemoveAt(index);
            for (int i = 0; i < _dirDats.Count; i++)
                _dirDats[i].DatIndex = i;
        }

        private void DirDatSearch(RvDat dat, out int index)
        {
            int intBottom = 0;
            int intTop = _dirDats.Count;
            int intMid = 0;
            int intRes = -1;

            //Binary chop to find the closest match
            while (intBottom < intTop && intRes != 0)
            {
                intMid = (intBottom + intTop) / 2;

                intRes = DBHelper.DatCompare(dat, _dirDats[intMid]);
                if (intRes < 0)
                    intTop = intMid;
                else if (intRes > 0)
                    intBottom = intMid + 1;
            }
            index = intMid;

            // if match was found check up the list for the first match
            if (intRes == 0)
            {
                int intRes1 = 0;
                while (index > 0 && intRes1 == 0)
                {
                    intRes1 = DBHelper.DatCompare(dat, _dirDats[index - 1]);
                    if (intRes1 == 0)
                        index--;
                }
            }
            // if the search is greater than the closest match move one up the list
            else if (intRes > 0)
                index++;
        }



        public int ChildCount
        {
            get
            {
                return _children.Count;
            }
        }
        public RvBase Child(int index)
        {
            return _children[index];
        }

        public int ChildAdd(RvBase child)
        {
            int index;
            ChildNameSearch(child, out index);
            ChildAdd(child, index);
            return index;
        }
        public void ChildAdd(RvBase child, int index)
        {
            if (
                (FileType == FileType.Dir && child.FileType == FileType.ZipFile) ||
                (FileType == FileType.Zip && child.FileType != FileType.ZipFile) 
                )
                ReportError.SendAndShow("Typing to add a " + child.FileType + " to a " + FileType);

            _children.Insert(index, child);
            child.Parent = this;
            UpdateRepStatusArrUpTree(child, 1);
        }

        public void ChildRemove(int index)
        {
            UpdateRepStatusArrUpTree(_children[index], -1);
            if (_children[index].Parent == this)
                _children[index].Parent = null;
            _children.RemoveAt(index);
        }

        public int ChildNameSearch(RvBase lName, out int index)
        {
            int intBottom = 0;
            int intTop = _children.Count;
            int intMid = 0;
            int intRes = -1;

            //Binary chop to find the closest match
            while (intBottom < intTop && intRes != 0)
            {
                intMid = (intBottom + intTop) / 2;

                intRes = DBHelper.CompareName(lName, _children[intMid]);
                if (intRes < 0)
                    intTop = intMid;
                else if (intRes > 0)
                    intBottom = intMid + 1;
            }
            index = intMid;

            // if match was found check up the list for the first match
            if (intRes == 0)
            {
                int intRes1 = 0;
                while (index > 0 && intRes1 == 0)
                {
                    intRes1 = DBHelper.CompareName(lName, _children[index - 1]);
                    if (intRes1 == 0)
                        index--;
                }
            }
            // if the search is greater than the closest match move one up the list
            else if (intRes > 0)
                index++;

            return intRes;
        }


        public bool FindChild(RvBase lName, out int index)
        {
            if (ChildNameSearch(lName, out index) != 0)
            {
                ReportError.UnhandledExceptionHandler("Could not find self in Parent " + FullName);
                return false;
            }

            do
            {
                if (_children[index] == lName)
                    return true;
                index++;

            } while (index < _children.Count && DBHelper.CompareName(lName, _children[index]) == 0);

            return false;
        }



        public void UpdateRepStatusUpTree(RepStatus rStat, int dir)
        {
            DirStatus.UpdateRepStatus(rStat, dir);
            if (Parent != null)
                Parent.UpdateRepStatusUpTree(rStat, dir);
        }

        private void UpdateRepStatusArrUpTree(RvBase child, int dir)
        {
            DirStatus.UpdateRepStatus(child.RepStatus, dir);
            RvDir rvDir = child as RvDir;
            if (rvDir != null)
                DirStatus.UpdateRepStatus(rvDir.DirStatus, dir);

            if (Parent != null)
                Parent.UpdateRepStatusArrUpTree(child, dir);
        }


    }

}
