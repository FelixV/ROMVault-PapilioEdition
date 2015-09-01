/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;

namespace ROMVault2.Utils
{
    public static class ULong
    {
        public static int iCompare(ulong? a, ulong? b)
        {
            if ((a == null) || (b == null))
            {
                ReportError.SendAndShow("comparing null ulong? ");
                return -1;
            }
            return Math.Sign(((ulong) a).CompareTo((ulong) b));
        }
    }
}
