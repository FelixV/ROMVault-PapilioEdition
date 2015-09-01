/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2013                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using ROMVault2.IO;
using ROMVault2.DatReaders;
using ROMVault2.Properties;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2
{
    public static class DatUpdate
    {
        private static int _datCount;
        private static int _datsProcessed;
        private static BackgroundWorker _bgw;

        public static void ShowDat(string message, string filename)
        {
            if (_bgw != null)
                _bgw.ReportProgress(0, new bgwShowError(filename, message));
        }

        public static void SendAndShowDat(string message, string filename)
        {
            if (_bgw != null)
                _bgw.ReportProgress(0, new bgwShowError(filename, message));
        }


        public static void UpdateDat(object sender, DoWorkEventArgs e)
        {
            try
            {
                _bgw = sender as BackgroundWorker;
                if (_bgw == null) return;

                Program.SyncCont = e.Argument as SynchronizationContext;
                if (Program.SyncCont == null)
                {
                    _bgw = null;
                    return;
                }


                _bgw.ReportProgress(0, new bgwText("Clearing DB Status"));
                RepairStatus.ReportStatusReset(DB.DirTree);

                _datCount = 0;

                _bgw.ReportProgress(0, new bgwText("Finding Dats"));
                RvDir DATRoot = new RvDir(FileType.Dir) { Name = "ROMVault", DatStatus = DatStatus.InDatCollect };

                // build a DATRoot tree of the DAT's in DATRoot, and count how many dats are found
                if (!RecursiveDatTree(DATRoot, out _datCount))
                {
                    _bgw.ReportProgress(0, new bgwText("Dat Update Complete"));
                    _bgw = null;
                    Program.SyncCont = null;
                    return;
                }

                _bgw.ReportProgress(0, new bgwText("Scanning Dats"));
                _datsProcessed = 0;

                // now compare the database DAT's with DATRoot removing any old DAT's
                RemoveOldDats(DB.DirTree.Child(0), DATRoot);

                // next clean up the File status removing any old DAT's
                RemoveOldDatsCleanUpFiles(DB.DirTree.Child(0));

                _bgw.ReportProgress(0, new bgwSetRange(_datCount - 1));

                // next add in new DAT and update the files
                UpdateDatList((RvDir)DB.DirTree.Child(0),DATRoot);

                // finally remove any unneeded DIR's from the TreeView
                RemoveOldTree(DB.DirTree.Child(0));

                _bgw.ReportProgress(0, new bgwText("Updating Cache"));
                DB.Write();

                _bgw.ReportProgress(0, new bgwText("Dat Update Complete"));
                _bgw = null;
                Program.SyncCont = null;
            }
            catch (Exception exc)
            {
                ReportError.UnhandledExceptionHandler(exc);

                if (_bgw != null) _bgw.ReportProgress(0, new bgwText("Updating Cache"));
                DB.Write();
                if (_bgw != null) _bgw.ReportProgress(0, new bgwText("Complete"));

                _bgw = null;
                Program.SyncCont = null;
            }
        }

        private static bool RecursiveDatTree(RvDir tDir, out int datCount)
        {
            datCount = 0;
            string strPath = tDir.DatFullName;

            if (!Directory.Exists(strPath))
            {
                ReportError.Show(Resources.DatUpdate_UpdateDatList_Path + strPath + Resources.DatUpdate_UpdateDatList_Not_Found);
                return false;
            }

            DirectoryInfo oDir = new DirectoryInfo(strPath);

            FileInfo[] oFilesIn = oDir.GetFiles("*.dat", false);
            datCount += oFilesIn.Length;
            foreach (FileInfo file in oFilesIn)
            {
                RvDat tDat = new RvDat();
                tDat.AddData(RvDat.DatData.DatFullName, file.FullName);
                tDat.TimeStamp = file.LastWriteTime;
                tDir.DirDatAdd(tDat);
            }


            oFilesIn = oDir.GetFiles("*.xml", false);
            datCount += oFilesIn.Length;
            foreach (FileInfo file in oFilesIn)
            {
                RvDat tDat = new RvDat();
                tDat.AddData(RvDat.DatData.DatFullName, file.FullName);
                tDat.TimeStamp = file.LastWriteTime;
                tDir.DirDatAdd(tDat);
            }

            if (tDir.DirDatCount > 1)
                for (int i = 0; i < tDir.DirDatCount; i++)
                    tDir.DirDat(i).AutoAddDirectory = true;

            DirectoryInfo[] oSubDir = oDir.GetDirectories(false);

            foreach (DirectoryInfo t in oSubDir)
            {
                RvDir cDir = new RvDir(FileType.Dir) { Name = t.Name, DatStatus = DatStatus.InDatCollect };
                int index = tDir.ChildAdd(cDir);

                int retDatCount;

                RecursiveDatTree(cDir, out retDatCount);
                datCount += retDatCount;

                if (retDatCount == 0)
                    tDir.ChildRemove(index);
            }

            return true;
        }


        private static void RemoveOldDats(RvBase dbDir, RvDir tmpDir)
        {
            // now compare the old and new dats removing any old dats
            // in the current directory

            RvDir lDir = dbDir as RvDir;
            if (lDir == null) return;

            int dbIndex = 0;
            int scanIndex = 0;

            while (dbIndex < lDir.DirDatCount || scanIndex < tmpDir.DirDatCount)
            {
                RvDat dbDat = null;
                RvDat fileDat = null;
                int res = 0;

                if (dbIndex < lDir.DirDatCount && scanIndex < tmpDir.DirDatCount)
                {
                    dbDat = lDir.DirDat(dbIndex);
                    fileDat = tmpDir.DirDat(scanIndex);
                    res = DBHelper.DatCompare(dbDat, fileDat);
                }
                else if (scanIndex < tmpDir.DirDatCount)
                {
                    //this is a new dat that we have now found at the end of the list
                    //fileDat = tmpDir.DirDat(scanIndex);
                    res = 1;
                }
                else if (dbIndex < lDir.DirDatCount)
                {
                    dbDat = lDir.DirDat(dbIndex);
                    res = -1;
                }

                switch (res)
                {
                    case 0:
                        dbDat.Status = DatUpdateStatus.Correct;
                        dbIndex++;
                        scanIndex++;
                        break;

                    case 1:
                        // this is a new dat that we will add next time around
                        scanIndex++;
                        break;
                    case -1:
                        dbDat.Status = DatUpdateStatus.Delete;
                        lDir.DirDatRemove(dbIndex);
                        break;
                }

            }

            // now scan the child directory structure of this directory
            dbIndex = 0;
            scanIndex = 0;

            while (dbIndex < lDir.ChildCount || scanIndex < tmpDir.ChildCount)
            {
                RvBase dbChild = null;
                RvBase fileChild = null;
                int res = 0;

                if (dbIndex < lDir.ChildCount && scanIndex < tmpDir.ChildCount)
                {
                    dbChild = lDir.Child(dbIndex);
                    fileChild = tmpDir.Child(scanIndex);
                    res = DBHelper.CompareName(dbChild, fileChild);
                }
                else if (scanIndex < tmpDir.ChildCount)
                {
                    //found a new directory on the end of the list
                    //fileChild = tmpDir.Child(scanIndex);
                    res = 1;
                }
                else if (dbIndex < lDir.ChildCount)
                {
                    dbChild = lDir.Child(dbIndex);
                    res = -1;
                }
                switch (res)
                {
                    case 0:
                        // found a matching directory in DATRoot So recurse back into it
                        RemoveOldDats(dbChild, (RvDir)fileChild);
                        dbIndex++;
                        scanIndex++;
                        break;

                    case 1:
                        // found a new directory will be added later
                        scanIndex++;
                        break;
                    case -1:
                        if (dbChild.FileType == FileType.Dir && dbChild.Dat == null)
                            RemoveOldDats(dbChild, new RvDir(FileType.Dir));
                        dbIndex++;
                        break;
                }
            }
        }

        private static EFile RemoveOldDatsCleanUpFiles(RvBase dbDir)
        {

            if (dbDir.Dat != null)
            {
                if (dbDir.Dat.Status == DatUpdateStatus.Correct)
                    return EFile.Keep;

                if (dbDir.Dat.Status == DatUpdateStatus.Delete)
                {
                    if (dbDir.DatRemove() == EFile.Delete)
                        return EFile.Delete; //delete
                }
            }

            FileType ft = dbDir.FileType;
            // if we are checking a dir or zip recurse into it.
            if (ft != FileType.Zip && ft != FileType.Dir) return EFile.Keep;
            RvDir tDir = dbDir as RvDir;

            // remove all DATStatus's here they will get set back correctly when adding dats back in below.
            dbDir.DatStatus = DatStatus.NotInDat;

            for (int i = 0; i < tDir.ChildCount; i++)
            {
                if (RemoveOldDatsCleanUpFiles(tDir.Child(i)) == EFile.Keep) continue;
                tDir.ChildRemove(i);
                i--;
            }
            if (ft == FileType.Zip && dbDir.GotStatus == GotStatus.Corrupt) return EFile.Keep;

            // if this directory is now empty it should be deleted
            return tDir.ChildCount == 0 ? EFile.Delete : EFile.Keep;
        }



        private static void UpdateDatList(RvDir dbDir, RvDir tmpDir)
        {
            AddNewDats(dbDir, tmpDir);
            UpdateDirs(dbDir, tmpDir);
        }

        /// <summary>
        /// Add the new DAT's into the DAT list
        /// And merge in the new DAT data into the database
        /// </summary>
        /// <param name="dbDir">The Current database dir</param>
        /// <param name="tmpDir">A temp directory containing the DAT found in this directory in DATRoot</param>
        private static void AddNewDats(RvDir dbDir, RvDir tmpDir)
        {
            bool autoAddDirectory = (tmpDir.DirDatCount) > 1;
            
            int dbIndex = 0;
            int scanIndex = 0;

            Debug.WriteLine("");
            Debug.WriteLine("Scanning for Adding new DATS");
            while (dbIndex < dbDir.DirDatCount || scanIndex < tmpDir.DirDatCount)
            {
                RvDat dbDat = null;
                RvDat fileDat = null;
                int res = 0;

                if (dbIndex < dbDir.DirDatCount && scanIndex < tmpDir.DirDatCount)
                {
                    dbDat = dbDir.DirDat(dbIndex);
                    fileDat = tmpDir.DirDat(scanIndex);
                    res = DBHelper.DatCompare(dbDat, fileDat);
                    Debug.WriteLine("Checking " + dbDat.GetData(RvDat.DatData.DatFullName) + " : and " + fileDat.GetData(RvDat.DatData.DatFullName) + " : " + res);
                }
                else if (scanIndex < tmpDir.DirDatCount)
                {
                    fileDat = tmpDir.DirDat(scanIndex);
                    res = 1;
                    Debug.WriteLine("Checking : and " + fileDat.GetData(RvDat.DatData.DatFullName) + " : " + res);
                }
                else if (dbIndex < dbDir.DirDatCount)
                {
                    dbDat = dbDir.DirDat(dbIndex);
                    res = -1;
                    Debug.WriteLine("Checking " + dbDat.GetData(RvDat.DatData.DatFullName) + " : and : " + res);
                }

                switch (res)
                {
                    case 0:
                        _datsProcessed++;
                        _bgw.ReportProgress(_datsProcessed);
                        _bgw.ReportProgress(0, new bgwText("Dat : " + Path.GetFileNameWithoutExtension(fileDat.GetData(RvDat.DatData.DatFullName))));


                        Debug.WriteLine("Correct");
                        // Should already be set as correct above
                        dbDat.Status = DatUpdateStatus.Correct;
                        dbIndex++;
                        scanIndex++;
                        break;

                    case 1:
                        _datsProcessed++;
                        _bgw.ReportProgress(_datsProcessed);
                        _bgw.ReportProgress(0, new bgwText("Scanning New Dat : " + Path.GetFileNameWithoutExtension(fileDat.GetData(RvDat.DatData.DatFullName))));


                        Debug.WriteLine("Adding new DAT");
                        if (UpdateDatFile(fileDat, autoAddDirectory, dbDir))
                            dbIndex++;
                        scanIndex++;
                        break;

                    case -1:
                        // This should not happen as deleted dat have been removed above
                        //dbIndex++;
                        ReportError.SendAndShow(Resources.DatUpdate_UpdateDatList_ERROR_Deleting_a_DAT_that_should_already_be_deleted);
                        break;
                }

            }
        }





        private static bool UpdateDatFile(RvDat file, bool autoAddDirectory, RvDir thisDirectory)
        {
            // Read the new Dat File into newDatFile
            RvDir newDatFile = DatReader.ReadInDatFile(file, _bgw);

            // If we got a valid Dat File back
            if (newDatFile == null || newDatFile.Dat == null)
            {
                ReportError.Show("Error reading Dat " + file.GetData(RvDat.DatData.DatFullName));
                return false;
            }

            newDatFile.Dat.AutoAddDirectory = autoAddDirectory;

            if ((autoAddDirectory || !String.IsNullOrEmpty(newDatFile.Dat.GetData(RvDat.DatData.RootDir))) && newDatFile.Dat.GetData(RvDat.DatData.DirSetup)!= "noautodir")
            {   // if we are auto adding extra directorys then create a new directory.

                newDatFile.Name = !String.IsNullOrEmpty(newDatFile.Dat.GetData(RvDat.DatData.RootDir)) ?
                    newDatFile.Dat.GetData(RvDat.DatData.RootDir) : newDatFile.Dat.GetData(RvDat.DatData.DatName);

                newDatFile.DatStatus = DatStatus.InDatCollect;
                newDatFile.Tree = new RvTreeRow();

                RvDir newDirectory = new RvDir(FileType.Dir) { Dat = newDatFile.Dat };

                // add the DAT into this directory
                newDirectory.ChildAdd(newDatFile);
                newDatFile = newDirectory;
            }

            if (thisDirectory.Tree == null)
                thisDirectory.Tree = new RvTreeRow();

            RvDat conflictDat;
            if (MergeInDat(thisDirectory, newDatFile, out conflictDat, true))
            {
                ReportError.Show("Dat Merge conflict occured Cache contains " + conflictDat.GetData(RvDat.DatData.DatFullName) + " new dat " + newDatFile.Dat.GetData(RvDat.DatData.DatFullName) + " is trying to use the same dirctory and so will be ignored.");
                return false;
            }

            //SetInDat(thisDirectory);

            // Add the new Dat 
            thisDirectory.DirDatAdd(newDatFile.Dat);

            // Merge the files/directories in the Dat
            MergeInDat(thisDirectory, newDatFile, out conflictDat, false);
            return true;
        }

        /*
        private static void SetInDat(RvDir tDir)
        {
            tDir.DatStatus = DatStatus.InDatCollect;
            if (tDir.Parent != null)
                SetInDat(tDir.Parent);
        }
        */

        private static Boolean MergeInDat(RvDir dbDat, RvDir newDat, out RvDat conflict, bool checkOnly)
        {
            conflict = null;
            int dbIndex = 0;
            int newIndex = 0;
            while (dbIndex < dbDat.ChildCount || newIndex < newDat.ChildCount)
            {
                RvBase dbChild = null;
                RvBase newDatChild = null;
                int res = 0;

                if (dbIndex < dbDat.ChildCount && newIndex < newDat.ChildCount)
                {
                    dbChild = dbDat.Child(dbIndex); // are files
                    newDatChild = newDat.Child(newIndex); // is from a dat item
                    res = DBHelper.CompareName(dbChild, newDatChild);
                }
                else if (newIndex < newDat.ChildCount)
                {
                    newDatChild = newDat.Child(newIndex);
                    res = 1;
                }
                else if (dbIndex < dbDat.ChildCount)
                {
                    dbChild = dbDat.Child(dbIndex);
                    res = -1;
                }

                if (res == 0)
                {
                    if (dbChild == null || newDatChild == null)
                    {
                        SendAndShowDat(Resources.DatUpdate_MergeInDat_Error_in_Logic, dbDat.FullName);
                        break;
                    }


                    List<RvBase> dbDats = new List<RvBase>();
                    List<RvBase> newDats = new List<RvBase>();
                    int dbDatsCount = 1;
                    int newDatsCount = 1;


                    dbDats.Add(dbChild);
                    newDats.Add(newDatChild);

                    while (dbIndex + dbDatsCount < dbDat.ChildCount && DBHelper.CompareName(dbChild, dbDat.Child(dbIndex + dbDatsCount)) == 0)
                    {
                        dbDats.Add(dbDat.Child(dbIndex + dbDatsCount));
                        dbDatsCount += 1;
                    }
                    while (newIndex + newDatsCount < newDat.ChildCount && DBHelper.CompareName(newDatChild, newDat.Child(newIndex + newDatsCount)) == 0)
                    {
                        newDats.Add(newDat.Child(newIndex + newDatsCount));
                        newDatsCount += 1;
                    }

                    if (dbDatsCount > 1 || newDatsCount > 1)
                    {
                        ReportError.SendAndShow("Double Name Found");
                    }

                    for (int indexdb = 0; indexdb < dbDatsCount; indexdb++)
                    {
                        if (dbDats[indexdb].DatStatus == DatStatus.NotInDat) continue;

                        if (checkOnly)
                        {
                            conflict = dbChild.Dat;
                            return true;
                        }

                        SendAndShowDat(Resources.DatUpdate_MergeInDat_Unkown_Update_Dat_Status + dbChild.DatStatus, dbDat.FullName);
                        break;
                    }

                    if (!checkOnly)
                    {
                        for (int indexNewDats = 0; indexNewDats < newDatsCount; indexNewDats++)
                        {
                            if (newDats[indexNewDats].SearchFound) continue;

                            for (int indexDbDats = 0; indexDbDats < dbDatsCount; indexDbDats++)
                            {
                                if (dbDats[indexDbDats].SearchFound) continue;

                                bool matched = FullCompare(dbDats[indexDbDats], newDats[indexNewDats]);
                                if (!matched) continue;

                                dbDats[indexDbDats].DatAdd(newDats[indexNewDats]);

                                FileType ft = dbChild.FileType;

                                if (ft == FileType.Zip || ft == FileType.Dir)
                                {
                                    RvDir dChild = (RvDir)dbChild;
                                    RvDir dNewChild = (RvDir)newDatChild;
                                    MergeInDat(dChild, dNewChild, out conflict, checkOnly);
                                }

                                dbDats[indexDbDats].SearchFound = true;
                                newDats[indexNewDats].SearchFound = true;
                            }
                        }

                        for (int indexNewDats = 0; indexNewDats < newDatsCount; indexNewDats++)
                        {
                            if (newDats[indexNewDats].SearchFound) continue;

                            dbDat.ChildAdd(newDats[indexNewDats], dbIndex);
                            dbChild = dbDat.Child(dbIndex);
                            SetMissingStatus(dbChild);

                            dbIndex++;
                        }
                    }

                    dbIndex += dbDatsCount;
                    newIndex += newDatsCount;

                }

                if (res == 1)
                {
                    if (!checkOnly)
                    {
                        dbDat.ChildAdd(newDatChild, dbIndex);
                        dbChild = dbDat.Child(dbIndex);
                        SetMissingStatus(dbChild);

                        dbIndex++;
                    }
                    newIndex++;
                }

                if (res == -1)
                {
                    dbIndex++;
                }
            }
            return false;
        }


        private static void SetMissingStatus(RvBase dbChild)
        {
            if (dbChild.FileRemove() == EFile.Delete)
            {
                ReportError.SendAndShow("Error is Set Mssing Status in DatUpdate");
                return;
            }


            FileType ft = dbChild.FileType;
            if (ft == FileType.Zip || ft == FileType.Dir)
            {
                RvDir dbDir = (RvDir)dbChild;
                for (int i = 0; i < dbDir.ChildCount; i++)
                    SetMissingStatus(dbDir.Child(i));
            }

        }

        private static bool FullCompare(RvBase var1, RvBase var2)
        {
            int retv = DBHelper.CompareName(var1, var2);
            if (retv != 0) return false;

            FileType v1 = var1.FileType;
            FileType v2 = var2.FileType;
            retv = Math.Sign(v1.CompareTo(v2));
            if (retv != 0) return false;

            // filetypes are now know to be the same

            // Dir's and Zip's are not deep scanned so matching here is done
            if ((v1 == FileType.Dir) || (v1 == FileType.Zip))
                return true;

            RvFile f1 = (RvFile)var1;
            RvFile f2 = (RvFile)var2;

            if (f1.Size != null && f2.Size != null)
            {
                retv = ULong.iCompare(f1.Size, f2.Size);
                if (retv != 0) return false;
            }

            if (f1.CRC != null && f2.CRC != null)
            {
                retv = ArrByte.iCompare(f1.CRC, f2.CRC);
                if (retv != 0) return false;
            }

            if (f1.SHA1 != null && f2.SHA1 != null)
            {
                retv = ArrByte.iCompare(f1.SHA1, f2.SHA1);
                if (retv != 0) return false;
            }

            if (f1.MD5 != null && f2.MD5 != null)
            {
                retv = ArrByte.iCompare(f1.MD5, f2.MD5);
                if (retv != 0) return false;
            }

            if (f1.SHA1CHD != null && f2.SHA1CHD != null)
            {
                retv = ArrByte.iCompare(f1.SHA1CHD, f2.SHA1CHD);
                if (retv != 0) return false;
            }

            if (f1.MD5CHD != null && f2.MD5CHD != null)
            {
                retv = ArrByte.iCompare(f1.MD5CHD, f2.MD5CHD);
                if (retv != 0) return false;
            }

            return true;
        }



        private static void UpdateDirs(RvDir dbDir, RvDir fileDir)
        {
            int dbIndex = 0;
            int scanIndex = 0;

            dbDir.DatStatus=DatStatus.InDatCollect;
            if (dbDir.Tree == null)
            {
                Debug.WriteLine("Adding Tree View to " + dbDir.Name);
                dbDir.Tree = new RvTreeRow();
            }
                        

            Debug.WriteLine("");
            Debug.WriteLine("Now scanning dirs");

            while (dbIndex < dbDir.ChildCount || scanIndex < fileDir.ChildCount)
            {
                RvBase dbChild = null;
                RvBase fileChild = null;
                int res = 0;

                if (dbIndex < dbDir.ChildCount && scanIndex < fileDir.ChildCount)
                {
                    dbChild = dbDir.Child(dbIndex);
                    fileChild = fileDir.Child(scanIndex);
                    res = DBHelper.CompareName(dbChild, fileChild);
                    Debug.WriteLine("Checking " + dbChild.Name + " : and " + fileChild.Name + " : " + res);
                }
                else if (scanIndex < fileDir.ChildCount)
                {
                    fileChild = fileDir.Child(scanIndex);
                    res = 1;
                    Debug.WriteLine("Checking : and " + fileChild.Name + " : " + res);
                }
                else if (dbIndex < dbDir.ChildCount)
                {
                    dbChild = dbDir.Child(dbIndex);
                    res = -1;
                }
                switch (res)
                {
                    case 0:
                        // found a matching directory in DATRoot So recurse back into it
                        
                        if (dbChild.GotStatus == GotStatus.Got)
                        {
                            if (dbChild.Name != fileChild.Name) // check if the case of the Item in the DB is different from the Dat Root Actual filename
                            {
                                if (!string.IsNullOrEmpty(dbChild.FileName)) // if we do not already have a different case name stored
                                {
                                    dbChild.FileName = dbChild.Name; // copy the DB filename to the FileName
                                }
                                else // We already have a different case filename found in ROMRoot
                                {
                                    if (dbChild.FileName == fileChild.Name) // check if the DATRoot name does now match the name in the DB Filename
                                    {
                                        dbChild.FileName = null; // if it does undo the BadCase Flag
                                    }
                                }
                                dbChild.Name = fileChild.Name; // Set the db Name to match the DATRoot Name.
                            }
                        }
                        else
                            dbChild.Name = fileChild.Name;

                        UpdateDatList((RvDir)dbChild,(RvDir)fileChild);
                        dbIndex++;
                        scanIndex++;
                        break;

                    case 1:
                        // found a new directory in Dat
                        RvDir tDir = new RvDir(FileType.Dir)
                        {
                            Name = fileChild.Name,
                            Tree = new RvTreeRow(),
                            DatStatus = DatStatus.InDatCollect,
                        };
                        dbDir.ChildAdd(tDir, dbIndex);
                        Debug.WriteLine("Adding new Dir and Calling back in to check this DIR " + tDir.Name);
                        UpdateDatList(tDir,(RvDir)fileChild);

                        dbIndex++;
                        scanIndex++;
                        break;
                    case -1:
                        // all files 
                        dbIndex++;
                        break;
                }
            }
        }

        private static void RemoveOldTree(RvBase dbBase)
        {
            RvDir dbDir = dbBase as RvDir;
            if (dbDir == null) return;

            if (dbDir.DatStatus == DatStatus.NotInDat && dbDir.Tree != null)
                dbDir.Tree = null;
            
            for (int i = 0; i < dbDir.ChildCount; i++)
                RemoveOldTree(dbDir.Child(i));
        }
    }
}
