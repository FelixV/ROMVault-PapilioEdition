/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2.DatReaders
{
    public static class DatReader
    {
        private static BackgroundWorker _bgw;

        public static RvDir ReadInDatFile(RvDat datFile, BackgroundWorker bgw)
        {
            _bgw = bgw;

            RvDir newDir = new RvDir(FileType.Dir);

            string datFullName = datFile.GetData(RvDat.DatData.DatFullName);

            System.IO.Stream fs;
            int errorCode = IO.FileStream.OpenFileRead(datFullName, out fs);
            if (errorCode != 0)
            {
                _bgw.ReportProgress(0, new bgwShowError(datFullName, errorCode + ": " + new Win32Exception(errorCode).Message));
                return null;
            }

            System.IO.StreamReader myfile = new System.IO.StreamReader(fs, Program.Enc);
            string strLine = myfile.ReadLine();
            myfile.Close();
            fs.Close();
            fs.Dispose();

            if (strLine == null)
                return null;

            if (strLine.ToLower().IndexOf("xml", StringComparison.Ordinal) >= 0)
            {
                if (!ReadXMLDat(ref newDir, datFullName))
                    return null;
            }

            else if (strLine.ToLower().IndexOf("clrmamepro", StringComparison.Ordinal) >= 0 || strLine.ToLower().IndexOf("ROMVault", StringComparison.Ordinal) >= 0 || strLine.ToLower().IndexOf("game", StringComparison.Ordinal) >= 0)
            {
                if (!DatCmpReader.ReadDat(ref newDir, datFullName))
                    return null;
            }
            else if (strLine.ToLower().IndexOf("doscenter", StringComparison.Ordinal) >= 0)
            {
                if (!DatDOSReader.ReadDat(ref newDir, datFullName))
                    return null;
            }
            else
            {
                _bgw.ReportProgress(0, new bgwShowError(datFullName, "Invalid DAT File"));
                return null;
            }

            if (newDir.Dat == null)
            {
                _bgw.ReportProgress(0, new bgwShowError(datFullName, "Invalid Header"));
                return null;
            }

            newDir.Dat.AddData(RvDat.DatData.DatFullName, datFullName);
            newDir.Dat.TimeStamp = datFile.TimeStamp;
            newDir.Dat.Status = DatUpdateStatus.Correct;

            DatSetRemoveUnneededDirs(newDir);
            DatSetCheckParentSets(newDir);
            DatSetRenameAndRemoveDups(newDir);

            if (newDir.Dat.GetData(RvDat.DatData.MergeType).ToLower() == "full")
                DatSetMergeSets(newDir);

            DatSetCheckCollect(newDir);

            DatSetCreateSubDirs(newDir);

            if (newDir.Dat.GetData(RvDat.DatData.DirSetup).ToLower() == "nogame")
            {
                DatSetRemoveGameDir(newDir);
            }

            return newDir;
        }




        private static bool ReadXMLDat(ref RvDir tDat, string strFilename)
        {
            System.IO.Stream fs;
            int errorCode = IO.FileStream.OpenFileRead(strFilename, out fs);
            if (errorCode != 0)
            {
                _bgw.ReportProgress(0, new bgwShowError(strFilename, errorCode + ": " + new Win32Exception(errorCode).Message));
                return false;
            }

            XmlDocument doc = new XmlDocument { XmlResolver = null };
            try
            {
                doc.Load(fs);
            }
            catch (Exception e)
            {
                fs.Close();
                fs.Dispose();
                _bgw.ReportProgress(0, new bgwShowError(strFilename, string.Format("Error Occured Reading Dat:\r\n{0}\r\n", e.Message)));
                return false;
            }
            fs.Close();
            fs.Dispose();

            if (doc.DocumentElement == null)
                return false;

            XmlNode mame = doc.SelectSingleNode("mame");
            if (mame != null)
                return DatXmlReader.ReadMameDat(ref tDat, doc);

            if (doc.DocumentElement != null)
            {
                XmlNode head = doc.DocumentElement.SelectSingleNode("header");
                if (head != null)
                    return DatXmlReader.ReadDat(ref tDat, doc);
            }

            XmlNodeList headList = doc.SelectNodes("softwarelist");
            if (headList != null)
                return DatMessXmlReader.ReadDat(ref tDat, doc);

            return false;
        }

        private static void DatSetRenameAndRemoveDups(RvDir tDat)
        {
            for (int g = 0; g < tDat.ChildCount; g++)
            {
                RvDir tDir = (RvDir)tDat.Child(g);
                if (tDir.Game == null)
                {
                    DatSetRenameAndRemoveDups(tDir);
                }
                else
                {
                    for (int r = 0; r < tDir.ChildCount - 1; r++)
                    {
                        RvFile f0 = (RvFile)tDir.Child(r);
                        RvFile f1 = (RvFile)tDir.Child(r + 1);

                        if (f0.Name != f1.Name)
                            continue;

                        if (f0.Size != f1.Size || !ArrByte.bCompare(f0.CRC, f1.CRC))
                        {
                            tDir.ChildRemove(r + 1); // remove F1
                            f1.Name = f1.Name + "_" + ArrByte.ToString(f1.CRC); // rename F1;
                            int pos = tDir.ChildAdd(f1);
                            if (pos < r)
                                r = pos;
                            // if this rename moved the File back up the list, start checking again from that file.
                        }
                        else
                        {
                            tDir.ChildRemove(r + 1);
                        }
                        r--;
                    }
                }
            }
        }
        private static void DatSetRemoveUnneededDirs(RvDir tDat)
        {
            for (int g = 0; g < tDat.ChildCount; g++)
            {
                RvDir tGame = (RvDir)tDat.Child(g);
                if (tGame.Game == null)
                {
                    DatSetRemoveUnneededDirs(tGame);
                }
                else
                {
                    for (int r = 0; r < tGame.ChildCount - 1; r++)
                    {
                        // first find any directories, zero length with filename ending in a '/'
                        // there are RvFiles that are really directories (probably inside a zip file)
                        RvFile f0 = (RvFile)tGame.Child(r);
                        if (f0.Name.Length == 0)
                            continue;
                        if (f0.Name.Substring(f0.Name.Length - 1, 1) != "/")
                            continue;

                        // if the next file contains that found directory, then the directory file can be deleted
                        RvFile f1 = (RvFile)tGame.Child(r + 1);
                        if (f1.Name.Length <= f0.Name.Length)
                            continue;

                        if (f0.Name != f1.Name.Substring(0, f0.Name.Length))
                            continue;

                        tGame.ChildRemove(r);
                        r--;
                    }
                }
            }
        }


        private static void DatSetCheckParentSets(RvDir tDat)
        {
            // First we are going to try and fix any missing CRC information by checking for roms with the same names
            // in Parent and Child sets, and if the same named rom is found and one has a CRC and the other does not
            // then we will set the missing CRC by using the CRC in the other set.

            // we keep trying to find fixes until no more fixes are found.
            // this is need as the first time round a fix could be found in a parent set from one child set.
            // then the second time around that fixed parent set could fix another of its childs sets.

            for (int g = 0; g < tDat.ChildCount; g++)
            {
                RvDir mGame = (RvDir)tDat.Child(g);
                if (mGame.Game == null)
                    // this is a directory so recuse into it
                    DatSetCheckParentSets(mGame);
            }

            bool fix = true;
            while (fix)
            {
                fix = false;

                // loop around every ROM Set looking for fixes.
                for (int g = 0; g < tDat.ChildCount; g++)
                {

                    // get a list of that ROM Sets parents.
                    RvDir mGame = (RvDir)tDat.Child(g);

                    if (mGame.Game == null)
                        continue;

                    List<RvDir> lstParentGames = new List<RvDir>();
                    FindParentSet(mGame, tDat, ref lstParentGames);

                    // if this set have parents
                    if (lstParentGames.Count == 0)
                        continue;

                    // now loop every ROM in the current set.
                    for (int r = 0; r < mGame.ChildCount; r++)
                    {
                        // and loop every ROM of every parent set of this current set.
                        // and see if anything can be fixed.
                        bool found = false;

                        // loop the parent sets
                        foreach (RvDir romofGame in lstParentGames)
                        {
                            // loop the ROMs in the parent sets
                            for (int r1 = 0; r1 < romofGame.ChildCount; r1++)
                            {
                                // only find fixes if the Name and the Size of the ROMs are the same
                                if (mGame.Child(r).Name != romofGame.Child(r1).Name || ((RvFile)mGame.Child(r)).Size != ((RvFile)romofGame.Child(r1)).Size)
                                    continue;

                                // now check if one of the matching roms has missing or incorrect CRC information
                                bool b1 = ((RvFile)mGame.Child(r)).CRC == null;
                                bool b2 = ((RvFile)romofGame.Child(r1)).CRC == null;

                                // if one has correct information and the other does not, fix the missing one
                                if (b1 == b2)
                                    continue;

                                if (b1)
                                {
                                    ((RvFile)mGame.Child(r)).CRC = ((RvFile)romofGame.Child(r1)).CRC;
                                    ((RvFile)mGame.Child(r)).Status = "(CRCFound)";
                                }
                                else
                                {
                                    ((RvFile)romofGame.Child(r1)).CRC = ((RvFile)mGame.Child(r)).CRC;
                                    ((RvFile)romofGame.Child(r1)).Status = "(CRCFound)";
                                }

                                // flag that a fix was found so that we will go all the way around again.
                                fix = true;
                                found = true;
                                break;
                            }
                            if (found) break;
                        }
                    }
                }
            }
        }

        private static void DatSetMergeSets(RvDir tDat)
        {
            for (int g = tDat.ChildCount - 1; g >= 0; g--)
            {
                RvDir mGame = (RvDir)tDat.Child(g);

                if (mGame.Game == null)
                {
                    DatSetMergeSets(mGame);
                    continue;
                }

                List<RvDir> lstParentGames = new List<RvDir>();
                FindParentSet(mGame, tDat, ref lstParentGames);
                while (lstParentGames.Count > 0 && lstParentGames[lstParentGames.Count - 1].Game.GetData(RvGame.GameData.IsBios).ToLower() == "yes")
                    lstParentGames.RemoveAt(lstParentGames.Count - 1);

                if (lstParentGames.Count <= 0) continue;

                RvDir romofGame = lstParentGames[lstParentGames.Count - 1];

                bool founderror = false;
                for (int r = 0; r < mGame.ChildCount; r++)
                {
                    string name = mGame.Child(r).Name;
                    string mergename = ((RvFile)mGame.Child(r)).Merge;

                    for (int r1 = 0; r1 < romofGame.ChildCount; r1++)
                    {
                        if ((name == romofGame.Child(r1).Name || mergename == romofGame.Child(r1).Name) &&
                             (ArrByte.iCompare(((RvFile)mGame.Child(r)).CRC, ((RvFile)romofGame.Child(r1)).CRC) != 0 ||
                             ((RvFile)mGame.Child(r)).Size != ((RvFile)romofGame.Child(r1)).Size))
                            founderror = true;

                    }
                }
                if (founderror)
                {
                    mGame.Game.DeleteData(RvGame.GameData.RomOf);
                    continue;
                }

                for (int r = 0; r < mGame.ChildCount; r++)
                {
                    string name = mGame.Child(r).Name;
                    string mergename = ((RvFile)mGame.Child(r)).Merge;

                    bool found = false;
                    for (int r1 = 0; r1 < romofGame.ChildCount; r1++)
                    {
                        if ((name == romofGame.Child(r1).Name || mergename == romofGame.Child(r1).Name) &&
                            (ArrByte.iCompare(((RvFile)mGame.Child(r)).CRC, ((RvFile)romofGame.Child(r1)).CRC) == 0 &&
                             ((RvFile)mGame.Child(r)).Size == ((RvFile)romofGame.Child(r1)).Size))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        romofGame.ChildAdd(mGame.Child(r));
                }
                tDat.ChildRemove(g);
            }
        }



        private static void DatSetCheckCollect(RvDir tDat)
        {
            // now look for merged roms.
            // check if a rom exists in a parent set where the Name,Size and CRC all match.

            for (int g = 0; g < tDat.ChildCount; g++)
            {
                RvDir mGame = (RvDir)tDat.Child(g);

                if (mGame.Game == null)
                    DatSetCheckCollect(mGame);
                else
                {
                    List<RvDir> lstParentGames = new List<RvDir>();
                    FindParentSet(mGame, tDat, ref lstParentGames);

                    if (lstParentGames.Count == 0)
                    {
                        for (int r = 0; r < mGame.ChildCount; r++)
                            RomCheckCollect((RvFile)mGame.Child(r), false);
                    }
                    else
                    {
                        for (int r = 0; r < mGame.ChildCount; r++)
                        {
                            bool found = false;
                            foreach (RvDir romofGame in lstParentGames)
                            {
                                for (int r1 = 0; r1 < romofGame.ChildCount; r1++)
                                {
                                    if (mGame.Child(r).Name != romofGame.Child(r1).Name) continue;

                                    ulong? Size0 = ((RvFile)mGame.Child(r)).Size;
                                    ulong? Size1 = ((RvFile)romofGame.Child(r1)).Size;
                                    if (Size0 != null && Size1 != null && Size0 != Size1) continue;

                                    byte[] CRC0 = ((RvFile)mGame.Child(r)).CRC;
                                    byte[] CRC1 = ((RvFile)romofGame.Child(r1)).CRC;
                                    if (CRC0 != null && CRC1 != null && !ArrByte.bCompare(CRC0, CRC1)) continue;

                                    byte[] SHA0 = ((RvFile)mGame.Child(r)).SHA1;
                                    byte[] SHA1 = ((RvFile)romofGame.Child(r1)).SHA1;
                                    if (SHA0 != null && SHA1 != null && !ArrByte.bCompare(SHA0, SHA1)) continue;

                                    byte[] MD50 = ((RvFile)mGame.Child(r)).MD5;
                                    byte[] MD51 = ((RvFile)romofGame.Child(r1)).MD5;
                                    if (MD50 != null && MD51 != null && !ArrByte.bCompare(MD50, MD51)) continue;

                                    byte[] chdSHA0 = ((RvFile)mGame.Child(r)).SHA1CHD;
                                    byte[] chdSHA1 = ((RvFile)romofGame.Child(r1)).SHA1CHD;
                                    if (chdSHA0 != null && chdSHA1 != null && !ArrByte.bCompare(chdSHA0, chdSHA1)) continue;

                                    byte[] chdMD50 = ((RvFile)mGame.Child(r)).MD5CHD;
                                    byte[] chdMD51 = ((RvFile)romofGame.Child(r1)).MD5CHD;
                                    if (chdMD50 != null && chdMD51 != null && !ArrByte.bCompare(chdMD50, chdMD51)) continue;


                                    found = true;
                                    break;
                                }
                                if (found) break;
                            }
                            RomCheckCollect((RvFile)mGame.Child(r), found);
                        }
                    }
                }
            }
        }
        private static void FindParentSet(RvDir searchGame, RvDir parentDir, ref List<RvDir> lstParentGames)
        {
            if (searchGame.Game == null)
                return;

            string parentName = searchGame.Game.GetData(RvGame.GameData.RomOf);
            if (String.IsNullOrEmpty(parentName) || parentName==searchGame.Name)
                parentName = searchGame.Game.GetData(RvGame.GameData.CloneOf);
            if (String.IsNullOrEmpty(parentName) || parentName==searchGame.Name)
                return;

            int intIndex;
            int intResult = parentDir.ChildNameSearch(new RvDir(searchGame.FileType) { Name = parentName }, out intIndex);
            if (intResult == 0)
            {
                RvDir parentGame = (RvDir)parentDir.Child(intIndex);
                lstParentGames.Add(parentGame);
                FindParentSet(parentGame, parentDir, ref lstParentGames);
            }
        }


        /*
         * In the mame Dat:
         * status="nodump" has a size but no CRC
         * status="baddump" has a size and crc
         */


        private static void RomCheckCollect(RvFile tRom, bool merge)
        {
            if (merge)
            {
                if (string.IsNullOrEmpty(tRom.Merge))
                    tRom.Merge = "(Auto Merged)";

                tRom.DatStatus = DatStatus.InDatMerged;
                return;
            }

            if (!string.IsNullOrEmpty(tRom.Merge))
                tRom.Merge = "(No-Merge) " + tRom.Merge;

            if (tRom.Status == "nodump")
            {
                tRom.CRC = null;
                tRom.DatStatus = DatStatus.InDatBad;
                return;
            }

            if (ArrByte.bCompare(tRom.CRC, new byte[] { 0, 0, 0, 0 }) && tRom.Size == 0)
            {
                tRom.DatStatus = DatStatus.InDatCollect;
                return;
            }

            /*
            if (ArrByte.bCompare(tRom.CRC, new byte[] { 0, 0, 0, 0 }) || (tRom.CRC.Length != 8))
            {
                tRom.CRC = null;
                tRom.DatStatus = DatStatus.InDatBad;
                return;
            }
            */

            tRom.DatStatus = DatStatus.InDatCollect;
        }

        private static void DatSetCreateSubDirs(RvDir tDat)
        {
            for (int g = 0; g < tDat.ChildCount; g++)
            {
                if (tDat.Child(g).FileType == FileType.Zip)
                    continue;

                RvDir datGame = (RvDir)tDat.Child(g);

                // first do a quick check to see if anything needs done.
                bool fixNeeded = false;
                for (int r = 0; r < datGame.ChildCount; r++)
                {
                    fixNeeded = datGame.Child(r).Name.Contains("/");

                    if (fixNeeded)
                        break;
                }
                // if nothing needs done skip to next game
                if (!fixNeeded)
                    continue;


                RvDir fixedGame = new RvDir(FileType.Dir);
                while (datGame.ChildCount > 0)
                {
                    RvBase nextChild = datGame.Child(0);
                    datGame.ChildRemove(0);
                    if (nextChild.GetType() == typeof(RvFile))
                    {
                        RvFile tFile = (RvFile)nextChild;
                        if (tFile.Name.Contains("/"))
                        {
                            RvDir tBase = fixedGame;
                            Debug.WriteLine("tFile " + tFile.TreeFullName);
                            while (tFile.Name.Contains("/"))
                            {
                                int dirIndex = tFile.Name.IndexOf("/", StringComparison.Ordinal);
                                string dirName = tFile.Name.Substring(0, dirIndex);
                                RvDir tDir = new RvDir(FileType.Dir)
                                               {
                                                   Name = dirName,
                                                   DatStatus = DatStatus.InDatCollect,
                                                   Dat = datGame.Dat
                                               };
                                int index;
                                if (tBase.ChildNameSearch(tDir, out index) != 0)
                                    tBase.ChildAdd(tDir, index);
                                tBase = (RvDir)tBase.Child(index);
                                tFile.Name = tFile.Name.Substring(tFile.Name.IndexOf("/", StringComparison.Ordinal) + 1);
                            }
                            tBase.ChildAdd(tFile);
                        }
                        else
                            fixedGame.ChildAdd(nextChild);
                    }
                    else
                        fixedGame.ChildAdd(nextChild);


                }

                for (int r = 0; r < fixedGame.ChildCount; r++)
                    datGame.ChildAdd(fixedGame.Child(r), r);
            }
        }


        private static void DatSetRemoveGameDir(RvDir newDir)
        {
            if (newDir.ChildCount != 1)
                return;

            RvDir child = newDir.Child(0) as RvDir;
            if (child.FileType != FileType.Dir)
                return;

            if (child.Game == null)
                return;

            newDir.ChildRemove(0);
            newDir.Game = child.Game;
            for (int i = 0; i < child.ChildCount; i++)
                newDir.ChildAdd(child.Child(i), i);
        }
    }
}
