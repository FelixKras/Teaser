﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeaserDSV
{
    internal class Listener
    {
        // State object for reading client data asynchronously  
        class StateObject
        {
            // Client  socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 1024;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        }



        public EventHandler evCommandReceived;

        // Thread signal.  
        private ManualResetEvent allDone;

        public Listener(string LocalIP, int PortNumber)
        {
            local_ip_receive_ = new IPEndPoint(IPAddress.Parse(LocalIP), PortNumber);
            _StopListening = false;
            IsClosing = false;
            by1ReceivedMessage = new byte[Marshal.SizeOf(new SixMsg())];
        }

        private IPEndPoint local_ip_receive_ { get; set; }

        //private IPEndPoint local_ip_send_ { get; set; }
        //private IPEndPoint ipeRemoteReceive { get; set; }
        //private IPEndPoint remote_ip_send { get; set; }

        private bool _StopListening { get; set; }
        public bool IsClosing { get; set; }
        private Socket listener;
        private Thread thReceive;

        private byte[] by1ReceivedMessage;

        public void StopListening()
        {
            _StopListening = true;
            IsClosing = true;
            try
            {
                listener.Shutdown(SocketShutdown.Both);
                listener.Close();
            }
            catch (ObjectDisposedException ex)
            {

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void StartListening()
        {
            // Create a UDP/IP socket.  

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                listener.Bind(local_ip_receive_);
            }
            catch (Exception e)
            {
                MessageBox.Show("Problem with network" + Environment.NewLine + e.Message, "Warning");
            }

            _StopListening = false;

            if (true)
            {
                StateObject state = new StateObject();
                state.workSocket = listener;
                listener.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            else
            {
                thReceive = new Thread(Receive) { IsBackground = true, Name = "Six udp listener" };
                thReceive.IsBackground = true;
                thReceive.Start();
            }



        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (ObjectDisposedException ex)
            {

            }
            catch (Exception ex)
            {
                LogWriter.Instance.WriteToLog(ex.Message);
            }
            if (bytesRead > 0)
            {
                SixMsg temp = new SixMsg();
                temp.FillFromArray(state.buffer);
                if (temp.Header != 0)
                {
                    evCommandReceived.Raise(temp);
                }

            }
            if (!IsClosing)
            {
                try
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        ReadCallback, state);
                }
                catch (ObjectDisposedException e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
           

        }

        private void Receive()
        {
            int iLenght = 0;
            while (!_StopListening)
            {

                if (listener.Available > 0)
                {
                    iLenght = listener.Receive(by1ReceivedMessage);
                    if (iLenght == by1ReceivedMessage.Length)
                    {
                        SixMsg temp = new SixMsg();
                        temp.FillFromArray(by1ReceivedMessage);
                        evCommandReceived.Raise(temp);
                        //evCommandReceived.Raise(by1ReceivedMessage.SixMsgFromByteArr());
                    }
                }
            }
        }
    }
}
