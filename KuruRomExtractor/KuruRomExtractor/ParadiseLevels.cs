﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace KuruRomExtractor
{
    [StructLayout(LayoutKind.Explicit, Size = 0x4C)]
    struct ParadiseLevelEntry
    {
        public const int BASE_ADDRESS = 0x2C884;
        public const int ROM_MEMORY_DOMAIN = 0x08000000;

        [FieldOffset(0)]
        public int addr00; // Graphical tiles
        [FieldOffset(4)]
        public int addr01; // Background tiles, sometimes zero
        [FieldOffset(8)]
        public int addr02; // Background tiles 2, Sometimes zero (in particular for first levels)
        [FieldOffset(12)]
        public int addr03; // Physical tiles
        [FieldOffset(16)]
        public int level_data_offset;
        [FieldOffset(20)]
        public int graphical_data_offset; // Sometimes zero
        [FieldOffset(24)]
        public int background_data_offset;
        [FieldOffset(28)]
        public int addr07; // Sometimes zero (in particular for first levels)
        [FieldOffset(32)]
        public int addr08; // Palette for background tiles
        [FieldOffset(36)]
        public int addr09; // Sometimes zero (in particular for first levels)
        [FieldOffset(40)]
        public int addr10; // Other versions of the first color set (32 first bytes)
        [FieldOffset(44)]
        public int minimap_offset;
        [FieldOffset(48)]
        public int flags;
        [FieldOffset(52)]
        public int addr13; // Lot of zeros...
        [FieldOffset(56)]
        public int addr14; // Sometimes zero (in particular for first levels)
        [FieldOffset(60)]
        public int object_data_offset;
        [FieldOffset(64)]
        public int addr16; // Sometimes zero
        [FieldOffset(68)]
        public int addr17; // Sometimes zero (in particular for first levels)
        [FieldOffset(72)]
        public int addr18; // Often zero (in particular for first levels)

        public int LevelDataOffset
        {
            get { return level_data_offset - ROM_MEMORY_DOMAIN; }
            set { level_data_offset = value + ROM_MEMORY_DOMAIN; }
        }
        public int ObjectDataOffset
        {
            get { return object_data_offset - ROM_MEMORY_DOMAIN; }
            set { object_data_offset = value + ROM_MEMORY_DOMAIN; }
        }
        public int GraphicalDataOffset
        {
            get { return graphical_data_offset != 0 ? graphical_data_offset - ROM_MEMORY_DOMAIN : 0; }
            set { graphical_data_offset = value == 0 ? 0 : value + ROM_MEMORY_DOMAIN; }
        }
        public int BackgroundDataOffset
        {
            get { return background_data_offset - ROM_MEMORY_DOMAIN; }
            set { background_data_offset = value + ROM_MEMORY_DOMAIN; }
        }
        public int MinimapOffset
        {
            get { return minimap_offset - ROM_MEMORY_DOMAIN; }
            set { minimap_offset = value + ROM_MEMORY_DOMAIN; }
        }
    }
    public class ParadiseLevels
    {
        public const int NUMBER_LEVELS = 75;
        public const int MINIMAP_SIZE = 64 * 64 / 2;
        public static int[] AllLevels()
        {
            List<int> res = new List<int>();
            for (int i = 0; i < NUMBER_LEVELS; i++)
                res.Add(i);
            return res.ToArray();
        }

        const int COMMON_PALETTE_11 = 0x1C5BF0;
        const int COMMON_PALETTE_12 = 0x1C5CF0;
        const int COMMON_PALETTE_13 = 0x1C5DF0;
        const int COMMON_PALETTE_14 = 0x19BF64;
        const int COMMON_PALETTE_15 = 0x19BF84;
        public const int COLORSET_SIZE = 0x20;

        FileStream rom;
        ParadiseLevelEntry[] level_entries;

        public struct LevelInfo
        {
            public int DataBaseAddress;
            public int DataUncompressedSize;
            public int ObjectsBaseAddress;
            public int ObjectsSize;
            public int GraphicalBaseAddress;
            public int GraphicalUncompressedSize;
            public int BackgroundBaseAddress;
            public int BackgroundUncompressedSize;
            public int MinimapBaseAddress;
            public int MinimapUncompressedSize;
            public int Flags;
        }
        public struct RawMapData
        {
            public byte[] CompressedData;
            public byte[] RawData;
            public byte[] RawObjects;
            public byte[] CompressedGraphical;
            public byte[] RawGraphical;
            public byte[] CompressedBackground;
            public byte[] RawBackground;
            public byte[] CompressedMinimap;
            public byte[] RawMinimap;
        }

        public ParadiseLevels(string romPath)
        {
            rom = File.Open(romPath, FileMode.Open, FileAccess.ReadWrite);
            LoadLevelInfos();
        }

        void LoadLevelInfos()
        {
            BinaryReader reader = new BinaryReader(rom);

            level_entries = new ParadiseLevelEntry[NUMBER_LEVELS];
            rom.Seek(ParadiseLevelEntry.BASE_ADDRESS, SeekOrigin.Begin);
            for (int l = 0; l < level_entries.Length; l++)
            {
                level_entries[l] = Utils.ByteToType<ParadiseLevelEntry>(reader);
            }
            // For debugging purpose
            /*for (int l = 0; l < level_entries.Length; l++)
            {
                ParadiseLevelEntry e = level_entries[l];
                int[] toTest = new int[] { e.addr07, e.addr09 };
                int k = 0;
                foreach (int addr in toTest)
                {
                    if (addr >= 0x08000000)
                    {
                        try
                        {
                            rom.Seek(addr - 0x08000000, SeekOrigin.Begin);
                            Console.WriteLine(l.ToString() + "." + k.ToString() + ":" + reader.ReadInt32());
                        }
                        catch { }
                    }
                    k++;
                }
                Console.ReadLine();
            }*/
            /*for (int l = 0; l < level_entries.Length; l++)
            {
                ParadiseLevelEntry e = level_entries[l];
                Console.WriteLine(l.ToString("D2") + ": " + Convert.ToString(e.addr12, 2).PadLeft(24, '0'));
            }
            Console.ReadLine();*/
        }

        public LevelInfo GetLevelInfo(int level)
        {
            LevelInfo res;
            BinaryReader reader = new BinaryReader(rom);

            int base_addr = level_entries[level].LevelDataOffset;
            rom.Seek(base_addr, SeekOrigin.Begin);
            res.DataUncompressedSize = reader.ReadInt32();
            res.DataBaseAddress = (int)rom.Position;

            res.ObjectsBaseAddress = level_entries[level].ObjectDataOffset;
            res.ObjectsSize = 0;
            rom.Seek(res.ObjectsBaseAddress, SeekOrigin.Begin);
            byte[] endDelim = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            while (!reader.ReadBytes(endDelim.Length).SequenceEqual(endDelim))
                res.ObjectsSize += endDelim.Length;

            base_addr = level_entries[level].GraphicalDataOffset;
            if (base_addr == 0)
            {
                res.GraphicalUncompressedSize = 0;
                res.GraphicalBaseAddress = 0;
            }
            else
            {
                rom.Seek(base_addr, SeekOrigin.Begin);
                res.GraphicalUncompressedSize = reader.ReadInt32();
                res.GraphicalBaseAddress = (int)rom.Position;
            }

            base_addr = level_entries[level].BackgroundDataOffset;
            rom.Seek(base_addr, SeekOrigin.Begin);
            res.BackgroundUncompressedSize = reader.ReadInt32();
            res.BackgroundBaseAddress = (int)rom.Position;

            res.MinimapBaseAddress = level_entries[level].MinimapOffset;
            res.MinimapUncompressedSize = MINIMAP_SIZE;

            res.Flags = level_entries[level].flags;

            return res;
        }

        const int BACKGROUND_CHALLENGE_MASK = 0x2000;
        const ushort BACKGROUND_CHALLENGE_WIDTH = 0x10;
        const ushort BACKGROUND_CHALLENGE_HEIGHT = 0x10;

        public RawMapData ExtractLevelData(int level)
        {
            RawMapData res;
            LevelInfo info = GetLevelInfo(level);
            BinaryReader reader = new BinaryReader(rom);

            rom.Seek(info.DataBaseAddress, SeekOrigin.Begin);
            long startPos = rom.Position;
            res.RawData = LzCompression.Decompress(rom, info.DataUncompressedSize);
            int length = (int)(rom.Position - startPos);
            rom.Seek(startPos, SeekOrigin.Begin);
            res.CompressedData = reader.ReadBytes(length);

            rom.Seek(info.ObjectsBaseAddress, SeekOrigin.Begin);
            res.RawObjects = reader.ReadBytes(info.ObjectsSize);

            rom.Seek(info.GraphicalBaseAddress, SeekOrigin.Begin);
            startPos = rom.Position;
            res.RawGraphical = LzCompression.Decompress(rom, info.GraphicalUncompressedSize);
            length = (int)(rom.Position - startPos);
            rom.Seek(startPos, SeekOrigin.Begin);
            res.CompressedGraphical = reader.ReadBytes(length);

            rom.Seek(info.BackgroundBaseAddress, SeekOrigin.Begin);
            startPos = rom.Position;
            res.RawBackground = LzCompression.Decompress(rom, info.BackgroundUncompressedSize);
            length = (int)(rom.Position - startPos);
            rom.Seek(startPos, SeekOrigin.Begin);
            res.CompressedBackground = reader.ReadBytes(length);
            if ((info.Flags & BACKGROUND_CHALLENGE_MASK) != 0)
            {
                byte[] newRaw = new byte[res.RawBackground.Length + 4];
                BinaryWriter bw = new BinaryWriter(new MemoryStream(newRaw));
                bw.Write(BACKGROUND_CHALLENGE_WIDTH);
                bw.Write(BACKGROUND_CHALLENGE_HEIGHT);
                bw.Write(res.RawBackground);
                bw.Close();
                res.RawBackground = newRaw;
            }

            rom.Seek(info.MinimapBaseAddress, SeekOrigin.Begin);
            startPos = rom.Position;
            res.RawMinimap = LzCompression.Decompress(rom, info.MinimapUncompressedSize);
            length = (int)(rom.Position - startPos);
            rom.Seek(startPos, SeekOrigin.Begin);
            res.CompressedMinimap = reader.ReadBytes(length);

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

        int WriteDataWithCompression(int level, byte[] original_raw, byte[] original_compressed, byte[] new_raw, int baseAddr, int endAddr)
        {
            rom.Seek(baseAddr, SeekOrigin.Begin);
            if (new_raw == null && (endAddr < 0 || baseAddr + original_compressed.Length <= endAddr))
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
        void WriteSizeAndDataWithCompression(int level, byte[] original_raw, byte[] original_compressed, byte[] new_raw, int baseAddr, int endAddr)
        {
            int size = WriteDataWithCompression(level, original_raw, original_compressed,new_raw, baseAddr + sizeof(int), endAddr);
            long position_bkp = rom.Position;
            rom.Seek(baseAddr, SeekOrigin.Begin);
            (new BinaryWriter(rom)).Write(size);
            rom.Seek(position_bkp, SeekOrigin.Begin);
        }
        byte[] StripFirstBytes(byte[] data, int nb)
        {
            byte[] res = new byte[data.Length - nb];
            Array.Copy(data, nb, res, 0, res.Length);
            return res;
        }
        public bool AlterLevelData(int level, byte[] new_data, byte[] new_objects, byte[] new_graphical, byte[] new_background, byte[] new_minimap)
        {
            RawMapData original = ExtractLevelData(level);
            if (new_data != null && original.RawData.SequenceEqual(new_data))
                new_data = null;
            if (new_objects != null && original.RawObjects.SequenceEqual(new_objects))
                new_objects = null;
            if (new_graphical != null && original.RawGraphical.SequenceEqual(new_graphical))
                new_graphical = null;
            if (new_background != null && original.RawBackground.SequenceEqual(new_background))
                new_background = null;
            if (new_minimap != null && original.RawMinimap.SequenceEqual(new_minimap))
                new_minimap = null;

            if (new_data == null && new_objects == null && new_graphical == null && new_background == null && new_minimap == null)
                return false;

            // Write compressed data
            LevelInfo info = GetLevelInfo(level);

            int pos = ceilToMultiple((int)rom.Length, 4);
            level_entries[level].LevelDataOffset = pos;
            WriteSizeAndDataWithCompression(level, original.RawData, original.CompressedData, new_data, pos, -1);

            if ((new_graphical == null && original.RawGraphical.Length == 0) ||
                (new_graphical != null && new_graphical.Length == 0))
            {
                level_entries[level].GraphicalDataOffset = 0;
            }
            else
            {
                pos = ceilToMultiple((int)rom.Position, 4);
                level_entries[level].GraphicalDataOffset = pos;
                WriteSizeAndDataWithCompression(level, original.RawGraphical, original.CompressedGraphical, new_graphical, pos, -1);
            }

            pos = ceilToMultiple((int)rom.Position, 4);
            if ((info.Flags & BACKGROUND_CHALLENGE_MASK) != 0)
            {
                original.RawBackground = StripFirstBytes(original.RawBackground, 4);
                if (new_background != null)
                    new_background = StripFirstBytes(new_background, 4);
            }
            level_entries[level].BackgroundDataOffset = pos;
            WriteSizeAndDataWithCompression(level, original.RawBackground, original.CompressedBackground, new_background, pos, -1);

            pos = ceilToMultiple((int)rom.Position, 4);
            level_entries[level].MinimapOffset = pos;
            WriteDataWithCompression(level, original.RawMinimap, original.CompressedMinimap, new_minimap, pos, -1);

            pos = ceilToMultiple((int)rom.Position, 4);
            level_entries[level].ObjectDataOffset = pos;
            byte[] objects = new_objects == null ? original.RawObjects : new_objects;
            rom.Seek(pos, SeekOrigin.Begin);
            rom.Write(objects, 0, objects.Length);

            // Update LevelEntry structure
            rom.Seek(ParadiseLevelEntry.BASE_ADDRESS, SeekOrigin.Begin);
            BinaryWriter writer = new BinaryWriter(rom);
            foreach (ParadiseLevelEntry entry in level_entries)
                Utils.TypeToByte(writer, entry);

            return true;
        }

        byte[] DecompressWorldData(int addr, int uncompressed_size)
        {
            if (addr == 0)
                return null;
            rom.Seek(addr - ParadiseLevelEntry.ROM_MEMORY_DOMAIN, SeekOrigin.Begin);
            return LzCompression.Decompress(rom, uncompressed_size);
        }
        byte[] ReadWorldData(int addr, int size)
        {
            if (addr == 0)
                return null;
            rom.Seek(addr - ParadiseLevelEntry.ROM_MEMORY_DOMAIN, SeekOrigin.Begin);
            return (new BinaryReader(rom)).ReadBytes(size);
        }

        public byte[][] ExtractTilesData(int level)
        {
            byte[][] res = new byte[6][];
            ParadiseLevelEntry ple = level_entries[level];
            res[0] = DecompressWorldData(ple.addr00, 0x4000);
            res[1] = DecompressWorldData(ple.addr01, 0x4000);
            res[2] = DecompressWorldData(ple.addr02, 0x4000);
            res[3] = DecompressWorldData(ple.addr03, 0x2000);
            res[4] = DecompressWorldData(ple.addr08, 512);
            res[5] = ReadWorldData(ple.addr10, COLORSET_SIZE);
            return res;
        }

        public byte[] ExtractCommonPaletteData()
        {
            byte[] res = new byte[5 * COLORSET_SIZE];
            BinaryWriter writer = new BinaryWriter(new MemoryStream(res));
            BinaryReader reader = new BinaryReader(rom);
            rom.Seek(COMMON_PALETTE_11, SeekOrigin.Begin);
            writer.Write(reader.ReadBytes(COLORSET_SIZE));
            rom.Seek(COMMON_PALETTE_12, SeekOrigin.Begin);
            writer.Write(reader.ReadBytes(COLORSET_SIZE));
            rom.Seek(COMMON_PALETTE_13, SeekOrigin.Begin);
            writer.Write(reader.ReadBytes(COLORSET_SIZE));
            rom.Seek(COMMON_PALETTE_14, SeekOrigin.Begin);
            writer.Write(reader.ReadBytes(COLORSET_SIZE));
            rom.Seek(COMMON_PALETTE_15, SeekOrigin.Begin);
            writer.Write(reader.ReadBytes(COLORSET_SIZE));
            writer.Close();
            return res;
        }

        public void Dispose()
        {
            rom.Close();
        }
    }
}
