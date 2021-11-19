using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Security.Cryptography;

namespace Communication
{
    public class FileSend
    {
        static private int BufSize = 1024 * 8;

        static private Byte[] ID = { 0xFF, 0xFE, 0xFD, 0xFC };

        //发送成功返回1，发送失败返回-1
        public int Start(String lineName, String path, String destDir, String dstAddr, int dstPort, int sndBlks, int sndSlp)
        {
            TcpConnect tcpConn = new TcpConnect();
            try
            {
                //文件信息帧缓存
                // | 帧长 | 目的目录:文件名:线路名称 | md5 | 最后修改时间 | 文件长 |
                // |  4   |              N           | 16  |      8       |   8    |
                MemoryStream head = new MemoryStream();
                int headlen = 0;

                //构成文件信息帧
                //填充文件信息帧长及文件名
                String filename = destDir + @":" + this.GetFileName(path) + @":" + lineName;
                Byte[] filenamebytes = Encoding.Unicode.GetBytes(filename);

                //帧长不包括帧长字节本身的4字节
                headlen = filenamebytes.Length + 32;
                Byte[] headlenbytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(headlen));
                head.Write(headlenbytes, 0, headlenbytes.Length);

                head.Write(filenamebytes, 0, filenamebytes.Length);

                //填充MD5值
                Byte[] md5hash = this.GetMd5Hash(path);
                head.Write(md5hash, 0, md5hash.Length);

                //填充文件最后修改时间
                Byte[] lmtime = this.GetLmTime(path);
                head.Write(lmtime, 0, lmtime.Length);

                //获得文件长度
                Int64 filelen = this.GetFileLength(path);
                Byte[] filelenbytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(filelen));
                head.Write(filelenbytes, 0, filelenbytes.Length);

                //0长度文件不处理
                if (filelen == 0)
                {
                    head.Close();
                    return -1;
                }

                //连接服务器
                if (tcpConn.Connect(dstAddr, dstPort) < 0)
                {
                    return -2;
                }

                //发送文件信息
                tcpConn.Write(head.GetBuffer(), headlen + 4);

                //关闭流
                head.Close();

                //读取已接收文件长度
                MemoryStream response = new MemoryStream();
                if (tcpConn.Read(ref response) < 0)
                {
                    return -1;
                }
                Int64 sendoffset = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(response.GetBuffer(), 0));
                response.Close();

                //偏移量过大
                if (sendoffset > filelen)
                {
                    return -1;
                }
                //发现同名文件
                if (sendoffset < 0)
                {
                    return -1;
                }

                //发送文件
                Byte[] bytes = new byte[BufSize];
                int cntBlks = 0;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    fs.Seek(sendoffset, SeekOrigin.Begin);
                    int i = 0;
                    while ((i = fs.Read(bytes, 0, BufSize)) > 0)
                    {
                        tcpConn.Write(bytes, i);

                        //发送速度控制
                        cntBlks++;
                        if (cntBlks == sndBlks)
                        {
                            cntBlks = 0;
                            Thread.Sleep(sndSlp);
                        }
                    }
                }

                //读取文件接收情况
                response = new MemoryStream();
                if (tcpConn.Read(ref response) < 0)
                {
                    return -1;
                }
                String status = Encoding.ASCII.GetString(response.GetBuffer(), 0, 2);
                response.Close();

                if (!status.Equals("ok"))
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();
                Log4y.WriteLog2File(Environment.CurrentDirectory, "filesend", logContent);

                return -1;
            }
            finally
            {
                tcpConn.CloseServe();
            }

            return 1;
        }

        private String GetFileName(String path)
        {
            string[] split = path.Split('\\');

            return split[split.Length - 1];
        }

        private Byte[] GetMd5Hash(String path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                MD5 md5 = MD5.Create();
                byte[] hash = md5.ComputeHash(fs);
                fs.Close();
                return hash;
            }
        }

        private Byte[] GetLmTime(String path)
        {
            DateTime dt = new FileInfo(path).LastWriteTime;
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(new FileInfo(path).LastWriteTime.Ticks));
        }

        private Int64 GetFileLength(String path)
        {
            return new FileInfo(path).Length;
        }
    }
}
