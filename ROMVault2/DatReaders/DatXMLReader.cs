/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Xml;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2.DatReaders
{
    public static class DatXmlReader
    {
        private static bool _cleanFileNames = true;

        public static bool ReadDat(ref RvDir tDat, XmlDocument doc)
        {
            FileType thisFileType = FileType.Unknown;

            if (!LoadHeaderFromDat(ref tDat, ref doc, ref thisFileType))
                return false;

            if (doc.DocumentElement == null)
                return false;

            XmlNodeList dirNodeList = doc.DocumentElement.SelectNodes("dir");
            if (dirNodeList != null)
            {
                for (int i = 0; i < dirNodeList.Count; i++)
                {
                    LoadDirFromDat(ref tDat, dirNodeList[i], thisFileType);
                }
            }

            XmlNodeList gameNodeList = doc.DocumentElement.SelectNodes("game");

            if (gameNodeList != null)
            {
                for (int i = 0; i < gameNodeList.Count; i++)
                {
                    LoadGameFromDat(ref tDat, gameNodeList[i], thisFileType);
                }
            }

            return true;
        }

        public static bool ReadMameDat(ref RvDir tDat, XmlDocument doc)
        {
            FileType thisFileType = FileType.Unknown;

            if (!LoadMameHeaderFromDat(ref tDat, ref doc, ref thisFileType))
                return false;

            if (doc.DocumentElement == null)
                return false;

            XmlNodeList dirNodeList = doc.DocumentElement.SelectNodes("dir");
            if (dirNodeList != null)
            {
                for (int i = 0; i < dirNodeList.Count; i++)
                {
                    LoadDirFromDat(ref tDat, dirNodeList[i], thisFileType);
                }
            }

            XmlNodeList gameNodeList = doc.DocumentElement.SelectNodes("game");

            if (gameNodeList != null)
            {
                for (int i = 0; i < gameNodeList.Count; i++)
                {
                    LoadGameFromDat(ref tDat, gameNodeList[i], thisFileType);
                }
            }

            return true;
        }



        private static bool LoadHeaderFromDat(ref RvDir tDir, ref XmlDocument doc, ref FileType thisFileType)
        {
            if (doc.DocumentElement == null)
                return false;
            XmlNode head = doc.DocumentElement.SelectSingleNode("header");

            if (head == null)
                return false;
            RvDat tDat = new RvDat();
            tDat.AddData(RvDat.DatData.DatName, VarFix.CleanFileName(head.SelectSingleNode("name")));
            tDat.AddData(RvDat.DatData.RootDir, VarFix.CleanFileName(head.SelectSingleNode("rootdir")));
            tDat.AddData(RvDat.DatData.Description, VarFix.String(head.SelectSingleNode("description")));
            tDat.AddData(RvDat.DatData.Category, VarFix.String(head.SelectSingleNode("category")));
            tDat.AddData(RvDat.DatData.Version, VarFix.String(head.SelectSingleNode("version")));
            tDat.AddData(RvDat.DatData.Date, VarFix.String(head.SelectSingleNode("date")));
            tDat.AddData(RvDat.DatData.Author, VarFix.String(head.SelectSingleNode("author")));
            tDat.AddData(RvDat.DatData.Email, VarFix.String(head.SelectSingleNode("email")));
            tDat.AddData(RvDat.DatData.HomePage, VarFix.String(head.SelectSingleNode("homepage")));
            tDat.AddData(RvDat.DatData.URL, VarFix.String(head.SelectSingleNode("url")));


            string superDAT = VarFix.String(head.SelectSingleNode("type"));
            _cleanFileNames = superDAT.ToLower() != "superdat" && superDAT.ToLower() != "gigadat";
            if (!_cleanFileNames) tDat.AddData(RvDat.DatData.SuperDat, "superdat");

            thisFileType = FileType.ZipFile;

            // Look for:   <ROMVault forcepacking="unzip"/>
            XmlNode packingNode = head.SelectSingleNode("ROMVault");
            if (packingNode == null)
                // Look for:   <clrmamepro forcepacking="unzip"/>
                packingNode = head.SelectSingleNode("clrmamepro");
            if (packingNode != null)
            {
                if (packingNode.Attributes != null)
                {
                    string val = VarFix.String(packingNode.Attributes.GetNamedItem("forcepacking")).ToLower();
                    switch (val.ToLower())
                    {
                        case "zip":
                            tDat.AddData(RvDat.DatData.FileType, "zip");
                            thisFileType = FileType.ZipFile;
                            break;
                        case "unzip":
                        case "file":
                            tDat.AddData(RvDat.DatData.FileType, "file");
                            thisFileType = FileType.File;
                            break;
                        default:
                            thisFileType = FileType.ZipFile;
                            break;
                    }

                    val = VarFix.String(packingNode.Attributes.GetNamedItem("forcemerging")).ToLower();
                    switch (val.ToLower())
                    {
                        case "split":
                            tDat.AddData(RvDat.DatData.MergeType, "split");
                            break;
                        case "full":
                            tDat.AddData(RvDat.DatData.MergeType, "full");
                            break;
                        default:
                            tDat.AddData(RvDat.DatData.MergeType, "split");
                            break;
                    }
                    val = VarFix.String(packingNode.Attributes.GetNamedItem("dir")).ToLower(); // noautodir , nogame
                    if (!String.IsNullOrEmpty(val))
                        tDat.AddData(RvDat.DatData.DirSetup,val);
                }
            }

            // Look for: <notzipped>true</notzipped>
            string notzipped = VarFix.String(head.SelectSingleNode("notzipped"));
            if (notzipped.ToLower() == "true" || notzipped.ToLower() == "yes") thisFileType = FileType.File;

            tDir.Dat = tDat;
            return true;
        }

        private static bool LoadMameHeaderFromDat(ref RvDir tDir, ref XmlDocument doc, ref FileType thisFileType)
        {
            if (doc.DocumentElement == null)
                return false;
            XmlNode head = doc.SelectSingleNode("mame");

            if (head == null || head.Attributes == null)
                return false;
            RvDat tDat = new RvDat();
            tDat.AddData(RvDat.DatData.DatName, VarFix.CleanFileName(head.Attributes.GetNamedItem("build")));
            tDat.AddData(RvDat.DatData.Description, VarFix.String(head.Attributes.GetNamedItem("build")));

            thisFileType = FileType.ZipFile;
            tDir.Dat = tDat;
            return true;
        }


        private static void LoadDirFromDat(ref RvDir tDat, XmlNode dirNode, FileType thisFileType)
        {
            if (dirNode.Attributes == null)
                return;

            RvDir parent = tDat;

            string fullname = VarFix.CleanFullFileName(dirNode.Attributes.GetNamedItem("name"));
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
                                 Name = fullname,
                                 DatStatus = DatStatus.InDatCollect,
                                 Dat = tDat.Dat,
                                 Tree = new RvTreeRow()
                             };

            int index1;
            if (parent.ChildNameSearch(tDir, out index1) == 0)
                tDir = (RvDir)parent.Child(index1);
            else
                tDat.ChildAdd(tDir, index1);

            XmlNodeList dirNodeList = dirNode.SelectNodes("dir");
            if (dirNodeList != null)
            {
                for (int i = 0; i < dirNodeList.Count; i++)
                {
                    LoadDirFromDat(ref tDir, dirNodeList[i], thisFileType);
                }
            }

            XmlNodeList gameNodeList = dirNode.SelectNodes("game");
            if (gameNodeList != null)
            {
                for (int i = 0; i < gameNodeList.Count; i++)
                {
                    LoadGameFromDat(ref tDir, gameNodeList[i], thisFileType);
                }
            }
        }

        private static void LoadGameFromDat(ref RvDir tDat, XmlNode gameNode, FileType thisFileType)
        {
            if (gameNode.Attributes == null)
                return;

            RvDir parent = tDat;
            RvDir tDir;
            int index1 = 0;

            string fullname = VarFix.CleanFullFileName(gameNode.Attributes.GetNamedItem("name"));
            if (_cleanFileNames)
                fullname = fullname.Replace("/", "-");
            else
            {
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
            }

            tDir = new RvDir(DBTypeGet.DirFromFile(thisFileType))
                       {
                           Name = fullname,
                           DatStatus = DatStatus.InDatCollect,
                           Dat = tDat.Dat
                       };

            string testName = tDir.Name;
            int nameCount = 0;
            while (parent.ChildNameSearch(tDir, out index1) == 0)
            {
                tDir.Name = testName + "_" + nameCount;
                nameCount++;
            }

            tDir.Game = new RvGame();
            tDir.Game.AddData(RvGame.GameData.RomOf, VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("romof")));
            tDir.Game.AddData(RvGame.GameData.Description, VarFix.String(gameNode.SelectSingleNode("description")));

            tDir.Game.AddData(RvGame.GameData.Sourcefile, VarFix.String(gameNode.Attributes.GetNamedItem("sourcefile")));
            tDir.Game.AddData(RvGame.GameData.IsBios, VarFix.String(gameNode.Attributes.GetNamedItem("isbios")));
            tDir.Game.AddData(RvGame.GameData.CloneOf, VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("cloneof")));
            tDir.Game.AddData(RvGame.GameData.SampleOf, VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("sampleof")));
            tDir.Game.AddData(RvGame.GameData.Board, VarFix.String(gameNode.Attributes.GetNamedItem("board")));
            tDir.Game.AddData(RvGame.GameData.Year, VarFix.String(gameNode.SelectSingleNode("year")));
            tDir.Game.AddData(RvGame.GameData.Manufacturer, VarFix.String(gameNode.SelectSingleNode("manufacturer")));
            tDir.Game.AddData(RvGame.GameData.GameGenre, VarFix.CleanFileName(gameNode.SelectSingleNode("genre")));
            
            // read papilio nodes here
            XmlNode papilio = gameNode.SelectSingleNode("papilio");
            
            if (papilio != null)
            {
                if (VarFix.String(papilio.SelectSingleNode("hardware")).Length > 0)
                {
                    tDir.Game.AddData(RvGame.GameData.Papilio, "yes");
                    tDir.Game.AddData(RvGame.GameData.papilioHardware, VarFix.String(papilio.SelectSingleNode("hardware")));
                    tDir.Game.AddData(RvGame.GameData.papilioScript, VarFix.String(papilio.SelectSingleNode("pscript")));
                    tDir.Game.AddData(RvGame.GameData.papilioNote, VarFix.String(papilio.SelectSingleNode("note")));
                } else {
                    tDir.Game.AddData(RvGame.GameData.Papilio, "no");
                    tDir.Game.AddData(RvGame.GameData.papilioHardware, "?");
                    tDir.Game.AddData(RvGame.GameData.papilioScript, "?");
                    tDir.Game.AddData(RvGame.GameData.papilioNote, "?");
                }
            }
            else
            {
                // no papilio stanza in the xml
                tDir.Game.AddData(RvGame.GameData.Papilio, "no");
            }

            /*
            XmlNode trurip = gameNode.SelectSingleNode("trurip");
            
            if (trurip != null)
            {
                tDir.Game.AddData(RvGame.GameData.Trurip, "yes");
                tDir.Game.AddData(RvGame.GameData.Year, VarFix.String(trurip.SelectSingleNode("year")));
                tDir.Game.AddData(RvGame.GameData.Publisher, VarFix.String(trurip.SelectSingleNode("publisher")));
                tDir.Game.AddData(RvGame.GameData.Developer, VarFix.String(trurip.SelectSingleNode("developer")));
                tDir.Game.AddData(RvGame.GameData.Edition, VarFix.String(trurip.SelectSingleNode("edition")));
                tDir.Game.AddData(RvGame.GameData.Version, VarFix.String(trurip.SelectSingleNode("version")));
                tDir.Game.AddData(RvGame.GameData.Type, VarFix.String(trurip.SelectSingleNode("type")));
                tDir.Game.AddData(RvGame.GameData.Media, VarFix.String(trurip.SelectSingleNode("media")));
                tDir.Game.AddData(RvGame.GameData.Language, VarFix.String(trurip.SelectSingleNode("language")));
                tDir.Game.AddData(RvGame.GameData.Players, VarFix.String(trurip.SelectSingleNode("players")));
                tDir.Game.AddData(RvGame.GameData.Ratings, VarFix.String(trurip.SelectSingleNode("ratings")));
                tDir.Game.AddData(RvGame.GameData.Peripheral, VarFix.String(trurip.SelectSingleNode("peripheral")));
                tDir.Game.AddData(RvGame.GameData.Genre, VarFix.String(trurip.SelectSingleNode("genre")));
                tDir.Game.AddData(RvGame.GameData.MediaCatalogNumber, VarFix.String(trurip.SelectSingleNode("mediacatalognumber")));
                tDir.Game.AddData(RvGame.GameData.BarCode, VarFix.String(trurip.SelectSingleNode("barcode")));
            }
            */
            RvDir tDirCHD = new RvDir(FileType.Dir)
                                {
                                    Name = tDir.Name,
                                    DatStatus = tDir.DatStatus,
                                    Dat = tDir.Dat,
                                    Game = tDir.Game
                                };

            XmlNodeList romNodeList = gameNode.SelectNodes("rom");
            if (romNodeList != null)
                for (int i = 0; i < romNodeList.Count; i++)
                    LoadRomFromDat(ref tDir, romNodeList[i], thisFileType);

            // CHD stuff (disabled)
            /*
            XmlNodeList diskNodeList = gameNode.SelectNodes("disk");
            if (diskNodeList != null)
                for (int i = 0; i < diskNodeList.Count; i++)
                    LoadDiskFromDat(ref tDirCHD, diskNodeList[i]);
            */

            if (tDir.ChildCount > 0)
                parent.ChildAdd(tDir, index1);
            if (tDirCHD.ChildCount > 0)
                parent.ChildAdd(tDirCHD);
        }

        private static void LoadRomFromDat(ref RvDir tGame, XmlNode romNode, FileType thisFileType)
        {
            if (romNode.Attributes == null)
                return;


            RvFile tRom = new RvFile(thisFileType)
                              {
                                  Dat = tGame.Dat,
                                  Size = VarFix.ULong(romNode.Attributes.GetNamedItem("size")),
                                  Name = VarFix.CleanFullFileName(romNode.Attributes.GetNamedItem("name")),
                                  CRC = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("crc"), 8),
                                  SHA1 = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("sha1"), 40),
                                  MD5 = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("md5"), 32),
                                  Merge = VarFix.CleanFullFileName(romNode.Attributes.GetNamedItem("merge")),
                                  Status = VarFix.ToLower(romNode.Attributes.GetNamedItem("status"))
                              };

            if (tRom.Size != null) tRom.FileStatusSet(FileStatus.SizeFromDAT);
            if (tRom.CRC != null) tRom.FileStatusSet(FileStatus.CRCFromDAT);
            if (tRom.SHA1 != null) tRom.FileStatusSet(FileStatus.SHA1FromDAT);
            if (tRom.MD5 != null) tRom.FileStatusSet(FileStatus.MD5FromDAT);

            tGame.ChildAdd(tRom);
        }

        private static void LoadDiskFromDat(ref RvDir tGame, XmlNode romNode)
        {
            if (romNode.Attributes == null)
                return;


            RvFile tRom = new RvFile(FileType.File)
            {
                Dat = tGame.Dat,
                Name = VarFix.CleanFullFileName(romNode.Attributes.GetNamedItem("name")) + ".chd",
                SHA1CHD = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("sha1"), 40),
                MD5CHD = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("md5"), 32),
                Merge = VarFix.CleanFullFileName(romNode.Attributes.GetNamedItem("merge")),
                Status = VarFix.ToLower(romNode.Attributes.GetNamedItem("status"))
            };

            if (tRom.SHA1CHD != null) tRom.FileStatusSet(FileStatus.SHA1CHDFromDAT);
            if (tRom.MD5CHD != null) tRom.FileStatusSet(FileStatus.MD5CHDFromDAT);

            tGame.ChildAdd(tRom);
        }

    }
}
