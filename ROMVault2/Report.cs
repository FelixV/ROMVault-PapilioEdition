/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2
{
    static class Report
    {
        private static StreamWriter _ts;
        private static RvDat _tDat;
        private static string _outdir;

        private static string Etxt(string e)
        {
            string ret = e;
            ret = ret.Replace("&", "&amp;");
            ret = ret.Replace("\"", "&quot;");
            ret = ret.Replace("'", "&apos;");
            ret = ret.Replace("<", "&lt;");
            ret = ret.Replace(">", "&gt;");

            return ret;
        }

        public static void MakeFixFiles()
        {
            _tDat = null;
            _ts = null;

            FolderBrowserDialog browse = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = @"Please select a folder for Dats",
                RootFolder = (Settings.IsMono ? Environment.SpecialFolder.MyComputer : Environment.SpecialFolder.DesktopDirectory),
                SelectedPath = @"apps"
            };

            if (browse.ShowDialog() != DialogResult.OK) return;

            _outdir = browse.SelectedPath;
            _tDat = null;
            MakeFixFilesRecurse(DB.DirTree.Child(0), true);

            if (_ts == null) return;

            _ts.WriteLine("</datafile>");
            _ts.Close();
        }

        private static void MakeFixFilesRecurse(RvBase b, bool selected)
        {
            if (selected)
            {
                if (b.Dat != null)
                {
                    RvDir tDir = b as RvDir;
                    if (tDir != null && tDir.Game != null && tDir.DirStatus.HasMissing())
                    {
                        if (_tDat != b.Dat)
                        {
                            if (_tDat != null)
                            {
                                _ts.WriteLine("</datafile>");
                                _ts.WriteLine();
                            }

                            if (_ts != null) _ts.Close();

                            _tDat = b.Dat;
                            int test = 0;
                            string datFilename = Path.Combine(_outdir, "fixDat_" + Path.GetFileNameWithoutExtension(_tDat.GetData(RvDat.DatData.DatFullName)) + ".dat");
                            while (File.Exists(datFilename))
                            {
                                test++;
                                datFilename = Path.Combine(_outdir, "fixDat_" + Path.GetFileNameWithoutExtension(_tDat.GetData(RvDat.DatData.DatFullName)) + "(" + test + ").dat");
                            }
                            _ts = new StreamWriter(datFilename);

                            _ts.WriteLine("<?xml version=\"1.0\"?>");
                            _ts.WriteLine(
                                "<!DOCTYPE datafile PUBLIC \"-//Logiqx//DTD ROM Management Datafile//EN\" \"http://www.logiqx.com/Dats/datafile.dtd\">");
                            _ts.WriteLine("");
                            _ts.WriteLine("<datafile>");
                            _ts.WriteLine("\t<header>");
                            _ts.WriteLine("\t\t<name>fix_" + Etxt(_tDat.GetData(RvDat.DatData.DatName)) + "</name>");
                            if (_tDat.GetData(RvDat.DatData.SuperDat) == "superdat")
                                _ts.WriteLine("\t\t<type>SuperDAT</type>");
                            _ts.WriteLine("\t\t<description>fix_" + Etxt(_tDat.GetData(RvDat.DatData.Description)) + "</description>");
                            _ts.WriteLine("\t\t<category>FIXDATFILE</category>");
                            _ts.WriteLine("\t\t<version>" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + "</version>");
                            _ts.WriteLine("\t\t<date>" + DateTime.Now.ToString("MM/dd/yyyy") + "</date>");
                            _ts.WriteLine("\t\t<author>ROMVault</author>");
                            _ts.WriteLine("\t</header>");
                        }

                        _ts.WriteLine("\t<game name=\"" + Etxt(tDir.SuperDatFileName()) + "\">");
                        if (!string.IsNullOrEmpty(tDir.Game.GetData(RvGame.GameData.Description)))
                            _ts.WriteLine("\t\t<description>" + Etxt(tDir.Game.GetData(RvGame.GameData.Description)) + "</description>");

                    }

                    RvFile tRom = b as RvFile;
                    if (tRom != null)
                    {

                        if (tRom.DatStatus == DatStatus.InDatCollect && tRom.GotStatus != GotStatus.Got)
                        {
                            string strRom;
                            if (tRom.FileStatusIs(FileStatus.SHA1CHDFromDAT | FileStatus.MD5CHDFromDAT))
                                strRom = "\t\t<disk name=\"" + Etxt(tRom.Name) + "\"";
                            else
                                strRom = "\t\t<rom name=\"" + Etxt(tRom.Name) + "\"";

                            if (tRom.FileStatusIs(FileStatus.SizeFromDAT) && tRom.Size != null)
                                strRom += " size=\"" + tRom.Size + "\"";

                            string strCRC = ArrByte.ToString(tRom.CRC);
                            if (tRom.FileStatusIs(FileStatus.CRCFromDAT) && !string.IsNullOrEmpty(strCRC))
                                strRom += " crc=\"" + strCRC + "\"";

                            string strSHA1 = ArrByte.ToString(tRom.SHA1);
                            if (tRom.FileStatusIs(FileStatus.SHA1FromDAT) && !string.IsNullOrEmpty(strSHA1))
                                strRom += " sha1=\"" + strSHA1 + "\"";

                            string strMD5 = ArrByte.ToString(tRom.MD5);
                            if (tRom.FileStatusIs(FileStatus.MD5FromDAT) && !string.IsNullOrEmpty(strMD5))
                                strRom += " md5=\"" + strMD5 + "\"";

                            string strSHA1CHD = ArrByte.ToString(tRom.SHA1CHD);
                            if (tRom.FileStatusIs(FileStatus.SHA1CHDFromDAT) && !string.IsNullOrEmpty(strSHA1CHD))
                                strRom += " sha1=\"" + strSHA1CHD + "\"";

                            string strMD5CHD = ArrByte.ToString(tRom.MD5CHD);
                            if (tRom.FileStatusIs(FileStatus.MD5CHDFromDAT) && !string.IsNullOrEmpty(strMD5CHD))
                                strRom += " md5=\"" + strMD5CHD + "\"";

                            strRom += "/>";

                            _ts.WriteLine(strRom);
                        }
                    }
                }
            }

            RvDir d = b as RvDir;
            if (d != null)
            {
                for (int i = 0; i < d.ChildCount; i++)
                {
                    bool nextSelected = selected;
                    if (d.Tree != null)
                        nextSelected = d.Tree.Checked == RvTreeRow.TreeSelect.Selected;
                    MakeFixFilesRecurse(d.Child(i), nextSelected);
                }
            }

            if (selected)
            {
                if (b.Dat != null)
                {
                    RvDir tDir = b as RvDir;
                    if (tDir != null && tDir.Game != null && tDir.DirStatus.HasMissing())
                    {
                        _ts.WriteLine("\t</game>");
                    }
                }
            }

        }

        private enum ReportType
        {
            Complete,
            CompletelyMissing,
            PartialMissing,
            Fixing
        }

        private static string CleanTime()
        {
            return " (" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ")";
        }

        public static void GenerateReport()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Title = @"Generate Full Report",
                FileName = @"RVFullReport"+CleanTime()+".txt",
                Filter = @"Rom Vault Report (*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _ts = new StreamWriter(saveFileDialog1.FileName);

                _ts.WriteLine("Complete DAT Sets");
                _ts.WriteLine("-----------------------------------------");
                FindAllDats(DB.DirTree.Child(0), ReportType.Complete);
                _ts.WriteLine("");
                _ts.WriteLine("");
                _ts.WriteLine("Empty DAT Sets");
                _ts.WriteLine("-----------------------------------------");
                FindAllDats(DB.DirTree.Child(0), ReportType.CompletelyMissing);
                _ts.WriteLine("");
                _ts.WriteLine("");
                _ts.WriteLine("Partial DAT Sets - (Listing Missing ROMs)");
                _ts.WriteLine("-----------------------------------------");
                FindAllDats(DB.DirTree.Child(0), ReportType.PartialMissing);
                _ts.Close();
            }
        }

        public static void GenerateFixReport()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Title = @"Generate Fix Report",
                FileName = @"RVFixReport"+CleanTime()+".txt",
                Filter = @"Rom Vault Fixing Report (*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _ts = new StreamWriter(saveFileDialog1.FileName);

                _ts.WriteLine("Listing Fixes");
                _ts.WriteLine("-----------------------------------------");
                FindAllDats(DB.DirTree.Child(0), ReportType.Fixing);
                _ts.Close();
            }
        }

        private static void FindAllDats(RvBase b, ReportType rt)
        {
            RvDir d = b as RvDir;
            if (d == null) return;
            if (d.DirDatCount > 0)
            {
                for (int i = 0; i < d.DirDatCount; i++)
                {
                    RvDat dat = d.DirDat(i);

                    int correct = 0;
                    int missing = 0;
                    int fixesNeeded = 0;

                    if (d.Dat == dat)
                    {
                        correct += d.DirStatus.CountCorrect();
                        missing += d.DirStatus.CountMissing();
                        fixesNeeded += d.DirStatus.CountFixesNeeded();
                    }
                    else
                    {
                        for (int j = 0; j < d.ChildCount; j++)
                        {
                            RvDir c = d.Child(j) as RvDir;

                            if (c == null || c.Dat != dat) continue;

                            correct += c.DirStatus.CountCorrect();
                            missing += c.DirStatus.CountMissing();
                            fixesNeeded += c.DirStatus.CountFixesNeeded();

                        }
                    }

                    switch (rt)
                    {
                        case ReportType.Complete:
                            if (correct > 0 && missing == 0 && fixesNeeded == 0)
                                _ts.WriteLine(RemoveBase(dat.GetData(RvDat.DatData.DatFullName)));
                            break;
                        case ReportType.CompletelyMissing:
                            if (correct == 0 && missing > 0 && fixesNeeded == 0)
                                _ts.WriteLine(RemoveBase(dat.GetData(RvDat.DatData.DatFullName)));
                            break;
                        case ReportType.PartialMissing:
                            if ((correct > 0 && missing > 0) || fixesNeeded > 0)
                            {
                                _ts.WriteLine(RemoveBase(dat.GetData(RvDat.DatData.DatFullName)));
                                _fileNameLength = 0;
                                _fileSizeLength = 0;
                                _repStatusLength = 0;
                                ReportMissingFindSizes(d, dat, rt);
                                ReportDrawBars();
                                ReportMissing(d, dat, rt);
                                ReportDrawBars();
                                _ts.WriteLine();
                            }
                            break;
                        case ReportType.Fixing:
                            if (fixesNeeded > 0)
                            {
                                _ts.WriteLine(RemoveBase(dat.GetData(RvDat.DatData.DatFullName)));
                                _fileNameLength = 0;
                                _fileSizeLength = 0;
                                _repStatusLength = 0;
                                ReportMissingFindSizes(d, dat, rt);
                                ReportDrawBars();
                                ReportMissing(d, dat, rt);
                                ReportDrawBars();
                                _ts.WriteLine();
                            }
                            break;
                    }
                }
            }

            if (b.Dat != null) return;

            for (int i = 0; i < d.ChildCount; i++)
                FindAllDats(d.Child(i), rt);
        }

        private static string RemoveBase(string name)
        {
            int p = name.IndexOf("\\", StringComparison.Ordinal);
            return p > 0 ? name.Substring(p + 1) : name;
        }

        private static int _fileNameLength;
        private static int _fileSizeLength;
        private static int _repStatusLength;

        private static readonly RepStatus[] Partial =
        {
            RepStatus.UnScanned,
            RepStatus.Missing,
            RepStatus.Corrupt,
            RepStatus.CanBeFixed,
            RepStatus.CorruptCanBeFixed,
        };

        private static readonly RepStatus[] Fixing =
        {
            RepStatus.CanBeFixed,
            RepStatus.MoveUnsorted,
            RepStatus.Delete,
            RepStatus.NeededForFix,
            RepStatus.Rename,
            RepStatus.CorruptCanBeFixed,
            RepStatus.MoveToCorrupt
        };


        private static void ReportMissingFindSizes(RvDir dir, RvDat dat, ReportType rt)
        {
            for (int i = 0; i < dir.ChildCount; i++)
            {
                RvBase b = dir.Child(i);
                if (b.Dat != null && b.Dat != dat)
                    continue;

                RvFile f = b as RvFile;

                if (f != null)
                {
                    if (
                        (rt == ReportType.PartialMissing && Partial.Contains(f.RepStatus)) ||
                        (rt == ReportType.Fixing && Fixing.Contains(f.RepStatus))
                        )
                    {
                        int fileNameLength = f.FileNameInsideGame().Length;
                        int fileSizeLength = f.Size.ToString().Length;
                        int repStatusLength = f.RepStatus.ToString().Length;

                        if (fileNameLength > _fileNameLength) _fileNameLength = fileNameLength;
                        if (fileSizeLength > _fileSizeLength) _fileSizeLength = fileSizeLength;
                        if (repStatusLength > _repStatusLength) _repStatusLength = repStatusLength;
                    }
                }
                RvDir d = b as RvDir;
                if (d != null)
                    ReportMissingFindSizes(d, dat, rt);
            }

        }

        private static void ReportDrawBars()
        {
            _ts.WriteLine("+" + new string('-', _fileNameLength + 2) + "+" + new string('-', _fileSizeLength + 2) + "+----------+" + new string('-', _repStatusLength + 2) + "+");
        }

        private static void ReportMissing(RvDir dir, RvDat dat, ReportType rt)
        {
            for (int i = 0; i < dir.ChildCount; i++)
            {
                RvBase b = dir.Child(i);
                if (b.Dat != null && b.Dat != dat)
                    continue;

                RvFile f = b as RvFile;

                if (f != null)
                {
                    if (
                       (rt == ReportType.PartialMissing && Partial.Contains(f.RepStatus)) ||
                       (rt == ReportType.Fixing && Fixing.Contains(f.RepStatus))
                       )
                    {
                        string filename = f.FileNameInsideGame();
                        string crc = ArrByte.ToString(f.CRC);
                        _ts.WriteLine("| " + filename + new string(' ', _fileNameLength + 1 - filename.Length) + "| "
                                          + f.Size + new string(' ', _fileSizeLength + 1 - f.Size.ToString().Length) + "| "
                                          + crc + new string(' ', 9 - crc.Length) + "| "
                                          + f.RepStatus + new string(' ', _repStatusLength + 1 - f.RepStatus.ToString().Length) + "|");
                    }
                }
                RvDir d = b as RvDir;
                if (d != null)
                    ReportMissing(d, dat, rt);
            }

        }

    }
}
