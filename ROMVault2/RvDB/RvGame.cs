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
    public class RvGame
    {
        // new xml here fjv
        public enum GameData
        {
            Description = 1,
            RomOf = 2,
            IsBios = 3,
            Sourcefile = 4,
            CloneOf = 5,
            SampleOf = 6,
            Board = 7,
            Year = 8,
            Manufacturer = 9,
            GameGenre=10,

            Papilio=11,
            papilioHardware=12,
            papilioScript=13,
            papilioNote=14,

            Trurip = 15,
            Publisher = 16,
            Developer = 17,
            Edition = 18,
            Version = 19,
            Type = 20,
            Media = 21,
            Language = 22,
            Players = 23,
            Ratings = 24,
            Peripheral = 25,
            Genre = 26,
            MediaCatalogNumber=27,
            BarCode=28
        }

        private class GameMetaData
        {
            public GameData Id { get; private set; }
            public String Value { get; private set; }

            public GameMetaData(GameData id, String value)
            {
                Id = id;
                Value = value;
            }
            public GameMetaData(BinaryReader br)
            {
                Id = (GameData)br.ReadByte();
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
            bw.Write((byte)_gameMetaData.Count);
            foreach (GameMetaData gameMD in _gameMetaData)
                gameMD.Write(bw);
        }

        public void Read(BinaryReader br)
        {
            byte c = br.ReadByte();
            _gameMetaData.Clear();
            _gameMetaData.Capacity = c;
            for (byte i = 0; i < c; i++)
                _gameMetaData.Add(new GameMetaData(br));
        }

        public void AddData(GameData id, string val)
        {
            if (string.IsNullOrEmpty(val))
                return;

            int pos = 0;
            while (pos < _gameMetaData.Count && _gameMetaData[pos].Id < id)
                pos++;

            _gameMetaData.Insert(pos, new GameMetaData(id, val));
        }

        public string GetData(GameData id)
        {
            foreach (GameMetaData gameMD in _gameMetaData)
            {
                if (id == gameMD.Id) return gameMD.Value;
                if (id < gameMD.Id) return "";
            }
            return "";
        }

        public void DeleteData(GameData id)
        {
            for (int i = 0; i < _gameMetaData.Count; i++)
            {
                if (id == _gameMetaData[i].Id)
                {
                    _gameMetaData.RemoveAt(i);
                    return;
                }
                if (id < _gameMetaData[i].Id) return;
            }
        }
    }

}
