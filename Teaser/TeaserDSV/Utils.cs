using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using SkiaSharp;
using TeaserDSV.Model;

namespace TeaserDSV
{
    static class Utils
    {
        static byte[] byTemp_ConvertToBitmap;

        public static Image ConvertToBitmap(this SKBitmap skbitmap, Bitmap Orig)
        {
            if (byTemp_ConvertToBitmap == null)
            {
                byTemp_ConvertToBitmap = new byte[skbitmap.ByteCount];
            }

            BitmapData bmpData = Orig.LockBits(new Rectangle(0, 0, Orig.Width, Orig.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

            Marshal.Copy(skbitmap.GetPixels(), byTemp_ConvertToBitmap, 0, byTemp_ConvertToBitmap.Length);
            Marshal.Copy(byTemp_ConvertToBitmap, 0, bmpData.Scan0, byTemp_ConvertToBitmap.Length);

            Orig.UnlockBits(bmpData);
            return Orig;
        }
        

        public static Image ConvertToBitmap(this SKBitmap skbitmap)
        {
            if (byTemp_ConvertToBitmap == null)
            {
                byTemp_ConvertToBitmap = new byte[skbitmap.ByteCount];
            }


            Bitmap bmpTemp = new Bitmap(skbitmap.Width, skbitmap.Height, PixelFormat.Format32bppRgb);

            //Bitmap result = new Bitmap(skbitmap.Width, skbitmap.Height, PixelFormat.Format32bppRgb);
            BitmapData bmpData = bmpTemp.LockBits(new Rectangle(0, 0, bmpTemp.Width, bmpTemp.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

            Marshal.Copy(skbitmap.GetPixels(), byTemp_ConvertToBitmap, 0, byTemp_ConvertToBitmap.Length);
            Marshal.Copy(byTemp_ConvertToBitmap, 0, bmpData.Scan0, byTemp_ConvertToBitmap.Length);
            bmpTemp.UnlockBits(bmpData);
            return bmpTemp;
        }
        public static SKBitmap ConvertToSKBitmap(this Bitmap bitmap)
        {
            SKBitmap result;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            SKImageInfo skinfo = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888);
            SKPixmap skmap = new SKPixmap(skinfo, bmpData.Scan0);
            result = new SKBitmap();
            result.InstallPixels(skmap);
            bitmap.UnlockBits(bmpData);
            return result;
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

        public static T DeepClone<T>(this T source, BinaryFormatter formatter, Stream stream)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
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

        public static PointF[] ToFloatPointArray(this BodyPoint[] bodyPoints)
        {
            int bodyPointsLength = bodyPoints.Length;
            PointF[] resultPoints = new PointF[bodyPointsLength];
            for (int i = 0; i < bodyPointsLength; i++)
            {
                resultPoints[i] = new PointF((float)bodyPoints[i].Position[0], (float)bodyPoints[i].Position[1]);
            }

            return resultPoints;
        }
        public static double Norm(this double[] vector)
        {
            double norm = 0;
            double sumOfSquares = 0;
            foreach (var item in vector)
            {
                sumOfSquares += item * item;
            }

            norm = Math.Sqrt(sumOfSquares);
            return norm;
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

        public static unsafe SixMsg SixMsgFromByteArr(this byte[] by1Arr)
        {
            unsafe
            {
                fixed (byte* msg = &by1Arr[0])
                {
                    return *(SixMsg*)msg;
                }
            }

        }

        /// <summary>
        /// Time a method in milliseconds
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public static double TimeThis(this Action act)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            act();
            sw.Stop();
            return sw.ElapsedTicks / (double)Stopwatch.Frequency * 1000;
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
