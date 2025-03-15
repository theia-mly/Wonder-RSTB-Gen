using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using ZstdSharp;
using System.IO.Hashing;
using System.IO;
using System.Linq.Expressions;
using CRC32B;

namespace ResourceSizeTable
{
    // Only resource size tables with 0x16 headers supported
    internal class ResourceSizeTable
    {
        string Magic;
        UInt32 Version;
        UInt32 StringSize;
        public UInt32 Crc32bPairCount;
        UInt32 CollisionPairCount;
        Dictionary<UInt32, UInt32> Crc32bTable = new Dictionary<UInt32, UInt32>();
        Dictionary<string, UInt32> CollisionPathPairTable = new Dictionary<string, UInt32>();

        string RSTBPath;

        // From https://github.com/dt-12345/totktools/blob/master/src/restbl.py and https://wonder.miraheze.org/wiki/RSIZETABLE_(File_Format)
        private Dictionary<string, UInt32> FileTypeSizeTable = new Dictionary<string, UInt32>()
        {
            {"ainb", 0x200 },
            {"asb", 0x228 },
            {"baatarc", 0x100 },
            {"baev", 0x120 },
            {"bagst", 0x100 },
            {"bars", 0x240 },
            {"bcul", 0x100 },
            {"beco", 0x100 },
            {"belnk", 0x100 },
            {"bfarc", 0x100 },
            {"bfevfl", 0x120 },
            {"bfsha", 0x100 },
            {"bhtmp", 0x100 },
            {"blal", 0x100 },
            {"blarc", 0x1000 },
            {"blwp", 0x100 },
            {"bnsh", 0x100 },
            {"bntx", 0x2000 },
            {"bphcl", 0x100 },
            {"bphhb", 0x100 },
            {"bphnm", 0x120 },
            {"bphsh", 0x170 },
            {"bslnk", 0x100 },
            {"bstar", 0x120 },
            {"bwav", 0x100 },
            {"byml", 0x100 },
            {"cai", 0x100 },
            {"chunk", 0x100 },
            {"crbin", 0x100 },
            {"cutinfo", 0x100 },
            {"dpi", 0x100 },
            {"genvb", 0x1000 },
            {"jpg", 0x100 },
            {"pack", 0x180 },
            {"png", 0x100 },
            {"quad", 0x100 },
            {"release.sarc", 0x1000 },
            {"rsizetable", 0x100 },
            {"sarc", 0x180 },
            {"tscb", 0x100 },
            {"txt", 0x100 },
            {"txtg", 0x100 },
            {"vsts", 0x100 },
            {"wbr", 0x100 },
        };

        public UInt32 GetFileTypeSize(string fileType)
        {
            try
            {
                return FileTypeSizeTable[fileType];
            }
            catch
            {
                return 0;
            }
        }

        public ResourceSizeTable(string path)
        {
            RSTBPath = path;
            Load(path);
        }

        public DecompressionStream DecompressStream(FileStream compressedStream)
        {
            return new DecompressionStream(compressedStream);
        }

        public static byte[] CompressData(byte[] fileBytes)
        {
            byte[] compressedData;

            using (var compressor = new ZstdSharp.Compressor(19))
            {
                compressedData = compressor.Wrap(new System.Span<byte>(fileBytes)).ToArray();
            }

            return compressedData;
        }

        public void ReadFile(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                // Get File Magic (Should be "RESTBL")
                Magic = new string(reader.ReadChars(6));
                Console.WriteLine($"Magic: {Magic}");

                // Get Version (should be 1)
                Version = reader.ReadUInt32();
                Console.WriteLine($"Version: v{Version}");

                // Get String Size
                StringSize = reader.ReadUInt32();
                Console.WriteLine($"String Size: {StringSize}");

                // Get Crc32b hash array size
                Crc32bPairCount = reader.ReadUInt32();
                Console.WriteLine($"Hash Array Size: {Crc32bPairCount}");

                // Get Collision Pair size
                CollisionPairCount = reader.ReadUInt32();
                Console.WriteLine($"Collision Pair Array Size: {CollisionPairCount}");

                for (int i = 0; i < Crc32bPairCount; i++)
                {
                    UInt32 hash = reader.ReadUInt32();
                    UInt32 value = reader.ReadUInt32();
                    Crc32bTable.Add(hash, value);
                }

                for (int i = 0; i < CollisionPairCount; i++)
                {
                    string key = new string(reader.ReadChars((int)StringSize));
                    UInt32 value = reader.ReadUInt32();
                    CollisionPathPairTable.Add(key, value);
                }
            }
        }

        private void EditCrc32bEntry(UInt32 hash, UInt32 value)
        {
            Crc32bTable[hash] = value;
            Crc32bPairCount = (UInt32)Crc32bTable.Count;
        }

        private void EditCrc32bEntryFromPath(string path, UInt32 value)
        {
            UInt32 hash = (UInt32)CRC32B.CRC32B.Compute(path);
            EditCrc32bEntry(hash, value);
        }

        private UInt32 CalcSizeFromFile(string path)
        {
            string fileType = path.Replace(".zs", "").Split('.').Last();
            UInt32 size = 0;

            if (path.Contains(".zs"))
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    byte[] decompressedData;

                    using (var decompressor = new ZstdSharp.Decompressor())
                    {
                        decompressedData = decompressor.Unwrap(new ReadOnlySpan<byte>(File.ReadAllBytes(path))).ToArray();
                        size = (UInt32)decompressedData.Length;
                    }
                }
            }
            else
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    size = (UInt32)fileStream.Length;
                }
            }

            switch (fileType)
            {
                case "bfres":
                    return CalcSizeForBFRES(size);
                case "bgyml":
                    return CalcSizeForBGYML(size);
                case "bkres":
                    return CalcSizeForBKRES(size);
                case "bphsc":
                    return CalcSizeForBPHSC(size);
                default:
                    // Round up
                    size = (UInt32)((size + 0x1F) & -0x20);

                    try
                    {
                        fileType = path.Split('.')[^3] + "." + path.Split('.')[^2];
                        size += FileTypeSizeTable[fileType];
                    }
                    catch
                    {
                        try
                        {
                            fileType = path.Split('.')[^2];
                            size += FileTypeSizeTable[fileType];
                        }
                        catch
                        {
                            try
                            {
                                fileType = path.Split('.')[^1];
                                size += FileTypeSizeTable[fileType];
                            }
                            catch
                            {
                                return UInt32.MaxValue;
                            }
                        }
                    }

                    return size;
            }
        }

        private UInt32 CalcSizeForBFRES(UInt32 fileSize)
        {
            return (UInt32)(1.01 * fileSize + 1205565);
        }

        private UInt32 CalcSizeForBGYML(UInt32 fileSize)
        {
            return (UInt32)(9627 * MathF.Exp(1.35E-4f * (float)fileSize) + 860);
        }

        private UInt32 CalcSizeForBKRES(UInt32 fileSize)
        {
            return (UInt32)(1.01 * fileSize + 77370);
        }

        private UInt32 CalcSizeForBPHSC(UInt32 fileSize)
        {
            return (UInt32)(1.01 * fileSize + 3200);
        }

        public void UpdateTable(string path)
        {
            string[] modFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

            foreach (string file in modFiles)
            {
                if(!File.Exists(file))
                {
                    continue;
                }

                if (!file.Contains("ResourceSizeTable") && !file.Contains("ignore__"))
                {
                    string relPath = file.Split(path).Last().Replace("\\", "/").Substring(1);
                    relPath = relPath.Replace(".zs", "");
                    UInt32 size = CalcSizeFromFile(file);

                    try
                    {
                        if (size != UInt32.MaxValue)
                        {
                            Console.WriteLine(relPath);
                            EditCrc32bEntryFromPath(relPath, size);
                        }
                        else
                        {
                            Console.WriteLine($"File {relPath} skipped");
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"File {relPath} skipped");
                    }
                }
            }

            string relPathRSTB = RSTBPath.Split(path).Last().Replace("\\", "/").Substring(1);
            relPathRSTB = relPathRSTB.Replace(".zs", "");
            UInt32 sizeRSTB = GetRSTBSize() + FileTypeSizeTable["rsizetable"];

            Console.WriteLine(relPathRSTB);
            EditCrc32bEntryFromPath(relPathRSTB, sizeRSTB);
        }

        private UInt32 GetRSTBSize()
        {
            // 0x12 = Header Size
            return 0x12 + 8 * Crc32bPairCount + (StringSize + 4) * CollisionPairCount;
        }

        public void Load(string path)
        {
            // Get file
            using (FileStream fileStream = File.OpenRead(path))
            {
                DecompressionStream fileStreamDecomp = DecompressStream(fileStream);
                ReadFile(fileStreamDecomp);

                fileStream.Close();
            }
        }

        public void Save(string path)
        {
            var memoryStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(memoryStream, Encoding.UTF8, false))
            {
                writer.Write(Encoding.UTF8.GetBytes(Magic));
                writer.Write(Version);
                writer.Write(StringSize);
                writer.Write(Crc32bPairCount);
                writer.Write(CollisionPairCount);

                foreach (var entry in Crc32bTable.OrderBy(x => x.Key))
                {
                    writer.Write(entry.Key);
                    writer.Write(entry.Value);
                }

                foreach (var entry in CollisionPathPairTable.OrderBy(x => x.Key))
                {
                    writer.Write(new byte[128]);
                    writer.Seek(-128, SeekOrigin.Current);
                    writer.Write(Encoding.UTF8.GetBytes(entry.Key));
                    writer.Write(entry.Value);
                }
            }

            File.WriteAllBytes(path, CompressData(memoryStream.ToArray()));
        }
    }
}
