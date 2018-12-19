using System;
using System.Collections.Generic;

using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;




namespace TeaserSixTester
{
    internal class cControl
    {

        //ScriptSettings oScriptSettings;
        cCommunication oComm;
        List<SixMsg> lstSixMsgs = new List<SixMsg>();
        public EventHandler evReceivedResponse;
        public EventHandler evSentMessage;


        public cControl()
        {
            oComm = new cCommunication();
        }

        public void SetIPSettings()
        {

            frmSettings frm = new frmSettings();

            frm.evClosePressed += new EventHandler((sender, args) =>
            {
                oComm.UpdateIPE(new Tuple<string, string>(SettingsHolder.Instance.ipAddress, SettingsHolder.Instance.port), EventArgs.Empty);
                frm.Close();
            });
            frm.Show();

        }


        internal bool SendMessage(string msg)
        {
            bool bRes = false;
            try
            {
                bRes = oComm.UDPSendMessage(Encoding.ASCII.GetBytes(msg));
            }
            catch (Exception e)
            {
                bRes = false;
                Console.WriteLine(e);
                throw;
            }
            return bRes;
        }

        public bool ParseScriptFile(string file)
        {
            lstSixMsgs.Clear();
            bool bRes = false;
            try
            {
                bRes = new FileInfo(file).Exists && Parse(file);
            }
            catch (Exception e)
            {
                bRes = false;
            }
            return bRes;
        }

        private bool Parse(string file)
        {

            bool bRes = false;
            try
            {
                Regex rgx = new Regex(@"^[^#].*", RegexOptions.Multiline);
                MatchCollection matches;
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sw = new StreamReader(fs))
                {
                    string sAll = sw.ReadToEnd();
                    //oScriptSettings.Checksum = sAll.ComputeCRC();
                    matches = rgx.Matches(sAll);
                }
                foreach (Match match in matches)
                {
                    string[] parts = match.Value.Split(',');
                    if (parts.Length == 15)
                    {
                        var msg = new SixMsg();
                        msg.Header = int.Parse(parts[0], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                        msg.Sender_ID = int.Parse(parts[1]);
                        msg.Target_ID = int.Parse(parts[2]);
                        msg.MSGCounter = int.Parse(parts[3]);
                        msg.Time = int.Parse(parts[4]);
                        msg.Object_X = double.Parse(parts[5]);
                        msg.Object_Y = double.Parse(parts[6]);
                        msg.Object_Z = double.Parse(parts[7]);
                        msg.Object_Roll = double.Parse(parts[8]);
                        msg.Object_Pitch = double.Parse(parts[9]);
                        msg.Object_Yaw = double.Parse(parts[10]);
                        msg.Smoke = int.Parse(parts[11]);
                        msg.Object_model = int.Parse(parts[12]);
                        msg.Spare = int.Parse(parts[13]);
                        msg.CheckSum = int.Parse(parts[14], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                        lstSixMsgs.Add(
                            msg);
                    }
                }
                bRes = true;

            }
            catch (Exception e)
            {
                bRes = false;
            }
            return bRes;
        }

        internal bool TryConnect()
        {
            return oComm.CreateSender();
        }

        public bool CheckConnection(bool shouldReconnect)
        {
            bool bRes = oComm.isConnected();
            //bool bRes = oComm.TCPSendMessage(Encoding.ASCII.GetBytes(Common.TerminationString));


            if (!bRes && shouldReconnect)
            {
                TryConnect();
                bRes = oComm.isConnected();
                //bRes = oComm.TCPSendMessage(Encoding.ASCII.GetBytes(Common.TerminationString));
            }
            if (bRes)
            {
                CheckIncomingMessages();
            }
            return bRes;
        }
        private void CheckIncomingMessages()
        {
            string[] s1RecvdMessages = null;
            oComm.UDPReceiveMessage(ref s1RecvdMessages);
            if (s1RecvdMessages != null)
            {
                foreach (string str in s1RecvdMessages)
                {
                    evReceivedResponse.Raise(str);
                }
            }
        }




        Task tskMessageSend;

        AutoResetEvent areMessageSend = new AutoResetEvent(false);

        private bool bStopSending = false;
        private bool bKillSwitch;


        private void SendSingleMessage(SixMsg oSixMsg)
        {
            //byte[] by1SixMsg_ = lstSixMsg.MakeByteArrayMarshal();
            byte[] by1SixMsg = oSixMsg.MakeByteArray();
            
            Buffer.BlockCopy(new byte[4], 0, by1SixMsg, by1SixMsg.Length - sizeof(int), sizeof(int));
            Common.CRC32 crc = new Common.CRC32();
            uint uiCheckSum = crc.ComputeCheckSum(by1SixMsg, Common.CRC32.CSMethod.Checksum);
            Buffer.BlockCopy(BitConverter.GetBytes(uiCheckSum), 0, by1SixMsg, by1SixMsg.Length - sizeof(int), sizeof(int));

            oComm.UDPSendMessage(by1SixMsg);
        }


        public bool StartSendingMessages()
        {
            bStopSending = false;
            areMessageSend.Set();

            return false;
        }
        public bool StopSendingMessages()
        {
            bStopSending = true;

            return false;
        }

        public void StartMessageTask()
        {

            tskMessageSend = new Task(
                () =>
                {
                    while (!bKillSwitch)
                    {
                        areMessageSend.WaitOne();
                        for (int i = 0; i < lstSixMsgs.Count; i++)
                        {
                            areMessageSend.WaitOne(TimeSpan.FromMilliseconds(SettingsHolder.Instance.SendFreq));
                            SendSingleMessage(lstSixMsgs[i]);
                            evSentMessage.Raise(lstSixMsgs[i].ToString());

                            if (bStopSending)
                            {
                                break;
                            }
                        }
                    }


                });

            tskMessageSend.Start();
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
       

        internal byte[] MakeByteArrayMarshal()
        {
            int msgSize = Marshal.SizeOf(typeof(SixMsg));
            byte[] by1Result = new byte[msgSize];
          
            

            try
            {
                IntPtr ptrStruct_ = Marshal.AllocHGlobal(msgSize);
                Marshal.StructureToPtr(this, ptrStruct_, true);
                Marshal.Copy(ptrStruct_, by1Result, 0, msgSize);
            }
            catch (Exception e)
            {

            }
            finally
            {

            }
            return by1Result;
        }

        internal byte[] MakeByteArray()
        {
            int msgSize = Marshal.SizeOf(typeof(SixMsg));
            byte[] by1Result = new byte[msgSize];

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
    }

}
