using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeaserSixTester
{
    public static class Common
    {
        public const string TerminationString = "<EOF>";

        public static PointF GetCenter(this Rectangle rectIn)
        {
            return new PointF((float)rectIn.X + (float)rectIn.Width / 2, (float)rectIn.Y + (float)rectIn.Height / 2);
        }

        public static T DeepClone<T>(this T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            BinaryFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }


        public static void Raise(this EventHandler handler, object sender, EventArgs args = null)
        {
            EventHandler localHandlerCopy = handler;
            if (args == null)
            {
                args = EventArgs.Empty;
            }
            if (localHandlerCopy != null)
            {
                localHandlerCopy(sender, args);
            }
        }

        public static void InvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
        {
            if (obj.InvokeRequired)
            {
                object[] args = new object[0];
                obj.Invoke(action, args);
            }
            else
            {
                action();
            }
        }
        public static void BeginInvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
        {
            if (obj.InvokeRequired)
            {
                object[] args = new object[0];
                obj.BeginInvoke(action, args);
            }
            else
            {
                action();
            }
        }

        public class CRC32
        {
            uint[] table;
            const int BufferSize = 20 * 1024 * 1024; // 20 MB buffer … can be configureable

            delegate uint dlgSummer(long l, uint ui, byte[] by1);

            public enum CSMethod
            {
                Checksum,
                CRC32
            }


            //Black magick.. copied from the net.
            public CRC32()
            {
                uint poly = 0xedb88320;
                table = new uint[256];
                for (uint i = 0; i < table.Length; ++i)
                {
                    uint temp = i;
                    for (int j = 8; j > 0; --j)
                    {
                        if ((temp & 1) == 1)
                        {
                            temp = (uint)((temp >> 1) ^ poly);
                        }
                        else
                        {
                            temp >>= 1;
                        }
                    }
                    table[i] = temp;
                }
            }

            public byte[] GetCheckSumBytes(FileInfo file, CSMethod eMethod)
            {
                return BitConverter.GetBytes(ComputeCheckSum(file, eMethod));
            }

            public byte[] GetCheckSumBytes(byte[] bytes, CSMethod eMethod)
            {
                return BitConverter.GetBytes(ComputeCheckSum(bytes, eMethod));
            }

            public byte[] GetCheckSumBytes(string str, CSMethod eMethod)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(str);
                return GetCheckSumBytes(bytes, eMethod);
            }


            public uint ComputeCheckSum(FileInfo file, CSMethod eCsMethod)
            {
                uint uiSummerValue = 0;
                bool StopReading = false;

                dlgSummer dlgSummer;
                switch (eCsMethod)
                {
                    case CSMethod.CRC32:
                        dlgSummer = new dlgSummer(SummerCRC32Logic);
                        uiSummerValue = 0xffffffff;
                        break;
                    case CSMethod.Checksum:
                        dlgSummer = new dlgSummer(SummerCSLogic);
                        break;
                    default:
                        dlgSummer = new dlgSummer(SummerCSLogic);
                        break;
                }

                using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    long TotalLength = Math.Min((long)BufferSize, fs.Length);
                    while (!StopReading)
                    {
                        byte[] by1FileChunk = new byte[TotalLength];
                        br.Read(by1FileChunk, 0, (int)TotalLength);

                        uiSummerValue = dlgSummer(TotalLength, uiSummerValue, by1FileChunk);

                        if (br.BaseStream.Position >= br.BaseStream.Length)
                        {
                            StopReading = true;
                        }
                    }
                }

                if (eCsMethod == CSMethod.CRC32)
                {
                    uiSummerValue = ~uiSummerValue;
                }
                return uiSummerValue;
            }

            public uint ComputeCheckSum(byte[] bytes, CSMethod eMethod)
            {
                uint uiSummerValue = 0;
                bool StopReading = false;

                dlgSummer dlgSummer;
                switch (eMethod)
                {
                    case CSMethod.CRC32:
                        dlgSummer = new dlgSummer(SummerCRC32Logic);
                        uiSummerValue = 0xffffffff;
                        break;
                    case CSMethod.Checksum:
                        dlgSummer = new dlgSummer(SummerCSLogic);
                        break;
                    default:
                        dlgSummer = new dlgSummer(SummerCSLogic);
                        break;
                }

                uiSummerValue = dlgSummer(bytes.Length, uiSummerValue, bytes);
                if (eMethod == CSMethod.CRC32)
                {
                    uiSummerValue = ~uiSummerValue;
                }
                return uiSummerValue;


            }

            public uint ComputeCheckSum(string str, CSMethod eMethod)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(str);
                return BitConverter.ToUInt32(GetCheckSumBytes(bytes, eMethod), 0);
            }

            private static uint SummerCSLogic(long TotalLength, uint uiSummer, byte[] by1FileChunk)
            {
                for (long jj = 0; jj < TotalLength; ++jj)
                {
                    uiSummer += by1FileChunk[jj];
                }
                return uiSummer;
            }

            private uint SummerCRC32Logic(long TotalLength, uint crc, byte[] by1FileChunk)
            {
                for (int i = 0; i < TotalLength; ++i)
                {
                    byte index = (byte)(((crc) & 0xff) ^ by1FileChunk[i]);
                    crc = (uint)((crc >> 8) ^ table[index]);
                }
                return crc;
            }

            private uint ComputeCRC32(byte[] bytes)
            {
                uint crc = 0xffffffff;
                crc = SummerCRC32Logic(bytes.LongLength, crc, bytes);
                return ~crc;
            }
        }
    }
  

}
