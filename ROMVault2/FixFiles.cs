/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using ROMVault2.IO;
using System.Threading;
using ROMVault2.Properties;
using ROMVault2.RvDB;
using ROMVault2.SupportedFiles;
using ROMVault2.SupportedFiles.Zip;

namespace ROMVault2
{

    public static class FixFiles
    {
        private static Stopwatch _cacheSaveTimer;

        private static string _error;


#if !NEWFINDFIX
        private static List<RvFile> _lstRomTableSortedCRCSize;
        private static List<RvFile> _lstRomTableSortedSHA1CHD;
#endif
        private static List<RvBase> _processList;

        private static int _fixed;
        private static int _reportedFixed;

        private static BackgroundWorker _bgw;

        private static void ReportProgress(object prog)
        {
            _bgw.ReportProgress(0, prog);
        }

        public static void PerformFixes(object sender, DoWorkEventArgs e)
        {
            try
            {

                _cacheSaveTimer = new Stopwatch();
                _cacheSaveTimer.Reset();
                if (Settings.CacheSaveTimerEnabled)
                    _cacheSaveTimer.Start();

                _bgw = sender as BackgroundWorker;
                if (_bgw == null) return;

                Program.SyncCont = e.Argument as SynchronizationContext;
                if (Program.SyncCont == null)
                {
                    _bgw = null;
                    return;
                }

                ReportProgress(new bgwText("Fixing Files"));

                int totalFixes = 0;
                _fixed = 0;
                _reportedFixed = 0;
                for (int i = 0; i < DB.DirTree.ChildCount; i++)
                {
                    RvDir tdir = (RvDir)DB.DirTree.Child(i);
                    totalFixes += CountFixDir(tdir, tdir.Tree.Checked == RvTreeRow.TreeSelect.Selected);
                }
                ReportProgress(new bgwSetRange(totalFixes));

#if !NEWFINDFIX

                DBHelper.GetSelectedFilesSortCRCSize(out _lstRomTableSortedCRCSize);
                DBHelper.GetSelectedFilesSortSHA1CHD(out _lstRomTableSortedSHA1CHD);

                ReportError.ReportList(_lstRomTableSortedCRCSize);
#endif
                _processList = new List<RvBase>();

                for (int i = 0; i < DB.DirTree.ChildCount; i++)
                {
                    RvDir tdir = (RvDir)DB.DirTree.Child(i);
                    ReturnCode returnCode = FixDir(tdir, tdir.Tree.Checked == RvTreeRow.TreeSelect.Selected);
                    if (returnCode != ReturnCode.Good)
                        break;

                    if (_bgw.CancellationPending) break;
                }

#if !NEWFINDFIX
                _lstRomTableSortedCRCSize = null;
                _lstRomTableSortedSHA1CHD = null;
#endif

                ReportProgress(new bgwText("Updating Cache"));
                DB.Write();
                ReportProgress(new bgwText("Complete"));

                _bgw = null;
                Program.SyncCont = null;

            }
            catch (Exception exc)
            {
                ReportError.UnhandledExceptionHandler(exc);

                if (_bgw != null) ReportProgress(new bgwText("Updating Cache"));
                DB.Write();
                if (_bgw != null) ReportProgress(new bgwText("Complete"));

                _bgw = null;
                Program.SyncCont = null;
            }
        }

        private static int CountFixDir(RvDir dir, bool lastSelected)
        {
            int count = 0;

            bool thisSelected = lastSelected;
            if (dir.Tree != null)
                thisSelected = dir.Tree.Checked == RvTreeRow.TreeSelect.Selected;

            for (int j = 0; j < dir.ChildCount; j++)
            {
                RvBase child = dir.Child(j);

                switch (child.FileType)
                {
                    case FileType.Zip:
                        if (!thisSelected)
                            continue;
                        RvDir tZip = (RvDir)child;
                        count += tZip.DirStatus.CountCanBeFixed();

                        break;

                    case FileType.Dir:

                        count += CountFixDir((RvDir)child, thisSelected);
                        break;

                    case FileType.File:
                        if (!thisSelected)
                            continue;
                        if (child.RepStatus == RepStatus.CanBeFixed)
                            count++;
                        break;
                }
            }
            return count;
        }

        private static ReturnCode FixDir(RvDir dir, bool lastSelected)
        {
            Debug.WriteLine(dir.FullName);
            bool thisSelected = lastSelected;
            if (dir.Tree != null)
                thisSelected = dir.Tree.Checked == RvTreeRow.TreeSelect.Selected;


            List<RvBase> lstToProcess = new List<RvBase>();
            for (int j = 0; j < dir.ChildCount; j++)
                lstToProcess.Add(dir.Child(j));

            foreach (RvBase child in lstToProcess)
            {
                ReturnCode returnCode = FixBase(child, thisSelected);
                if (returnCode != ReturnCode.Good)
                    return returnCode;

                while (_processList.Count > 0)
                {
                    returnCode = FixBase(_processList[0], true);
                    if (returnCode != ReturnCode.Good)
                        return returnCode;
                    _processList.RemoveAt(0);
                }

                if (_fixed != _reportedFixed)
                {
                    ReportProgress(new bgwProgress(_fixed));
                    _reportedFixed = _fixed;
                }
                if (_bgw.CancellationPending) break;
            }
            // here we check to see if the directory we just scanned should be deleted
            CheckDeleteObject(dir);
            return ReturnCode.Good;
        }

        private static ReturnCode FixBase(RvBase child, bool thisSelected)
        {
            // skip any files that have already been deleted
            if (child.RepStatus == RepStatus.Deleted)
                return ReturnCode.Good;


            if (_cacheSaveTimer.Elapsed.Minutes > Settings.CacheSaveTimePeriod)
            {
                ReportProgress("Saving Cache");
                DB.Write();
                ReportProgress("Saving Cache Complete");
                _cacheSaveTimer.Reset();
                _cacheSaveTimer.Start();
            }

            ReturnCode returnCode = ReturnCode.LogicError;
            switch (child.FileType)
            {
                case FileType.Zip:
                    if (!thisSelected)
                        return ReturnCode.Good;

                    if (!String.IsNullOrEmpty(child.FileName))
                    {
                        string strDir = child.Parent.FullName;
                        File.Move(Path.Combine(strDir, child.FileName + ".zip"), Path.Combine(strDir, child.Name + ".zip"));
                        child.FileName = null;
                    }

                    do
                    {
                        returnCode = FixZip((RvDir)child);
                    } while (returnCode == ReturnCode.StartOver);
                    break;

                case FileType.Dir:
                    if (thisSelected)
                    {
                        if (!String.IsNullOrEmpty(child.FileName))
                        {
                            string strDir = child.Parent.FullName;
                            System.IO.Directory.Move(Path.Combine(strDir, child.FileName), Path.Combine(strDir, "__ROMVault.tmpDir"));
                            Directory.Move(Path.Combine(strDir, "__ROMVault.tmpDir"), Path.Combine(strDir, child.Name));
                            child.FileName = null;
                        }
                    }

                    returnCode = FixDir((RvDir)child, thisSelected);
                    return returnCode;

                case FileType.File:
                    if (!thisSelected)
                        return ReturnCode.Good;

                    do
                    {
                        returnCode = FixFile((RvFile)child);
                    } while (returnCode == ReturnCode.StartOver);
                    break;
            }
            switch (returnCode)
            {
                case ReturnCode.Good:
                    // all good, move alone.
                    break;
                case ReturnCode.RescanNeeded:
                    ReportError.Show(_error);
                    break;
                case ReturnCode.LogicError:
                    ReportError.UnhandledExceptionHandler(_error);
                    break;
                case ReturnCode.FileSystemError:
                    ReportError.Show(_error);
                    break;
                case ReturnCode.FindFixes:
                    ReportError.Show("You Need to Find Fixes before Fixing. (Incorrect File Status's found for fixing.)");
                    break;
                default:
                    ReportError.UnhandledExceptionHandler(Resources.FixFiles_FixDirChildren_Unknown_result_type + returnCode);
                    break;
            }
            return returnCode;

        }


        private static ReturnCode FixFile(RvFile fixFile)
        {
            switch (fixFile.RepStatus)
            {
                case RepStatus.Unknown:
                    return ReturnCode.FindFixes;


                case RepStatus.UnScanned:
                    return ReturnCode.Good;

                case RepStatus.Missing:
                    // nothing can be done so moving right along
                    return ReturnCode.Good;


                case RepStatus.Correct:
                    // this is correct nothing to be done here
                    FixFileCheckName(fixFile);
                    return ReturnCode.Good;


                case RepStatus.NotCollected:
                    // this is correct nothing to be done here
                    return ReturnCode.Good;

                // Unknown

                case RepStatus.Ignore:
                    // this is correct nothing to be done here
                    return ReturnCode.Good;

                // Corrupt 

                case RepStatus.InUnsorted:
                    // this is correct nothing to be done here
                    return ReturnCode.Good;


                case RepStatus.Delete:
                    return FixFileDelete(fixFile);

                case RepStatus.MoveUnsorted:
                    return FixFileMoveUnsorted(fixFile);

                case RepStatus.MoveToCorrupt:
                    return FixFileMoveToCorrupt(fixFile);

                case RepStatus.CanBeFixed:
                case RepStatus.CorruptCanBeFixed:
                    return FixFileCanBeFixed(fixFile);

                case RepStatus.NeededForFix:
                    // this file can be left as is, it will be used to fix a file, and then marked to be deleted.
                    return ReturnCode.Good;

                    // this is for a corrupt CHD already in Unsorted
                case RepStatus.Corrupt:
                    return ReturnCode.Good;

                case RepStatus.Rename:
                    // this file will be used and mark to be deleted in the CanBeFixed
                    // so nothing to be done to it here
                    return ReturnCode.Good;


                default:
                    ReportError.UnhandledExceptionHandler(Resources.FixFiles_FixFile_Unknown_fix_file_type + fixFile.RepStatus + " Dat Status = " + fixFile.DatStatus + " GotStatus " + fixFile.GotStatus);
                    return ReturnCode.LogicError;
            }
        }

        private static void FixFileCheckName(RvFile fixFile)
        {
            if (!String.IsNullOrEmpty(fixFile.FileName))
            {
                string sourceFullName = Path.Combine(fixFile.Parent.FullName, fixFile.FileName);
                if (!File.SetAttributes(sourceFullName, FileAttributes.Normal))
                {
                    int error = Error.GetLastError();
                    ReportProgress(new bgwShowError(sourceFullName, "Error Setting File Attributes to Normal. Before Case correction Rename. Code " + error));
                }

                File.Move(sourceFullName, fixFile.FullName);
                fixFile.FileName = null;
            }
        }

        private static ReturnCode FixFileDelete(RvFile fixFile)
        {
            ReturnCode retCode = DoubleCheckDelete(fixFile);
            if (retCode != ReturnCode.Good)
                return retCode;

            string filename = fixFile.FullName;
            if (File.Exists(filename))
            {
                if (!File.SetAttributes(filename, FileAttributes.Normal))
                {
                    int error = Error.GetLastError();
                    ReportProgress(new bgwShowError(filename, "Error Setting File Attributes to Normal. Before Delete. Code " + error));
                }
                File.Delete(filename);
            }
            // here we just deleted a file so also delete it from the DB,
            // and recurse up deleting unnedded DIR's
            CheckDeleteObject(fixFile);
            return ReturnCode.Good;
        }

        private static ReturnCode FixFileMoveUnsorted(RvFile fixFile)
        {
            string fixFileFullName = fixFile.FullName;

            string UnsortedFullName = Path.Combine(Settings.Unsorted(), fixFile.Name);
            string UnsortedFileName = fixFile.Name;
            int fileC = 0;
            while (File.Exists(UnsortedFullName))
            {
                fileC++;
                UnsortedFullName = Path.Combine(Settings.Unsorted(), fixFile.Name + fileC);
                UnsortedFileName = fixFile.Name + fileC;
            }

            //create new Unsorted record
            // FileInfo UnsortedFile = new FileInfo(UnsortedFullName);
            RvFile UnsortedRom = new RvFile(FileType.File)
            {
                Name = UnsortedFileName,
                Size = fixFile.Size,
                CRC = fixFile.CRC,
                //TimeStamp = UnsortedFile.LastWriteTime,
                DatStatus = DatStatus.InUnsorted
            };

            ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixFileFullName), "", Path.GetFileName(fixFileFullName), fixFile.Size, "-->", "Unsorted", "", fixFile.Name));

            ZipFile tempZipOut = null;
            RvFile foundFile;
            ReturnCode returnCode = FixFileCopy.CopyFile(fixFile, ref tempZipOut, UnsortedFullName, UnsortedRom, false, out _error, out foundFile);
            switch (returnCode)
            {
                case ReturnCode.Good: // correct reply to continue;
                    break;
                default:
                    return returnCode;
            }

            string fixFilePath = fixFile.FullName;
            if (!File.SetAttributes(fixFilePath, FileAttributes.Normal))
            {
                int error = Error.GetLastError();
                ReportProgress(new bgwShowError(fixFilePath, "Error Setting File Attributes to Normal. Before Delete Moving Unsorted. Code " + error));
            }
            File.Delete(fixFilePath);

            // here we just deleted a file so also delete it from the DB,
            // and recurse up deleting unnedded DIR's
            CheckDeleteObject(fixFile);

            RvDir Unsorted = (RvDir)DB.DirTree.Child(1);
            Unsorted.ChildAdd(UnsortedRom);

            return ReturnCode.Good;

        }

        private static ReturnCode FixFileMoveToCorrupt(RvFile fixFile)
        {
            string corruptDir = Path.Combine(Settings.Unsorted(), "Corrupt");
            if (!Directory.Exists(corruptDir))
            {
                Directory.CreateDirectory(corruptDir);
            }

            string fixFileFullName = fixFile.FullName;

            string UnsortedCorruptFullName = Path.Combine(corruptDir, fixFile.Name);
            string UnsortedCorruptFileName = fixFile.Name;
            int fileC = 0;
            while (File.Exists(UnsortedCorruptFullName))
            {
                fileC++;
                UnsortedCorruptFileName = fixFile.Name + fileC;
                UnsortedCorruptFullName = Path.Combine(corruptDir, UnsortedCorruptFileName);
            }

            //create new Unsorted record
            // FileInfo UnsortedCorruptFile = new FileInfo(UnsortedCorruptFullName);
            RvFile UnsortedCorruptRom = new RvFile(FileType.File)
            {
                Name = UnsortedCorruptFileName,
                Size = fixFile.Size,
                CRC = fixFile.CRC,
                //TimeStamp = UnsortedFile.LastWriteTime,
                DatStatus = DatStatus.InUnsorted
            };

            _bgw.ReportProgress(0, new bgwShowFix(Path.GetDirectoryName(fixFileFullName), "", Path.GetFileName(fixFileFullName), fixFile.Size, "-->", "Corrupt", "", fixFile.Name));

            ZipFile tempZipOut = null;
            RvFile foundFile;
            ReturnCode returnCode = FixFileCopy.CopyFile(fixFile, ref tempZipOut, UnsortedCorruptFullName, UnsortedCorruptRom, false, out _error, out foundFile);
            switch (returnCode)
            {
                case ReturnCode.Good: // correct reply to continue;
                    break;
                default:
                    return returnCode;
            }

            string fixFilePath = fixFile.FullName;
            if (!File.SetAttributes(fixFilePath, FileAttributes.Normal))
            {
                int error = Error.GetLastError();
                _bgw.ReportProgress(0, new bgwShowError(fixFilePath, "Error Setting File Attributes to Normal. Before Delete Moving Unsorted. Code " + error));
            }
            File.Delete(fixFilePath);

            // here we just deleted a file so also delete it from the DB,
            // and recurse up deleting unnedded DIR's
            CheckDeleteObject(fixFile);

            RvDir Unsorted = (RvDir)DB.DirTree.Child(1);
            int indexcorrupt;
            RvDir rvCorruptDir = new RvDir(FileType.Dir) { Name = "Corrupt", DatStatus = DatStatus.InUnsorted };
            int found = Unsorted.ChildNameSearch(rvCorruptDir, out indexcorrupt);
            if (found != 0)
            {
                rvCorruptDir.GotStatus = GotStatus.Got;
                indexcorrupt = Unsorted.ChildAdd(rvCorruptDir);
            }

            ((RvDir)Unsorted.Child(indexcorrupt)).ChildAdd(UnsortedCorruptRom);

            return ReturnCode.Good;
        }


        private static ReturnCode FixFilePreCheckFixFile(RvFile fixFile)
        {
            string fileName = fixFile.FullName;

            // find all files in the DB with this name
            // there could be another file if:
            // there is a wrong file with the same name that can just be deleted
            // there is a wrong file with the same name that needs moved to Unsorted
            // there is a wrong file with the same name that is needed to fix another file
            List<RvBase> testList = new List<RvBase>();

            RvDir parent = fixFile.Parent;
            int index;
            // start by finding the first file in the DB. (This should always work, as it will at least find the current file that CanBeFixed
            if (parent.ChildNameSearch(fixFile, out index) != 0)
            {
                ReportError.Show("Logic error trying to find the file we are fixing " + fileName);
                return ReturnCode.LogicError;
            }
            testList.Add(parent.Child(index++));

            // now loop to see if there are any more files with the same name. (This is a case insensative compare)                        
            while (index < parent.ChildCount && DBHelper.CompareName(fixFile, parent.Child(index)) == 0)
            {
                testList.Add(parent.Child(index));
                index++;
            }

            // if we found more that one file in the DB then we need to process the incorrect file first.
            if (testList.Count > 1)
            {
                foreach (RvBase testChild in testList)
                {
                    if (testChild == fixFile)
                        continue;

                    if (testChild.DatStatus != DatStatus.NotInDat)
                    {
                        ReportError.Show(Resources.FixFiles_FixFile_Trying_to_fix_a_file_that_already_exists + fileName);
                        return ReturnCode.LogicError;
                    }

                    RvFile testFile = testChild as RvFile;
                    if (testFile == null)
                    {
                        ReportError.Show("Did not find a file logic error while fixing duplicate named file. in FixFile");
                        return ReturnCode.LogicError;
                    }

                    switch (testFile.RepStatus)
                    {
                        case RepStatus.Delete:
                            {
                                ReturnCode ret = FixFileDelete(testFile);
                                if (ret != ReturnCode.Good)
                                    return ret;
                                break;
                            }
                        case RepStatus.MoveUnsorted:
                            {
                                ReturnCode ret = FixFileMoveUnsorted(testFile);
                                if (ret != ReturnCode.Good)
                                    return ret;
                                break;
                            }
                        case RepStatus.MoveToCorrupt:
                            {
                                ReturnCode ret = FixFileMoveToCorrupt(testFile);
                                if (ret != ReturnCode.Good)
                                    return ret;
                                break;
                            }
                        case RepStatus.NeededForFix:
                        case RepStatus.Rename:
                            {
                                // so now we have found the file with the same case insensative name and can rename it to something else to get it out of the way for now.
                                // need to check that the .tmp filename does not already exists.
                                File.SetAttributes(testChild.FullName, FileAttributes.Normal);
                                File.Move(testChild.FullName, testChild.FullName + ".tmp");

                                if (!parent.FindChild(testChild, out index))
                                {
                                    ReportError.Show("Unknown file status in Matching File found of " + testFile.RepStatus);
                                    return ReturnCode.LogicError;
                                }
                                parent.ChildRemove(index);
                                testChild.Name = testChild.Name + ".tmp";
                                parent.ChildAdd(testChild);
                                break;
                            }
                        default:
                            {
                                ReportError.Show("Unknown file status in Matching File found of " + testFile.RepStatus);
                                return ReturnCode.LogicError;
                            }
                    }
                }
            }
            else
            {
                // if there is only one file in the DB then it must be the current file that CanBeFixed
                if (testList[0] != fixFile)
                {
                    ReportError.Show("Logic error trying to find the file we are fixing " + fileName + " DB found file does not match");
                    return ReturnCode.LogicError;
                }
            }
            return ReturnCode.Good;
        }

        private static ReturnCode FixFileCanBeFixed(RvFile fixFile)
        {
            string fixFileFullName = fixFile.FullName;
            CheckCreateParent(fixFile.Parent);

            // check to see if there is already a file with the name of the fixFile, and move it out the way.
            ReturnCode rc = FixFilePreCheckFixFile(fixFile);
            if (rc != ReturnCode.Good)
                return rc;


            // now we can fix the file.
            ZipFile tempZipOut = null;
            RvFile foundFile;
            ReturnCode returnCode;

            if (DBHelper.IsZeroLengthFile(fixFile))
            {
                RvFile fileIn = new RvFile(FileType.File);
                returnCode = FixFileCopy.CopyFile(fileIn, ref tempZipOut, fixFile.FullName, fixFile, false, out _error, out foundFile);

                switch (returnCode)
                {
                    case ReturnCode.Good: // correct reply to continue;
                        break;
                    default:
                        _error = fixFile.FullName + " " + fixFile.RepStatus + " " + returnCode + " : " + _error;
                        ReCheckFile(fixFile);
                        return ReturnCode.StartOver;
                }
                _fixed++;
                return ReturnCode.Good;
            }

#if NEWFINDFIX
                List<RvFile> lstFixRomTable = new List<RvFile>();
                List<RvFile> family = fixFile.MyFamily.Family;
                for (int iFind = 0; iFind < family.Count; iFind++)
                {
                    if (family[iFind].GotStatus == GotStatus.Got && FindFixes.CheckIfMissingFileCanBeFixedByGotFile(fixFile, family[iFind]))
                        lstFixRomTable.Add(family[iFind]);
                }
            RvFile fixingFile = lstFixRomTable[0];
#else

            // search for the database for the file to be used to repair this file:
            List<RvFile> lstFixRomTableCRC;
            DBHelper.RomSearchFindFixes(fixFile, _lstRomTableSortedCRCSize, out lstFixRomTableCRC);

            List<RvFile> lstFixRomTableSHA1CHD;
            DBHelper.RomSearchFindFixesSHA1CHD(fixFile, _lstRomTableSortedSHA1CHD, out lstFixRomTableSHA1CHD);

            if (lstFixRomTableCRC.Count == 0 && lstFixRomTableSHA1CHD.Count == 0)
            {
                // thought we could fix the file, turns out we cannot
                fixFile.GotStatus = GotStatus.NotGot;
                return ReturnCode.Good;
            }

            RvFile fixingFile =
                lstFixRomTableCRC.Count > 0 ?
                lstFixRomTableCRC[0] :
                lstFixRomTableSHA1CHD[0];
#endif
            string fts = fixingFile.FullName;
            ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixFileFullName), "", Path.GetFileName(fixFileFullName), fixFile.Size, "<--", Path.GetDirectoryName(fts), Path.GetFileName(fts), fixingFile.Name));



            returnCode = FixFileCopy.CopyFile(fixingFile, ref tempZipOut, fixFile.FullName, fixFile, false, out _error, out foundFile);

            switch (returnCode)
            {
                case ReturnCode.Good: // correct reply to continue;
                    break;

                case ReturnCode.SourceCRCCheckSumError:
                    {
                        ReportProgress(new bgwShowFixError("CRC Error"));
                        // the file we used for fix turns out to be corrupt

                        // mark the source file as Corrupt
                        fixingFile.GotStatus = GotStatus.Corrupt;

                        // recheck for the fix
                        ReCheckFile(fixFile);

                        CheckReprocess(fixingFile);

                        // and go back one and try again.
                        return ReturnCode.StartOver;
                    }


                case ReturnCode.SourceCheckSumError:
                    {
                        // the file we used for fix turns out not not match its own DAT's correct MD5/SHA1
                        // (Problem with logic here is that it could still match the file being fixed, but this case is not correctly handled)
                        ReportProgress(new bgwShowFixError("Failed"));


                        // remove the file we thought we correctly had (The file that we where trying to use for the fix)
                        if (fixingFile.FileRemove() == EFile.Delete)
                        {
                            _error = "Should not mark for delete as it is in a DAT";
                            return ReturnCode.LogicError;
                        }

                        // possibly use a check here to see if the index of the found file is futher down the zip and so we can just contine
                        // instead of restarting.

                        // add in the actual file we found
                        fixingFile.Parent.ChildAdd(foundFile);
                        AddFoundFile(foundFile);

                        // recheck for the fix
                        ReCheckFile(fixFile);

                        CheckReprocess(fixingFile);

                        // and go back one and try again.
                        return ReturnCode.StartOver;
                    }
                case ReturnCode.DestinationCheckSumError:
                    {
                        ReportProgress(new bgwShowFixError("Failed"));

                        // recheck for the fix
                        ReCheckFile(fixFile);
                        return ReturnCode.StartOver;
                    }
                default:
                    return returnCode;
            }


            CheckReprocessClearList();
            // Check the files that we found that where used to fix this file, and if they not listed as correct files, they can be set to be deleted.
            
#if NEWFINDFIX
            foreach (RvFile file in lstFixRomTable)
            {
                if (file.RepStatus != RepStatus.NeededForFix && file.RepStatus != RepStatus.Rename) continue;
                file.RepStatus = RepStatus.Delete;
                CheckReprocess(file, true);
            }
#else

            foreach (RvFile file in lstFixRomTableCRC)
            {
                if (file.RepStatus != RepStatus.NeededForFix && file.RepStatus != RepStatus.Rename) continue;
                file.RepStatus = RepStatus.Delete;
                CheckReprocess(file, true);
            }
            foreach (RvFile file in lstFixRomTableSHA1CHD)
            {
                if (file.RepStatus != RepStatus.NeededForFix && file.RepStatus != RepStatus.Rename) continue;
                file.RepStatus = RepStatus.Delete;
                CheckReprocess(file, true);
            }
#endif
            CheckReprocessFinalCheck();

            _fixed++;

            return ReturnCode.Good;
        }



        private static ReturnCode FixZip(RvDir fixZip)
        {
            //Check for error status
            if (fixZip.DirStatus.HasUnknown())
                return ReturnCode.FindFixes; // Error

            bool needsTrrntzipped = fixZip.ZipStatus != ZipStatus.TrrntZip && fixZip.GotStatus == GotStatus.Got && fixZip.DatStatus == DatStatus.InDatCollect && (Settings.FixLevel == eFixLevel.TrrntZipLevel1 || Settings.FixLevel == eFixLevel.TrrntZipLevel2 || Settings.FixLevel == eFixLevel.TrrntZipLevel3);

            // file corrupt and not in Unsorted
            //      if file cannot be fully fixed copy to corrupt
            //      process zipfile

            if (fixZip.GotStatus == GotStatus.Corrupt && fixZip.DatStatus != DatStatus.InUnsorted)
            {
                ReturnCode movReturnCode = MoveZiptoCorrupt(fixZip);
                if (movReturnCode != ReturnCode.Good)
                    return movReturnCode;
            }

            // has fixable
            //      process zipfile

            else if (fixZip.DirStatus.HasFixable())
            {
                // do nothing here but continue on to process zip.
            }

            // need trrntzipped
            //      process zipfile

            else if (needsTrrntzipped)
            {
                // do nothing here but continue on to process zip.
            }


            // got empty zip that should be deleted
            //      process zipfile
            else if (fixZip.GotStatus == GotStatus.Got && fixZip.GotStatus != GotStatus.Corrupt && !fixZip.DirStatus.HasAnyFiles())
            {
                // do nothing here but continue on to process zip.
            }

            // else
            //      skip this zipfile
            else
            {
                // nothing can be done to return
                return ReturnCode.Good;
            }



            string fixZipFullName = fixZip.TreeFullName;

            if (!fixZip.DirStatus.HasFixable() && needsTrrntzipped)
                ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixZipFullName), Path.GetFileName(fixZipFullName), "", 0, "TrrntZipping", "", "", ""));


            CheckCreateParent(fixZip.Parent);
            ReportError.LogOut("");
            ReportError.LogOut(fixZipFullName + " : " + fixZip.RepStatus);
            ReportError.LogOut("------------------------------------------------------------");
            Debug.WriteLine(fixZipFullName + " : " + fixZip.RepStatus);
            ReportError.LogOut("Zip File Status Before Fix:");
            for (int intLoop = 0; intLoop < fixZip.ChildCount; intLoop++)
                ReportError.LogOut((RvFile)fixZip.Child(intLoop));
            ReportError.LogOut("");

            ZipFile tempZipOut = null;

            ZipFile UnsortedCorruptOut = null;
            ZipFile UnsortedZipOut = null;

            RvDir UnsortedGame = null;
            RvDir UnsortedCorruptGame = null;

            ReturnCode returnCode;
            List<RvFile> fixZipTemp = new List<RvFile>();

            for (int iRom = 0; iRom < fixZip.ChildCount; iRom++)
            {
                RvFile zipFileFixing = new RvFile(FileType.ZipFile);
                fixZip.Child(iRom).CopyTo(zipFileFixing);

                if (iRom == fixZipTemp.Count)
                    fixZipTemp.Add(zipFileFixing);
                else
                    fixZipTemp[iRom] = zipFileFixing;

                ReportError.LogOut(zipFileFixing.RepStatus + " : " + fixZip.Child(iRom).FullName);

                switch (zipFileFixing.RepStatus)
                {
                    #region Nothing to copy
                    // any file we do not have or do not want in the destination zip
                    case RepStatus.Missing:
                    case RepStatus.NotCollected:
                    case RepStatus.Rename:
                    case RepStatus.Delete:
                        if (!
                              (
                            // got the file in the original zip but will be deleting it
                                (zipFileFixing.DatStatus == DatStatus.NotInDat && zipFileFixing.GotStatus == GotStatus.Got) ||
                                (zipFileFixing.DatStatus == DatStatus.NotInDat && zipFileFixing.GotStatus == GotStatus.Corrupt) ||
                                (zipFileFixing.DatStatus == DatStatus.InDatMerged && zipFileFixing.GotStatus == GotStatus.Got) ||
                                (zipFileFixing.DatStatus == DatStatus.InUnsorted && zipFileFixing.GotStatus == GotStatus.Got) ||
                                (zipFileFixing.DatStatus == DatStatus.InUnsorted && zipFileFixing.GotStatus == GotStatus.Corrupt) ||

                                // do not have this file and cannot fix it here
                                (zipFileFixing.DatStatus == DatStatus.InDatCollect && zipFileFixing.GotStatus == GotStatus.NotGot) ||
                                (zipFileFixing.DatStatus == DatStatus.InDatBad && zipFileFixing.GotStatus == GotStatus.NotGot) ||
                                (zipFileFixing.DatStatus == DatStatus.InDatMerged && zipFileFixing.GotStatus == GotStatus.NotGot)
                                )
                            )
                            ReportError.SendAndShow(Resources.FixFiles_FixZip_Error_in_Fix_Rom_Status + zipFileFixing.RepStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.DatStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.GotStatus);

                        if (zipFileFixing.RepStatus == RepStatus.Delete)
                        {
                            returnCode = DoubleCheckDelete(zipFileFixing);
                            if (returnCode != ReturnCode.Good)
                                goto ZipOpenFailed;
                        }

                        zipFileFixing.GotStatus = GotStatus.NotGot;
                        break;
                    #endregion
                    #region Copy from Original to Destination
                    // any files we are just moving from the original zip to the destination zip
                    case RepStatus.Correct:
                    case RepStatus.InUnsorted:
                    case RepStatus.NeededForFix:
                    case RepStatus.Corrupt:
                        {
                            if (!
                                  (
                                    (zipFileFixing.DatStatus == DatStatus.InDatCollect && zipFileFixing.GotStatus == GotStatus.Got) ||
                                    (zipFileFixing.DatStatus == DatStatus.InDatMerged && zipFileFixing.GotStatus == GotStatus.Got) ||
                                    (zipFileFixing.DatStatus == DatStatus.NotInDat && zipFileFixing.GotStatus == GotStatus.Got) ||
                                    (zipFileFixing.DatStatus == DatStatus.InUnsorted && zipFileFixing.GotStatus == GotStatus.Got) ||
                                    (zipFileFixing.DatStatus == DatStatus.InUnsorted && zipFileFixing.GotStatus == GotStatus.Corrupt)
                                  )
                                )
                                ReportError.SendAndShow(Resources.FixFiles_FixZip_Error_in_Fix_Rom_Status + zipFileFixing.RepStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.DatStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.GotStatus);

                            RvFile foundFile;

                            bool rawcopy = (zipFileFixing.RepStatus == RepStatus.InUnsorted) || (zipFileFixing.RepStatus == RepStatus.Corrupt);
                            // Correct      rawcopy=false
                            // NeededForFix rawcopy=false
                            // InUnsorted     rawcopy=true
                            // Corrupt      rawcopy=true
                            RepStatus originalStatus = zipFileFixing.RepStatus;

                            returnCode = FixFileCopy.CopyFile(
                                (RvFile)fixZip.Child(iRom),
                                ref tempZipOut,
                                Path.Combine(fixZip.Parent.FullName, "__ROMVault.tmp"),
                                zipFileFixing, rawcopy,
                                out _error, out foundFile);

                            switch (returnCode)
                            {
                                case ReturnCode.Good: // correct reply to continue;
                                    if (originalStatus == RepStatus.NeededForFix)
                                        zipFileFixing.RepStatus = RepStatus.NeededForFix;
                                    break;
                                case ReturnCode.SourceCRCCheckSumError:
                                    {
                                        RvFile tFile = (RvFile)fixZip.Child(iRom);
                                        tFile.GotStatus = GotStatus.Corrupt;
                                        ReCheckFile(tFile);

                                        //decrease index so this file gets reprocessed
                                        iRom--;

                                        continue;
                                    }
                                case ReturnCode.SourceCheckSumError:
                                    {
                                        // Set the file in the zip that we thought we correctly had as missing
                                        RvFile tFile = (RvFile)fixZip.Child(iRom);
                                        if (tFile.FileRemove() == EFile.Delete)
                                        {
                                            _error = "Should not mark for delete as it is in a DAT";
                                            return ReturnCode.LogicError;
                                        }

                                        // Add in at the current location the incorrect file. (This file will be reprocessed and then at some point deleted.)
                                        fixZip.ChildAdd(foundFile, iRom);
                                        AddFoundFile(foundFile);

                                        ReCheckFile(tFile);

                                        //decrease index so this file gets reprocessed
                                        iRom--;

                                        continue;
                                    }
                                // not needed as source and destination are the same
                                // case ReturnCode.DestinationCheckSumError:  
                                default:
                                    _error = zipFileFixing.FullName + " " + zipFileFixing.RepStatus + " " + returnCode + " : " + _error;
                                    goto ZipOpenFailed;
                            }
                        }
                        break;
                    #endregion

                    #region Case.CanBeFixed
                    case RepStatus.CanBeFixed:
                    case RepStatus.CorruptCanBeFixed:
                        {
                            if (!(zipFileFixing.DatStatus == DatStatus.InDatCollect && (zipFileFixing.GotStatus == GotStatus.NotGot || zipFileFixing.GotStatus == GotStatus.Corrupt)))
                                ReportError.SendAndShow(Resources.FixFiles_FixZip_Error_in_Fix_Rom_Status + zipFileFixing.RepStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.DatStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.GotStatus);

                            ReportError.LogOut("Fixing File:");
                            ReportError.LogOut(zipFileFixing);

                            string strPath = fixZip.Parent.FullName;
                            string tempZipFilename = Path.Combine(strPath, "__ROMVault.tmp");


                            if (DBHelper.IsZeroLengthFile(zipFileFixing))
                            {
                                RvFile fileIn = new RvFile(FileType.ZipFile) { Size = 0 };
                                RvFile foundFile;
                                returnCode = FixFileCopy.CopyFile(fileIn, ref tempZipOut, tempZipFilename, zipFileFixing, false, out _error, out foundFile);

                                switch (returnCode)
                                {
                                    case ReturnCode.Good: // correct reply to continue;
                                        break;
                                    default:
                                        _error = zipFileFixing.FullName + " " + zipFileFixing.RepStatus + " " + returnCode + " : " + _error;
                                        goto ZipOpenFailed;
                                }
                                break;
                            }

#if NEWFINDFIX
                            List<RvFile> lstFixRomTable = new List<RvFile>();
                            List<RvFile> family = zipFileFixing.MyFamily.Family;
                            for (int iFind = 0; iFind < family.Count; iFind++)
                            {
                                if (family[iFind].GotStatus == GotStatus.Got && FindFixes.CheckIfMissingFileCanBeFixedByGotFile(zipFileFixing, family[iFind]))
                                    lstFixRomTable.Add(family[iFind]);
                            }
#else
                            List<RvFile> lstFixRomTable;
                            DBHelper.RomSearchFindFixes(zipFileFixing, _lstRomTableSortedCRCSize, out lstFixRomTable);
#endif

                            ReportError.LogOut("Found Files To use for Fixes:");
                            foreach (RvFile t in lstFixRomTable)
                                ReportError.LogOut(t);

                            if (lstFixRomTable.Count > 0)
                            {

                                string ts = lstFixRomTable[0].Parent.FullName;
                                string sourceDir;
                                string sourceFile;
                                if (lstFixRomTable[0].FileType == FileType.ZipFile)
                                {
                                    sourceDir = Path.GetDirectoryName(ts);
                                    sourceFile = Path.GetFileName(ts);
                                }
                                else
                                {
                                    sourceDir = ts;
                                    sourceFile = "";
                                }
                                ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixZipFullName), Path.GetFileName(fixZipFullName), zipFileFixing.Name, zipFileFixing.Size, "<--", sourceDir, sourceFile, lstFixRomTable[0].Name));

                                RvFile foundFile;
                                returnCode = FixFileCopy.CopyFile(lstFixRomTable[0], ref tempZipOut, tempZipFilename, zipFileFixing, false, out _error, out foundFile);
                                switch (returnCode)
                                {
                                    case ReturnCode.Good: // correct reply so continue;
                                        break;

                                    case ReturnCode.SourceCRCCheckSumError:
                                        {
                                            ReportProgress(new bgwShowFixError("CRC Error"));
                                            // the file we used for fix turns out to be corrupt

                                            RvFile tFile = (RvFile)fixZip.Child(iRom);

                                            // mark the source file as Corrupt
                                            lstFixRomTable[0].GotStatus = GotStatus.Corrupt;

                                            // recheck for the fix
                                            ReCheckFile(tFile);

                                            // if the file being used from the fix is actually from this file then we have a big mess, and we are just going to
                                            // start over on this zip.
                                            if (lstFixRomTable[0].Parent == fixZip)
                                            {
                                                returnCode = ReturnCode.StartOver;
                                                goto ZipOpenFailed;
                                            }

                                            // add the fixing source zip into the processList so that it is also reprocessed and we just changed it.
                                            if (!_processList.Contains(lstFixRomTable[0].Parent))
                                                _processList.Add(lstFixRomTable[0].Parent);

                                            // and go back one and try again.
                                            iRom--;
                                            continue;
                                        }


                                    case ReturnCode.SourceCheckSumError:
                                        {
                                            ReportProgress(new bgwShowFixError("Failed"));
                                            // the file we used for fix turns out not not match its own DAT's correct MD5/SHA1
                                            // (Problem with logic here is that it could still match the file being fixed, but this case is not correctly handled)

                                            RvFile tFile = (RvFile)fixZip.Child(iRom);

                                            // remove the file we thought we correctly had (The file that we where trying to use for the fix)
                                            if (lstFixRomTable[0].FileRemove() == EFile.Delete)
                                            {
                                                _error = "Should not mark for delete as it is in a DAT";
                                                return ReturnCode.LogicError;
                                            }

                                            // possibly use a check here to see if the index of the found file is futher down the zip and so we can just contine
                                            // instead of restarting.

                                            // add in the actual file we found
                                            lstFixRomTable[0].Parent.ChildAdd(foundFile);
                                            AddFoundFile(foundFile);

                                            // recheck for the fix
                                            ReCheckFile(tFile);

                                            // if the file being used from the fix is actually from this file then we have a big mess, and we are just going to
                                            // start over on this zip.
                                            if (lstFixRomTable[0].Parent == fixZip)
                                            {
                                                returnCode = ReturnCode.StartOver;
                                                goto ZipOpenFailed;
                                            }

                                            // add the fixing source zip into the processList so that it is also reprocessed and we just changed it.
                                            if (!_processList.Contains(lstFixRomTable[0].Parent))
                                                _processList.Add(lstFixRomTable[0].Parent);

                                            // and go back one and try again.
                                            iRom--;
                                            continue;
                                        }
                                    case ReturnCode.DestinationCheckSumError:
                                        {
                                            ReportProgress(new bgwShowFixError("Failed"));
                                            // the file we used for fix turns out not to have the correct MD5/SHA1 
                                            RvFile tFile = (RvFile)fixZip.Child(iRom);

                                            // recheck for the fix
                                            ReCheckFile(tFile);

                                            // if the file being used from the fix is actually from this file then we have a big mess, and we are just going to
                                            // start over on this zip.
                                            // The need for this is that the file being pulled in from inside this zip will be marked as Rename
                                            // and so would then automatically be deleted, in the case this exception happens, this source file instead
                                            // should be set to move to Unsorted. 
                                            if (lstFixRomTable[0].Parent == fixZip)
                                            {
                                                returnCode = ReturnCode.StartOver;
                                                goto ZipOpenFailed;
                                            }

                                            // and go back one and try again.
                                            iRom--;
                                            continue;
                                        }
                                    default:
                                        //_error = zipFileFixing.FullName + " " + zipFileFixing.RepStatus + " " + returnCode + Environment.NewLine + _error;
                                        goto ZipOpenFailed;
                                }

                                //Check to see if the files used for fix, can now be set to delete
                                CheckReprocessClearList();

                                foreach (RvFile tFixRom in lstFixRomTable)
                                {
                                    if (tFixRom.RepStatus == RepStatus.NeededForFix)
                                    {
                                        tFixRom.RepStatus = RepStatus.Delete;
                                        ReportError.LogOut("Setting File Status to Delete:");
                                        ReportError.LogOut(tFixRom);
                                        CheckReprocess(tFixRom, true);
                                    }
                                }
                                CheckReprocessFinalCheck();

                                _fixed++;
                            }
                            else
                                // thought we could fix it, turns out we cannot
                                zipFileFixing.GotStatus = GotStatus.NotGot;
                        }
                        break;
                    #endregion
                    #region Case.MoveUnsorted
                    case RepStatus.MoveUnsorted:
                        {
                            if (!(zipFileFixing.DatStatus == DatStatus.NotInDat && zipFileFixing.GotStatus == GotStatus.Got))
                                ReportError.SendAndShow(Resources.FixFiles_FixZip_Error_in_Fix_Rom_Status + zipFileFixing.RepStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.DatStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.GotStatus);

                            ReportError.LogOut("Moving File out to Unsorted:");
                            ReportError.LogOut(zipFileFixing);
                            // move the rom out to the To Sort Directory

                            if (UnsortedGame == null)
                            {
                                string UnsortedFullName = Path.Combine(Settings.Unsorted(), fixZip.Name + ".zip");
                                string UnsortedFileName = fixZip.Name;
                                int fileC = 0;
                                while (File.Exists(UnsortedFullName))
                                {
                                    fileC++;
                                    UnsortedFullName = Path.Combine(Settings.Unsorted(), fixZip.Name + fileC + ".zip");
                                    UnsortedFileName = fixZip.Name + fileC;
                                }

                                UnsortedGame = new RvDir(FileType.Zip)
                                                 {
                                                     Name = UnsortedFileName,
                                                     DatStatus = DatStatus.InUnsorted,
                                                     GotStatus = GotStatus.Got
                                                 };
                            }

                            RvFile UnsortedRom = new RvFile(FileType.ZipFile)
                                                      {
                                                          Name = zipFileFixing.Name,
                                                          Size = zipFileFixing.Size,
                                                          CRC = zipFileFixing.CRC,
                                                          SHA1 = zipFileFixing.SHA1,
                                                          MD5 = zipFileFixing.MD5
                                                      };
                            UnsortedRom.SetStatus(DatStatus.InUnsorted, GotStatus.Got);
                            UnsortedRom.FileStatusSet(
                                FileStatus.SizeFromHeader | FileStatus.SizeVerified |
                                FileStatus.CRCFromHeader | FileStatus.CRCVerified |
                                FileStatus.SHA1FromHeader | FileStatus.SHA1Verified |
                                FileStatus.MD5FromHeader | FileStatus.MD5Verified
                                , zipFileFixing);

                            UnsortedGame.ChildAdd(UnsortedRom);

                            string destination = Path.Combine(Settings.Unsorted(), UnsortedGame.Name + ".zip");
                            ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixZipFullName), Path.GetFileName(fixZipFullName), zipFileFixing.Name, zipFileFixing.Size, "-->", "Unsorted", Path.GetFileName(destination), UnsortedRom.Name));

                            RvFile foundFile;
                            returnCode = FixFileCopy.CopyFile((RvFile)fixZip.Child(iRom), ref UnsortedZipOut, destination, UnsortedRom, true, out _error, out foundFile);
                            switch (returnCode)
                            {
                                case ReturnCode.Good: // correct reply to continue;
                                    break;
                                //raw copying so Checksums are not checked
                                //case ReturnCode.SourceCRCCheckSumError: 
                                //case ReturnCode.SourceCheckSumError:
                                //case ReturnCode.DestinationCheckSumError: 
                                default:
                                    _error = zipFileFixing.FullName + " " + zipFileFixing.RepStatus + " " + returnCode + " : " + _error;
                                    goto ZipOpenFailed;
                            }
                            zipFileFixing.GotStatus = GotStatus.NotGot; // Changes RepStatus to Deleted
                        }
                        break;
                    #endregion
                    #region Case.MoveToCorrupt
                    case RepStatus.MoveToCorrupt:
                        {
                            if (!((zipFileFixing.DatStatus == DatStatus.InDatCollect || zipFileFixing.DatStatus == DatStatus.NotInDat) && zipFileFixing.GotStatus == GotStatus.Corrupt))
                                ReportError.SendAndShow(Resources.FixFiles_FixZip_Error_in_Fix_Rom_Status + zipFileFixing.RepStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.DatStatus + Resources.FixFiles_FixZip_Colon + zipFileFixing.GotStatus);

                            ReportError.LogOut("Moving File to Corrupt");
                            ReportError.LogOut(zipFileFixing);

                            string UnsortedFullName;
                            if (UnsortedCorruptGame == null)
                            {
                                string corruptDir = Path.Combine(Settings.Unsorted(), "Corrupt");
                                if (!Directory.Exists(corruptDir))
                                {
                                    Directory.CreateDirectory(corruptDir);
                                }

                                UnsortedFullName = Path.Combine(corruptDir, fixZip.Name + ".zip");
                                string UnsortedFileName = fixZip.Name;
                                int fileC = 0;
                                while (File.Exists(UnsortedFullName))
                                {
                                    fileC++;
                                    UnsortedFullName = Path.Combine(corruptDir, fixZip.Name + fileC + ".zip");
                                    UnsortedFileName = fixZip.Name + fileC;
                                }

                                UnsortedCorruptGame = new RvDir(FileType.Zip)
                                                        {
                                                            Name = UnsortedFileName,
                                                            DatStatus = DatStatus.InUnsorted,
                                                            GotStatus = GotStatus.Got
                                                        };
                            }
                            else
                            {
                                string corruptDir = Path.Combine(Settings.Unsorted(), "Corrupt");
                                UnsortedFullName = Path.Combine(corruptDir, UnsortedCorruptGame.Name + ".zip");
                            }

                            RvFile UnsortedCorruptRom = new RvFile(FileType.ZipFile)
                                                             {
                                                                 Name = zipFileFixing.Name,
                                                                 Size = zipFileFixing.Size,
                                                                 CRC = zipFileFixing.CRC
                                                             };
                            UnsortedCorruptRom.SetStatus(DatStatus.InUnsorted, GotStatus.Corrupt);
                            UnsortedCorruptGame.ChildAdd(UnsortedCorruptRom);

                            ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixZipFullName), Path.GetFileName(fixZipFullName), zipFileFixing.Name, zipFileFixing.Size, "-->", "Corrupt", Path.GetFileName(UnsortedFullName), zipFileFixing.Name));

                            RvFile foundFile;
                            returnCode = FixFileCopy.CopyFile((RvFile)fixZip.Child(iRom), ref UnsortedCorruptOut, UnsortedFullName, UnsortedCorruptRom, true, out _error, out foundFile);
                            switch (returnCode)
                            {
                                case ReturnCode.Good: // correct reply to continue;
                                    break;

                                // doing a raw copy so not needed
                                // case ReturnCode.SourceCRCCheckSumError: 
                                // case ReturnCode.SourceCheckSumError:
                                // case ReturnCode.DestinationCheckSumError: 
                                default:
                                    _error = zipFileFixing.FullName + " " + zipFileFixing.RepStatus + " " + returnCode + " : " + _error;
                                    goto ZipOpenFailed;
                            }
                            zipFileFixing.GotStatus = GotStatus.NotGot;
                        }
                        break;
                    #endregion
                    default:



                        ReportError.UnhandledExceptionHandler("Unknown file status found " + zipFileFixing.RepStatus + " while fixing file " + fixZip.Name + " Dat Status = " + zipFileFixing.DatStatus + " GotStatus " + zipFileFixing.GotStatus);
                        break;
                }
            }


            #region if Unsorted Zip Made then close the zip and add this new zip to the Database
            if (UnsortedGame != null)
            {
                UnsortedZipOut.ZipFileClose();

                UnsortedGame.TimeStamp = UnsortedZipOut.TimeStamp;
                UnsortedGame.DatStatus = DatStatus.InUnsorted;
                UnsortedGame.GotStatus = GotStatus.Got;
                UnsortedGame.ZipStatus = UnsortedZipOut.ZipStatus;

                RvDir Unsorted = (RvDir)DB.DirTree.Child(1);
                Unsorted.ChildAdd(UnsortedGame);
            }
            #endregion

            #region if Corrupt Zip Made then close the zip and add this new zip to the Database
            if (UnsortedCorruptGame != null)
            {
                UnsortedCorruptOut.ZipFileClose();

                UnsortedCorruptGame.TimeStamp = UnsortedCorruptOut.TimeStamp;
                UnsortedCorruptGame.DatStatus = DatStatus.InUnsorted;
                UnsortedCorruptGame.GotStatus = GotStatus.Got;

                RvDir Unsorted = (RvDir)DB.DirTree.Child(1);
                int indexcorrupt;
                RvDir corruptDir = new RvDir(FileType.Dir) { Name = "Corrupt", DatStatus = DatStatus.InUnsorted };
                int found = Unsorted.ChildNameSearch(corruptDir, out indexcorrupt);
                if (found != 0)
                {
                    corruptDir.GotStatus = GotStatus.Got;
                    indexcorrupt = Unsorted.ChildAdd(corruptDir);
                }
                ((RvDir)Unsorted.Child(indexcorrupt)).ChildAdd(UnsortedCorruptGame);
            }
            #endregion



            #region Process original Zip
            string filename = fixZip.FullName;
            if (File.Exists(filename))
            {
                if (!File.SetAttributes(filename, FileAttributes.Normal))
                {
                    int error = Error.GetLastError();
                    ReportProgress(new bgwShowError(filename, "Error Setting File Attributes to Normal. Deleting Original Fix File. Code " + error));
                }
                try
                {
                    File.Delete(filename);
                }
                catch (Exception)
                {
                    int error = Error.GetLastError();
                    _error = "Error While trying to delete file " + filename + ". Code " + error;

                    if (tempZipOut != null && tempZipOut.ZipOpen != ZipOpenType.Closed)
                        tempZipOut.ZipFileClose();

                    return ReturnCode.RescanNeeded;
                }

            }
            #endregion

            bool checkDelete = false;
            #region process the temp Zip rename it to the original Zip
            if (tempZipOut != null && tempZipOut.ZipOpen != ZipOpenType.Closed)
            {
                string tempFilename = tempZipOut.ZipFilename;
                tempZipOut.ZipFileClose();

                if (tempZipOut.LocalFilesCount() > 0)
                {
                    // now rename the temp fix file to the correct filename
                    File.Move(tempFilename, filename);
                    FileInfo nFile = new FileInfo(filename);
                    RvDir tmpZip = new RvDir(FileType.Zip)
                                       {
                                           Name = Path.GetFileNameWithoutExtension(filename),
                                           TimeStamp = nFile.LastWriteTime
                                       };
                    tmpZip.SetStatus(fixZip.DatStatus, GotStatus.Got);

                    fixZip.FileAdd(tmpZip);
                    fixZip.ZipStatus = tempZipOut.ZipStatus;
                }
                else
                {
                    File.Delete(tempFilename);
                    checkDelete = true;
                }
            }
            else
                checkDelete = true;
            #endregion

            #region Now put the New Game Status information into the Database.
            int intLoopFix = 0;
            foreach (RvFile tmpZip in fixZipTemp)
            {
                tmpZip.CopyTo(fixZip.Child(intLoopFix));

                if (fixZip.Child(intLoopFix).RepStatus == RepStatus.Deleted)
                    if (fixZip.Child(intLoopFix).FileRemove() == EFile.Delete)
                    {
                        fixZip.ChildRemove(intLoopFix);
                        continue;
                    }

                intLoopFix++;
            }
            #endregion

            if (checkDelete)
                CheckDeleteObject(fixZip);

            ReportError.LogOut("");
            ReportError.LogOut("Zip File Status After Fix:");
            for (int intLoop = 0; intLoop < fixZip.ChildCount; intLoop++)
                ReportError.LogOut((RvFile)fixZip.Child(intLoop));
            ReportError.LogOut("");

            return ReturnCode.Good;


        ZipOpenFailed:
            if (tempZipOut != null) tempZipOut.ZipFileCloseFailed();
            if (UnsortedZipOut != null) UnsortedZipOut.ZipFileCloseFailed();
            if (UnsortedCorruptOut != null) UnsortedCorruptOut.ZipFileCloseFailed();
            return returnCode;

        }

        private static void AddFoundFile(RvFile foundFile)
        {
            if ((foundFile.Size == null || foundFile.CRC == null) && foundFile.Size != 0) return;

            int intIndex;
            int intRes = DBHelper.RomSearchCRCSize(foundFile, _lstRomTableSortedCRCSize, out intIndex);
            if (intRes == 0)
                _lstRomTableSortedCRCSize.Insert(intIndex, foundFile);
        }

        private static void ReCheckFile(RvFile searchFile)
        {
#if NEWFINDFIX
            FindFixesNew.ListCheck(searchFile.MyFamily);
#else
            int index;
            int length;

            DBHelper.RomSearchFindMatchingFiles(searchFile, _lstRomTableSortedCRCSize, out index, out length);

            for (int i = index; i < index + length; i++)
                _lstRomTableSortedCRCSize[i].RepStatusReset();

            FindFixes.ListCheck(_lstRomTableSortedCRCSize, index, length);
#endif
        }

        private static ReturnCode DoubleCheckDelete(RvFile fileDeleting)
        {
            if (!Settings.DoubleCheckDelete)
                return ReturnCode.Good;


            ReportError.LogOut("Double Check deleting file ");
            ReportError.LogOut(fileDeleting);

            if (DBHelper.IsZeroLengthFile(fileDeleting))
                return ReturnCode.Good;

#if NEWFINDFIX
            List<RvFile> lstFixRomTable = new List<RvFile>();
            List<RvFile> family = fileDeleting.MyFamily.Family;
            for (int iFind = 0; iFind < family.Count; iFind++)
            {
                if (family[iFind].GotStatus == GotStatus.Got && FindFixes.CheckIfMissingFileCanBeFixedByGotFile(fileDeleting, family[iFind]))
                    lstFixRomTable.Add(family[iFind]);
            }
#else
            List<RvFile> lstFixRomTableCRCSize;
            List<RvFile> lstFixRomTableSHA1CHD;

            DBHelper.RomSearchFindFixes(fileDeleting, _lstRomTableSortedCRCSize, out lstFixRomTableCRCSize);
            DBHelper.RomSearchFindFixesSHA1CHD(fileDeleting, _lstRomTableSortedSHA1CHD, out lstFixRomTableSHA1CHD);

            List<RvFile> lstFixRomTable = new List<RvFile>();
            lstFixRomTable.AddRange(lstFixRomTableCRCSize);
            lstFixRomTable.AddRange(lstFixRomTableSHA1CHD);
#endif
            RvFile fileToCheck = null;
            int i = 0;
            while (i < lstFixRomTable.Count && fileToCheck == null)
            {

                switch (lstFixRomTable[i].RepStatus)
                {
                    case RepStatus.Delete:
                        i++;
                        break;
                    case RepStatus.Correct:
                    case RepStatus.InUnsorted:
                    case RepStatus.Rename:
                    case RepStatus.NeededForFix:
                    case RepStatus.MoveUnsorted:
                    case RepStatus.Ignore:
                        fileToCheck = lstFixRomTable[i];
                        break;
                    default:

                        ReportError.LogOut("Double Check Delete Error Unknown " + lstFixRomTable[i].FullName + " " + lstFixRomTable[i].RepStatus);
                        ReportError.UnhandledExceptionHandler("Unknown double check delete status " + lstFixRomTable[i].RepStatus);
                        break;
                }
            }
            //ReportError.LogOut("Found Files when double check deleting");
            //foreach (RvFile t in lstFixRomTable)
            //    ReportError.LogOut(t);

            if (fileToCheck == null)
            {
                ReportError.UnhandledExceptionHandler("Double Check Delete could not find the correct file. (" + fileDeleting.FullName + ")");
                //this line of code never gets called because the above line terminates the program.
                return ReturnCode.LogicError;
            }

            //if it is a file then 
            // check it exists and the filestamp matches
            //if it is a ZipFile then
            // check the parent zip exists and the filestamp matches

            switch (fileToCheck.FileType)
            {
                case FileType.ZipFile:
                    {
                        string fullPathCheckDelete = fileToCheck.Parent.FullName;
                        if (!File.Exists(fullPathCheckDelete))
                        {
                            _error = "Deleting " + fileDeleting.FullName + " Correct file not found. Resan for " + fullPathCheckDelete;
                            return ReturnCode.RescanNeeded;
                        }
                        FileInfo fi = new FileInfo(fullPathCheckDelete);
                        if (fi.LastWriteTime != fileToCheck.Parent.TimeStamp)
                        {
                            _error = "Deleting " + fileDeleting.FullName + " Correct file timestamp not found. Resan for " + fileToCheck.FullName;
                            return ReturnCode.RescanNeeded;
                        }
                        break;
                    }
                case FileType.File:
                    {
                        string fullPathCheckDelete = fileToCheck.FullName;
                        if (!File.Exists(fullPathCheckDelete))
                        {
                            _error = "Deleting " + fileDeleting.FullName + " Correct file not found. Resan for " + fullPathCheckDelete;
                            return ReturnCode.RescanNeeded;
                        }
                        FileInfo fi = new FileInfo(fullPathCheckDelete);
                        if (fi.LastWriteTime != fileToCheck.TimeStamp)
                        {
                            _error = "Deleting " + fileDeleting.FullName + " Correct file timestamp not found. Resan for " + fileToCheck.FullName;
                            return ReturnCode.RescanNeeded;
                        }
                        break;
                    }
                default:
                    ReportError.UnhandledExceptionHandler("Unknown double check delete status " + fileToCheck.RepStatus);
                    break;
            }


            return ReturnCode.Good;
        }

        private static ReturnCode MoveZiptoCorrupt(RvDir fixZip)
        {
            string fixZipFullName = fixZip.FullName;
            if (!File.Exists(fixZipFullName))
            {
                _error = "File for move to corrupt not found " + fixZip.FullName;
                return ReturnCode.RescanNeeded;
            }
            FileInfo fi = new FileInfo(fixZipFullName);
            if (fi.LastWriteTime != fixZip.TimeStamp)
            {
                _error = "File for move to corrupt timestamp not correct " + fixZip.FullName;
                return ReturnCode.RescanNeeded;
            }

            string corruptDir = Path.Combine(Settings.Unsorted(), "Corrupt");
            if (!Directory.Exists(corruptDir))
            {
                Directory.CreateDirectory(corruptDir);
            }

            RvDir Unsorted = (RvDir)DB.DirTree.Child(1);
            int indexcorrupt;
            RvDir corruptDirNew = new RvDir(FileType.Dir) { Name = "Corrupt", DatStatus = DatStatus.InUnsorted };
            int found = Unsorted.ChildNameSearch(corruptDirNew, out indexcorrupt);
            if (found != 0)
            {
                corruptDirNew.GotStatus = GotStatus.Got;
                indexcorrupt = Unsorted.ChildAdd(corruptDirNew);
            }

            string UnsortedFullName = Path.Combine(corruptDir, fixZip.Name + ".zip");
            string UnsortedFileName = fixZip.Name;
            int fileC = 0;
            while (File.Exists(UnsortedFullName))
            {
                fileC++;
                UnsortedFullName = Path.Combine(corruptDir, fixZip.Name + fileC + ".zip");
                UnsortedFileName = fixZip.Name + fileC;
            }

            if (!File.SetAttributes(fixZipFullName, FileAttributes.Normal))
            {
                int error = Error.GetLastError();
                ReportProgress(new bgwShowError(fixZipFullName, "Error Setting File Attributes to Normal. Before Moving To Corrupt. Code " + error));
            }


            File.Copy(fixZipFullName, UnsortedFullName);
            FileInfo UnsortedCorruptFile = new FileInfo(UnsortedFullName);

            RvDir UnsortedCorruptGame = new RvDir(FileType.Zip)
            {
                Name = UnsortedFileName,
                DatStatus = DatStatus.InUnsorted,
                TimeStamp = UnsortedCorruptFile.LastWriteTime,
                GotStatus = GotStatus.Corrupt
            };
            ((RvDir)Unsorted.Child(indexcorrupt)).ChildAdd(UnsortedCorruptGame);

            return ReturnCode.Good;
        }

        private static void CheckCreateParent(RvBase parent)
        {
            if (parent == DB.DirTree)
                return;

            string parentDir = parent.FullName;
            if (Directory.Exists(parentDir) && parent.GotStatus == GotStatus.Got) return;

            CheckCreateParent(parent.Parent);
            if (!Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);
            parent.GotStatus = GotStatus.Got;
        }

        private static void CheckDeleteObject(RvBase tBase)
        {
            if (tBase.RepStatus == RepStatus.Deleted)
                return;

            // look at the directories childrens status's to figure out if the directory should be deleted.
            if (tBase.FileType == FileType.Dir)
            {
                RvDir tDir = tBase as RvDir;
                if (tDir == null || tDir.ChildCount != 0) return;
                // check if we are at the root of the tree so that we do not delete ROMRoot and Unsorted
                if (tDir.Parent == DB.DirTree) return;

                string fullPath = tDir.FullName;
                try
                {
                    if (Directory.Exists(fullPath))
                        Directory.Delete(fullPath);
                }
                catch (Exception e)
                {
                    //need to report this to an error window
                    Debug.WriteLine(e.ToString());
                }
            }

            // else check if this tBase should be removed from the DB
            if (tBase.FileRemove() == EFile.Delete)
            {
                RvDir parent = tBase.Parent;

                if (parent == null)
                {
                    ReportError.UnhandledExceptionHandler("Item being deleted had no parent " + tBase.FullName);
                    return; // this never happens as UnhandledException Terminates the program
                }

                int index;

                if (!parent.FindChild(tBase, out index))
                {
                    ReportError.UnhandledExceptionHandler("Could not find self in delete code " + parent.FullName);
                }
                parent.ChildRemove(index);
                CheckDeleteObject(parent);
            }
        }




        private static List<RvDir> _checkList;
        private static void CheckReprocessClearList()
        {
            _checkList = new List<RvDir>();
        }
        private static void CheckReprocess(RvBase tCheck, bool fastProcess = false)
        {
            switch (tCheck.FileType)
            {
                case FileType.File:
                    if (tCheck.RepStatus == RepStatus.Delete && !_processList.Contains(tCheck))
                        _processList.Add(tCheck);
                    break;
                case FileType.ZipFile:

                    RvDir p = tCheck.Parent;
                    if (fastProcess)
                    {
                        if (!_checkList.Contains(p))
                            _checkList.Add(p);
                        break;
                    }
                    if (!_processList.Contains(p))
                    {
                        bool hasdelete = false;
                        bool hasNeededForFix = false;
                        for (int i = 0; i < p.ChildCount; i++)
                        {
                            RvBase f = p.Child(i);

                            if (f.RepStatus == RepStatus.Delete)
                                hasdelete = true;
                            else if (f.RepStatus == RepStatus.NeededForFix || f.RepStatus == RepStatus.Rename)
                            {
                                hasNeededForFix = true;
                                break;
                            }
                        }
                        if (hasdelete && !hasNeededForFix)
                        {
                            Debug.WriteLine(tCheck.Parent.FullName + " adding to process list.");
                            _processList.Add(p);
                        }
                    }
                    break;
                default:
                    ReportError.SendAndShow("Unknow repair file type recheck.");
                    break;

            }
        }
        private static void CheckReprocessFinalCheck()
        {
            foreach (RvDir p in _checkList)
            {

                if (_processList.Contains(p))
                    continue;
                bool hasdelete = false;
                bool hasNeededForFix = false;
                for (int i = 0; i < p.ChildCount; i++)
                {
                    RvBase f = p.Child(i);

                    if (f.RepStatus == RepStatus.Delete)
                        hasdelete = true;
                    else if (f.RepStatus == RepStatus.NeededForFix || f.RepStatus == RepStatus.Rename)
                    {
                        hasNeededForFix = true;
                        break;
                    }
                }
                if (!hasdelete || hasNeededForFix) continue;

                Debug.WriteLine(p.FullName + " adding to process list.");
                _processList.Add(p);
            }
        }
    }
}