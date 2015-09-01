/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System.Xml;
using ROMVault2.RvDB;
using ROMVault2.Utils;

namespace ROMVault2.DatReaders
{
    public static class DatMessXmlReader
    {
        private static int _indexContinue;

        public static bool ReadDat(ref RvDir tDat, XmlDocument doc)
        {
            if (!LoadHeaderFromDat(ref tDat, ref doc))
                return false;

            if (doc.DocumentElement == null)
                return false;
            XmlNodeList gameNodeList = doc.DocumentElement.SelectNodes("software");

            if (gameNodeList == null)
                return false;
            for (int i = 0; i < gameNodeList.Count; i++)
            {
                LoadGameFromDat(ref tDat, gameNodeList[i]);
            }

            return true;
        }

        private static bool LoadHeaderFromDat(ref RvDir tDir, ref XmlDocument doc)
        {
            XmlNodeList head = doc.SelectNodes("softwarelist");
            if (head == null)
                return false;

            if (head.Count == 0)
                return false;

            if (head[0].Attributes == null)
                return false;

            RvDat tDat = new RvDat();
            tDat.AddData(RvDat.DatData.DatName, VarFix.CleanFileName(head[0].Attributes.GetNamedItem("name")));
            tDat.AddData(RvDat.DatData.Description, VarFix.String(head[0].Attributes.GetNamedItem("description")));

            string val = VarFix.String(head[0].Attributes.GetNamedItem("forcemerging")).ToLower();
            switch (val.ToLower())
            {
                case "split":
                    tDat.AddData(RvDat.DatData.MergeType, "split");
                    break;
                case "full":
                    tDat.AddData(RvDat.DatData.MergeType, "full");
                    break;
                default:
                    tDat.AddData(RvDat.DatData.MergeType, "split");
                    break;
            }



            tDir.Dat = tDat;
            return true;
        }

        private static void LoadGameFromDat(ref RvDir tDat, XmlNode gameNode)
        {
            if (gameNode.Attributes == null)
                return;

            RvDir parent = tDat;

            RvDir tDir = new RvDir(FileType.Zip)
                        {
                            Name = VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("name")),
                            Game = new RvGame(),
                            Dat = tDat.Dat,
                            DatStatus = DatStatus.InDatCollect
                        };


            tDir.Game.AddData(RvGame.GameData.Description, VarFix.String(gameNode.SelectSingleNode("description")));

            tDir.Game.AddData(RvGame.GameData.RomOf, VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("cloneof")));
            tDir.Game.AddData(RvGame.GameData.CloneOf, VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("cloneof")));
            tDir.Game.AddData(RvGame.GameData.Year, VarFix.CleanFileName(gameNode.SelectSingleNode("year")));
            tDir.Game.AddData(RvGame.GameData.Manufacturer, VarFix.CleanFileName(gameNode.SelectSingleNode("publisher")));



            RvDir tDirCHD = new RvDir(FileType.Dir)
                        {
                            Name = VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("name")),
                            Game = new RvGame(),
                            Dat = tDat.Dat,
                            DatStatus = DatStatus.InDatCollect

                        };

            tDirCHD.Game.AddData(RvGame.GameData.Description, VarFix.String(gameNode.SelectSingleNode("description")));

            tDirCHD.Game.AddData(RvGame.GameData.RomOf, VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("cloneof")));
            tDirCHD.Game.AddData(RvGame.GameData.CloneOf, VarFix.CleanFileName(gameNode.Attributes.GetNamedItem("cloneof")));
            tDirCHD.Game.AddData(RvGame.GameData.Year, VarFix.CleanFileName(gameNode.SelectSingleNode("year")));
            tDirCHD.Game.AddData(RvGame.GameData.Manufacturer, VarFix.CleanFileName(gameNode.SelectSingleNode("publisher")));



            int index1;
            string testName = tDir.Name;
            int nameCount = 0;
            while (parent.ChildNameSearch(tDir, out index1) == 0)
            {
                tDir.Name = testName + "_" + nameCount;
                nameCount++;
            }
            tDirCHD.Name = tDir.Name;


            XmlNodeList partNodeList = gameNode.SelectNodes("part");
            if (partNodeList == null) return;

            for (int iP = 0; iP < partNodeList.Count; iP++)
            {
                _indexContinue = -1;
                XmlNodeList dataAreaNodeList = partNodeList[iP].SelectNodes("dataarea");
                if (dataAreaNodeList != null)
                    for (int iD = 0; iD < dataAreaNodeList.Count; iD++)
                    {
                        XmlNodeList romNodeList = dataAreaNodeList[iD].SelectNodes("rom");
                        if (romNodeList != null)
                            for (int iR = 0; iR < romNodeList.Count; iR++)
                            {
                                LoadRomFromDat(ref tDir, romNodeList[iR]);
                            }
                    }
            }
            for (int iP = 0; iP < partNodeList.Count; iP++)
            {
                XmlNodeList diskAreaNodeList = partNodeList[iP].SelectNodes("diskarea");
                if (diskAreaNodeList != null)
                    for (int iD = 0; iD < diskAreaNodeList.Count; iD++)
                    {
                        XmlNodeList romNodeList = diskAreaNodeList[iD].SelectNodes("disk");
                        if (romNodeList != null)
                            for (int iR = 0; iR < romNodeList.Count; iR++)
                            {
                                LoadDiskFromDat(ref tDirCHD, romNodeList[iR]);
                            }
                    }
            }

            if (tDir.ChildCount > 0)
                parent.ChildAdd(tDir, index1);
            if (tDirCHD.ChildCount > 0)
                parent.ChildAdd(tDirCHD, index1);


        }

        private static void LoadRomFromDat(ref RvDir tGame, XmlNode romNode)
        {
            if (romNode.Attributes == null)
                return;

            XmlNode name = romNode.Attributes.GetNamedItem("name");
            string loadflag = VarFix.String(romNode.Attributes.GetNamedItem("loadflag"));
            if (name != null)
            {
                RvFile tRom = new RvFile(FileType.ZipFile)
                                     {
                                         Name = VarFix.CleanFullFileName(name),
                                         Size = VarFix.ULong(romNode.Attributes.GetNamedItem("size")),
                                         CRC = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("crc"), 8),
                                         SHA1 = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("sha1"), 40),
                                         Status = VarFix.ToLower(romNode.Attributes.GetNamedItem("status")),

                                         Dat = tGame.Dat
                                     };

                if (tRom.Size != null) tRom.FileStatusSet(FileStatus.SizeFromDAT);
                if (tRom.CRC != null) tRom.FileStatusSet(FileStatus.CRCFromDAT);
                if (tRom.SHA1 != null) tRom.FileStatusSet(FileStatus.SHA1FromDAT);

                _indexContinue = tGame.ChildAdd(tRom);
            }
            else if (loadflag.ToLower() == "continue")
            {
                RvFile tZippedFile = (RvFile)tGame.Child(_indexContinue);
                tZippedFile.Size += VarFix.ULong(romNode.Attributes.GetNamedItem("size"));
            }

        }

        private static void LoadDiskFromDat(ref RvDir tGame, XmlNode romNode)
        {
            if (romNode.Attributes == null)
                return;

            XmlNode name = romNode.Attributes.GetNamedItem("name");
            RvFile tRom = new RvFile(FileType.File)
            {
                Name = VarFix.CleanFullFileName(name) + ".chd",
                SHA1CHD = VarFix.CleanMD5SHA1(romNode.Attributes.GetNamedItem("sha1"), 40),
                Status = VarFix.ToLower(romNode.Attributes.GetNamedItem("status")),

                Dat = tGame.Dat
            };

            if (tRom.SHA1CHD != null) tRom.FileStatusSet(FileStatus.SHA1CHDFromDAT);

            tGame.ChildAdd(tRom);
        }


    }
}
