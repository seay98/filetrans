using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Communication
{
    public class TcpConnect
    {
        private TcpClient client = null;

        private NetworkStream stream = null;

        //缓存大小
        static private int MaxBufSize = 1024 * Communication.Properties.Settings.Default.BufSize;

        //返回值 1：连接成功， 0：连接已经建立， -1：连接失败
        public int Connect(String server, int port)
        {
            int status = 1;

            if (this.client != null)
            {
                return 0;
            }

            try
            {
                this.client = new TcpClient();
                this.client.Connect(IPAddress.Parse(server), port);

                this.stream = this.client.GetStream();
                //设置读取操作阻止等待数据的时间量
                this.stream.ReadTimeout = 5000;
            }
            catch (SocketException e)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();
                Log4y.WriteLog2File(Environment.CurrentDirectory, "tcpconnect", logContent);
                status = -1;
            }

            return status;
        }

        //读取成功返回1，失败返回-1
        public int Read(ref MemoryStream data)
        {
            try
            {
                //判断stream是否可读
                if (this.stream.CanRead)
                {
                    //接收缓存
                    byte[] bytes = new byte[MaxBufSize];

                    //实际接收大小
                    int numberOfBytesRead = 0;

                    //接收缓存有可能小了，需要循环就收
                    do
                    {
                        numberOfBytesRead = this.stream.Read(bytes, 0, bytes.Length);

                        //处理接收数据
                        data.Write(bytes, 0, numberOfBytesRead);

                    } while (this.stream.DataAvailable);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();

                Log4y.WriteLog2File(Environment.CurrentDirectory, "tcpconnect", logContent);

                return -1;
            }
            return 1;
        }

        //写成功返回1，失败返回-1
        public int Write(Byte[] bytes, int length)
        {
            if (this.stream == null)
            {
                return -1;
            }
            try
            {
                this.stream.Write(bytes, 0, length);
            }
            catch (Exception e)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();

                Log4y.WriteLog2File(Environment.CurrentDirectory, "tcpconnect", logContent);

                return -1;
            }

            return 1;
        }

        public void CloseServe()
        {
            if (this.stream != null)
            {
                this.stream.Close();
            }
            if (this.client != null)
            {
                this.client.Close();
            }
        }
    }
}
