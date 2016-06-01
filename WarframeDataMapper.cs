using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeDataMapper
    {
        private Dictionary<string, string> itemList { get; set; }
        private Dictionary<string, string> mapList { get; set; }

        public WarframeDataMapper()
        {
            itemList = new Dictionary<string, string>();
            mapList = new Dictionary<string, string>();

            string line;
            StreamReader file = new StreamReader("wf-solmap.txt");

            while ((line = file.ReadLine()) != null)
            {
                var splText = line.Split('=');
                try
                {
                    mapList.Add(splText[0], splText[1]);
                }
                catch(ArgumentException)
                {
                    Console.WriteLine($"Same key blah blah {splText[0]}");
                }
            }

            file.Close();

            file = new StreamReader("wf-items.txt");
            while ((line = file.ReadLine()) != null)
            {
                var splText = line.Split('=');
                //Get the mod name and the containing directory name to try and ensure uniqueness of keys.
                string[] splitItem = splText[0].Split('/');
                string target = splitItem[splitItem.Length - 3] + splitItem[splitItem.Length - 2] + splitItem.Last();

                itemList.Add(target, splText[1]);
            }

            file.Close();
        }

        public string GetItemName(string item)
        {
            string[] splitItem = item.Split('/');
            string target = splitItem[splitItem.Length - 3] + splitItem[splitItem.Length - 2] + splitItem.Last();
            return itemList.ContainsKey(target) ? itemList[target] : target;
        }

        public string GetNodeName(string node)
        {
            //bool s = mapList.ContainsKey(node);
            //string m = s ? mapList[node] : "nope";
            return mapList.ContainsKey(node) ? mapList[node] : node;
        }
    }
}
