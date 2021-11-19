using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Communication
{
    public class Log4y
    {
        static int logMaxNum = 5;

        static int logMaxSize = 5 * 1024 * 1024;

        public static void WriteLog2File(string exePath, string logName, string content)
        {
            //创建日志保存目录
            string logDir = exePath + @"\log";
            try
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                //循环写日志
                string logPath = logDir + @"\" + logName;
                bool find = false;
                for (int i = 0; i < logMaxNum; i++)
                {
                    string logwPath = logPath + i.ToString() + "w";
                    FileInfo fi = new FileInfo(logwPath);
                    if (fi.Exists)
                    {
                        if (fi.Length >= logMaxSize)
                        {
                            File.Move(logwPath, logPath + i.ToString());
                            int j = i + 1;
                            if (j >= logMaxNum) j = 0;
                            logwPath = logPath + j.ToString() + "w";
                            File.Delete(logPath + j.ToString());
                        }
                        logPath = logwPath;
                        find = true;
                        break;
                    }
                }
                if (!find)
                {
                    File.Delete(logPath + "0");
                    logPath += "0w";
                }
                
                using (StreamWriter sw = new StreamWriter(logPath, true, Encoding.Default))
                {
                    string text = DateTime.Now.ToString() + ": " + content;
                    sw.Write(text);
                    sw.WriteLine();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }
            finally { }
        }
    }
}
