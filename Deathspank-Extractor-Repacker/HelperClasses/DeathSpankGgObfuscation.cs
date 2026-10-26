using System;
using System.IO;
using System.Text;
using System.Windows.Documents;

namespace Deathspank_Extractor_Repacker
{
    public static class DeathSpankGgObfuscation
    {
        public sealed class Tables
        {
            // Plain byte -> archive/obfuscated byte.
            public byte[] Encode { get; }


            // Archive/obfuscated byte -> plain byte.
            public byte[] Decode { get; }

            public uint Seed { get; }
            public int ShufflePasses { get; }
            public string Key { get; }

            internal Tables(
                byte[] encode,
                byte[] decode,
                uint seed,
                int shufflePasses,
                string key)
            {
                Encode = encode;
                Decode = decode;
                Seed = seed;
                ShufflePasses = shufflePasses;
                Key = key;
            }
        }

        public static Tables BuildTables(string archivePath)
        {
            //
            // 0x5186A0:
            //
            // Equivalent to:
            //   _splitpath(path, NULL, NULL, filename, NULL);
            //   if ((p = strchr(filename, '-')) != NULL)
            //       *p = '\0';
            //
            string key = Path.GetFileNameWithoutExtension(archivePath);

            int dash = key.IndexOf('-');
            if (dash >= 0)
                key = key.Substring(0, dash);

            // 0x528190 calls _strlwr() when invoked by the
            // archive-table builder.
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);

            for (int i = 0; i < keyBytes.Length; i++)
            {
                if (keyBytes[i] >= (byte)'A' &&
                    keyBytes[i] <= (byte)'Z')
                {
                    keyBytes[i] += 0x20;
                }
            }

            string normalizedKey = Encoding.ASCII.GetString(keyBytes);

            //
            // Generate seed.
            //
            uint accumulator = 0;

            for (int i = 0; i < keyBytes.Length; i++)
            {
                uint value = keyBytes[i];

                accumulator = unchecked(
                    accumulator +
                    value +
                    (value << (i & 0x0F))
                );
            }

            int signedSeed = unchecked((int)accumulator);

            if (signedSeed < 0)
                signedSeed = unchecked(-signedSeed);

            uint seed = unchecked((uint)signedSeed);

            //
            // A zero seed follows a special runtime/global-RNG path in
            // the original executable. Normal game archive names do not
            // hit this case.
            //
            if (seed == 0)
                throw new InvalidOperationException(
                    "Archive key produced a zero seed; " +
                    "the native game uses its global RNG state in this case.");

            uint state = seed;

            byte[] encode = new byte[256];

            for (int i = 0; i < 256; i++)
                encode[i] = (byte)i;

            //
            // Native LCG:
            //
            // state = (state * 0x6255 + 0x3619) & 0xffff
            //
            state = Next(ref state);

            int passes =
                (int)(((state * 10u) + 0x8000u) >> 16)
                + 10;

            for (int pass = 0; pass < passes; pass++)
            {
                for (int i = 0; i < 256; i++)
                {
                    state = Next(ref state);

                    int j = (int)(
                        ((state * 0xFFu) + 0x8000u) >> 16
                    );

                    byte temp = encode[i];
                    encode[i] = encode[j];
                    encode[j] = temp;
                }
            }

            //
            // Native code creates the inverse permutation:
            //
            // decode[encode[i]] = i;
            //
            byte[] decode = new byte[256];

            for (int i = 0; i < 256; i++)
                decode[encode[i]] = (byte)i;

            return new Tables(
                encode,
                decode,
                seed,
                passes,
                normalizedKey
            );
        }

        private static uint Next(ref uint state)
        {
            state = unchecked(
                (state * 0x6255u + 0x3619u) & 0xFFFFu
            );

            return state;
        }

        public static void DeobfuscateInPlace(
            byte[] data,
            byte[] decodeTable)
        {
            if (decodeTable.Length != 256)
                throw new ArgumentException(
                    "Decode table must contain exactly 256 bytes.",
                    nameof(decodeTable));

            // The game itself goes backwards.
            for (int i = data.Length - 1; i >= 0; i--)
                data[i] = decodeTable[data[i]];

        }

        public static void ObfuscateInPlace(
            byte[] data,
            byte[] encodeTable)
        {
            if (encodeTable.Length != 256)
                throw new ArgumentException(
                    "Encode table must contain exactly 256 bytes.",
                    nameof(encodeTable));

            for (int i = 0; i < data.Length; i++)
                data[i] = encodeTable[data[i]];
        }
    }
}
