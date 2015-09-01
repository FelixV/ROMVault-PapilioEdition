/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using ROMVault2.Utils;

namespace ROMVault2.RvDB
{
    public class RvFile : RvBase
    {
        public ulong? Size;
        public byte[] CRC;
        public byte[] SHA1;
        public byte[] MD5;
        public byte[] SHA1CHD;
        public byte[] MD5CHD;

        public string Merge = "";
        public string Status;
        private FileStatus _fileStatus;

        public int ZipFileIndex = -1;
        public ulong? ZipFileHeaderPosition;

        public uint? CHDVersion;

        public RvFile(FileType type)
            : base(type)
        {
            if (type != FileType.File && type != FileType.ZipFile)
                ReportError.SendAndShow("Trying to set file type to " + type);
        }

        [Flags]
        private enum FileFlags
        {
            Size = 0x01,
            CRC = 0x02,
            SHA1 = 0x04,
            MD5 = 0x08,
            SHA1CHD = 0x10,
            MD5CHD = 0x20,
            Merge = 0x40,
            Status = 0x80,
            ZipFileIndex = 0x100,
            ZipFileHeader = 0x200,
            CHDVersion = 0x400
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            FileFlags fFlags = 0;
            if (Size != null) fFlags |= FileFlags.Size;
            if (CRC != null) fFlags |= FileFlags.CRC;
            if (SHA1 != null) fFlags |= FileFlags.SHA1;
            if (MD5 != null) fFlags |= FileFlags.MD5;
            if (SHA1CHD != null) fFlags |= FileFlags.SHA1CHD;
            if (MD5CHD != null) fFlags |= FileFlags.MD5CHD;
            if (!String.IsNullOrEmpty(Merge)) fFlags |= FileFlags.Merge;
            if (!String.IsNullOrEmpty(Status)) fFlags |= FileFlags.Status;
            if (ZipFileIndex >= 0) fFlags |= FileFlags.ZipFileIndex;
            if (ZipFileHeaderPosition != null) fFlags |= FileFlags.ZipFileHeader;
            if (CHDVersion != null) fFlags |= FileFlags.CHDVersion;

            bw.Write((UInt16)fFlags);

            if (Size != null) bw.Write((ulong)Size);
            if (CRC != null) ArrByte.Write(bw, CRC);
            if (SHA1 != null) ArrByte.Write(bw, SHA1);
            if (MD5 != null) ArrByte.Write(bw, MD5);
            if (SHA1CHD != null) ArrByte.Write(bw, SHA1CHD);
            if (MD5CHD != null) ArrByte.Write(bw, MD5CHD);
            if (!String.IsNullOrEmpty(Merge)) bw.Write(Merge);
            if (!String.IsNullOrEmpty(Status)) bw.Write(Status);
            if (ZipFileIndex >= 0) bw.Write(ZipFileIndex);
            if (ZipFileHeaderPosition != null) bw.Write((long)ZipFileHeaderPosition);
            if (CHDVersion != null) bw.Write((uint)CHDVersion);

            bw.Write((uint)_fileStatus);
        }
        public override void Read(BinaryReader br, List<RvDat> parentDirDats)
        {
            base.Read(br, parentDirDats);

            FileFlags fFlags = (FileFlags)br.ReadUInt16();

            Size = (fFlags & FileFlags.Size) > 0 ? (ulong?)br.ReadUInt64() : null;
            CRC = (fFlags & FileFlags.CRC) > 0 ? ArrByte.Read(br) : null;
            SHA1 = (fFlags & FileFlags.SHA1) > 0 ? ArrByte.Read(br) : null;
            MD5 = (fFlags & FileFlags.MD5) > 0 ? ArrByte.Read(br) : null;
            SHA1CHD = (fFlags & FileFlags.SHA1CHD) > 0 ? ArrByte.Read(br) : null;
            MD5CHD = (fFlags & FileFlags.MD5CHD) > 0 ? ArrByte.Read(br) : null;
            Merge = (fFlags & FileFlags.Merge) > 0 ? br.ReadString() : null;
            Status = (fFlags & FileFlags.Status) > 0 ? br.ReadString() : null;
            ZipFileIndex = (fFlags & FileFlags.ZipFileIndex) > 0 ? br.ReadInt32() : -1;
            ZipFileHeaderPosition = (fFlags & FileFlags.ZipFileHeader) > 0 ? (ulong?)br.ReadUInt64() : null;
            CHDVersion = (fFlags & FileFlags.CHDVersion) > 0 ? (uint?)br.ReadInt32() : null;

            _fileStatus = (FileStatus)br.ReadUInt32();
        }

        public override EFile DatRemove()
        {
            if (!FileStatusIs(FileStatus.SizeFromHeader) && !FileStatusIs(FileStatus.SizeVerified)) Size = null;
            if (!FileStatusIs(FileStatus.CRCFromHeader) && !FileStatusIs(FileStatus.CRCVerified)) CRC = null;
            if (!FileStatusIs(FileStatus.SHA1FromHeader) && !FileStatusIs(FileStatus.SHA1Verified)) SHA1 = null;
            if (!FileStatusIs(FileStatus.MD5FromHeader) && !FileStatusIs(FileStatus.MD5Verified)) MD5 = null;
            if (!FileStatusIs(FileStatus.SHA1CHDFromHeader) && !FileStatusIs(FileStatus.SHA1CHDVerified)) SHA1CHD = null;
            if (!FileStatusIs(FileStatus.MD5CHDFromHeader) && !FileStatusIs(FileStatus.MD5CHDVerified)) MD5CHD = null;

            FileStatusClear(FileStatus.SizeFromDAT | FileStatus.CRCFromDAT | FileStatus.SHA1FromDAT | FileStatus.MD5FromDAT | FileStatus.SHA1CHDFromDAT | FileStatus.MD5CHDFromDAT);

            Merge = "";
            Status = "";
            return base.DatRemove();
        }
        public override void DatAdd(RvBase file)
        {
            RvFile tFile = file as RvFile;
            if (tFile == null)
            {
                ReportError.SendAndShow("Error setting Dat Set Got");
                return;
            }

            if (Size == null && tFile.Size != null) Size = tFile.Size;
            if (CRC == null && tFile.CRC != null) CRC = tFile.CRC;
            if (SHA1 == null && tFile.SHA1 != null) SHA1 = tFile.SHA1;
            if (MD5 == null && tFile.MD5 != null) MD5 = tFile.MD5;
            if (SHA1CHD == null && tFile.SHA1CHD != null) SHA1CHD = tFile.SHA1CHD;
            if (MD5CHD == null && tFile.MD5CHD != null) MD5CHD = tFile.MD5CHD;

            FileStatusSet(
                FileStatus.SizeFromDAT | FileStatus.CRCFromDAT | FileStatus.SHA1FromDAT | FileStatus.MD5FromDAT | FileStatus.SHA1CHDFromDAT | FileStatus.MD5CHDFromDAT,
                tFile);

            Merge = tFile.Merge;
            Status = tFile.Status;
            base.DatAdd(file);
        }

        public override EFile FileRemove()
        {
            ZipFileIndex = -1;
            ZipFileHeaderPosition = null;

            if (base.FileRemove() == EFile.Delete)
                return EFile.Delete;

            if (!FileStatusIs(FileStatus.SizeFromDAT)) Size = null;
            if (!FileStatusIs(FileStatus.CRCFromDAT)) CRC = null;
            if (!FileStatusIs(FileStatus.SHA1FromDAT)) SHA1 = null;
            if (!FileStatusIs(FileStatus.MD5FromDAT)) MD5 = null;
            if (!FileStatusIs(FileStatus.SHA1CHDFromDAT)) SHA1CHD = null;
            if (!FileStatusIs(FileStatus.MD5CHDFromDAT)) MD5CHD = null;

            CHDVersion = null;

            FileStatusClear(
                FileStatus.SizeFromHeader | FileStatus.CRCFromHeader | FileStatus.SHA1FromHeader | FileStatus.MD5FromHeader | FileStatus.SHA1CHDFromHeader | FileStatus.MD5CHDFromHeader |
                FileStatus.SizeVerified | FileStatus.CRCVerified | FileStatus.SHA1Verified | FileStatus.MD5Verified | FileStatus.SHA1CHDVerified | FileStatus.MD5CHDVerified);

            return EFile.Keep;
        }
        public override void FileAdd(RvBase file)
        {
            RvFile tFile = file as RvFile;
            if (tFile == null)
            {
                ReportError.SendAndShow("Error setting File Got");
                return;
            }

            if (Size == null && tFile.Size != null) Size = tFile.Size;
            if (CRC == null && tFile.CRC != null) CRC = tFile.CRC;
            if (SHA1 == null && tFile.SHA1 != null) SHA1 = tFile.SHA1;
            if (MD5 == null && tFile.MD5 != null) MD5 = tFile.MD5;
            if (SHA1CHD == null && tFile.SHA1CHD != null) SHA1CHD = tFile.SHA1CHD;
            if (MD5CHD == null && tFile.MD5CHD != null) MD5CHD = tFile.MD5CHD;

            CHDVersion = tFile.CHDVersion;

            FileStatusSet(
                FileStatus.SizeFromHeader | FileStatus.CRCFromHeader | FileStatus.SHA1FromHeader | FileStatus.MD5FromHeader | FileStatus.SHA1CHDFromHeader | FileStatus.MD5CHDFromHeader |
                FileStatus.SizeVerified | FileStatus.CRCVerified | FileStatus.SHA1Verified | FileStatus.MD5Verified | FileStatus.SHA1CHDVerified | FileStatus.MD5CHDVerified,
                tFile);

            ZipFileIndex = tFile.ZipFileIndex;
            ZipFileHeaderPosition = tFile.ZipFileHeaderPosition;

            base.FileAdd(file);
        }

        public override void CopyTo(RvBase c)
        {
            RvFile cf = c as RvFile;
            if (cf != null)
            {
                cf.Size = Size;
                cf.CRC = CRC;
                cf.SHA1 = SHA1;
                cf.MD5 = MD5;
                cf.Merge = Merge;
                cf.Status = Status;
                cf._fileStatus = _fileStatus;
                cf.SHA1CHD = SHA1CHD;
                cf.MD5CHD = MD5CHD;

                cf.ZipFileIndex = ZipFileIndex;
                cf.ZipFileHeaderPosition = ZipFileHeaderPosition;

                cf.CHDVersion = CHDVersion;
            }
            base.CopyTo(c);
        }




        public void FileStatusSet(FileStatus flag)
        {
            _fileStatus |= flag;
        }
        public void FileStatusSet(FileStatus flag, RvFile copyFrom)
        {
            _fileStatus |= (flag & copyFrom._fileStatus);
        }



        private void FileStatusClear(FileStatus flag)
        {
            _fileStatus &= ~flag;
        }
        public bool FileStatusIs(FileStatus flag)
        {
            return (_fileStatus & flag) == flag;
        }



    }

}
