using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FileTranWin
{
    public class MySets : INotifyPropertyChanged
    {
        private Boolean beRun;
        public Boolean BeRun
        {
            get { return beRun; }
            set
            {
                beRun = value;
                OnPropertyChanged(new PropertyChangedEventArgs("BeRun"));
            }
        }

        public String LineName { get; set; }

        private String source;
        public String Source
        {
            get { return source; }
            set
            {
                source = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Source"));
            }
        }

        public Boolean AllDirectories { get; set; }

        public String Server { get; set; }

        public int Port { get; set; }

        public int SendBlocks { get; set; }

        public int SendSleep { get; set; }

        public String Dest { get; set; }

        public String FileType { get; set; }

        public Boolean BeBack { get; set; }

        private String backPath;
        public String BackPath
        {
            get { return backPath; }
            set
            {
                backPath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("BackPath"));
            }
        }

        private String desc;
        public String Desc
        {
            get { return desc; }
            set
            {
                desc = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Desc"));
            }
        }

        private String status;
        public String Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Status"));
            }
        }

        public MySets(Boolean BeRun, String LineName, String Source, Boolean AllDirectories, String Server, int Port, String Dest, String FileType, Boolean BeBack, String BackPath, String Desc)
        {
            this.BeRun = BeRun;
            this.LineName = LineName;
            this.Source = Source;
            this.AllDirectories = AllDirectories;
            this.Server = Server;
            this.Port = Port;
            this.Dest = Dest;
            this.FileType = FileType;
            this.BeBack = BeBack;
            this.BackPath = BackPath;
            this.Desc = Desc;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }
    }
}
