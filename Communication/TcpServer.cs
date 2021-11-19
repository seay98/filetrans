using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Communication
{
    public class TcpServer
    {
        private TcpListener server = null;

        public void StartListen(int port, String recDir, Boolean recSme)
        {
            this.server = null;

            try
            {
                //Set the TcpListener on port listenPort.
                server = new TcpListener(IPAddress.Any, port);

                //Start listening for client request.
                server.Start();

                while (true)
                {
                    //等待客户端连接
                    TcpClient client = server.AcceptTcpClient();

                    //用线程进行异步处理
                    RecParam rp = new RecParam(client);
                    rp.RecDir = recDir;
                    rp.RecSme = recSme;
                    //加入线程池队列
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ServeClient), rp);
                }

            }
            catch (SocketException e)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();
                Log4y.WriteLog2File(Environment.CurrentDirectory, "tcplisten", logContent);
            }
            finally
            {
                this.server.Stop();
            }
        }

        public void StopListen()
        {
            this.server.Stop();
        }

        static private void ServeClient(object o)
        {
            RecParam rp = (RecParam)o;
            TcpClient client = rp.client;
            try
            {
                NetworkStream stream = client.GetStream();

                //接收处理
                ITcpReceiveDataProc fr = (ITcpReceiveDataProc)new FileReceive();
                fr.ReceiveData(stream, o);

                stream.Close();

            }
            catch (SocketException e)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();
                Log4y.WriteLog2File(Environment.CurrentDirectory, "tcpclients", logContent);
            }
            finally
            {
                client.Close();
            }
        }

    }

}
