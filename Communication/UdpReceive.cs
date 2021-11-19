using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Communication
{
    //UDP数据接收处理委托
    public delegate void UdpReceiveDelegate(byte[] data, string threadMsg);

    //UDP采用线程方式接收处理
    public class UdpReceive
    {
        public string ErrMsg { get; set; }

        private UdpClient receiveClient;

        private IAsyncResult asyncResult;

        private UdpReceiveDelegate receiveDelegate;

        private bool keepReceive;

        #region Receive region
        public int StartReceive(int listenPort, UdpReceiveDelegate receiveDelegate)
        {
            this.keepReceive = false;
            this.receiveDelegate = receiveDelegate;
            try
            {
                //启动异步UDP接收
                IPEndPoint e = new IPEndPoint(IPAddress.Any, listenPort);
                this.receiveClient = new UdpClient(e);

                this.receiveClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);

                this.keepReceive = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + ex.ToString();
                Log4y.WriteLog2File(Environment.CurrentDirectory, "udpreceive", logContent);
                return -1;
            }
            return 0;
        }

        public int StopReceive()
        {
            this.keepReceive = false;
            //this.ReceiveProc();
            this.receiveClient.Close();
            return 0;
        }

        private void ReceiveProc()
        {
            IPEndPoint remotePoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] receiveBytes = null;
            string msg = null;
            try
            {
                receiveBytes = this.receiveClient.EndReceive(this.asyncResult, ref remotePoint);
                msg = String.Format("Running in thread {0}, id: {1}. {2} bytes received from {3}", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId, receiveBytes.Length, remotePoint.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + ex.ToString();
                Log4y.WriteLog2File(Environment.CurrentDirectory, "udpreceive", logContent);
            }
            this.receiveDelegate(receiveBytes, msg);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            this.asyncResult = ar;

            this.ReceiveProc();

            if (this.keepReceive)
            {
                this.receiveClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
            }
        }
        #endregion
    }
}
