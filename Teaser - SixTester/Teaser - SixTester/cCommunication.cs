using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TeaserSixTester
{
    internal class cCommunication
    {
        private UdpClient udpClient;
        private bool bConnectStop;
        private Socket udpSock;
        private IPEndPoint ipeRemoteSend;

        public EventHandler evCommandReceived;
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

        public cCommunication(IPEndPoint ipe)
        {
            ipeRemoteSend = ipe;

        }

        public cCommunication()
        {
            ipeRemoteSend = new IPEndPoint(IPAddress.Parse(SettingsHolder.Instance.ipAddress)
                , int.Parse(SettingsHolder.Instance.port));
            CreateSender();
        }

        public void StopSending()
        {

        }

        public bool CreateSender()
        {
            // local x.x.x.217:50011 -> remote x.x.x.216:50010
            bool bRes = false;
            try
            {
                if (udpSock == null)
                {
                    udpSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                }
                if (!isConnected())
                {
                    udpSock.Connect(ipeRemoteSend);
                    bRes = true;
                }
            }
            catch (Exception ex)
            {
                //LogWriter.Instance.WriteToLog(ex.ToString());
                Disconnect();
                return false;
            }
            finally
            {

            }
            return bRes;

        }
        public bool UDPSendMessage(byte[] aMessage)
        {
            bool bRes = false;
            try
            {
                if (udpSock != null)
                {
                    udpSock.Send(aMessage);
                    bRes = true;
                }
                else
                {
                    bRes = false;
                }

            }
            catch (Exception ex)
            {
                //LogWriter.Instance.WriteToLog(ex.ToString());
                if (udpSock != null)
                {
                    udpSock.Close();
                }
                udpSock = null;
                bRes = false;
            }
            finally
            {

            }
            return bRes;
        }

        private void ReadCallback(IAsyncResult ar)
        {
            string content = String.Empty;

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
                throw ex;
            }
            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                content = state.sb.ToString();

                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the   
                    // client. Display it on the console.  


                    // Echo the data back to the client.  
                    // Send(handler, content);

                    state.sb.Clear();

                    evCommandReceived(content, EventArgs.Empty);

                }

                if (isConnected())
                {
                    try
                    {
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback), state);
                    }
                    catch (ObjectDisposedException e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                }

            }
        }
        private StringBuilder recvContent = new StringBuilder();
        public bool UDPReceiveMessage(ref string[] Recvdmessages)
        {
            int bytesRead;
            string sAllRecvdMessage = String.Empty;

            recvContent = new StringBuilder();
            try
            {
                while (udpSock.Available > 0)
                {
                    byte[] by1Recvd = new byte[udpSock.Available];
                    bytesRead = udpSock.Receive(by1Recvd);
                    if (bytesRead > 0)
                    {
                        // There  might be more data, so store the data received so far.  
                        recvContent.Append(Encoding.ASCII.GetString(by1Recvd, 0, bytesRead));

                        // Check for end-of-file tag. If it is not there, read   
                        // more data.  
                        sAllRecvdMessage = recvContent.ToString();
                        Recvdmessages = sAllRecvdMessage.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries);
                        //int iEndOfFilePos = sRecvmessage.IndexOf("<EOF>", StringComparison.Ordinal);
                        if (Recvdmessages.Length > 0)
                        {
                            recvContent.Clear();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                //LogWriter.Instance.WriteToLog(ex.ToString());

                /*
                 The virtual circuit was reset by the remote side executing a hard or abortive close.
                 The application should close the socket; it is no longer usable.
                 On a UDP-datagram socket this error indicates a previous send operation resulted in an ICMP Port Unreachable message.
                 */
                const int SIO_UDP_CONNRESET = -1744830452;
                udpSock.IOControl( (IOControlCode)SIO_UDP_CONNRESET,new byte[] { 0, 0, 0, 0 },null);
                return false;
            }
            finally
            {

            }
        }

        public void Disconnect()
        {
            if (udpSock != null && isConnected())
            {
                udpSock.Shutdown(SocketShutdown.Both);
            }
            else if (udpSock != null)
            {
                udpSock.Close();
                udpSock = null;
            }

        }

        public bool isConnected()
        {
            bool bRes = false;
            try
            {
                int iSent = udpSock.Send(new byte[1] { 0 }, 0, 0);
                bRes = true;
            }
            catch (Exception e)
            {
                bRes = false;
            }
            return bRes;
        }

        public void UpdateIPE(object sender, EventArgs e)
        {
            Tuple<string, string> tupIpPort = sender as Tuple<string, string>;
            ipeRemoteSend = new IPEndPoint(IPAddress.Parse(tupIpPort.Item1), int.Parse(tupIpPort.Item2));
            Disconnect();
            CreateSender();
        }
    }
}