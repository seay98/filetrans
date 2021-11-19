using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FileTranWin
{
    public class StoreSets
    {
        private String GetCfgPath()
        {
            String currentDir = Environment.CurrentDirectory;
            if (!currentDir.Substring(currentDir.Length - 1).Equals(@"\"))
            {
                currentDir += (@"\");
            }

            return currentDir + @"\conf.xml";
        }

        public List<MySets> GetMySets()
        {
            List<MySets> mySets = new List<MySets>();

            XDocument xdoc = XDocument.Load(GetCfgPath());

            var query = from el in xdoc.Descendants("Set")
                        select new
                        {
                            BeRun = el.Element("BeRun").Value,
                            LineName = el.Element("LineName").Value,
                            Source = el.Element("Source").Value,
                            AllDirectories = el.Element("AllDirectories").Value,
                            Server = el.Element("Server").Value,
                            Port = el.Element("Port").Value,
                            SendBlocks = el.Element("SendBlocks").Value,
                            SendSleep = el.Element("SendSleep").Value,
                            Dest = el.Element("Dest").Value,
                            FileType = el.Element("Type").Value,
                            BeBack = el.Element("Back").Value,
                            BackPath = el.Element("BackPath").Value,
                            Desc = el.Element("Desc").Value,
                        };

            foreach (var item in query)
            {
                MySets sets = new MySets(Convert.ToBoolean(item.BeRun), item.LineName, item.Source, Convert.ToBoolean(item.AllDirectories), item.Server, Convert.ToInt32(item.Port), item.Dest, item.FileType, Convert.ToBoolean(item.BeBack), item.BackPath, item.Desc);
                sets.SendBlocks = Convert.ToInt32(item.SendBlocks);
                sets.SendSleep = Convert.ToInt32(item.SendSleep);
                
                mySets.Add(sets);
            }
            return mySets;
        }

        public List<RecSets> GetRecSets()
        {
            List<RecSets> recSets = new List<RecSets>();

            XDocument xdoc = XDocument.Load(GetCfgPath());

            var query = from el in xdoc.Descendants("Receive")
                        select new
                        {
                            BeRun = el.Element("BeRun").Value,
                            RecDir = el.Element("ReceiveDir").Value,
                            LstPort = el.Element("ListenPort").Value,
                            RecSme = el.Element("ReceiveSame").Value,
                            Desc = el.Element("Desc").Value,
                        };

            foreach (var item in query)
            {
                RecSets sets = new RecSets(Convert.ToBoolean(item.BeRun), item.RecDir, Convert.ToInt32(item.LstPort), item.Desc);
                sets.RecSme = Convert.ToBoolean(item.RecSme);

                recSets.Add(sets);
            }
            return recSets;
        }

        public void SetIterm(int index, String key, String value)
        {
            String xpath = GetCfgPath();
            XDocument xdoc = XDocument.Load(xpath);

            IEnumerable<XElement> elements = from el in xdoc.Elements("Set") select el;

            int i = 0;
            XElement item = null;
            foreach (XElement el in elements)
            {
                if (i == index)
                {
                    item = el;
                    break;
                }
                i++;
            }

            if (item != null)
            {
                item.SetElementValue(key, value);
                item.Save(xpath);
            }
        }

        public void SaveSendCfg(List<MySets> sets)
        {
            String xmlPath = GetCfgPath();
            XDocument xdoc = XDocument.Load(xmlPath);

            XElement xelment = xdoc.Element("Sets");
            xelment.Descendants("Set").Remove();
            foreach (MySets set in sets)
            {
                XElement element = new XElement("Set",
                    new XElement("BeRun", set.BeRun),
                    new XElement("LineName", set.LineName),
                    new XElement("Source", set.Source),
                    new XElement("AllDirectories", set.AllDirectories),
                    new XElement("Server", set.Server),
                    new XElement("Port", set.Port),
                    new XElement("SendBlocks", set.SendBlocks),
                    new XElement("SendSleep", set.SendSleep),
                    new XElement("Dest", set.Dest),
                    new XElement("Type", set.FileType),
                    new XElement("Back", set.BeBack),
                    new XElement("BackPath", set.BackPath),
                    new XElement("Desc", set.Desc));

                xelment.Add(element);
            }

            xdoc.Save(xmlPath);
        }

    }
}
