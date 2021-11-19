using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Communication;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace FileTranMain
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread serverThread = null;
            TcpServer server = null;
            string line;
            Console.WriteLine("Enter 'start' to start, press CTRL+Z or enter 'quit' to exit:");
            Console.WriteLine();
            do
            {
                line = Console.ReadLine();
                if (line != null)
                {
                    if (line.Equals("start"))
                    {
                        Console.WriteLine("I'm running...");
                        server = new TcpServer();
                        //server.StartListen(60010, @"D:\2");
                    }
                    else if (line.Equals("quit"))
                    {
                        if (server != null)
                        {
                            server.StopListen();
                            serverThread.Abort();
                            serverThread.Join();
                        }
                        break;
                    }
                    else if (line.Equals("send"))
                    {
                        FileSend fs = new FileSend();
                        //fs.Start(@"h:\fr345.ip", "192.168.0.180", 60010);
                        //if (fs.Start(@"e:\tu.rar", "", "192.168.0.180", 60010) > 0)
                        {
                            Console.WriteLine("Sent ok!");
                        }
                    }
                    else if (line.Equals("sendall"))
                    {
                        DirectoryInfo di = new DirectoryInfo(@"D:\2\vn\84987e");
                        FileInfo[] fiarr = di.GetFiles();
                        FileSend fs = new FileSend();
                        foreach (FileInfo fi in fiarr)
                        {
                            //fs.Start(fi.FullName, "", "192.168.0.180", 60010);
                        }
                    }
                }
            } while (line != null);
        }
    }
}
