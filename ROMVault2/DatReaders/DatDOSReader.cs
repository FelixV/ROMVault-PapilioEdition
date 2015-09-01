/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.ComponentModel;
using System.IO;
using ROMVault2.Properties;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2.DatReaders
{
    public static class DatDOSReader
    {
        private static bool _cleanFileNames = true;


        public static bool ReadDat(ref RvDir tDat, string strFilename)
        {
            RvDir tNow = tDat;
            FileType thisFileType = FileType.Unknown;

            int errorCode = DatFileLoader.LoadDat(strFilename);
            if (errorCode != 0)
            {
                DatUpdate.ShowDat(new Win32Exception(errorCode).Message, strFilename);
                return false;
            }

            DatFileLoader.Gn();
            while (!DatFileLoader.EndOfStream())
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "doscenter":
                        _cleanFileNames = true;
                        DatFileLoader.Gn();
                        if (!LoadHeaderFromDat(ref tNow, ref thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    case "game":
                        DatFileLoader.Gn();
                        if (!LoadGameFromDat(ref tNow, thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + Resources.DatCmpReader_ReadDat_not_known, DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }

            DatFileLoader.Close();
            return true;
        }

        private static bool LoadHeaderFromDat(ref RvDir tDir, ref FileType thisFileType)
        {

            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadHeaderFromDat_not_found_after_clrmamepro, DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();

            RvDat tDat = new RvDat();
            while (DatFileLoader.Next != ")")
            {
                string nextstr = DatFileLoader.Next.ToLower();
                if (nextstr.Length > 5 && nextstr.Substring(0, 5) == "name:")
                {
                    tDat.AddData(RvDat.DatData.DatName,  VarFix.CleanFileName(DatFileLoader.Next.Substring(5) + " " + DatFileLoader.GnRest()).Trim()); DatFileLoader.Gn();
                }
                else
                {
                    switch (nextstr)
                    {
                        case "name:": tDat.AddData(RvDat.DatData.DatName, VarFix.CleanFileName(DatFileLoader.GnRest())); DatFileLoader.Gn(); break;
                        case "description:": tDat.AddData(RvDat.DatData.Description , DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                        case "version:": tDat.AddData(RvDat.DatData.Version , DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                        case "date:": tDat.AddData(RvDat.DatData.Date , DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                        case "author:": tDat.AddData(RvDat.DatData.Author , DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                        case "homepage:": tDat.AddData(RvDat.DatData.HomePage , DatFileLoader.GnRest()); DatFileLoader.Gn(); break;

                        case "comment:": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                        default:
                            DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + Resources.DatCmpReader_LoadHeaderFromDat_not_known_in_clrmamepro, DatFileLoader.Filename);
                            DatFileLoader.Gn();
                            break;
                    }
                }
            }

            thisFileType = FileType.ZipFile;
            tDir.Dat = tDat;
            return true;

        }




        private static bool LoadGameFromDat(ref RvDir tDat, FileType thisFileType)
        {
            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_not_found_after_game, DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();

            string snext = DatFileLoader.Next.ToLower();

            if (snext != "name")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_Name_not_found_as_first_object_in, DatFileLoader.Filename);
                return false;
            }

            RvDir parent = tDat;

            string fullname = VarFix.CleanFullFileName(DatFileLoader.GnRest());
            if (_cleanFileNames)
                fullname = fullname.Replace("/", "-");

            while (fullname.Contains("/"))
            {
                int firstSlash = fullname.IndexOf("/", StringComparison.Ordinal);
                string dir = fullname.Substring(0, firstSlash);
                fullname = fullname.Substring(firstSlash + 1);
                int index;
                if (parent.ChildNameSearch(new RvDir(FileType.Dir) { Name = dir }, out index) == 0)
                {
                    parent = (RvDir)parent.Child(index);
                }
                else
                {
                    RvDir tpDir = new RvDir(FileType.Dir)
                    {
                        Name = dir,
                        DatStatus = DatStatus.InDatCollect,
                        Dat = tDat.Dat,
                        Tree = new RvTreeRow()
                    };
                    parent.ChildAdd(tpDir, index);
                    parent = tpDir;
                }
            }

            if (fullname.Length > 4 && fullname.ToLower().Substring(fullname.Length - 4, 4) == ".zip")
                fullname = fullname.Substring(0, fullname.Length - 4);

            RvDir tDir = new RvDir(thisFileType == FileType.File ? FileType.Dir : FileType.Zip) { Name = fullname };

            DatFileLoader.Gn();
            tDir.Game = new RvGame();
            tDir.DatStatus = DatStatus.InDatCollect;
            tDir.Dat = tDat.Dat;
            int index1;

            string testName = tDir.Name;
            int nameCount = 0;
            while (parent.ChildNameSearch(tDir, out index1) == 0)
            {
                tDir.Name = testName + "_" + nameCount;
                nameCount++;
            }
            parent.ChildAdd(tDir, index1);


            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "file":
                        DatFileLoader.Gn();
                        if (!LoadRomFromDat(ref tDir, thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + Resources.DatCmpReader_LoadGameFromDat_not_known_in_game, DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }


            return true;
        }

        private static bool LoadRomFromDat(ref RvDir tGame, FileType thisFileType)
        {

            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadRomFromDat_not_found_after_rom, DatFileLoader.Filename);
                return false;
            }
            string line=DatFileLoader.GnRest();
            string linelc = line.ToLower();

            int posName = linelc.IndexOf("name ", StringComparison.Ordinal);
            int posSize = linelc.IndexOf(" size ", posName+5,StringComparison.Ordinal);
            int posDate = linelc.IndexOf(" date ", posSize+6,StringComparison.Ordinal);
            int posCrc = linelc.IndexOf(" crc ", posDate+6,StringComparison.Ordinal);
            int posEnd = linelc.IndexOf(" )", posCrc+5,StringComparison.Ordinal);

            if (posName < 0 || posSize < 0 || posDate < 0 || posCrc < 0 || posEnd < 0)
            {
                DatFileLoader.Gn();
                return false;
            }

            string name = line.Substring(posName + 5, posSize - (posName + 5));
            string size = line.Substring(posSize + 6, posDate - (posSize + 6));
            //string date = line.Substring(posDate + 6, posCrc - (posDate + 6));
            string crc = line.Substring(posCrc + 5, posEnd - (posCrc + 5));

            RvFile tRom = new RvFile(thisFileType)
                              {
                                  Dat = tGame.Dat, 
                                  Name = VarFix.CleanFullFileName(name.Trim()), 
                                  Size = VarFix.ULong(size.Trim()), 
                                  CRC = VarFix.CleanMD5SHA1(crc.Trim(), 8)
                              };


            if (tRom.Size != null) tRom.FileStatusSet(FileStatus.SizeFromDAT);
            if (tRom.CRC != null) tRom.FileStatusSet(FileStatus.CRCFromDAT);

            tGame.ChildAdd(tRom);

            return true;
        }



        private static class DatFileLoader
        {
            public static String Filename { get; private set; }
            private static Stream _fileStream;
            private static StreamReader _streamReader;
            private static string _line = "";
            public static string Next;

            public static int LoadDat(string strFilename)
            {
                Filename = strFilename;
                _streamReader = null;
                int errorCode = IO.FileStream.OpenFileRead(strFilename, out _fileStream);
                if (errorCode != 0)
                    return errorCode;
                _streamReader = new StreamReader(_fileStream, Program.Enc);
                return 0;
            }
            public static void Close()
            {
                _streamReader.Close();
                _fileStream.Close();
                _streamReader.Dispose();
                _fileStream.Dispose();
            }

            public static bool EndOfStream()
            {
                return _streamReader.EndOfStream;
            }

            public static string GnRest()
            {
                string strret = _line.Replace("\"", "");
                _line = "";
                Next = strret;
                return strret;
            }

            public static void Gn()
            {
                string ret;
                while ((_line.Trim().Length == 0) && (!_streamReader.EndOfStream))
                {
                    _line = _streamReader.ReadLine();
                    if (String.IsNullOrEmpty(_line)) _line = "";
                    _line = _line.Replace("" + (char)9, " ");
                    _line = _line.Trim() + " ";
                }

                if (_line.Trim().Length > 0)
                {
                    int intS;
                    if (_line.Substring(0, 1) == "\"")
                    {
                        intS = (_line + "\"").IndexOf("\"", 1, StringComparison.Ordinal);
                        ret = _line.Substring(1, intS - 1);
                        _line = (_line + " ").Substring(intS + 1).Trim();
                    }
                    else
                    {
                        intS = (_line + " ").IndexOf(" ", StringComparison.Ordinal);
                        ret = _line.Substring(0, intS);
                        _line = (_line + " ").Substring(intS).Trim();
                    }
                }
                else
                {
                    ret = "";
                }

                Next = ret;
            }
        }

    }
}
