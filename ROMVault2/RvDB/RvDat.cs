/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.IO;

namespace ROMVault2.RvDB
{
    public enum DatUpdateStatus
    {
        Delete,
        Correct
    }

    public class RvDat
    {
        public int DatIndex = -1;
        public DatUpdateStatus Status;

        public long TimeStamp;
        public bool AutoAddDirectory;

        public enum DatData
        {
            DatName = 1,
            DatFullName = 2,

            RootDir = 3,
            Description = 4,
            Category = 5,
            Version = 6,
            Date = 7,
            Author = 8,
            Email = 9,
            HomePage = 10,
            URL = 11,
            FileType = 12,
            MergeType = 13,
            SuperDat = 14,
            DirSetup = 15
        }

        private class GameMetaData
        {
            public DatData Id { get; private set; }
            public String Value { get; private set; }

            public GameMetaData(DatData id, String value)
            {
                Id = id;
                Value = value;
            }
            public GameMetaData(BinaryReader br)
            {
                Id = (DatData)br.ReadByte();
                Value = br.ReadString();
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write((byte)Id);
                bw.Write(DB.Fn(Value));
            }
        }

        private readonly List<GameMetaData> _gameMetaData = new List<GameMetaData>();


        public void Write(BinaryWriter bw)
        {
            bw.Write(TimeStamp);
            bw.Write(AutoAddDirectory);

            bw.Write((byte)_gameMetaData.Count);
            foreach (GameMetaData gameMD in _gameMetaData)
                gameMD.Write(bw);
        }

        public void Read(BinaryReader br)
        {
            TimeStamp = br.ReadInt64();
            AutoAddDirectory = br.ReadBoolean();

            byte c = br.ReadByte();
            _gameMetaData.Clear();
            _gameMetaData.Capacity = c;
            for (byte i = 0; i < c; i++)
                _gameMetaData.Add(new GameMetaData(br));
        }

        public void AddData(DatData id, string val)
        {
            if (string.IsNullOrEmpty(val))
                return;

            int pos = 0;
            while (pos < _gameMetaData.Count && _gameMetaData[pos].Id < id)
                pos++;

            _gameMetaData.Insert(pos, new GameMetaData(id, val));
        }

        public string GetData(DatData id)
        {
            foreach (GameMetaData gameMD in _gameMetaData)
            {
                if (id == gameMD.Id) return gameMD.Value;
                if (id < gameMD.Id) return "";
            }
            return "";
        }
    }

}
