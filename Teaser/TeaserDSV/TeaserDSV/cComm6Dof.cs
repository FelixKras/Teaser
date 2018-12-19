using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TeaserDSV
{
    class cComm6Dof
    {
        public EventHandler evSicPositionArrived;
        public cComm6Dof()
        {

        }



    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SixMsg
    {
        internal int Header;
        internal int Sender_ID;
        internal int Target_ID;
        internal int MSGCounter;
        internal int Time;
        internal double Object_X;
        internal double Object_Y;
        internal double Object_Z;
        internal double Object_Roll;
        internal double Object_Pitch;
        internal double Object_Yaw;
        internal int Smoke;
        internal int Object_model;
        internal int Spare;
        internal int CheckSum;

        internal byte[] MakeByteArray()
        {
            byte[] by1Result = new byte[Marshal.SizeOf(typeof(SixMsg))];
            GCHandle ptrStruct = GCHandle.Alloc(this, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(ptrStruct.AddrOfPinnedObject(), by1Result, 0, by1Result.Length);
            }
            catch (Exception e)
            {

            }
            finally
            {
                ptrStruct.Free();
            }


            return by1Result;
        }
        public override string ToString()
        {
            string sResult = string.Format(
                "Header: {0},Sender ID: {1},Target ID: {2},MSG Counter: {3},Time: {4}, Object X: {5},Object Y: {6},Object Z: {7}," +
                "Object Roll: {8},Object Pitch: {9},Object Yaw: {10},Smoke: {11},Object Model: {12},spare: {13},CheckSum: {14}",
                Header, Sender_ID, Target_ID, MSGCounter, Time, Object_X, Object_Y, Object_Z, Object_Roll, Object_Pitch,
                Object_Yaw, Smoke,
                Object_model, Spare, CheckSum);
            return sResult;
        }

        public void FillFromArray(byte[] by1Arr)
        {
            unsafe
            {
                fixed (byte* msg = &by1Arr[0])
                {
                    this = *(SixMsg*)msg;
                }
            }
        }
    }




    
}
