/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using ROMVault2.RvDB;

namespace ROMVault2.Utils
{
    public class DBTypeGet
    {
        public static FileType DirFromFile(FileType ft)
        {
            switch (ft)
            {
                case FileType.File:
                    return FileType.Dir;
                case FileType.ZipFile:
                    return FileType.Zip;
            }
            return FileType.Zip;
        }

        public static FileType FileFromDir(FileType ft)
        {
            switch (ft)
            {
                case FileType.Dir:
                    return FileType.File;
                case FileType.Zip:
                    return FileType.ZipFile;
            }
            return FileType.Zip;
        }

        public static bool isCompressedDir(FileType fileType)
        {
            return (fileType == FileType.Zip);
        }

        public static RvBase GetRvType(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Dir: 
                case FileType.Zip: 
                    return new RvDir(fileType);
                case FileType.File:
                case FileType.ZipFile:
                    return new RvFile(fileType);
                default:
                    throw new Exception("Unknown file type");
            }
        }
    }
}
