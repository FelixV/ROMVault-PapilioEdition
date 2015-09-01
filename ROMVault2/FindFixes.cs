/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using ROMVault2.Properties;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2
{

    public static class FindFixes
    {
        private static BackgroundWorker _bgw;
        
        public static void ScanFiles(object sender, DoWorkEventArgs e)
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

                List<RvFile> lstRomTableSortedCRCSize;
                List<RvFile> lstRomTableSortedSHA1CHD;

                _bgw.ReportProgress(0, new bgwText("Loading Rom List"));
                DBHelper.GetSelectedFilesSortCRCSize(out lstRomTableSortedCRCSize);
                DBHelper.GetSelectedFilesSortSHA1CHD(out lstRomTableSortedSHA1CHD);

                _bgw.ReportProgress(0, new bgwText("Scanning for Fixes"));
                _bgw.ReportProgress(0, new bgwSetRange(lstRomTableSortedCRCSize.Count));

                int romIndex0 = 0;
                int romIndex1 = 1;
                while (romIndex1 < lstRomTableSortedCRCSize.Count)
                {
                    if (romIndex1 % 100 == 0) _bgw.ReportProgress(romIndex1);

                    if (!ArrByte.bCompare(lstRomTableSortedCRCSize[romIndex0].CRC,lstRomTableSortedCRCSize[romIndex1].CRC) || lstRomTableSortedCRCSize[romIndex0].Size != lstRomTableSortedCRCSize[romIndex1].Size)
                    {
                        ListCheck(lstRomTableSortedCRCSize, romIndex0, romIndex1 - romIndex0);
                        romIndex0 = romIndex1;
                    }
                    romIndex1++;
                }

                ListCheck(lstRomTableSortedCRCSize, romIndex0, romIndex1 - romIndex0);



                _bgw.ReportProgress(0, new bgwSetRange(lstRomTableSortedSHA1CHD.Count));

                romIndex0 = 0;
                romIndex1 = 1;
                while (romIndex1 < lstRomTableSortedSHA1CHD.Count)
                {
                    if (romIndex1 % 100 == 0) _bgw.ReportProgress(romIndex1);

                    if (!ArrByte.bCompare(lstRomTableSortedSHA1CHD[romIndex0].SHA1CHD,lstRomTableSortedSHA1CHD[romIndex1].SHA1CHD) )
                    {
                        ListCheckSHA1CHD(lstRomTableSortedSHA1CHD, romIndex0, romIndex1 - romIndex0);
                        romIndex0 = romIndex1;
                    }
                    romIndex1++;
                }

                ListCheckSHA1CHD(lstRomTableSortedSHA1CHD, romIndex0, romIndex1 - romIndex0);


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

        public static void ListCheck(List<RvFile> lstRomTableSortedCRC, int start, int length)
        {
            if (lstRomTableSortedCRC.Count == 0)
                return;
            
            List<RvFile> missingFiles = new List<RvFile>(); // files we dont have that we need
            
            List<RvFile> correctFiles = new List<RvFile>();   // files we have that are in the correct place
            List<RvFile> unNeededFiles = new List<RvFile>();  // files we have that are not in the correct place
            List<RvFile> inUnsortedFiles = new List<RvFile>();  // files we have that are in Unsorted
            List<RvFile> allGotFiles = new List<RvFile>();    // all files we have

            List<RvFile> corruptFiles = new List<RvFile>(); // corrupt files that we do not need, a corrupt file is missing if it is needed
            

            // set the found status of this file
            for (int iLoop = 0; iLoop < length; iLoop++)
            {
                RvFile tFile = lstRomTableSortedCRC[start + iLoop];

                switch (tFile.RepStatus)
                {
                    case RepStatus.UnScanned:
                        break;
                    case RepStatus.Missing:
                        missingFiles.Add(tFile); // these are checked in step 1 to fixes from the allGotFiles List.
                        break;
                    case RepStatus.Correct:
                        correctFiles.Add(tFile);
                        break;
                    case RepStatus.Corrupt:
                        if (tFile.DatStatus == DatStatus.InDatCollect)
                            missingFiles.Add(tFile); // corrupt files that are also InDatcollect are treated as missing files, and a fix should be found.
                        else
                            corruptFiles.Add(tFile); // all other corrupt files should be deleted or moved to Unsorted/corrupt
                        break;
                    case RepStatus.UnNeeded:   
                    case RepStatus.Unknown:
                        unNeededFiles.Add(tFile);
                        break;
                    case RepStatus.NotCollected:
                        break;
                    case RepStatus.InUnsorted:
                        inUnsortedFiles.Add(tFile);
                        break;
                    case RepStatus.Ignore:
                        break; // Ignore File
                    default:
                        ReportError.SendAndShow(Resources.FindFixes_ListCheck_Unknown_test_status + tFile.DatFullName + Resources.Comma + tFile.DatStatus + Resources.Comma + tFile.RepStatus);
                        break;

                }
            }
            allGotFiles.AddRange(correctFiles);
            allGotFiles.AddRange(unNeededFiles);
            allGotFiles.AddRange(inUnsortedFiles);

            #region Step 1 Check the Missing files from the allGotFiles List.
            // check to see if we can find any of the missing files in the gotFiles list.
            // if we find them mark them as CanBeFixed, 
            // or if they are missing corrupt files set then as corruptCanBefixed
            
            foreach (RvFile missingFile in missingFiles)
            {
                if (DBHelper.IsZeroLengthFile(missingFile))
                {
                    missingFile.RepStatus = missingFile.RepStatus == RepStatus.Corrupt ? RepStatus.CorruptCanBeFixed : RepStatus.CanBeFixed;
                    continue;
                }

                foreach (RvFile gotFile in allGotFiles)
                {
                    if (!CheckIfMissingFileCanBeFixedByGotFile(missingFile, gotFile)) continue;
                    missingFile.RepStatus = missingFile.RepStatus == RepStatus.Corrupt ? RepStatus.CorruptCanBeFixed : RepStatus.CanBeFixed;
                    break;
                }
                if (missingFile.RepStatus == RepStatus.Corrupt) missingFile.RepStatus = RepStatus.MoveToCorrupt;
            }
            #endregion

            #region Step 2 Check all corrupt files.
            // if we have a correct version of the corrupt file then the corrput file can just be deleted,
            // otherwise if the corrupt file is not already in Unsorted it should be moved out to Unsorted.
            
            // we can only check corrupt files using the CRC from the ZIP header, as it is corrupt so we cannot get a correct SHA1 / MD5 to check with

            foreach (RvFile corruptFile in corruptFiles)
            {
                if (allGotFiles.Count>0)
                    corruptFile.RepStatus = RepStatus.Delete;

                if (corruptFile.RepStatus == RepStatus.Corrupt && corruptFile.DatStatus != DatStatus.InUnsorted)
                    corruptFile.RepStatus = RepStatus.MoveToCorrupt;
            }
            #endregion

            #region Step 3 Check if unNeeded files are needed for a fix, otherwise delete them or move them to Unsorted
            foreach (RvFile unNeededFile in unNeededFiles)
            {
                /*
                // check if we have a confirmed SHA1 / MD5 match of this file, and if we do we just mark this file to be deleted.
                foreach (RvFile correctFile in correctFiles)
                {
                    if (!FindSHA1MD5MatchingFiles(unNeededFile, correctFile)) continue;
                    unNeededFile.RepStatus = RepStatus.Delete;
                    break;
                }
                if (unNeededFile.RepStatus == RepStatus.Delete) continue;
                */

                if (DBHelper.IsZeroLengthFile(unNeededFile))
                {
                    unNeededFile.RepStatus = RepStatus.Delete;
                    continue;
                }

                // check if the unNeededFile is needed to fix a missing file
                foreach (RvFile missingFile in missingFiles)
                {
                    if (!CheckIfMissingFileCanBeFixedByGotFile(missingFile, unNeededFile)) continue;
                    unNeededFile.RepStatus = RepStatus.NeededForFix;
                    break;
                }
                if (unNeededFile.RepStatus == RepStatus.NeededForFix) continue;

                // now that we know this file is not needed for a fix do a CRC only find against correct files to see if this file can be deleted.
                foreach (RvFile correctFile in correctFiles)
                {
                    if (!CheckIfGotfileAndMatchingFileAreFullMatches(unNeededFile, correctFile)) continue;
                    unNeededFile.RepStatus = RepStatus.Delete;
                    break;
                }
                if (unNeededFile.RepStatus == RepStatus.Delete) continue;

                // and finally see if the file is already in Unsorted, and if it is deleted.
                foreach (RvFile inUnsortedFile in inUnsortedFiles)
                {
                    if (!CheckIfGotfileAndMatchingFileAreFullMatches(unNeededFile, inUnsortedFile)) continue;
                    unNeededFile.RepStatus = RepStatus.Delete;
                    break;
                }
                if (unNeededFile.RepStatus == RepStatus.Delete) continue;

                // otherwise move the file out to Unsorted
                unNeededFile.RepStatus = RepStatus.MoveUnsorted;
            }
            #endregion

            #region Step 4 Check if Unsorted files are needed for a fix, otherwise delete them or leave them in Unsorted
            foreach (RvFile inUnsortedFile in inUnsortedFiles)
            {
                /*
                // check if we have a confirmed SHA1 / MD5 match of this file, and if we do we just mark this file to be deleted.
                foreach (RvFile correctFile in correctFiles)
                {
                    if (!FindSHA1MD5MatchingFiles(inUnsortedFile, correctFile)) continue;
                    inUnsortedFile.RepStatus = RepStatus.Delete;
                    break;
                }
                if (inUnsortedFile.RepStatus == RepStatus.Delete) continue;
                */

                // check if the UnsortedFile is needed to fix a missing file
                foreach (RvFile missingFile in missingFiles)
                {
                    if (!CheckIfMissingFileCanBeFixedByGotFile(missingFile, inUnsortedFile)) continue;
                    inUnsortedFile.RepStatus = RepStatus.NeededForFix;
                    break;
                }
                if (inUnsortedFile.RepStatus == RepStatus.NeededForFix) continue;

                // now that we know this file is not needed for a fix do a CRC only find against correct files to see if this file can be deleted.
                foreach (RvFile correctFile in correctFiles)
                {
                    if (!CheckIfGotfileAndMatchingFileAreFullMatches(inUnsortedFile, correctFile)) continue;
                    inUnsortedFile.RepStatus = RepStatus.Delete;
                    break;
                }

                // otherwise leave the file in Unsorted
            }
            #endregion


            //need to check here for roms that just need renamed inside the one ZIP
            //this prevents Zips from self deadlocking
            for (int iLoop0 = 0; iLoop0 < length; iLoop0++)
            {
                if (lstRomTableSortedCRC[start + iLoop0].RepStatus != RepStatus.NeededForFix) continue;
                for (int iLoop1 = 0; iLoop1 < length; iLoop1++)
                {
                    if (lstRomTableSortedCRC[start + iLoop1].RepStatus != RepStatus.CanBeFixed) continue;

                    if (!CheckIfMissingFileCanBeFixedByGotFile(lstRomTableSortedCRC[start + iLoop1], lstRomTableSortedCRC[start + iLoop0])) continue;

                    if (DBHelper.RomFromSameGame(lstRomTableSortedCRC[start + iLoop0], lstRomTableSortedCRC[start + iLoop1]))
                        lstRomTableSortedCRC[start + iLoop0].RepStatus = RepStatus.Rename;
                }
            }

        }

        // find fix files, if the gotFile has been fully scanned check the SHA1/MD5, if not then just return true as the CRC/Size is all we have to go on.
        // this means that if the gotfile has not been fully scanned this will return true even with the source and destination SHA1/MD5 possibly different.
        public static bool CheckIfMissingFileCanBeFixedByGotFile(RvFile missingFile, RvFile gotFile)
        {

            if (missingFile.FileStatusIs(FileStatus.SHA1FromDAT) &&  gotFile.FileStatusIs(FileStatus.SHA1Verified) && !ArrByte.bCompare(missingFile.SHA1, gotFile.SHA1))
                return false;
            if (missingFile.FileStatusIs(FileStatus.MD5FromDAT) &&  gotFile.FileStatusIs(FileStatus.MD5Verified) && !ArrByte.bCompare(missingFile.MD5, gotFile.MD5))
                return false;

            return true;
        }

     

        private static bool CheckIfGotfileAndMatchingFileAreFullMatches(RvFile gotFile, RvFile matchingFile)
        {
            if (gotFile.FileStatusIs(FileStatus.SHA1Verified) && matchingFile.FileStatusIs(FileStatus.SHA1Verified) && !ArrByte.bCompare(gotFile.SHA1, matchingFile.SHA1))
                return false;
            if (gotFile.FileStatusIs(FileStatus.MD5Verified) && matchingFile.FileStatusIs(FileStatus.MD5Verified) && !ArrByte.bCompare(gotFile.MD5, matchingFile.MD5))
                return false;

            return true;
        }


        private static void ListCheckSHA1CHD(List<RvFile> lstRomTableSortedSHA1CHD, int start, int length)
        {
            if (lstRomTableSortedSHA1CHD.Count == 0)
                return;

            List<RvFile> missingFiles = new List<RvFile>(); // files we done have have that we need

            List<RvFile> correctFiles = new List<RvFile>(); // files we have that are in the correct place
            List<RvFile> unNeededFiles = new List<RvFile>(); // files we have that are not in the correct place
            List<RvFile> inUnsortedFiles = new List<RvFile>(); // files we have that are in Unsorted
            List<RvFile> allGotFiles = new List<RvFile>(); // all files we have

            List<RvFile> corruptFiles = new List<RvFile>(); // corrupt files that we do not need, a corrupt file is missing if it is needed


            // set the found status of this file
            for (int iLoop = 0; iLoop < length; iLoop++)
            {
                RvFile tFile = lstRomTableSortedSHA1CHD[start + iLoop];

                switch (tFile.RepStatus)
                {
                    case RepStatus.Missing:
                        missingFiles.Add(tFile); // these are checked in step 1 to fixes from the allGotFiles List.
                        break;
                    case RepStatus.Correct:
                        correctFiles.Add(tFile);
                        break;
                    case RepStatus.Corrupt:
                    case RepStatus.MoveToCorrupt:
                        if (tFile.DatStatus == DatStatus.InDatCollect)
                            missingFiles.Add(tFile); // corrupt files that are also InDatcollect are treated as missing files, and a fix should be found.
                        else
                            corruptFiles.Add(tFile); // all other corrupt files should be deleted or moved to Unsorted/corrupt
                        break;
                    case RepStatus.UnNeeded:
                    case RepStatus.Unknown:
                    case RepStatus.MoveUnsorted:
                    case RepStatus.InUnsorted:
                    case RepStatus.Delete:
                    case RepStatus.NeededForFix:
                    case RepStatus.Rename:
                        if (tFile.IsInUnsorted)
                            inUnsortedFiles.Add(tFile);
                        else
                            unNeededFiles.Add(tFile);
                        break;
                    case RepStatus.NotCollected:
                        break;
                    case RepStatus.Ignore:
                        break; // Ignore File

                    default:
                        ReportError.SendAndShow(Resources.FindFixes_ListCheck_Unknown_test_status + tFile.DatFullName + Resources.Comma + tFile.DatStatus + Resources.Comma + tFile.RepStatus);
                        break;

                }
            }
            allGotFiles.AddRange(correctFiles);
            allGotFiles.AddRange(unNeededFiles);
            allGotFiles.AddRange(inUnsortedFiles);


            #region Step 1 Check the Missing files from the allGotFiles List.
            // check to see if we can find any of the missing files in the gotFiles list.
            // if we find them mark them as CanBeFixed, 

            foreach (RvFile missingFile in missingFiles)
            {
                if (allGotFiles.Count>0)
                    missingFile.RepStatus = (missingFile.RepStatus == RepStatus.Corrupt) || (missingFile.RepStatus == RepStatus.MoveToCorrupt) ? RepStatus.CorruptCanBeFixed : RepStatus.CanBeFixed;
            }
            #endregion

            #region Step 2 Check all corrupt files.
            // if we have a correct version of the corrupt file then the corrput file can just be deleted,
            // otherwise if the corrupt file is not already in Unsorted it should be moved out to Unsorted.

            // we can only check corrupt files using the CRC from the ZIP header, as it is corrupt so we cannot get a correct SHA1 / MD5 to check with

            foreach (RvFile corruptFile in corruptFiles)
            {
                if (allGotFiles.Count > 0)
                    corruptFile.RepStatus = RepStatus.Delete;

                if (corruptFile.RepStatus == RepStatus.Corrupt && corruptFile.DatStatus != DatStatus.InUnsorted)
                    corruptFile.RepStatus = RepStatus.MoveToCorrupt;
            }
            #endregion


            #region Step 3 Check if unNeeded files are needed for a fix, otherwise delete them or move them to Unsorted
            foreach (RvFile unNeededFile in unNeededFiles)
            {
                // check if the unNeededFile is needed to fix a missing file
                if (missingFiles.Count > 0)
                {
                    unNeededFile.RepStatus = RepStatus.NeededForFix;
                    continue;
                }

                // now that we know this file is not needed for a fix if we have a correct version of it, it can be deleted.
                if (correctFiles.Count > 0)
                {
                    // this probably should check its old state
                 if (unNeededFile.RepStatus!=RepStatus.NeededForFix)
                    unNeededFile.RepStatus = RepStatus.Delete;
                    continue;
                }

                if (inUnsortedFiles.Count > 0)
                {
                    if (unNeededFile.RepStatus != RepStatus.NeededForFix)
                        unNeededFile.RepStatus = RepStatus.Delete;
                    continue;
                }

                // otherwise move the file out to Unsorted
                if (unNeededFile.RepStatus != RepStatus.NeededForFix)
                    unNeededFile.RepStatus = RepStatus.MoveUnsorted;
            }
            #endregion


            #region Step 4 Check if Unsorted files are needed for a fix, otherwise delete them or leave them in Unsorted
            foreach (RvFile inUnsortedFile in inUnsortedFiles)
            {
                // check if the UnsortedFile is needed to fix a missing file
                if (missingFiles.Count > 0)
                {
                    inUnsortedFile.RepStatus=RepStatus.NeededForFix;
                    continue;
                }
                
                // now that we know this file is not needed for a fix do a CRC only find against correct files to see if this file can be deleted.
                if (correctFiles.Count <= 0) continue;

                if (inUnsortedFile.RepStatus != RepStatus.NeededForFix)
                    inUnsortedFile.RepStatus = RepStatus.Delete;

                // otherwise leave the file in Unsorted
            }
            #endregion
        }
    }


    

}
