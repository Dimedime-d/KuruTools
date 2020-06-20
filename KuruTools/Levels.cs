﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace KuruTools
{
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    struct WorldEntry
    {
        public const int BASE_ADDRESS = 0xC2E68;
        const int ROM_MEMORY_DOMAIN = 0x08000000;

        [FieldOffset(0)]
        int world_info_mem_address;
        [FieldOffset(4)]
        int world_data_mem_address;
        [FieldOffset(8)]
        int dummy1; // Seems related to bonuses

        public int WorldInfoBaseAddress
        {
            get { return world_info_mem_address - ROM_MEMORY_DOMAIN; }
        }
        public int WorldDataBaseAddress
        {
            get { return world_data_mem_address - ROM_MEMORY_DOMAIN; }
        }
    }
    [StructLayout(LayoutKind.Explicit, Size = 0x80)]
    struct WorldInfo
    {
        [FieldOffset(0)]
        public int addr1_offset; // Seems interesting
        [FieldOffset(4)]
        public int addr1_uncompressed_size;
        [FieldOffset(8)]
        public int addr2_offset; // Seems interesting
        [FieldOffset(12)]
        public int addr2_uncompressed_size;
        [FieldOffset(16)]
        public int addr3_offset;
        [FieldOffset(20)]
        public int addr3_uncompressed_size;
        [FieldOffset(24)]
        public int addr4_offset;
        [FieldOffset(28)]
        public int addr4_uncompressed_size;
        [FieldOffset(32)]
        public int addr5_offset;
        [FieldOffset(36)]
        public int addr5_uncompressed_size;
        [FieldOffset(40)]
        public int addr6_offset;
        [FieldOffset(44)]
        public int addr6_uncompressed_size;
        [FieldOffset(48)]
        public int addr7_offset;
        [FieldOffset(52)]
        public int addr7_uncompressed_size;
        [FieldOffset(56)]
        public int addr8_offset;
        [FieldOffset(60)]
        public int addr8_uncompressed_size;
        [FieldOffset(64)]
        public int addr9_offset;
        [FieldOffset(68)]
        public int addr9_uncompressed_size;
        [FieldOffset(72)]
        public int addr10_offset;
        [FieldOffset(76)]
        public int addr10_uncompressed_size;
        [FieldOffset(80)]
        public int addr11_offset;
        [FieldOffset(84)]
        public int addr11_uncompressed_size;
        [FieldOffset(88)]
        public int addr12_offset;
        [FieldOffset(92)]
        public int addr12_uncompressed_size;
        [FieldOffset(96)]
        public int addr13_offset;
        [FieldOffset(100)]
        public int addr13_uncompressed_size;
        [FieldOffset(104)]
        public int addr14_offset;
        [FieldOffset(108)]
        public int addr14_uncompressed_size;
        [FieldOffset(112)]
        public int addr15_offset;
        [FieldOffset(116)]
        public int addr15_uncompressed_size;
        [FieldOffset(120)]
        public int addr16_offset;
        [FieldOffset(124)]
        public int addr16_uncompressed_size;
    }
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct LevelEntry
    {
        [FieldOffset(0)]
        public int level_data_offset;
        [FieldOffset(4)]
        public int level_uncompressed_size;
        [FieldOffset(8)]
        public int graphical_data_offset;
        [FieldOffset(12)]
        public int graphical_uncompressed_size;
        [FieldOffset(16)]
        public int background_data_offset;
        [FieldOffset(20)]
        public int background_uncompressed_size;
        [FieldOffset(24)]
        public int minimap_data_offset;
        [FieldOffset(28)]
        public int minimap_size; // (0x800)
    }
    public class Levels
    {
        public enum World
        {
            TRAINING = 0,
            GRASS,
            OCEAN,
            JUNGLE,
            CAKE,
            CAVE,
            CLOUD,
            STAR,
            ICE,
            MACHINE,
            GHOST,
            LAST,
            CHALLENGE,
        };
        static readonly int[] NUMBER_LEVELS = { 5, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 55 };
        public struct LevelInfo
        {
            public int DataBaseAddress;
            public int DataUncompressedSize;
            public int GraphicalBaseAddress;
            public int GraphicalUncompressedSize;
            public int BackgroundBaseAddress;
            public int BackgroundUncompressedSize;
            public int MinimapBaseAddress;
            public int MinimapSize;
        }
        public struct RawMapData
        {
            public byte[] CompressedData;
            public byte[] RawData;
            public byte[] CompressedGraphical;
            public byte[] RawGraphical;
            public byte[] CompressedBackground;
            public byte[] RawBackground;
            public byte[] RawMinimap;
        }
        public struct LevelIdentifier
        {
            public LevelIdentifier(World w, int l)
            {
                world = w;
                level = l;
            }
            public World world;
            public int level;
            public string ShortName()
            {
                return string.Format("{2:D2}_{0}_{1:D2}", Enum.GetName(typeof(World), world).ToLowerInvariant(), level+1, (int)world);
            }
            public string ToString()
            {
                return string.Format("{0} {1}", Enum.GetName(typeof(World), world), level + 1);
            }
        }

        public static int NumberOfLevels(World world)
        {
            return NUMBER_LEVELS[(int)world];
        }

        public static LevelIdentifier[] AllLevels()
        {
            List<LevelIdentifier> res = new List<LevelIdentifier>();
            foreach (World w in Enum.GetValues(typeof(World)))
            {
                for (int l = 0; l < NumberOfLevels(w); l++)
                    res.Add(new LevelIdentifier(w, l));
            }
            return res.ToArray();
        }

        WorldEntry[] world_entries;
        WorldInfo[] world_infos;
        LevelEntry[][] level_entries;
        FileStream rom;

        public Levels(string romPath)
        {
            rom = File.Open(romPath, FileMode.Open, FileAccess.ReadWrite);
            LoadLevelInfos();
        }

        void LoadLevelInfos()
        {
            BinaryReader reader = new BinaryReader(rom);

            // World entries
            world_entries = new WorldEntry[Enum.GetValues(typeof(World)).Length];
            rom.Seek(WorldEntry.BASE_ADDRESS, SeekOrigin.Begin);
            for (int w = 0; w < world_entries.Length; w++)
                world_entries[w] = Utils.ByteToType<WorldEntry>(reader);

            // World infos and level entries
            world_infos = new WorldInfo[world_entries.Length];
            level_entries = new LevelEntry[world_entries.Length][];
            for (int w = 0; w < world_entries.Length; w++)
            {
                WorldEntry we = world_entries[w];
                rom.Seek(we.WorldInfoBaseAddress, SeekOrigin.Begin);
                world_infos[w] = Utils.ByteToType<WorldInfo>(reader);
                LevelEntry[] le = new LevelEntry[NUMBER_LEVELS[w]];
                for (int l = 0; l < le.Length; l++)
                    le[l] = Utils.ByteToType<LevelEntry>(reader);
                level_entries[w] = le;
            }
        }

        public LevelInfo GetLevelInfo(LevelIdentifier level)
        {
            int w = (int)level.world;
            int l = level.level;
            int base_addr = world_entries[w].WorldDataBaseAddress;
            LevelInfo res;
            res.DataBaseAddress = base_addr + level_entries[w][l].level_data_offset;
            res.DataUncompressedSize = level_entries[w][l].level_uncompressed_size;
            res.GraphicalBaseAddress = base_addr + level_entries[w][l].graphical_data_offset;
            res.GraphicalUncompressedSize = level_entries[w][l].graphical_uncompressed_size;
            res.BackgroundBaseAddress = base_addr + level_entries[w][l].background_data_offset;
            res.BackgroundUncompressedSize = level_entries[w][l].background_uncompressed_size;
            res.MinimapBaseAddress = base_addr + level_entries[w][l].minimap_data_offset;
            res.MinimapSize = level_entries[w][l].minimap_size;
            return res;
        }

        public RawMapData ExtractLevelData(LevelIdentifier level)
        {
            RawMapData res;
            LevelInfo info = GetLevelInfo(level);

            rom.Seek(info.DataBaseAddress, SeekOrigin.Begin);
            long startPos = rom.Position;
            res.RawData = LzCompression.Decompress(rom, info.DataUncompressedSize);
            int length = (int)(rom.Position - startPos);
            rom.Seek(startPos, SeekOrigin.Begin);
            res.CompressedData = (new BinaryReader(rom)).ReadBytes(length);

            rom.Seek(info.GraphicalBaseAddress, SeekOrigin.Begin);
            startPos = rom.Position;
            res.RawGraphical = LzCompression.Decompress(rom, info.GraphicalUncompressedSize);
            length = (int)(rom.Position - startPos);
            rom.Seek(startPos, SeekOrigin.Begin);
            res.CompressedGraphical = (new BinaryReader(rom)).ReadBytes(length);

            rom.Seek(info.BackgroundBaseAddress, SeekOrigin.Begin);
            startPos = rom.Position;
            res.RawBackground = LzCompression.Decompress(rom, info.BackgroundUncompressedSize);
            length = (int)(rom.Position - startPos);
            rom.Seek(startPos, SeekOrigin.Begin);
            res.CompressedBackground = (new BinaryReader(rom)).ReadBytes(length);

            rom.Seek(info.MinimapBaseAddress, SeekOrigin.Begin);
            res.RawMinimap = (new BinaryReader(rom)).ReadBytes(info.MinimapSize);

            return res;
        }

        int floorToMultiple(int v, int multiple)
        {
            return v - (v % multiple);
        }
        int ceilToMultiple(int v, int multiple)
        {
            if (v % multiple == 0) return v;
            return floorToMultiple(v, multiple) + multiple;
        }

        int WriteDataWithCompression(LevelIdentifier level, byte[] original_raw, byte[] original_compressed, byte[] new_raw, int baseAddr, int endAddr)
        {
            rom.Seek(baseAddr, SeekOrigin.Begin);
            if (new_raw == null && (endAddr < 0 ||baseAddr + original_compressed.Length <= endAddr))
            {
                rom.Write(original_compressed);
                return original_raw.Length;
            }
            else if (new_raw == null)
                new_raw = original_raw;
            int uncompressed_length_written = LzCompression.Compress(rom, new_raw, endAddr);
            if (uncompressed_length_written < new_raw.Length)
                Console.WriteLine(string.Format("Warning: The new level {0} has been truncated.", level.ToString()));
            Debug.Assert(uncompressed_length_written <= new_raw.Length);
            return floorToMultiple(uncompressed_length_written, 4);
            //return new_raw.Length;
        }
        public bool AlterLevelData(LevelIdentifier level, byte[] new_data, byte[] new_graphical, byte[] new_background, byte[] new_minimap, bool relocate)
        {
            RawMapData original = ExtractLevelData(level);
            if (new_data != null && original.RawData.SequenceEqual(new_data))
                new_data = null;
            if (new_graphical != null && original.RawGraphical.SequenceEqual(new_graphical))
                new_graphical = null;
            if (new_background != null && original.RawBackground.SequenceEqual(new_background))
                new_background = null;
            if (new_minimap != null && original.RawMinimap.SequenceEqual(new_minimap))
                new_minimap = null;

            if (new_data == null && new_graphical == null && new_background == null && new_minimap == null)
                return false;

            // Write compressed data
            int w = (int)level.world;
            int l = level.level;
            LevelInfo info = GetLevelInfo(level);

            int pos0 = relocate ? ceilToMultiple((int)rom.Length, 4) : info.DataBaseAddress;
            level_entries[w][l].level_data_offset = pos0 - world_entries[w].WorldDataBaseAddress;
            level_entries[w][l].level_uncompressed_size =
                WriteDataWithCompression(level, original.RawData, original.CompressedData, new_data, pos0, relocate ? -1 : info.GraphicalBaseAddress);

            int pos1 = ceilToMultiple((int)rom.Position, 4);
            level_entries[w][l].graphical_data_offset = pos1 - world_entries[w].WorldDataBaseAddress;
            level_entries[w][l].graphical_uncompressed_size =
                WriteDataWithCompression(level, original.RawGraphical, original.CompressedGraphical, new_graphical, pos1, relocate ? -1 : info.BackgroundBaseAddress);

            int pos2 = ceilToMultiple((int)rom.Position, 4);
            level_entries[w][l].background_data_offset = pos2 - world_entries[w].WorldDataBaseAddress;
            level_entries[w][l].background_uncompressed_size =
                WriteDataWithCompression(level, original.RawBackground, original.CompressedBackground, new_background, pos2, relocate ? -1 : info.MinimapBaseAddress);

            int pos3 = ceilToMultiple((int)rom.Position, 4);
            level_entries[w][l].minimap_data_offset = pos3 - world_entries[w].WorldDataBaseAddress;
            byte[] minimap = new_minimap == null ? original.RawMinimap : new_minimap;
            int endAddr = (relocate ? pos3 : info.MinimapBaseAddress) + info.MinimapSize;
            rom.Seek(pos3, SeekOrigin.Begin);
            rom.Write(minimap, Math.Max(0, minimap.Length + pos3 - endAddr), Math.Min(endAddr - pos3, minimap.Length));

            // Update LevelEntry structure
            rom.Seek(world_entries[w].WorldInfoBaseAddress + Marshal.SizeOf(typeof(WorldInfo)), SeekOrigin.Begin);
            BinaryWriter writer = new BinaryWriter(rom);
            foreach (LevelEntry entry in level_entries[w])
                Utils.TypeToByte(writer, entry);

            return true;
        }

        public void Dispose()
        {
            rom.Close();
        }
    }
}
