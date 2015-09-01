/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;

namespace ROMVault2.SupportedFiles
{   
    public enum ZipReturn
    {
        ZipGood,
        ZipFileLocked,
        ZipFileCountError,
        ZipSignatureError,
        ZipExtraDataOnEndOfZip,
        ZipUnsupportedCompression,
        ZipLocalFileHeaderError,
        ZipCenteralDirError,
        ZipEndOfCentralDirectoryError,
        Zip64EndOfCentralDirError,
        Zip64EndOfCentralDirectoryLocatorError,
        ZipReadingFromOutputFile,
        ZipWritingToInputFile,
        ZipErrorGettingDataStream,
        ZipCRCDecodeError,
        ZipDecodeError,
        ZipFileNameToLong,
        ZipFileAlreadyOpen,
        ZipCannotFastOpen,
        ZipErrorOpeningFile,
        ZipErrorFileNotFound,
        ZipErrorReadingFile,
        ZipErrorTimeStamp,
        ZipErrorRollBackFile,
        ZipUntested

    }

    public enum ZipOpenType
    {
        Closed,
        OpenRead,
        OpenWrite
    }

    [Flags]
    public enum ZipStatus
    {
        None = 0x0,
        TrrntZip = 0x1,
        ExtraData = 0x2
    }


}
