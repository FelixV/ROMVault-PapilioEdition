﻿/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System.IO;
using System.Security.Cryptography;
using ROMVault2.SupportedFiles.Zip.ZLib;

namespace ROMVault2.SupportedFiles.Files
{
    public static class UnCompFiles
    {
        private const int Buffersize = 4096;
        private static readonly byte[] Buffer;

        static UnCompFiles()
        {
            Buffer = new byte[Buffersize];
        }

        public static int CheckSumRead(string filename, bool testDeep, out byte[] crc, out byte[] bMD5, out byte[] bSHA1)
        {
            bMD5 = null;
            bSHA1 = null;
            crc = null;

            Stream ds;
            int errorCode = IO.FileStream.OpenFileRead(filename, out ds);
            if (errorCode != 0)
                return errorCode;

            CRC32Hash crc32 = new CRC32Hash();

            MD5 md5 = null;
            if (testDeep) md5 = MD5.Create();
            SHA1 sha1 = null;
            if (testDeep) sha1 = SHA1.Create();

            long sizetogo = ds.Length;
        
            while (sizetogo > 0)
            {
                int sizenow = sizetogo > Buffersize ? Buffersize : (int)sizetogo;

                ds.Read(Buffer, 0, sizenow);
                crc32.TransformBlock(Buffer, 0, sizenow, null, 0);
                if (testDeep) md5.TransformBlock(Buffer, 0, sizenow, null, 0);
                if (testDeep) sha1.TransformBlock(Buffer, 0, sizenow, null, 0);
                sizetogo -= sizenow;
            }

            crc32.TransformFinalBlock(Buffer, 0, 0);
            if (testDeep) md5.TransformFinalBlock(Buffer, 0, 0);
            if (testDeep) sha1.TransformFinalBlock(Buffer, 0, 0);

            ds.Close();

            crc = crc32.Hash;
            if (testDeep) bMD5 = md5.Hash;
            if (testDeep) bSHA1 = sha1.Hash;

            return 0;
        }



    }
}
