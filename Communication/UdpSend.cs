using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Communication
{
    public class UdpSend
    {
        public static void SendList(List<byte[]> list, int sendPort, string serverAddr, int serverPort)
        {
            if (list == null)
            {
                return;
            }
            try
            {
                IPEndPoint e = new IPEndPoint(IPAddress.Any, sendPort);
                UdpClient sendClient = new UdpClient(e);

                sendClient.Connect(serverAddr, serverPort);
                foreach (byte[] b in list)
                {
                    sendClient.Send(b, b.Length);
                }
                sendClient.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + ex.ToString();
                Log4y.WriteLog2File(Environment.CurrentDirectory, "udpsend", logContent);
            }
        }
    }
}
