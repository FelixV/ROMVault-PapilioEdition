/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using ROMVault2.RvDB;

namespace ROMVault2
{

    public enum eScanLevel
    {
        Level1,
        Level2,
        Level3
    }


    public enum eFixLevel
    {
        TrrntZipLevel1,
        TrrntZipLevel2,
        TrrntZipLevel3,
        Level1,
        Level2,
        Level3
    }

    public static class Settings
    {

        public static string DATRoot;
        public static string CacheFile;
        public static eScanLevel ScanLevel;
        public static eFixLevel FixLevel;

        public static List<DirMap> DirPathMap;

        public static List<string> IgnoreFiles;

        public static bool DoubleCheckDelete = true;
        public static bool DebugLogsEnabled;
        public static bool CacheSaveTimerEnabled = true;
        public static int CacheSaveTimePeriod = 10;

		private static string ROMVault2Data = "ROMVault2Data";

        public static bool IsUnix
        {
            get
            {
               return ((Environment.OSVersion.Platform == PlatformID.Unix)|| (Environment.OSVersion.Platform == PlatformID.MacOSX))? true : false;
            }
        }

        public static bool IsMono { get { return (Type.GetType ("Mono.Runtime") != null); } }


		public static string getUserHomeFolder()
		{
			string homePath = (Environment.OSVersion.Platform == PlatformID.Unix)? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
			Console.WriteLine("User Home Folder: " + homePath);
			return homePath;
		} 	

        public static void SetDefaults()
        {
            CacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ROMVault2_" + DBVersion.Version + ".Cache");

            DATRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datroot");
			//DATRoot = "datroot";

            ScanLevel = eScanLevel.Level2;
            FixLevel = eFixLevel.TrrntZipLevel2;

            IgnoreFiles = new List<string> { "_ReadMe_.txt" };

            ResetDirectories();

            ReadConfig();

            DirPathMap.Sort();
        }

        public static void ResetDirectories()
        {
            DirPathMap = new List<DirMap>
                             {
                                 new DirMap("ROMVault", "ROMRoot"),
                                 new DirMap("Unsorted", "Unsorted")
                             };
        }

        public static string Unsorted()
        {
            foreach (DirMap t in DirPathMap)
            {
                if (t.DirKey == "Unsorted")
                    return t.DirPath;
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Unsorted");
        }

        public static void WriteConfig()
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ROMVault2.cfg")))
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ROMVault2.cfg"));

            FileStream fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ROMVault2.cfg"), FileMode.CreateNew, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            const int ver = 5;

            bw.Write(ver);                  //int
            bw.Write(DATRoot);              //string
            bw.Write((Int32)ScanLevel);
            bw.Write((Int32)FixLevel);
            bw.Write(DebugLogsEnabled);     //bool

            bw.Write(IgnoreFiles.Count);    //int
            foreach (string t in IgnoreFiles)
            {
                bw.Write(t);                //string
            }

            bw.Write(DirPathMap.Count);     //int
            foreach (DirMap t in DirPathMap)
            {
                bw.Write(t.DirKey);         //string
                bw.Write(t.DirPath);        //string
            }

            bw.Write(CacheSaveTimerEnabled); //bool
            bw.Write(CacheSaveTimePeriod);   //int
            bw.Write(DoubleCheckDelete);     //bool

            fs.Flush();
            fs.Close();
        }

        private static void ReadConfig()
        {
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ROMVault2.cfg"))) return;

            FileStream fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ROMVault2.cfg"), FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);

            int ver = br.ReadInt32();
            if (ver == 1)
            {
                DATRoot = br.ReadString();
                ScanLevel = eScanLevel.Level1;
                FixLevel = (eFixLevel)br.ReadInt32();

                IgnoreFiles = new List<string>();
                int c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    IgnoreFiles.Add(br.ReadString());

                DirPathMap = new List<DirMap>();
                c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    DirPathMap.Add(new DirMap(br.ReadString(), br.ReadString()));
            }
            if (ver == 2)
            {
                DATRoot = br.ReadString();
                ScanLevel = (eScanLevel)br.ReadInt32();
                FixLevel = (eFixLevel)br.ReadInt32();

                IgnoreFiles = new List<string>();
                int c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    IgnoreFiles.Add(br.ReadString());

                DirPathMap = new List<DirMap>();
                c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    DirPathMap.Add(new DirMap(br.ReadString(), br.ReadString()));
            }
            if (ver == 3)
            {
                DATRoot = br.ReadString();
                ScanLevel = (eScanLevel)br.ReadInt32();
                FixLevel = (eFixLevel)br.ReadInt32();
                DebugLogsEnabled = br.ReadBoolean();

                IgnoreFiles = new List<string>();
                int c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    IgnoreFiles.Add(br.ReadString());

                DirPathMap = new List<DirMap>();
                c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    DirPathMap.Add(new DirMap(br.ReadString(), br.ReadString()));
            }

            if (ver == 4)
            {
                DATRoot = br.ReadString();
                ScanLevel = (eScanLevel)br.ReadInt32();
                FixLevel = (eFixLevel)br.ReadInt32();
                DebugLogsEnabled = br.ReadBoolean();

                IgnoreFiles = new List<string>();
                int c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    IgnoreFiles.Add(br.ReadString());

                DirPathMap = new List<DirMap>();
                c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    DirPathMap.Add(new DirMap(br.ReadString(), br.ReadString()));

                CacheSaveTimerEnabled = br.ReadBoolean();
                CacheSaveTimePeriod = br.ReadInt32();
            }

            if (ver == 5)
            {
                DATRoot = br.ReadString();
                ScanLevel = (eScanLevel)br.ReadInt32();
                FixLevel = (eFixLevel)br.ReadInt32();
                DebugLogsEnabled = br.ReadBoolean();

                IgnoreFiles = new List<string>();
                int c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    IgnoreFiles.Add(br.ReadString());

                DirPathMap = new List<DirMap>();
                c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    DirPathMap.Add(new DirMap(br.ReadString(), br.ReadString()));

                CacheSaveTimerEnabled = br.ReadBoolean();
                CacheSaveTimePeriod = br.ReadInt32();

                DoubleCheckDelete = br.ReadBoolean();
            }

            if (ver == 6)
            {
                DATRoot = br.ReadString();
                ScanLevel = (eScanLevel)br.ReadInt32();
                FixLevel = (eFixLevel)br.ReadInt32();
                DebugLogsEnabled = br.ReadBoolean();
                bool UserLongFilenames = br.ReadBoolean();

                IgnoreFiles = new List<string>();
                int c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    IgnoreFiles.Add(br.ReadString());

                DirPathMap = new List<DirMap>();
                c = br.ReadInt32();
                for (int i = 0; i < c; i++)
                    DirPathMap.Add(new DirMap(br.ReadString(), br.ReadString()));

                CacheSaveTimerEnabled = br.ReadBoolean();
                CacheSaveTimePeriod = br.ReadInt32();

                DoubleCheckDelete = br.ReadBoolean();
            }


            br.Close();
            fs.Close();
        }
    }


    public class DirMap : IComparable<DirMap>
    {
        public readonly string DirKey;
        public readonly string DirPath;

        public DirMap(string key, string path)
        {
            DirKey = key;
            DirPath = path;
        }

        public int CompareTo(DirMap obj)
        {
            return Math.Sign(String.Compare(DirKey, obj.DirKey, StringComparison.Ordinal));
        }
    }
}


