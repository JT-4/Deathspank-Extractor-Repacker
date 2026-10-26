using System;
using System.IO;

namespace Deathspank_Extractor_Repacker
{
    /// <summary>
    /// Rebuilds the 800-byte validation area used by the analyzed
    /// original DeathSpank.exe .gg archives.
    ///
    /// Layout:
    ///   0x000-0x00F : GG header
    ///   0x010-0x0AF : A - executable/build token
    ///   0x0B0-0x14F : B - main validation CRC
    ///   0x150-0x1EF : C - filename hash
    ///   0x1F0-0x28F : D - CRC of complete B block
    ///   0x290-0x32F : F - CRC-covered filler/data
    ///   0x330-...   : normal GG content
    ///
    /// A and F are preserved. Only the significant encoded bits in
    /// B, C and D are changed.
    /// </summary>
    public static class GG_1_resealer
    {
        public const uint Magic = 0xD9B48B81u;
        public const uint Version = 0x00020000u;

        public const uint BuildToken = 0x0002A2F1u;
        public const uint BuildCrcSeed = 0x0583D8F9u;

        public const uint Polynomial = 0x04C11DB7u;
        public const uint FilenameHashSeed = 0x87568965u;

        public const int HeaderSize = 0x10;
        public const int BlockSize = 0xA0;
        public const int ValidationBlockSize = 800;
        public const int ValidationEnd = 0x330;
        public const int CrcWindowEnd = 0x8000;

        public const int OffA = 0x010;
        public const int OffB = 0x0B0;
        public const int OffC = 0x150;
        public const int OffD = 0x1F0;
        public const int OffF = 0x290;

        private static readonly uint[] CrcTable = CreateCrcTable();

        /// <summary>
        /// Rebuilds the archive validation metadata in place.
        ///
        /// logicalName must be the basename the game will see, e.g.
        /// "GameData-000001000.gg".
        ///
        /// The analyzed executable reads and hashes a fixed 0x8000-byte
        /// validation buffer. This implementation therefore requires at
        /// least 0x8000 bytes so that its result is deterministic and
        /// identical to the validated bytes.
        /// </summary>
        public static void Reseal(byte[] data, string logicalName)
        {
            ValidateArguments(data, logicalName);
            ValidateHeader(data);

            uint existingA = DecodeBlockValue(data, OffA);
            if (existingA != BuildToken)
            {
                throw new InvalidDataException(
                    string.Format(
                        "Block A is 0x{0:X8}; expected build token 0x{1:X8}.",
                        existingA,
                        BuildToken));
            }

            // C = case-insensitive filename hash.
            uint nameHash = GetFilenameHash(logicalName);
            EncodeBlockValue(data, OffC, nameHash);

            // B = main validation CRC.
            uint mainCrc = GetMainCrc(data);
            EncodeBlockValue(data, OffB, mainCrc);

            // D = CRC of the complete, newly updated 160-byte B record.
            uint blockBCrc = UpdateCrc(BuildCrcSeed, data, OffB, BlockSize);
            EncodeBlockValue(data, OffD, blockBCrc);
        }

        /// <summary>
        /// Returns a resealed copy, leaving the input array unchanged.
        /// </summary>
        public static byte[] ResealCopy(byte[] data, string logicalName)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            byte[] copy = (byte[])data.Clone();
            Reseal(copy, logicalName);
            return copy;
        }

        /// <summary>
        /// Convenience file wrapper. If outputPath is null/empty, overwrites
        /// inputPath. If logicalName is null/empty, the input basename is used.
        /// </summary>
        public static void ResealFile(
            string inputPath,
            string outputPath = null,
            string logicalName = null)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                throw new ArgumentException("Input path is required.", nameof(inputPath));

            byte[] data = File.ReadAllBytes(inputPath);

            if (string.IsNullOrWhiteSpace(logicalName))
                logicalName = Path.GetFileName(inputPath);

            Reseal(data, logicalName);

            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = inputPath;

            File.WriteAllBytes(outputPath, data);
        }

        /// <summary>
        /// Verifies the current archive metadata without modifying the buffer.
        /// </summary>
        public static GG_1_ValidationReport Verify(byte[] data, string logicalName)
        {
            ValidateArguments(data, logicalName);
            ValidateHeader(data);

            uint a = DecodeBlockValue(data, OffA);
            uint b = DecodeBlockValue(data, OffB);
            uint c = DecodeBlockValue(data, OffC);
            uint d = DecodeBlockValue(data, OffD);

            uint calcB = GetMainCrc(data);
            uint calcC = GetFilenameHash(logicalName);
            uint calcD = UpdateCrc(BuildCrcSeed, data, OffB, BlockSize);

            return new GG_1_ValidationReport
            {
                A = a,
                B = b,
                C = c,
                D = d,

                CalculatedB = calcB,
                CalculatedC = calcC,
                CalculatedD = calcD,

                BuildTokenValid = a == BuildToken,
                MainCrcValid = b == calcB,
                FilenameHashValid = c == calcC,
                BlockBCrcValid = d == calcD
            };
        }

        /// <summary>
        /// Returns exactly the 800-byte validation area at offsets 0x10-0x32F.
        /// </summary>
        public static byte[] GetValidationBlock(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < ValidationEnd)
            {
                throw new InvalidDataException(
                    string.Format(
                        "File is only 0x{0:X} bytes; expected at least 0x{1:X}.",
                        data.Length,
                        ValidationEnd));
            }

            byte[] block = new byte[ValidationBlockSize];
            Buffer.BlockCopy(data, HeaderSize, block, 0, ValidationBlockSize);
            return block;
        }

        /// <summary>
        /// Reseals a copy and returns only the resulting 800-byte block.
        /// </summary>
        public static byte[] CreateValidationBlock(byte[] data, string logicalName)
        {
            byte[] copy = ResealCopy(data, logicalName);
            return GetValidationBlock(copy);
        }

        /// <summary>
        /// Decodes the 32-bit value hidden in one 160-byte record.
        /// DWORD i contributes bit i from its own value.
        /// </summary>
        public static uint DecodeBlockValue(byte[] data, int offset)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (offset < 0 || offset + BlockSize > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            uint value = 0;

            for (int i = 0; i < 32; i++)
            {
                uint word = ReadUInt32LE(data, offset + (i * 4));
                uint mask = 1u << i;
                value |= word & mask;
            }

            return value;
        }

        /// <summary>
        /// Encodes a 32-bit value while preserving every unrelated/noise bit.
        /// </summary>
        public static void EncodeBlockValue(byte[] data, int offset, uint value)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (offset < 0 || offset + BlockSize > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            for (int i = 0; i < 32; i++)
            {
                int pos = offset + (i * 4);
                uint word = ReadUInt32LE(data, pos);
                uint mask = 1u << i;

                if ((value & mask) != 0)
                    word |= mask;
                else
                    word &= ~mask;

                WriteUInt32LE(data, pos, word);
            }
        }

        /// <summary>
        /// Case-insensitive polynomial-31 filename hash used by the game.
        /// ASCII A-Z is converted to lowercase.
        /// </summary>
        public static uint GetFilenameHash(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            uint hash = FilenameHashSeed;

            unchecked
            {
                foreach (char ch in name)
                {
                    uint c = ch;

                    if (c >= 'A' && c <= 'Z')
                        c += 0x20;

                    hash = (hash * 31u) + c;
                }
            }

            return hash;
        }

        /// <summary>
        /// Computes validation field B.
        ///
        /// Included:
        ///   0x0000-0x00AF : header + A
        ///   0x0150-0x01EF : C
        ///   0x0290-0x032F : F
        ///   0x0330-0x7FFF : ordinary GG data
        ///
        /// Excluded:
        ///   0x00B0-0x014F : B
        ///   0x01F0-0x028F : D
        /// </summary>
        public static uint GetMainCrc(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < CrcWindowEnd)
            {
                throw new InvalidDataException(
                    string.Format(
                        "File is only 0x{0:X} bytes; this DeathSpank validator " +
                        "hashes a fixed 0x{1:X}-byte buffer.",
                        data.Length,
                        CrcWindowEnd));
            }

            uint crc = BuildCrcSeed;

            crc = UpdateCrc(crc, data, 0x000, 0x0B0);
            crc = UpdateCrc(crc, data, 0x150, 0x0A0);
            crc = UpdateCrc(crc, data, 0x290, 0x0A0);
            crc = UpdateCrc(crc, data, 0x330, 0x7CD0);

            return crc;
        }

        /// <summary>
        /// Non-reflected, MSB-first CRC-32 update using polynomial 0x04C11DB7.
        /// </summary>
        public static uint UpdateCrc(uint crc, byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (offset < 0 || count < 0 || offset > data.Length - count)
                throw new ArgumentOutOfRangeException();

            unchecked
            {
                uint value = crc;
                int end = offset + count;

                for (int i = offset; i < end; i++)
                {
                    int index = (int)(((value >> 24) ^ data[i]) & 0xFFu);
                    value = (value << 8) ^ CrcTable[index];
                }

                return value;
            }
        }

        private static uint[] CreateCrcTable()
        {
            uint[] table = new uint[256];

            unchecked
            {
                for (uint i = 0; i < 256; i++)
                {
                    uint c = i << 24;

                    for (int bit = 0; bit < 8; bit++)
                    {
                        if ((c & 0x80000000u) != 0)
                            c = (c << 1) ^ Polynomial;
                        else
                            c <<= 1;
                    }

                    table[i] = c;
                }
            }

            return table;
        }

        private static void ValidateArguments(byte[] data, string logicalName)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(logicalName))
            {
                throw new ArgumentException(
                    "The logical GG filename is required.",
                    nameof(logicalName));
            }

            if (data.Length < CrcWindowEnd)
            {
                throw new InvalidDataException(
                    string.Format(
                        "File is only 0x{0:X} bytes; this DeathSpank validator " +
                        "hashes a fixed 0x{1:X}-byte buffer.",
                        data.Length,
                        CrcWindowEnd));
            }
        }

        private static void ValidateHeader(byte[] data)
        {
            uint magic = ReadUInt32LE(data, 0);
            uint version = ReadUInt32LE(data, 4);

            if (magic != Magic)
            {
                throw new InvalidDataException(
                    string.Format(
                        "Unexpected magic 0x{0:X8}; expected 0x{1:X8}.",
                        magic,
                        Magic));
            }

            if (version != Version)
            {
                throw new InvalidDataException(
                    string.Format(
                        "Unexpected GG version 0x{0:X8}; expected 0x{1:X8}.",
                        version,
                        Version));
            }
        }

        private static uint ReadUInt32LE(byte[] data, int offset)
        {
            return
                (uint)data[offset] |
                ((uint)data[offset + 1] << 8) |
                ((uint)data[offset + 2] << 16) |
                ((uint)data[offset + 3] << 24);
        }

        private static void WriteUInt32LE(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }
    }

    public sealed class GG_1_ValidationReport
    {
        public uint A { get; internal set; }
        public uint B { get; internal set; }
        public uint C { get; internal set; }
        public uint D { get; internal set; }

        public uint CalculatedB { get; internal set; }
        public uint CalculatedC { get; internal set; }
        public uint CalculatedD { get; internal set; }

        public bool BuildTokenValid { get; internal set; }
        public bool MainCrcValid { get; internal set; }
        public bool FilenameHashValid { get; internal set; }
        public bool BlockBCrcValid { get; internal set; }

        public bool IsValid
        {
            get
            {
                return
                    BuildTokenValid &&
                    MainCrcValid &&
                    FilenameHashValid &&
                    BlockBCrcValid;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "A:       0x{0:X8}\n" +
                "B:       0x{1:X8}\n" +
                "C:       0x{2:X8}\n" +
                "D:       0x{3:X8}\n\n" +
                "calc B:  0x{4:X8}\n" +
                "calc C:  0x{5:X8}\n" +
                "calc D:  0x{6:X8}\n\n" +
                "build token:   {7}\n" +
                "main CRC:      {8}\n" +
                "filename hash: {9}\n" +
                "block-B CRC:   {10}",
                A,
                B,
                C,
                D,
                CalculatedB,
                CalculatedC,
                CalculatedD,
                BuildTokenValid ? "OK" : "FAIL",
                MainCrcValid ? "OK" : "FAIL",
                FilenameHashValid ? "OK" : "FAIL",
                BlockBCrcValid ? "OK" : "FAIL");
        }
    }
}
