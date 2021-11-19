using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.IO;
using Communication;

namespace FileTranWin
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        static private List<MySets> mySets;

        static private List<RecSets> recSets;

        static private Boolean run;

        static private Boolean serverun;

        private Thread mainProc;

        public MainWindow()
        {
            InitializeComponent();

            run = false;
            serverun = false;

            mySets = App.StoreSets.GetMySets();
            this.gridSets.ItemsSource = mySets;

            recSets = App.StoreSets.GetRecSets();
            this.gridReceiveSets.ItemsSource = recSets;

            //开始文件接收监听
            serverun = true;
            this.ButtonStartReceive.Content = "停止文件接收服务";
            this.StartServer();
        }

        private void ButtonSaveSets_Click(object sender, RoutedEventArgs e)
        {
            App.StoreSets.SaveSendCfg(mySets);
        }

        private void buttonSelPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int index = gridSets.SelectedIndex;
                mySets[index].Source = folder.SelectedPath;
            }
        }

        private void buttonSelDestPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int index = gridSets.SelectedIndex;
                mySets[index].Dest = folder.SelectedPath;
            }
        }

        private void buttonSelBackPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int index = gridSets.SelectedIndex;
                mySets[index].BackPath = folder.SelectedPath;
            }
        }

        #region 开始发送文件及相关函数
        //开始按钮
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (run)
            {
                run = false;
                this.ButtonStartSend.IsEnabled = false;

                mainProc.Join();

                this.ButtonStartSend.IsEnabled = true;
                this.ButtonStartSend.Content = "开始发送";
                this.gridSets.IsReadOnly = false;
            }
            else
            {
                App.StoreSets.SaveSendCfg(mySets);
                mainProc = new Thread(new ThreadStart(this.StartProc));
                run = true;
                mainProc.Start();

                this.ButtonStartSend.Content = "停止发送";
                this.gridSets.IsReadOnly = true;
            }
        }

        //将发送任务加入线程池
        private void StartProc()
        {
            int setId = 0;
            foreach (MySets set in mySets)
            {
                //参数简单检查
                if (this.CheckParam(set) && set.BeRun)
                {
                    //加入线程队列进行处理
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), setId);
                }
                setId++;
            }
        }

        //简单参数校验
        private Boolean CheckParam(MySets set)
        {
            if (set.Source == null)
            {
                return false;
            }
            if (set.BeBack && (set.BackPath == null))
            {
                return false;
            }
            if (set.Server == null)
            {
                return false;
            }

            return true;
        }

        //线程回调函数
        static private void ThreadProc(Object o)
        {
            //参数接收和初始化
            int setId = (int)o;

            mySets[setId].Status = "running";

            MySets set = mySets[setId];
            //Boolean beRun = set.BeRun;
            String sendPath = set.Source;
            String lineName = set.LineName;
            Boolean allDirectories = set.AllDirectories;
            String server = set.Server;
            int port = set.Port;
            int sndBlks = set.SendBlocks;
            int sndSlp = set.SendSleep;
            String dest = set.Dest;
            String fileType = set.FileType;
            String desc = set.Desc;
            Boolean beBack = set.BeBack;
            String backPath = null;
            if (beBack)
            {
                backPath = set.BackPath;
            }

            if (!sendPath.Substring(sendPath.Length - 1).Equals(@"\"))
            {
                sendPath += (@"\");
            }
            if ((backPath !=null) && !backPath.Substring(backPath.Length - 1).Equals(@"\"))
            {
                backPath += (@"\");
            }
            if (port == 0)
            {
                port = 60010;
            }
            if ((dest == null) || (dest.Equals(@"\")))
            {
                dest = String.Empty;
            }

            //遍历源目录
            int sendcnt = 0;
            while (run)
            {
                try
                {
                    List<FileInfo> fiList = GetAllFiles(sendPath, fileType, allDirectories);
                    
                    FileSend fs = new FileSend();
                    foreach (FileInfo fi in fiList)
                    {
                        String fullName = fi.FullName;

                        //检查文件是否可用
                        try
                        {
                            FileStream testFs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                            testFs.Close();
                        }
                        catch (IOException)
                        {
                            continue;
                        }

                        //传输文件
                        mySets[setId].Status = "sending";
                        mySets[setId].Desc = desc + ":[" + DateTime.Now.ToString() + "/" + sendcnt.ToString() + "]" + Environment.NewLine + fi.Name;
                        int status = 0;

                        //目的为本地则进行复制，为远端则发送
                        if (mySets[setId].Server.Equals("localhost") || mySets[setId].Server.Equals("127.0.0.1"))
                        {
                            String localPath = dest;
                            if (!localPath.EndsWith("\\"))
                            {
                                localPath += "\\";
                            }
                            localPath = localPath + fi.Name;
                            try
                            {
                                File.Copy(fullName, localPath, true);
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                                String logContent = st.GetFrame(0).ToString().TrimEnd(new char[] { '\r', '\n' }) + e.ToString();
                                Log4y.WriteLog2File(Environment.CurrentDirectory, "filesend", logContent);

                                status = -1;
                            }
                            status = 1;
                        }
                        else
                        {
                            status = fs.Start(lineName, fullName, dest, server, port, sndBlks, sndSlp);

                            //网络连接失败，（5*次数）秒后重试
                            int trycnt = 0;
                            while (status == -2)
                            {
                                mySets[setId].Status = "connecting";
                                trycnt++;
                                //trycnt为12时归零
                                if (trycnt == 12)
                                {
                                    trycnt = 0;
                                }
                                int sec = 5000 * trycnt;
                                Thread.Sleep(5000);

                                //重试
                                mySets[setId].Status = "sending";
                                status = fs.Start(lineName, fullName, dest, server, port, sndBlks, sndSlp);
                            }

                        }

                        //发送成功后备份
                        if (status > 0)
                        {
                            sendcnt++;
                            //在日志中记录已发送文件
                            Log4y.WriteLog2File(Environment.CurrentDirectory, "filelog", fullName);

                            //是否要备份
                            mySets[setId].Desc = desc + ":[" + DateTime.Now.ToString() + "/" + sendcnt.ToString() + "]";
                            if (beBack)
                            {
                                mySets[setId].Status = "backing";
                                String destPath = backPath + fi.Name;
                                if (File.Exists(destPath))
                                {
                                    destPath = SetNewFilename(destPath);
                                }

                                File.Move(fullName, destPath);
                            }
                            else
                            {
                                File.Delete(fullName);
                            }
                        }
                        else
                        {
                            mySets[setId].Status = "send err";
                        }
                    }
                    fiList.Clear();
                    mySets[setId].Status = "running";
                }
                catch (Exception e)
                {
                    e.ToString();
                }
                finally { }

                Thread.Sleep(1000);
            }
            mySets[setId].Status = "stopped";
        }

        //遍历文件时，根据指定后缀获得文件列表
        static private List<FileInfo> GetAllFiles(String sendPath, String fileType, Boolean allDirectories)
        {
            String[] split = fileType.Split(new Char[] { ',', ' ', ';' });

            DirectoryInfo di = new DirectoryInfo(sendPath);
            if (!di.Exists)
            {
                return null;
            }
            
            List<FileInfo> list = new List<FileInfo>();
            foreach (String item in split)
            {
                FileInfo[] fiArr = null;
                if (allDirectories)
                {
                    fiArr = di.GetFiles(item, SearchOption.AllDirectories);
                }
                else
                {
                    fiArr = di.GetFiles(item, SearchOption.TopDirectoryOnly);
                }
                SortByTime st = new SortByTime();
                Array.Sort(fiArr, st);
                foreach (FileInfo fi in fiArr)
                {
                    String ext = fi.Extension.ToLower();
                    if (ext.Equals(".tmp") || ext.Equals(".mbtemp"))
                    {
                        continue;
                    }
                    list.Add(fi);
                }
            }
            return list;
        }

        //排序类
        public class SortByTime : IComparer<FileInfo>
        {
            public int Compare(FileInfo a, FileInfo b)
            {
                if (a.LastAccessTime >= b.LastAccessTime)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        //重名文件加时间戳
        static private String SetNewFilename(String filename)
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
        #endregion

        #region 文件接收相关函数
        private void ButtonStartReceive_Click(object sender, RoutedEventArgs e)
        {
            if (serverun)
            {
                this.ButtonStartReceive.Content = "开始文件接收服务";
                serverun = false;
            }
            else
            {
                serverun = true;
                this.ButtonStartReceive.Content = "停止文件接收服务";
                this.StartServer();
            }
        }

        private void StartServer()
        {
            int setId = 0;
            foreach (RecSets set in recSets)
            {
                if (CheckRecParam(set) && set.BeRun)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ServeStart), setId);
                }
                setId++;
            }
        }

        private Boolean CheckRecParam(RecSets set)
        {
            if (set.RecDir == null)
            {
                return false;
            }
            if (set.LstPort == 0)
            {
                set.LstPort = 60010;
            }
            return true;
        }

        static private void ServeStart(Object o)
        {
            int id = (int)o;

            RecSets set = recSets[id];
            set.Status = "running";
            TcpServer servier = new TcpServer();
            servier.StartListen(set.LstPort, set.RecDir, set.RecSme);
        }

        #endregion

    }
}
