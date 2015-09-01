/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2013                                *
 ******************************************************/

using System;
using System.Collections.Generic;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2
{
    public enum EFile
    {
        Keep,
        Delete
    }

    public static class DBHelper
    {


        private static readonly byte[] ZeroByteMD5;
        private static readonly byte[] ZeroByteSHA1;
        private static readonly byte[] ZeroByteCRC;

        static DBHelper()
        {
            ZeroByteMD5 = VarFix.CleanMD5SHA1("d41d8cd98f00b204e9800998ecf8427e", 32);
            ZeroByteSHA1 = VarFix.CleanMD5SHA1("da39a3ee5e6b4b0d3255bfef95601890afd80709", 40);
            ZeroByteCRC = VarFix.CleanMD5SHA1("00000000", 8);
        }



        public static void GetSelectedDirList(ref List<RvDir> lstDir)
        {
            GetSelectedDirList(ref lstDir, DB.DirTree);
        }
        private static void GetSelectedDirList(ref List<RvDir> lstDir, RvDir thisDir)
        {
            for (int i = 0; i < thisDir.ChildCount; i++)
            {
                if (thisDir.DatStatus != DatStatus.InDatCollect) continue;
                RvDir tDir = thisDir.Child(i) as RvDir;
                if (tDir == null) continue;
                if (tDir.Tree == null) continue;
                if (tDir.Tree.Checked == RvTreeRow.TreeSelect.Selected)
                    lstDir.Add(tDir);

                GetSelectedDirList(ref lstDir, tDir);
            }
        }


        public static int CompareName(RvBase var1, RvBase var2)
        {
            int retv = TrrntZipStringCompare(var1.Name, var2.Name);
            if (retv != 0) return retv;

            FileType f1 = var1.FileType;
            FileType f2 = var2.FileType;

            if (f1 == FileType.ZipFile)
            {
                if (f2 != FileType.ZipFile)
                    ReportError.SendAndShow("Incompatible Compare type");

                return Math.Sign(String.Compare(var1.Name, var2.Name, StringComparison.Ordinal));
            }
            return f1.CompareTo(f2);
        }

        private static int TrrntZipStringCompare(string string1, string string2)
        {
            char[] bytes1 = string1.ToCharArray();
            char[] bytes2 = string2.ToCharArray();

            int pos1 = 0;
            int pos2 = 0;

            for (; ; )
            {
                if (pos1 == bytes1.Length)
                    return ((pos2 == bytes2.Length) ? 0 : -1);
                if (pos2 == bytes2.Length)
                    return 1;

                int byte1 = bytes1[pos1++];
                int byte2 = bytes2[pos2++];

                if (byte1 >= 65 && byte1 <= 90) byte1 += 0x20;
                if (byte2 >= 65 && byte2 <= 90) byte2 += 0x20;

                if (byte1 < byte2)
                    return -1;
                if (byte1 > byte2)
                    return 1;
            }
        }




        public static int DatCompare(RvDat var1, RvDat var2)
        {
            int retv = Math.Sign(string.Compare(var1.GetData(RvDat.DatData.DatFullName), var2.GetData(RvDat.DatData.DatFullName), StringComparison.CurrentCultureIgnoreCase));
            if (retv != 0) return retv;


            retv = Math.Sign(var1.TimeStamp.CompareTo(var2.TimeStamp));
            if (retv != 0) return retv;

            retv = Math.Sign(var1.AutoAddDirectory.CompareTo(var2.AutoAddDirectory));
            if (retv != 0) return retv;

            return 0;
        }

        public static string GetRealPath(string rootPath)
        {
            string strFullPath = "";
            int lenFound = 0;
            foreach (DirMap dirPathMap in Settings.DirPathMap)
            {
                string dirKey = dirPathMap.DirKey;
                int dirKeyLen = dirKey.Length;

                if (rootPath.Length >= dirKeyLen)
                    if (String.Compare(rootPath.Substring(0, dirKeyLen), dirKey, StringComparison.Ordinal) == 0)
                    {
                        if (lenFound < dirKeyLen)
                        {
                            string dirPath = dirPathMap.DirPath;
                            lenFound = dirKeyLen;
                            strFullPath = rootPath == dirKey ? dirPath : IO.Path.Combine(dirPath, rootPath.Substring(dirKeyLen + 1));
                        }
                    }
            }
            return strFullPath;
        }

        public static string GetDatPath(string rootPath)
        {
            if (rootPath == "")
                return Settings.DATRoot;
            if (rootPath.Substring(0, 6) == "Unsorted")
                return "Error";
            if (rootPath.Substring(0, 8) == "ROMVault")
                return Settings.DATRoot + rootPath.Substring(8);

            return Settings.DATRoot;

        }







        public static void GetSelectedFilesSortCRCSize(out List<RvFile> lstFiles)
        {
            lstFiles = new List<RvFile>();
            GetSelectedFilesSortCRCSize(ref lstFiles, DB.DirTree, true);
            RomSortCRCSize(lstFiles);
        }
        private static void GetSelectedFilesSortCRCSize(ref List<RvFile> lstFiles, RvBase val, bool selected)
        {
            if (selected)
            {
                RvFile rvFile = val as RvFile;
                if (rvFile != null)
                {
                    if ((rvFile.Size != null && rvFile.CRC != null) || rvFile.Size == 0)
                        lstFiles.Add(rvFile);
                }
            }

            RvDir rvVal = val as RvDir;
            if (rvVal == null) return;

            for (int i = 0; i < rvVal.ChildCount; i++)
            {
                bool nextSelect = selected;
                if (rvVal.Tree != null)
                    nextSelect = rvVal.Tree.Checked == RvTreeRow.TreeSelect.Selected;
                GetSelectedFilesSortCRCSize(ref lstFiles, rvVal.Child(i), nextSelect);
            }
        }

        private static void RomSortCRCSize(List<RvFile> lstFiles)
        {
            RomSortCRCSize(0, lstFiles.Count, lstFiles);
        }
        private static void RomSortCRCSize(int intBase, int intTop, List<RvFile> lstFiles)
        {
            if ((intTop - intBase) <= 1) return;
            int intMiddle = (intTop + intBase) / 2;

            if ((intMiddle - intBase) > 1)
                RomSortCRCSize(intBase, intMiddle, lstFiles);

            if ((intTop - intMiddle) > 1)
                RomSortCRCSize(intMiddle, intTop, lstFiles);

            int intBottomSize = intMiddle - intBase;
            int intTopSize = intTop - intMiddle;

            RvFile[] lstBottom = new RvFile[intBottomSize];
            RvFile[] lstTop = new RvFile[intTopSize];

            for (int intloop = 0; intloop < intBottomSize; intloop++)
                lstBottom[intloop] = lstFiles[intBase + intloop];

            for (int intloop = 0; intloop < intTopSize; intloop++)
                lstTop[intloop] = lstFiles[intMiddle + intloop];

            int intBottomCount = 0;
            int intTopCount = 0;
            int intCount = intBase;

            while (intBottomCount < intBottomSize && intTopCount < intTopSize)
            {
                if (RomSortCRCSizeFunc(lstBottom[intBottomCount], lstTop[intTopCount]) < 1)
                {
                    lstFiles[intCount] = lstBottom[intBottomCount];
                    intCount++;
                    intBottomCount++;
                }
                else
                {
                    lstFiles[intCount] = lstTop[intTopCount];
                    intCount++;
                    intTopCount++;
                }
            }

            while (intBottomCount < intBottomSize)
            {
                lstFiles[intCount] = lstBottom[intBottomCount];
                intCount++;
                intBottomCount++;
            }
            while (intTopCount < intTopSize)
            {
                lstFiles[intCount] = lstTop[intTopCount];
                intCount++;
                intTopCount++;
            }
        }
        private static int RomSortCRCSizeFunc(RvFile a, RvFile b)
        {
            int retv = ArrByte.iCompare(a.CRC, b.CRC);

            if (retv == 0)
                retv = ULong.iCompare(a.Size, b.Size);

            return retv;
        }

        public static int RomSearchCRCSize(RvFile tRom, List<RvFile> lstFiles, out int index)
        {
            if (lstFiles.Count == 0)
            {
                index = 0;
                return -1;
            }

            // this one below method will always return the first item in a list if there is more than one matching result.
            int intBottom = -1;
            int intTop = lstFiles.Count - 1;

            while (intBottom + 1 < intTop)
            {
                int intMid = (intBottom + intTop + 1) / 2;

                int intRes = RomSortCRCSizeFunc(lstFiles[intMid], tRom);
                if (intRes >= 0)
                    intTop = intMid;
                else
                    intBottom = intMid;

            }
            intBottom++;
            index = intBottom;
            return RomSortCRCSizeFunc(lstFiles[intBottom], tRom);
        }

        // find all of the files that we think we have that match the needed CRC and Size.
        public static void RomSearchFindFixes(RvFile tRom, List<RvFile> lstFiles, out List<RvFile> lstFilesOut)
        {
            lstFilesOut = new List<RvFile>();
            if (tRom.CRC == null || tRom.Size == null)
                return;

            int intIndex;
            int intRes = RomSearchCRCSize(tRom, lstFiles, out intIndex);

            while (intRes == 0)
            {
                if (lstFiles[intIndex].GotStatus == GotStatus.Got && FindFixes.CheckIfMissingFileCanBeFixedByGotFile(tRom, lstFiles[intIndex]))
                    lstFilesOut.Add(lstFiles[intIndex]);

                intIndex++;
                intRes = intIndex < lstFiles.Count ? RomSortCRCSizeFunc(lstFiles[intIndex], tRom) : 1;
            }
        }

        // find all of the files that we think we have that match the needed CRC and Size.
        public static void RomSearchFindMatchingFiles(RvFile tRom, List<RvFile> lstFiles, out int startIndex, out int length)
        {
            int intIndex;
            int intRes = RomSearchCRCSize(tRom, lstFiles, out intIndex);
            startIndex = intIndex;

            while (intRes == 0)
            {
                intIndex++;
                intRes = intIndex < lstFiles.Count ? RomSortCRCSizeFunc(lstFiles[intIndex], tRom) : 1;
            }
            length = intIndex - startIndex;
        }


        public static bool IsZeroLengthFile(RvFile tFile)
        {
            if (tFile.MD5 != null)
            {
                if (!ArrByte.bCompare(tFile.MD5, ZeroByteMD5))
                    return false;
            }

            if (tFile.SHA1 != null)
            {
                if (!ArrByte.bCompare(tFile.SHA1, ZeroByteSHA1))
                    return false;
            }

            if (tFile.CRC != null)
                if (!ArrByte.bCompare(tFile.CRC, ZeroByteCRC))
                    return false;

            return tFile.Size == 0;
        }

        public static bool RomFromSameGame(RvFile a, RvFile b)
        {
            if (a.Parent == null)
                return false;
            if (b.Parent == null)
                return false;

            return a.Parent == b.Parent;
        }



        public static void GetSelectedFilesSortSHA1CHD(out List<RvFile> lstFiles)
        {
            lstFiles = new List<RvFile>();
            GetSelectedFilesSortSHA1CHD(ref lstFiles, DB.DirTree, true);
            RomSortSHA1CHD(lstFiles);
        }
        private static void GetSelectedFilesSortSHA1CHD(ref List<RvFile> lstFiles, RvBase val, bool selected)
        {
            if (selected)
            {
                RvFile rvFile = val as RvFile;
                if (rvFile != null)
                {
                    if (rvFile.SHA1CHD != null)
                        lstFiles.Add(rvFile);
                }
            }

            RvDir rvVal = val as RvDir;
            if (rvVal == null) return;

            for (int i = 0; i < rvVal.ChildCount; i++)
            {
                bool nextSelect = selected;
                if (rvVal.Tree != null)
                    nextSelect = rvVal.Tree.Checked == RvTreeRow.TreeSelect.Selected;
                GetSelectedFilesSortSHA1CHD(ref lstFiles, rvVal.Child(i), nextSelect);
            }
        }

        private static void RomSortSHA1CHD(List<RvFile> lstFiles)
        {
            RomSortSHA1CHD(0, lstFiles.Count, lstFiles);
        }
        private static void RomSortSHA1CHD(int intBase, int intTop, List<RvFile> lstFiles)
        {
            if ((intTop - intBase) <= 1) return;
            int intMiddle = (intTop + intBase) / 2;

            if ((intMiddle - intBase) > 1)
                RomSortSHA1CHD(intBase, intMiddle, lstFiles);

            if ((intTop - intMiddle) > 1)
                RomSortSHA1CHD(intMiddle, intTop, lstFiles);

            int intBottomSize = intMiddle - intBase;
            int intTopSize = intTop - intMiddle;

            RvFile[] lstBottom = new RvFile[intBottomSize];
            RvFile[] lstTop = new RvFile[intTopSize];

            for (int intloop = 0; intloop < intBottomSize; intloop++)
                lstBottom[intloop] = lstFiles[intBase + intloop];

            for (int intloop = 0; intloop < intTopSize; intloop++)
                lstTop[intloop] = lstFiles[intMiddle + intloop];

            int intBottomCount = 0;
            int intTopCount = 0;
            int intCount = intBase;

            while (intBottomCount < intBottomSize && intTopCount < intTopSize)
            {
                if (RomSortSHA1CHDFunc(lstBottom[intBottomCount], lstTop[intTopCount]) < 1)
                {
                    lstFiles[intCount] = lstBottom[intBottomCount];
                    intCount++;
                    intBottomCount++;
                }
                else
                {
                    lstFiles[intCount] = lstTop[intTopCount];
                    intCount++;
                    intTopCount++;
                }
            }

            while (intBottomCount < intBottomSize)
            {
                lstFiles[intCount] = lstBottom[intBottomCount];
                intCount++;
                intBottomCount++;
            }
            while (intTopCount < intTopSize)
            {
                lstFiles[intCount] = lstTop[intTopCount];
                intCount++;
                intTopCount++;
            }
        }
        private static int RomSortSHA1CHDFunc(RvFile a, RvFile b)
        {
            int retv = ArrByte.iCompare(a.SHA1CHD, b.SHA1CHD);

            return retv;
        }


        private static int RomSearchSHA1CHD(RvFile tRom, List<RvFile> lstFiles, out int index)
        {
            if (lstFiles.Count == 0)
            {
                index = 0;
                return -1;
            }

            // this one below method will always return the first item in a list if there is more than one matching result.
            int intBottom = -1;
            int intTop = lstFiles.Count - 1;

            while (intBottom + 1 < intTop)
            {
                int intMid = (intBottom + intTop + 1) / 2;

                int intRes = RomSortSHA1CHDFunc(lstFiles[intMid], tRom);
                if (intRes >= 0)
                    intTop = intMid;
                else
                    intBottom = intMid;

            }
            intBottom++;
            index = intBottom;
            return RomSortSHA1CHDFunc(lstFiles[intBottom], tRom);
        }


        // find all of the files that we have that match the needed SHA1 CHD.
        public static void RomSearchFindFixesSHA1CHD(RvFile tRom, List<RvFile> lstFiles, out List<RvFile> lstFilesOut)
        {
            lstFilesOut = new List<RvFile>();
            if (tRom.SHA1CHD == null)
                return;

            int intIndex;
            int intRes = RomSearchSHA1CHD(tRom, lstFiles, out intIndex);

            while (intRes == 0)
            {
                if (lstFiles[intIndex].GotStatus == GotStatus.Got)
                    lstFilesOut.Add(lstFiles[intIndex]);

                intIndex++;
                intRes = intIndex < lstFiles.Count ? RomSortSHA1CHDFunc(lstFiles[intIndex], tRom) : 1;
            }
        }
    }
}
