using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Communication
{
    public class FileReceive : ITcpReceiveDataProc
    {
        //接收缓存大小
        static private int MaxBufSize = 1024 * Communication.Properties.Settings.Default.BufSize;

        public int ReceiveData(NetworkStream stream, Object o)
        {
            RecParam rp = (RecParam)o;
            String Dir = rp.RecDir;
            Boolean recSme = rp.RecSme;
            FileStream fs = null;
            try
            {
                //接收缓存
                Byte[] data = new Byte[MaxBufSize];

                //文件信息变量
                int headlen = 0;
                String path = null;
                String receivepath = null;
                String filename = null;
                String tmppath = null;
                String tmpfilename = null;
                String md5hash = null;
                DateTime lm_time = DateTime.Now;
                Int64 filelen = 0;
                Int64 receivedlen = 0;

                //接收处理
                int numOfRead = 0;
                while ((numOfRead = stream.Read(data, 0, MaxBufSize)) > 0)
                {
                    //接收文件信息
                    if (fs == null)
                    {
                        //文件信息帧缓存
                        // | 帧长 | 目的目录:文件名 | md5 | 最后修改时间 | 文件长 |
                        // |  4   |         N       | 16  |      8       |   8    |
                        //文件信息头长度
                        headlen = this.GetHeadLen(data);

                        //文件名及临时文件名
                        int filenamelen = headlen - 32;
                        receivepath = this.GetReceivePath(data, filenamelen);
                        filename = this.GetFileName(data, filenamelen);
                        path = this.GetFilePath(Dir, receivepath, filename);
                        tmpfilename = filename + @".tmp";
                        tmppath = this.GetFilePath(Dir, receivepath, tmpfilename);

                        //md5值
                        md5hash = this.GetMd5Hash(data, 4 + filenamelen);

                        //文件最后修改日期
                        lm_time = this.GetDateTime(data, 4 + filenamelen + 16);

                        //文件长度
                        filelen = this.GetFileLen(data, 4 + filenamelen + 16 + 8);

                        //响应返回文件已接收长度
                        //如果已传完，检查是否是同一份文件
                        String tmp = path;
                        if (File.Exists(path))
                        {
                            receivedlen = this.GetReceivedLen(path);
                            //检查是否传完
                            //校验md5，不是同一文件给文件名加时间戳，返回0长度，开始传送，
                            //是则返回文件长度，结束传送
                            if (!this.Md5Check(path, md5hash) && recSme)
                            {
                                filename = this.SetNewFilename(filename);
                                path = this.GetFilePath(Dir, receivepath, filename);
                                tmpfilename = filename + @".tmp";
                                tmppath = this.GetFilePath(Dir, receivepath, tmpfilename);
                                receivedlen = 0;
                            }
                            
                        }
                        else//看同名临时文件
                        {
                            receivedlen = this.GetReceivedLen(tmppath);
                            if (receivedlen == filelen)
                            {
                                if (this.Md5Check(tmppath, md5hash))
                                {
                                    tmp = tmppath;
                                }
                                else
                                {
                                    //不保留错误临时文件，删除，重新传输
                                    File.Delete(tmppath);
                                    receivedlen = 0;
                                }
                            }
                        }

                        //返回文件长度，如果长度等于文件长度，则不会发数据回来，下面直接做结束处理
                        stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(receivedlen)), 0, 8);

                        //对已完成文件作处理
                        if (receivedlen == filelen)
                        {
                            String status = null;
                            //恢复文件属性
                            this.RecoverFileAttr(path, tmp, lm_time);
                            status = "ok";
                            Byte[] statusbytes = Encoding.ASCII.GetBytes(status);
                            stream.Write(statusbytes, 0, statusbytes.Length);
                            return 1;
                        }
                        //删除无用tmp文件
                        else if (receivedlen > filelen)
                        {
                            File.Delete(tmppath);
                            return 1;
                        }
                        //打开文件准备写入
                        fs = new FileStream(tmppath, FileMode.Append, FileAccess.Write, FileShare.None);
                    }
                    else//接收文件
                    {
                        //this.SaveFile(data, 0, numOfRead, tmppath);
                        //写入已打开文件
                        fs.Write(data, 0, numOfRead);
                        receivedlen += numOfRead;

                        //检查是否传完
                        if (receivedlen == filelen)
                        {
                            fs.Close();
                            String status = null;
                            //校验md5
                            if (this.Md5Check(tmppath, md5hash))
                            {
                                //恢复文件属性
                                this.RecoverFileAttr(path, tmppath, lm_time);
                                status = "ok";
                            }
                            else
                            {
                                //打上错误后缀
                                File.Move(tmppath, path + ".err");
                                status = "err";
                            }
                            Byte[] statusbytes = Encoding.ASCII.GetBytes(status);
                            stream.Write(statusbytes, 0, statusbytes.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();

                Log4y.WriteLog2File(Environment.CurrentDirectory, "filereceive", logContent);

                return -1;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            return 1;
        }

        //解析信息头，获取信息头总长度，不包括长度字段本身的4字节
        private int GetHeadLen(Byte[] data)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 0));
        }

        //解析信息头，获取发送文件的长度
        private Int64 GetFileLen(Byte[] data, int offset)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(data, offset));
        }

        //解析信息头，获取文件接收目的目录
        private String GetReceivePath(Byte[] data, int length)
        {
            return Encoding.Unicode.GetString(data, 4, length).Split(new Char[] { ':' })[0];
        }

        //解析信息头，获取发送文件的文件名
        private String GetFileName(Byte[] data, int length)
        {
            //String[] split = Encoding.Unicode.GetString(data, 4, length).Split(new Char[] { ':' });
            return Encoding.Unicode.GetString(data, 4, length).Split(new Char[]{':'})[1];
        }

        //解析信息头，获取发送文件的目的目录，并与接收目录一起构成完整报文路径
        private String GetFilePath(String dir, String receivepath, String filename)
        {
            if (!dir.Substring(dir.Length - 1, 1).Equals("\\"))
            {
                dir += "\\";
            }
            String path = dir + receivepath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!path.Substring(path.Length - 1, 1).Equals("\\"))
            {
                path += "\\";
            }

            return path + filename;
        }

        //解析信息头，获取发送文件的MD5值
        private String GetMd5Hash(Byte[] data, int offset)
        {
            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < 16; i++)
            {
                sBuilder.Append(data[offset + i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        //解析信息头，获取时间字节，并转换返回为DateTime类型
        private DateTime GetDateTime(Byte[] data, int offset)
        {
            Int64 ticks = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(data, offset));

            return new DateTime(ticks);
        }

        //获取path指向的文件的文件长度
        private Int64 GetReceivedLen(String path)
        {
            FileInfo fi = new FileInfo(path);

            if (!fi.Exists)
            {
                return 0;
            }

            return fi.Length;
        }

        //将数据写入tmppath文件
        private void SaveFile(Byte[] data, int offset, int length, String tmppath)
        {
            using (FileStream fs = new FileStream(tmppath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                fs.Write(data, offset, length);
                fs.Close();
            }
        }

        //恢复tmppath文件的属性，包括恢复最后修改时间和去tmp后缀
        private void RecoverFileAttr(String path, String tmppath, DateTime lm_time)
        {
            FileInfo fi = new FileInfo(tmppath);
            fi.LastWriteTime = lm_time;

            if (!path.Equals(tmppath))
            {
                File.Move(tmppath, path);
            }
        }

        //对tmppath文件的MD5值与指定的MD5值进行比较
        private bool Md5Check(String tmppath, String md5)
        {
            MD5 md5Hasher = MD5.Create();

            String hash = null;

            using (FileStream fs = new FileStream(tmppath, FileMode.Open))
            {
                byte[] data = md5Hasher.ComputeHash(fs);
                fs.Close();

                StringBuilder sBuilder = new StringBuilder();

                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                hash = sBuilder.ToString();
            }

            if (hash.Equals(md5))
            {
                return true;
            }
            return false;
        }

        //从配置文件中获取接收文件的目录
        private String GetReceiveDir()
        {
            String path = Environment.CurrentDirectory;
            if (!path.Substring(path.Length - 1).Equals(@"\"))
            {
                path += (@"\");
            }

            XDocument xdoc = XDocument.Load(path + @"\conf.xml");
            var query = from el in xdoc.Descendants("Receive") select el;
            String rd = null;
            foreach (var el in query)
            {
                rd = el.Element("ReceiveDir").Value;
            }

            if ((rd == null) || (rd.Equals(String.Empty)))
            {
                rd = @"\";
            }

            return rd;
        }

        //重名文件加时间戳
        private String SetNewFilename(String filename)
        {
            int n = filename.LastIndexOf('.');
            String title = null;
            String ext = null;
            if (n > 0)
            {
                title = filename.Substring(0, n);
                ext = filename.Substring(n);
            }
            else
            {
                title = filename;
                ext = String.Empty;
            }

            return title + DateTime.Now.ToString("_yyyyMMddHHmmss") + ext;
        }
    }
}
