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
using ROMVault2.SupportedFiles.CHD;
using ROMVault2.SupportedFiles.Files;
using ROMVault2.SupportedFiles.Zip;
using ROMVault2.Utils;
using FileInfo = ROMVault2.IO.FileInfo;

namespace ROMVault2
{
    public static class FileScanning
    {
        private static Stopwatch _cacheSaveTimer;
        private static long _lastUpdateTime;
        private static BackgroundWorker _bgw;
        public static eScanLevel EScanLevel;
        private static bool _fileErrorAbort;


        public static void ScanFiles(object sender, DoWorkEventArgs e)
        {

#if !Debug
            try
            {
#endif
                _fileErrorAbort = false;
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


                _bgw.ReportProgress(0, new bgwText("Clearing DB Status"));
                RepairStatus.ReportStatusReset(DB.DirTree);

                _bgw.ReportProgress(0, new bgwText("Finding Dir's to Scan"));
                //Next get a list of all the directories to be scanned
                List<RvDir> lstDir = new List<RvDir>();
                DBHelper.GetSelectedDirList(ref lstDir);


                _bgw.ReportProgress(0, new bgwText("Scanning Dir's"));
                _bgw.ReportProgress(0, new bgwSetRange(lstDir.Count - 1));
                //Scan the list of directories.
                for (int i = 0; i < lstDir.Count; i++)
                {
                    _bgw.ReportProgress(i);
                    _bgw.ReportProgress(0, new bgwText("Scanning Dir : " + lstDir[i].FullName));
                    string lDir = lstDir[i].FullName;
                    Console.WriteLine(lDir);
                    if (Directory.Exists(lDir))
                        CheckADir(lstDir[i], true);
                    else
                        MarkAsMissing(lstDir[i]);

                    if (_bgw.CancellationPending || _fileErrorAbort) break;
                }

                _bgw.ReportProgress(0, new bgwText("Updating Cache"));
                DB.Write();


                _bgw.ReportProgress(0, new bgwText("File Scan Complete"));

                _bgw = null;
                Program.SyncCont = null;
#if !Debug
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
#endif
        }



        private static void CheckADir(RvDir dbDir, bool report)
        {
            if (_cacheSaveTimer.Elapsed.TotalMinutes > Settings.CacheSaveTimePeriod)
            {
                _bgw.ReportProgress(0, "Saving Cache");
                DB.Write();
                _bgw.ReportProgress(0, "Saving Cache Complete");
                _cacheSaveTimer.Reset();
                _cacheSaveTimer.Start();
            }

            string fullDir = dbDir.FullName;
            if (report)
                _bgw.ReportProgress(0, new bgwText2(fullDir));

            DatStatus chechingDatStatus = dbDir.IsInUnsorted ? DatStatus.InUnsorted : DatStatus.NotInDat;

            // this is a temporary rvDir structure to store the data about the actual directory/files we are scanning
            // we will first populate this variable with the real file data from the directory, and then compare it
            // with the data in dbDir.
            RvDir fileDir = null;


            Debug.WriteLine(fullDir);

            FileType ft = dbDir.FileType;

            #region "Populate fileDir"

            // if we are scanning a ZIP file then populate scanDir from the ZIP file
            switch (ft)
            {
                case FileType.Zip:
                    {
                        fileDir = new RvDir(ft);

                        // open the zip file
                        ZipFile checkZ = new ZipFile();

                        ZipReturn zr = checkZ.ZipFileOpen(fullDir, dbDir.TimeStamp, true);

                        if (zr == ZipReturn.ZipGood)
                        {
                            dbDir.ZipStatus = checkZ.ZipStatus;

                            // to be Scanning a ZIP file means it is either new or has changed.
                            // as the code below only calls back here if that is true.
                            //
                            // Level1: Only use header CRC's
                            // Just get the CRC for the ZIP headers.
                            //
                            // Level2: Fully checksum changed only files
                            // We know this file has been changed to do a full checksum scan.
                            //
                            // Level3: Fully checksum everything
                            // So do a full checksum scan.
                            if (EScanLevel == eScanLevel.Level2 || EScanLevel == eScanLevel.Level3)
                                checkZ.DeepScan();

                            // add all of the file information from the zip file into scanDir
                            for (int i = 0; i < checkZ.LocalFilesCount(); i++)
                            {
                                RvFile tFile = new RvFile(DBTypeGet.FileFromDir(ft))
                                                   {
                                                       Name = checkZ.Filename(i),
                                                       ZipFileIndex = i,
                                                       ZipFileHeaderPosition = checkZ.LocalHeader(i),
                                                       Size = checkZ.UncompressedSize(i),
                                                       CRC = checkZ.CRC32(i)
                                                   };
                                // all 3 levels read the CRC from the ZIP header
                                tFile.SetStatus(chechingDatStatus, GotStatus.Got);
                                tFile.FileStatusSet(FileStatus.SizeFromHeader | FileStatus.CRCFromHeader);

                                // if we are in level 2 or level 3 then do a full CheckSum Scan.
                                if (EScanLevel == eScanLevel.Level2 || EScanLevel == eScanLevel.Level3)
                                {
                                    // DeepScan will return ZipReturn.ZipCRCDecodeError if the headers CRC and 
                                    // the actual CRC do not match.
                                    // So we just need to set the MD5 and SHA1 from the ZIP file.
                                    zr = checkZ.FileStatus(i);
                                    if (zr == ZipReturn.ZipUntested)
                                    {
                                        _bgw.ReportProgress(0, new bgwShowCorrupt(zr, fullDir + " : " + checkZ.Filename(i)));
                                    }
                                    else if (zr != ZipReturn.ZipGood)
                                    {
                                        _bgw.ReportProgress(0, new bgwShowCorrupt(zr, fullDir + " : " + checkZ.Filename(i)));
                                        tFile.GotStatus = GotStatus.Corrupt;
                                    }
                                    else
                                    {
                                        tFile.MD5 = checkZ.MD5(i);
                                        tFile.SHA1 = checkZ.SHA1(i);
                                        tFile.FileStatusSet(FileStatus.SizeVerified | FileStatus.CRCVerified | FileStatus.SHA1Verified | FileStatus.MD5Verified);
                                    }
                                }

                                fileDir.ChildAdd(tFile);
                            }
                        }
                        else if (zr == ZipReturn.ZipFileLocked)
                        {
                            _bgw.ReportProgress(0, new bgwShowError(fullDir, "Zip File Locked"));
                            dbDir.TimeStamp = 0;
                            dbDir.GotStatus = GotStatus.FileLocked;
                        }
                        else
                        {
                            _bgw.ReportProgress(0, new bgwShowCorrupt(zr, fullDir));
                            dbDir.GotStatus = GotStatus.Corrupt;
                        }
                        checkZ.ZipFileClose();
                    }
                    break;

                case FileType.Dir:
                    {
                        fileDir = new RvDir(FileType.Dir);


                        DirectoryInfo oDir = new DirectoryInfo(fullDir);
                        DirectoryInfo[] oDirs = oDir.GetDirectories();
                        FileInfo[] oFiles = oDir.GetFiles();

                        // add all the subdirectories into scanDir 
                        foreach (DirectoryInfo dir in oDirs)
                        {
                            RvBase tDir = new RvDir(FileType.Dir)
                                              {
                                                  Name = dir.Name,
                                                  TimeStamp = dir.LastWriteTime,
                                              };
                            tDir.SetStatus(chechingDatStatus, GotStatus.Got);
                            fileDir.ChildAdd(tDir);
                        }

                        // add all the files into scanDir
                        foreach (FileInfo oFile in oFiles)
                        {
                            // if we find any zip files add them as zip files.
                            string fExt = Path.GetExtension(oFile.Name);
                            switch (fExt.ToLower())
                            {
                                case ".zip":
                                    {
                                        RvDir tGame = new RvDir(FileType.Zip)
                                                          {
                                                              Name = Path.GetFileNameWithoutExtension(oFile.Name),
                                                              TimeStamp = oFile.LastWriteTime,
                                                          };
                                        tGame.SetStatus(chechingDatStatus, GotStatus.Got);
                                        fileDir.ChildAdd(tGame);
                                    }
                                    break;
                                default:
                                    {
                                        string fName = oFile.Name;
                                        if (fName == "__ROMVault.tmp")
                                        {
                                            File.Delete(oFile.FullName);
                                            continue;
                                        }

                                        // Scanning a file
                                        //
                                        // Level1 & 2 : (are the same for files) Fully checksum changed only files
                                        // Here we are just getting the TimeStamp of the File, and later
                                        // if the TimeStamp was not matched we will have to read the files CRC, MD5 & SHA1
                                        //
                                        // Level3: Fully checksum everything
                                        // Get everything about the file right here so
                                        // read CRC, MD5 & SHA1


                                        // add all the files in the sub-directory to scanDir
                                        RvFile tFile = new RvFile(FileType.File)
                                                           {
                                                               Name = fName,
                                                               Size = (ulong)oFile.Length,
                                                               TimeStamp = oFile.LastWriteTime
                                                           };

                                        tFile.FileStatusSet(FileStatus.SizeVerified);

                                        int errorCode = CHD.CheckFile(oFile, out tFile.SHA1CHD, out tFile.MD5CHD, out tFile.CHDVersion);

                                        if (errorCode == 0)
                                        {
                                            if (tFile.SHA1CHD != null) tFile.FileStatusSet(FileStatus.SHA1CHDFromHeader);
                                            if (tFile.MD5CHD != null) tFile.FileStatusSet(FileStatus.MD5CHDFromHeader);

                                            tFile.SetStatus(chechingDatStatus, GotStatus.Got);

                                            // if we are scanning at Level3 then we get all the info here
                                            if (EScanLevel == eScanLevel.Level3)
                                            {
                                                DeepScanFile(fullDir, tFile);
                                                ChdManCheck(fullDir, tFile);
                                            }
                                        }
                                        else if (errorCode == 32)
                                        {
                                            tFile.GotStatus = GotStatus.FileLocked;
                                            _bgw.ReportProgress(0, new bgwShowError(fullDir, "File Locked"));
                                        }
                                        else
                                        {
                                            string filename = Path.Combine(fullDir, tFile.Name);
                                            ReportError.Show("File: " + filename + " Error: " + new Win32Exception(errorCode).Message + ". Scan Aborted.");
                                            _fileErrorAbort = true;
                                            return;
                                        }
                                        fileDir.ChildAdd(tFile);
                                    }
                                    break;
                            }
                        }
                    }
                    break;
                default:
                    ReportError.SendAndShow("Un supported file type in CheckADir " + ft);
                    break;
            }
            #endregion

            if (fileDir == null)
            {
                ReportError.SendAndShow("Unknown Reading File Type in Dir Scanner");
                return;
            }

            if (report)
            {
                _bgw.ReportProgress(0, new bgwSetRange2(fileDir.ChildCount - 1));

                _bgw.ReportProgress(0, new bgwRange2Visible(true));
            }

            if (!DBTypeGet.isCompressedDir(ft) && _bgw.CancellationPending) return;

            Compare(dbDir, fileDir, report, true);
        }

        public static void Compare(RvDir dbDir, RvDir fileDir, bool report, bool enableCancel)
        {

            string fullDir = dbDir.FullName;
            FileType ft = dbDir.FileType;

            // now we scan down the dbDir and the scanDir, comparing them.
            // if we find a match we mark dbDir as found.
            // if we are missing a file in scanDir we mark that file in dbDir as missing.
            // if we find extra files in scanDir we add it to dbDir and mark it as unknown.
            // we also recurse into any sub directories.
            int dbIndex = 0;
            int fileIndex = 0;


            while (dbIndex < dbDir.ChildCount || fileIndex < fileDir.ChildCount)
            {
                RvBase dbChild = null;
                RvBase fileChild = null;
                int res = 0;

                if (dbIndex < dbDir.ChildCount && fileIndex < fileDir.ChildCount)
                {
                    dbChild = dbDir.Child(dbIndex);
                    fileChild = fileDir.Child(fileIndex);
                    res = DBHelper.CompareName(dbChild, fileChild);
                }
                else if (fileIndex < fileDir.ChildCount)
                {
                    //Get any remaining filedir's
                    fileChild = fileDir.Child(fileIndex);
                    res = 1;
                }
                else if (dbIndex < dbDir.ChildCount)
                {
                    //Get any remaining dbDir's
                    dbChild = dbDir.Child(dbIndex);
                    res = -1;
                }

                if (report)
                {
                    if (fileChild != null)
                    {
                        long timenow = DateTime.Now.Ticks;
                        if ((timenow - _lastUpdateTime) > (TimeSpan.TicksPerSecond / 10))
                        {
                            _lastUpdateTime = timenow;
                            _bgw.ReportProgress(0, new bgwValue2(fileIndex));
                            _bgw.ReportProgress(0, new bgwText2(Path.Combine(fullDir, fileChild.Name)));
                        }
                    }
                }

                // if this file was found in the DB
                switch (res)
                {
                    case 0:

                        if (dbChild == null || fileChild == null)
                        {
                            ReportError.SendAndShow(Resources.FileScanning_CheckADir_Error_in_File_Scanning_Code);
                            break;
                        }

                        //Complete MultiName Compare
                        List<RvBase> dbs = new List<RvBase>();
                        List<RvBase> files = new List<RvBase>();
                        int dbsCount = 1;
                        int filesCount = 1;


                        dbs.Add(dbChild);
                        files.Add(fileChild);

                        while (dbIndex + dbsCount < dbDir.ChildCount && DBHelper.CompareName(dbChild, dbDir.Child(dbIndex + dbsCount)) == 0)
                        {
                            dbs.Add(dbDir.Child(dbIndex + dbsCount));
                            dbsCount += 1;
                        }
                        while (fileIndex + filesCount < fileDir.ChildCount && DBHelper.CompareName(fileChild, fileDir.Child(fileIndex + filesCount)) == 0)
                        {
                            files.Add(fileDir.Child(fileIndex + filesCount));
                            filesCount += 1;
                        }

                        for (int indexfile = 0; indexfile < filesCount; indexfile++)
                        {
                            if (files[indexfile].SearchFound) continue;

                            for (int indexdb = 0; indexdb < dbsCount; indexdb++)
                            {
                                if (dbs[indexdb].SearchFound) continue;

                                bool matched = FullCompare(dbs[indexdb], files[indexfile], false, fullDir, EScanLevel);
                                if (!matched) continue;

                                MatchFound(dbs[indexdb], files[indexfile]);
                                dbs[indexdb].SearchFound = true;
                                files[indexfile].SearchFound = true;
                            }

                            if (files[indexfile].SearchFound) continue;

                            for (int indexdb = 0; indexdb < dbsCount; indexdb++)
                            {
                                if (dbs[indexdb].SearchFound) continue;

                                bool matched = FullCompare(dbs[indexdb], files[indexfile], true, fullDir, EScanLevel);
                                if (!matched) continue;

                                MatchFound(dbs[indexdb], files[indexfile]);
                                dbs[indexdb].SearchFound = true;
                                files[indexfile].SearchFound = true;
                            }
                        }


                        for (int indexdb = 0; indexdb < dbsCount; indexdb++)
                        {
                            if (dbs[indexdb].SearchFound)
                            {
                                dbIndex++;
                                continue;
                            }
                            DBFileNotFound(dbs[indexdb], dbDir, ref dbIndex);
                        }

                        for (int indexfile = 0; indexfile < filesCount; indexfile++)
                        {
                            if (files[indexfile].SearchFound)
                                continue;
                            NewFileFound(files[indexfile], dbDir, dbIndex);
                            dbIndex++;
                        }

                        fileIndex += filesCount;
                        break;
                    case 1:
                        NewFileFound(fileChild, dbDir, dbIndex);
                        dbIndex++;
                        fileIndex++;
                        break;
                    case -1:
                        DBFileNotFound(dbChild, dbDir, ref dbIndex);
                        break;
                }

                if (_fileErrorAbort) return;
                if (enableCancel && !DBTypeGet.isCompressedDir(ft) && _bgw.CancellationPending) return;
            }
        }

        private static void MatchFound(RvBase dbChild, RvBase fileChild)
        {
            // only check a zip if the filestamp has changed, we asume it is the same if the filestamp has not changed.
            switch (dbChild.FileType)
            {
                case FileType.Zip:
                    if (dbChild.TimeStamp != fileChild.TimeStamp || EScanLevel == eScanLevel.Level3 ||
                        (EScanLevel == eScanLevel.Level2 && !IsDeepScanned((RvDir)dbChild)))
                    {
                        // this is done first as the CheckADir could change this value if the zip turns out to be corrupt
                        dbChild.FileAdd(fileChild);
                        CheckADir((RvDir)dbChild, false);
                    }
                    else
                        // this is still needed incase the filenames case (upper/lower characters) have changed, but nothing else
                        dbChild.FileCheckName(fileChild);
                    break;
                case FileType.Dir:
                    RvDir tDir = (RvDir)dbChild;
                    if (tDir.Tree == null) // do not recurse into directories that are in the tree, as they are processed by the top level code.
                        CheckADir(tDir, true);
                    if (_fileErrorAbort) return;
                    dbChild.FileAdd(fileChild);
                    break;
                case FileType.File:
                case FileType.ZipFile:
                if (dbChild.TimeStamp == fileChild.TimeStamp && dbChild.GotStatus == GotStatus.Corrupt)
                        fileChild.GotStatus = GotStatus.Corrupt;

                    dbChild.FileAdd(fileChild);
                    break;
                default:
                    throw new Exception("Unsuported file type " + dbChild.FileType);
            }
        }

        private static void NewFileFound(RvBase fileChild, RvDir dbDir, int dbIndex)
        {
            if (fileChild == null)
            {
                ReportError.SendAndShow(Resources.FileScanning_CheckADir_Error_in_File_Scanning_Code);
                return;
            }

            // this could be an unknown file, or dirctory.
            // if item is a directory add the directory and call back in again

            // add the newly found item
            switch (fileChild.FileType)
            {
                case FileType.Zip:
                    dbDir.ChildAdd(fileChild, dbIndex);
                    CheckADir((RvDir)fileChild, false);
                    break;
                case FileType.Dir:
                    dbDir.ChildAdd(fileChild, dbIndex);
                    CheckADir((RvDir)fileChild, true);
                    break;
                case FileType.File:

                    RvFile tChild = (RvFile)fileChild;
                    // if we have not read the files CRC in the checking code, we need to read it now.
                    if (tChild.GotStatus != GotStatus.FileLocked)
                    {
                        if (!IsDeepScanned(tChild))
                            DeepScanFile(dbDir.FullName, tChild);
                        if (!IschdmanScanned(tChild) && EScanLevel == eScanLevel.Level2)
                            ChdManCheck(dbDir.FullName, tChild);
                    }
                    dbDir.ChildAdd(fileChild, dbIndex);
                    break;
                case FileType.ZipFile:
                    dbDir.ChildAdd(fileChild, dbIndex);
                    break;
                default:
                    throw new Exception("Unsuported file type " + fileChild.FileType);
            }

        }

        private static void DBFileNotFound(RvBase dbChild, RvDir dbDir, ref int dbIndex)
        {
            if (dbChild == null)
            {
                ReportError.SendAndShow(Resources.FileScanning_CheckADir_Error_in_File_Scanning_Code);
                return;
            }

            if (dbChild.FileRemove() == EFile.Delete)
                dbDir.ChildRemove(dbIndex);
            else
            {
                switch (dbChild.FileType)
                {
                    case FileType.Zip:
                        MarkAsMissing((RvDir)dbChild);
                        break;
                    case FileType.Dir:
                        RvDir tDir = (RvDir)dbChild;
                        if (tDir.Tree == null)
                            MarkAsMissing(tDir);
                        break;
                }
                dbIndex++;
            }
        }


        private static bool IsDeepScanned(RvDir tZip)
        {
            for (int i = 0; i < tZip.ChildCount; i++)
            {
                RvFile zFile = tZip.Child(i) as RvFile;
                if (zFile != null && zFile.GotStatus == GotStatus.Got &&
                    (!zFile.FileStatusIs(FileStatus.SizeVerified) || !zFile.FileStatusIs(FileStatus.CRCVerified) || !zFile.FileStatusIs(FileStatus.SHA1Verified) || !zFile.FileStatusIs(FileStatus.MD5Verified)))
                    return false;
            }
            return true;
        }

        private static void MarkAsMissing(RvDir dbDir)
        {
            for (int i = 0; i < dbDir.ChildCount; i++)
            {
                RvBase dbChild = dbDir.Child(i);

                if (dbChild.FileRemove() == EFile.Delete)
                {
                    dbDir.ChildRemove(i);
                    i--;
                }
                else
                {
                    switch (dbChild.FileType)
                    {
                        case FileType.Zip:
                            MarkAsMissing((RvDir)dbChild);
                            break;
                        case FileType.Dir:
                            RvDir tDir = (RvDir)dbChild;
                            if (tDir.Tree == null)
                                MarkAsMissing(tDir);
                            break;
                    }
                }
            }
        }


        private static bool FullCompare(RvBase dbFile, RvBase testFile, bool secondPass, string fullDir = "", eScanLevel sLevel = eScanLevel.Level3)
        {
            Debug.WriteLine("Comparing Dat File " + dbFile.TreeFullName);
            Debug.WriteLine("Comparing File     " + testFile.TreeFullName);

            int retv = DBHelper.CompareName(dbFile, testFile);
            if (retv != 0) return false;

            FileType dbfileType = dbFile.FileType;
            FileType dbtestFile = testFile.FileType;
            retv = Math.Sign(dbfileType.CompareTo(dbtestFile));
            if (retv != 0) return false;

            // filetypes are now know to be the same

            // Dir's and Zip's are not deep scanned so matching here is done
            if ((dbfileType == FileType.Dir) || (dbfileType == FileType.Zip))
                return true;

            RvFile dbFileF = (RvFile)dbFile;
            RvFile testFileF = (RvFile)testFile;


            if (dbFileF.Size != null && testFileF.Size != null)
            {
                retv = ULong.iCompare(dbFileF.Size, testFileF.Size);
                if (retv != 0) return false;
            }

            if (dbFileF.CRC != null && testFileF.CRC != null)
            {
                retv = ArrByte.iCompare(dbFileF.CRC, testFileF.CRC);
                if (retv != 0) return false;
            }

            if (dbFileF.SHA1 != null && testFileF.SHA1 != null)
            {
                retv = ArrByte.iCompare(dbFileF.SHA1, testFileF.SHA1);
                if (retv != 0) return false;
            }

            if (dbFileF.MD5 != null && testFileF.MD5 != null)
            {
                retv = ArrByte.iCompare(dbFileF.MD5, testFileF.MD5);
                if (retv != 0) return false;
            }

            if (dbFileF.SHA1CHD != null && testFileF.SHA1CHD != null)
            {
                retv = ArrByte.iCompare(dbFileF.SHA1CHD, testFileF.SHA1CHD);
                if (retv != 0) return false;
            }

            if (dbFileF.MD5CHD != null && testFileF.MD5CHD != null)
            {
                retv = ArrByte.iCompare(dbFileF.MD5CHD, testFileF.MD5CHD);
                if (retv != 0) return false;
            }

            // beyond here we only test files
            if (dbtestFile != FileType.File)
                return true;

            // if scanning at level 3 then we have already deep checked the file so all is OK.
            if (sLevel == eScanLevel.Level3)
                return true;

            // if we got this far then everything we have so far has matched, but that may not be enough.
            // now we see if we need to get any more info and try again.


            // if the file stamps do not match or the file in the DB we are comparing with has not been deep scanned
            // and the file we are scanning has not already been deep scanned, then we need to do a deep scan, and check the deep scan values

            // files are always deep scanned, so the test for IsDeepScanned(dbFileF) is probably not really needed here.

            if ((dbFileF.TimeStamp != testFileF.TimeStamp || !IsDeepScanned(dbFileF)) && !IsDeepScanned(testFileF))
            {
                if (!secondPass)
                    return false;

                DeepScanFile(fullDir, testFileF);
                if (dbFileF.CRC != null && testFileF.CRC != null)
                {
                    retv = ArrByte.iCompare(dbFileF.CRC, testFileF.CRC);
                    if (retv != 0) return false;
                }

                if (dbFileF.SHA1 != null && testFileF.SHA1 != null)
                {
                    retv = ArrByte.iCompare(dbFileF.SHA1, testFileF.SHA1);
                    if (retv != 0) return false;
                }

                if (dbFileF.MD5 != null && testFileF.MD5 != null)
                {
                    retv = ArrByte.iCompare(dbFileF.MD5, testFileF.MD5);
                    if (retv != 0) return false;
                }
            }

            // CHDman test, if we are only scanning at level 1 then don't do CHDman test so we are done. 
            if (sLevel == eScanLevel.Level1)
                return true;

            if ((dbFileF.TimeStamp != testFileF.TimeStamp || (!IschdmanScanned(dbFileF)) && !IschdmanScanned(testFileF)))
            {
                ChdManCheck(fullDir, testFileF);
            }
            return true;
        }


        private static bool IsDeepScanned(RvFile tFile)
        {
            return (
                       tFile.FileStatusIs(FileStatus.SizeVerified) &&
                       tFile.FileStatusIs(FileStatus.CRCVerified) &&
                       tFile.FileStatusIs(FileStatus.SHA1Verified) &&
                       tFile.FileStatusIs(FileStatus.MD5Verified)
                   );
        }

        private static bool IschdmanScanned(RvFile tFile)
        {
            //if (!tFile.FileStatusIs(FileStatus.SHA1CHDFromHeader))
            //    return true;

            if (tFile.GotStatus == GotStatus.Corrupt)
                return true;

            return tFile.FileStatusIs(FileStatus.SHA1CHDVerified);
        }

        private static void DeepScanFile(string directory, RvFile tFile)
        {
            string filename = Path.Combine(directory, tFile.Name);
            int errorCode = UnCompFiles.CheckSumRead(filename, true, out tFile.CRC, out tFile.MD5, out tFile.SHA1);
            if (errorCode == 32)
            {
                tFile.GotStatus = GotStatus.FileLocked;
                return;
            }
            if (errorCode != 0)
            {
                ReportError.Show("File: " + filename + " Error: " + new Win32Exception(errorCode).Message + ". Scan Aborted.");
                _fileErrorAbort = true;
                return;
            }
            tFile.FileStatusSet(FileStatus.SizeVerified | FileStatus.CRCVerified | FileStatus.SHA1Verified | FileStatus.MD5Verified);
        }

        private static void ChdManCheck(string directory, RvFile tFile)
        {
            string filename = Path.Combine(directory, tFile.Name);

            if (!tFile.FileStatusIs(FileStatus.SHA1CHDFromHeader)) return;
            _bgw.ReportProgress(0, new bgwText2(filename));

            string error;
            CHD.CHDManCheck res = CHD.ChdmanCheck(filename, _bgw, out error);
            switch (res)
            {
                case CHD.CHDManCheck.Good:
                    tFile.FileStatusSet(FileStatus.SHA1CHDVerified);
                    return;
                case CHD.CHDManCheck.Corrupt:
                    _bgw.ReportProgress(0, new bgwShowError(filename, error));
                    tFile.GotStatus = GotStatus.Corrupt;
                    return;
                case CHD.CHDManCheck.CHDReturnError:
                case CHD.CHDManCheck.CHDUnknownError:
                    _bgw.ReportProgress(0, new bgwShowError(filename, error));
                    return;
                case CHD.CHDManCheck.ChdmanNotFound:
                    return;
                case CHD.CHDManCheck.CHDNotFound:
                    ReportError.Show("File: " + filename + " Error: Not Found scan Aborted.");
                    _fileErrorAbort = true;
                    return;
                default:
                    ReportError.UnhandledExceptionHandler(error);
                    return;
            }
        }
    }
}

