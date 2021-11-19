using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FileTranWin
{
    public class RecSets : INotifyPropertyChanged
    {
        public Boolean BeRun { get; set; }

        public String RecDir { get; set; }

        public int LstPort { get; set; }

        public Boolean RecSme { get; set; }

        public String Desc { get; set; }

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

        public RecSets(Boolean BeRun, String RecDir, int LstPort, String Desc)
        {
            this.BeRun = BeRun;
            this.RecDir = RecDir;
            this.LstPort = LstPort;
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
