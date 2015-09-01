/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;

namespace ROMVault2.RvDB
{
    [Flags]
    public enum FileStatus
    {
        SizeFromDAT = 0x00000001,
        CRCFromDAT = 0x00000002,
        SHA1FromDAT = 0x00000004,
        MD5FromDAT = 0x00000008,
        SHA1CHDFromDAT = 0x00000010,
        MD5CHDFromDAT = 0x00000020,

        SizeFromHeader = 0x00000100,
        CRCFromHeader = 0x00000200,
        SHA1FromHeader = 0x00000400,
        MD5FromHeader = 0x00000800,
        SHA1CHDFromHeader = 0x00001000,
        MD5CHDFromHeader = 0x00002000,

        SizeVerified = 0x00010000,
        CRCVerified = 0x00020000,
        SHA1Verified = 0x00040000,
        MD5Verified = 0x00080000,
        SHA1CHDVerified = 0x00100000,
        MD5CHDVerified = 0x00200000
    }
}
