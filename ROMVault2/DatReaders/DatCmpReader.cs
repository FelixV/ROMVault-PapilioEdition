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
    public static class DatCmpReader
    {
        private static bool _cleanFileNames = true;


        public static bool ReadDat(ref RvDir tDat, string strFilename)
        {
            RvDir tNow = tDat;
            FileType thisFileType = FileType.ZipFile;

            int errorCode = DatFileLoader.LoadDat(strFilename);
            if (errorCode != 0)
            {
                DatUpdate.ShowDat(new Win32Exception(errorCode).Message, strFilename);
                return false;
            }

            DatFileLoader.Gn();
            if (DatFileLoader.EndOfStream())
                return false;
            if (DatFileLoader.Next.ToLower() == "clrmamepro")
            {
                _cleanFileNames = true;
                DatFileLoader.Gn();
                if (!LoadHeaderFromDat(ref tNow, ref thisFileType))
                    return false;
                DatFileLoader.Gn();
            }
            if (DatFileLoader.Next.ToLower() == "ROMVault")
            {
                _cleanFileNames = false;
                DatFileLoader.Gn();
                if (!LoadHeaderFromDat(ref tNow, ref thisFileType))
                    return false;
                DatFileLoader.Gn();
            }

            if (tNow.Dat == null)
            {
                tNow.Dat = new RvDat();
                string cleanedName = IO.Path.GetFileNameWithoutExtension(strFilename);
                tNow.Dat.AddData(RvDat.DatData.DatName, cleanedName);
                tNow.Dat.AddData(RvDat.DatData.Description, cleanedName);
            }


            while (!DatFileLoader.EndOfStream())
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "dir":
                        DatFileLoader.Gn();
                        if (!LoadDirFromDat(ref tNow, ref thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    case "game":
                        DatFileLoader.Gn();
                        if (!LoadGameFromDat(ref tNow, false, thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    case "resource":
                        DatFileLoader.Gn();
                        if (!LoadGameFromDat(ref tNow, true, thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    case "emulator":
                        DatFileLoader.Gn();
                        if (!LoadEmulator())
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

            string forceZipping = "";

            RvDat tDat = new RvDat();
            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "name": tDat.AddData(RvDat.DatData.DatName, VarFix.CleanFileName(DatFileLoader.GnRest())); DatFileLoader.Gn(); break;
                    case "description": tDat.AddData(RvDat.DatData.Description, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "category": tDat.AddData(RvDat.DatData.Category, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "version": tDat.AddData(RvDat.DatData.Version, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "date": tDat.AddData(RvDat.DatData.Date, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "author": tDat.AddData(RvDat.DatData.Author, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "email": tDat.AddData(RvDat.DatData.Email, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "homepage": tDat.AddData(RvDat.DatData.HomePage, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "url": tDat.AddData(RvDat.DatData.URL, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;

                    case "comment": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "header": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "forcezipping": forceZipping = DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "forcepacking": DatFileLoader.GnRest(); DatFileLoader.Gn(); break; // incorrect usage
                    case "forcemerging": tDat.AddData(RvDat.DatData.MergeType, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "forcenodump": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "dir": tDat.AddData(RvDat.DatData.DirSetup, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + Resources.DatCmpReader_LoadHeaderFromDat_not_known_in_clrmamepro, DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }

            thisFileType = forceZipping.ToLower() != "no" ? FileType.ZipFile : FileType.File;
            tDir.Dat = tDat;
            return true;

        }

        private static bool LoadEmulator()
        {
            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat("( not found after emulator", DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();
            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "name": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "version": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                }
            }
            return true;
        }



        private static bool LoadDirFromDat(ref RvDir tDat, ref FileType thisFileType)
        {
            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_not_found_after_game, DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();

            if (DatFileLoader.Next.ToLower() != "name")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_Name_not_found_as_first_object_in, DatFileLoader.Filename);
                return false;
            }

            RvDir parent = tDat;

            string fullname = VarFix.CleanFullFileName(DatFileLoader.GnRest());
            while (fullname.Contains("/"))
            {
                int firstSlash = fullname.IndexOf("/", StringComparison.Ordinal);
                string dir = fullname.Substring(0, firstSlash);
                dir = VarFix.CleanFileName(dir);

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

            RvDir tDir = new RvDir(FileType.Dir)
                             {
                                 Name = fullname
                             };

            DatFileLoader.Gn();
            tDir.DatStatus = DatStatus.InDatCollect;
            tDir.Dat = tDat.Dat;

            int index1;
            if (parent.ChildNameSearch(tDir, out index1) == 0)
                tDir = (RvDir)parent.Child(index1);
            else
                parent.ChildAdd(tDir, index1);

            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "dir":
                        DatFileLoader.Gn();
                        if (!LoadDirFromDat(ref tDir, ref thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    case "game":
                        DatFileLoader.Gn();
                        if (!LoadGameFromDat(ref tDir, false, thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    case "resource":
                        DatFileLoader.Gn();
                        if (!LoadGameFromDat(ref tDir, true, thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadDirFromDat_Error_Keyword + DatFileLoader.Next + Resources.DatCmpReader_LoadDirFromDat_not_know_in_dir, DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }
            return true;
        }

        private static bool LoadGameFromDat(ref RvDir tDat, bool bolresource, FileType thisFileType)
        {
            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_not_found_after_game, DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();

            string snext = DatFileLoader.Next.ToLower();

            string pathextra = "";
            if (snext == "rebuildto")
            {
                pathextra = VarFix.CleanFullFileName(DatFileLoader.Gn()); DatFileLoader.Gn();
                snext = DatFileLoader.Next.ToLower();
            }

            if (snext != "name")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_Name_not_found_as_first_object_in, DatFileLoader.Filename);
                return false;
            }

            RvDir parent = tDat;

            string fullname = VarFix.CleanFullFileName(DatFileLoader.GnRest());
            if (_cleanFileNames)
                fullname = fullname.Replace("/", "-");

            if (!String.IsNullOrEmpty(pathextra))
                fullname = pathextra + "/" + fullname;

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

            RvDir tDir = new RvDir(thisFileType == FileType.File ? FileType.Dir : FileType.Zip)
                             {
                                 Name = fullname,
                                 DatStatus = DatStatus.InDatCollect,
                                 Dat = tDat.Dat
                             };

            int index1;
            string testName = tDir.Name;
            int nameCount = 0;
            while (parent.ChildNameSearch(tDir, out index1) == 0)
            {
                tDir.Name = testName + "_" + nameCount;
                nameCount++;
            }

            DatFileLoader.Gn();
            tDir.Game = new RvGame();
            tDir.Game.AddData(RvGame.GameData.IsBios, bolresource ? "Yes" : "No");

            RvDir tDirCHD = new RvDir(FileType.Dir)
            {
                Name = tDir.Name,
                DatStatus = tDir.DatStatus,
                Dat = tDir.Dat,
                Game = tDir.Game
            };

            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "romof": tDir.Game.AddData(RvGame.GameData.RomOf, VarFix.CleanFileName(DatFileLoader.GnRest())); DatFileLoader.Gn(); break;
                    case "description": tDir.Game.AddData(RvGame.GameData.Description, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;

                    case "sourcefile": tDir.Game.AddData(RvGame.GameData.Sourcefile, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "cloneof": tDir.Game.AddData(RvGame.GameData.CloneOf, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "sampleof": tDir.Game.AddData(RvGame.GameData.SampleOf, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "board": tDir.Game.AddData(RvGame.GameData.Board, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "year": tDir.Game.AddData(RvGame.GameData.Year, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "manufacturer": tDir.Game.AddData(RvGame.GameData.Manufacturer, DatFileLoader.GnRest()); DatFileLoader.Gn(); break;
                    case "serial": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "rebuildto": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;

                    case "sample": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "biosset": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;

                    case "chip": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "video": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "sound": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "input": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "dipswitch": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;
                    case "driver": DatFileLoader.GnRest(); DatFileLoader.Gn(); break;


                    case "rom":
                        DatFileLoader.Gn();
                        if (!LoadRomFromDat(ref tDir, thisFileType))
                            return false;
                        DatFileLoader.Gn();
                        break;
                    case "disk":
                        DatFileLoader.Gn();
                        if (!LoadDiskFromDat(ref tDirCHD))
                            return false;
                        DatFileLoader.Gn();
                        break;

                    case "archive":
                        DatFileLoader.Gn();
                        if (!LoadArchiveFromDat())
                            return false;
                        DatFileLoader.Gn();
                        break;

                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + Resources.DatCmpReader_LoadGameFromDat_not_known_in_game, DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }

            if (tDir.ChildCount > 0)
                parent.ChildAdd(tDir, index1);
            if (tDirCHD.ChildCount > 0)
                parent.ChildAdd(tDirCHD);

            return true;
        }

        private static bool LoadRomFromDat(ref RvDir tGame, FileType thisFileType)
        {

            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadRomFromDat_not_found_after_rom, DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();

            if (DatFileLoader.Next.ToLower() != "name")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_Name_not_found_as_first_object_in, DatFileLoader.Filename);
                return false;
            }


            string filename = VarFix.CleanFullFileName(DatFileLoader.Gn());
            RvFile tRom = new RvFile(thisFileType) { Name = filename };

            DatFileLoader.Gn();
            tRom.Dat = tGame.Dat;

            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "size": tRom.Size = VarFix.ULong(DatFileLoader.Gn()); DatFileLoader.Gn(); break;
                    case "crc": tRom.CRC = VarFix.CleanMD5SHA1(DatFileLoader.Gn(), 8); DatFileLoader.Gn(); break;
                    case "sha1": tRom.SHA1 = VarFix.CleanMD5SHA1(DatFileLoader.Gn(), 40); DatFileLoader.Gn(); break;
                    case "md5": tRom.MD5 = VarFix.CleanMD5SHA1(DatFileLoader.Gn(), 32); DatFileLoader.Gn(); break;
                    case "merge": tRom.Merge = VarFix.CleanFullFileName(DatFileLoader.Gn()); DatFileLoader.Gn(); break;
                    case "flags": tRom.Status = VarFix.ToLower(DatFileLoader.Gn()); DatFileLoader.Gn(); break;
                    case "date": DatFileLoader.Gn(); DatFileLoader.Gn(); break;
                    case "bios": DatFileLoader.Gn(); DatFileLoader.Gn(); break;
                    case "region": DatFileLoader.Gn(); DatFileLoader.Gn(); break;
                    case "offs": DatFileLoader.Gn(); DatFileLoader.Gn(); break;
                    case "nodump": tRom.Status = "nodump"; DatFileLoader.Gn(); break;
                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + Resources.DatCmpReader_LoadRomFromDat_not_known_in_rom, DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }

            if (tRom.Size != null) tRom.FileStatusSet(FileStatus.SizeFromDAT);
            if (tRom.CRC != null) tRom.FileStatusSet(FileStatus.CRCFromDAT);
            if (tRom.SHA1 != null) tRom.FileStatusSet(FileStatus.SHA1FromDAT);
            if (tRom.MD5 != null) tRom.FileStatusSet(FileStatus.MD5FromDAT);

            tGame.ChildAdd(tRom);

            return true;
        }

        private static bool LoadDiskFromDat(ref RvDir tGame)
        {

            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadRomFromDat_not_found_after_rom, DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();

            if (DatFileLoader.Next.ToLower() != "name")
            {
                DatUpdate.SendAndShowDat(Resources.DatCmpReader_LoadGameFromDat_Name_not_found_as_first_object_in, DatFileLoader.Filename);
                return false;
            }


            string filename = VarFix.CleanFullFileName(DatFileLoader.Gn());
            RvFile tRom = new RvFile(FileType.File) { Name = filename };

            DatFileLoader.Gn();
            tRom.Dat = tGame.Dat;

            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "sha1": tRom.SHA1CHD = VarFix.CleanMD5SHA1(DatFileLoader.Gn(), 40); DatFileLoader.Gn(); break;
                    case "md5": tRom.MD5CHD = VarFix.CleanMD5SHA1(DatFileLoader.Gn(), 32); DatFileLoader.Gn(); break;
                    case "merge": tRom.Merge = VarFix.CleanFullFileName(DatFileLoader.Gn()); DatFileLoader.Gn(); break;
                    case "flags": tRom.Status = VarFix.ToLower(DatFileLoader.Gn()); DatFileLoader.Gn(); break;
                    case "nodump": tRom.Status = "nodump"; DatFileLoader.Gn(); break;
                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + Resources.DatCmpReader_LoadRomFromDat_not_known_in_rom, DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }

            if (tRom.SHA1CHD != null) tRom.FileStatusSet(FileStatus.SHA1CHDFromDAT);
            if (tRom.MD5CHD != null) tRom.FileStatusSet(FileStatus.MD5CHDFromDAT);

            tGame.ChildAdd(tRom);

            return true;
        }

        private static bool LoadArchiveFromDat()
        {

            if (DatFileLoader.Next != "(")
            {
                DatUpdate.SendAndShowDat("( not found after Archive", DatFileLoader.Filename);
                return false;
            }
            DatFileLoader.Gn();

            while (DatFileLoader.Next != ")")
            {
                switch (DatFileLoader.Next.ToLower())
                {
                    case "name": DatFileLoader.Gn(); DatFileLoader.Gn(); break;
                    default:
                        DatUpdate.SendAndShowDat(Resources.DatCmpReader_ReadDat_Error_keyword + DatFileLoader.Next + " not know in Archive", DatFileLoader.Filename);
                        DatFileLoader.Gn();
                        break;
                }
            }
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

            public static string Gn()
            {
                string ret;
                while ((_line.Trim().Length == 0) && (!_streamReader.EndOfStream))
                {
                    _line = _streamReader.ReadLine();
                    _line = (_line??"").Replace("" + (char)9, " ");
                    if (_line.TrimStart().Length > 2 && _line.TrimStart().Substring(0, 2) == @"//") _line = "";
                    if (_line.TrimStart().Length > 1 && _line.TrimStart().Substring(0, 1) == @"#") _line = "";
                    if (_line.TrimStart().Length > 1 && _line.TrimStart().Substring(0, 1) == @";") _line = "";
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
                return ret;
            }
        }

    }
}
