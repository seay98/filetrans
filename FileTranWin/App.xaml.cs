using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace FileTranWin
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static StoreSets storeSets = new StoreSets();
        public static StoreSets StoreSets
        {
            get { return storeSets; }
        }

        //private List<MySets> mySets;
        //public List<MySets> MySets
        //{
        //    get { return mySets; }
        //    set { this.mySets = value; }
        //}

        //private List<RecSets> recSets;
        //public List<RecSets> RecSets
        //{
        //    get { return recSets; }
        //    set { this.recSets = value; }
        //}
    }
}
