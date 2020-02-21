using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MealTimeMS.Util;
namespace MealTimeMS.IO
{
    class ConfigParser
    {
		//currently not in use at all
        public static void PaseConfig(String configFile)
        {
            Dictionary<string, string> options= new Dictionary<string, string>();
            StreamReader reader = new StreamReader(configFile);
            String line = reader.ReadLine();
            while(!String.IsNullOrEmpty(line))
            {
                line = line.Replace(" ", "");
                String[] st = line.Split("=".ToCharArray());
                options.Add(st[0], st[1]);
                line = reader.ReadLine();
            }

            foreach(String name in options.Keys)
            {
                if (name.Equals("outputFileName"))
                {
                    //InputFileOrganizer.OutputFile = InputFileOrganizer.OutputRoot+ options[name];
                }
                else if (name.Equals("isSimulation"))
                {
                    GlobalVar.IsSimulation = options[name].Equals(true);
                }
                else if (name.Equals("ScansPerOutput"))
                {
                    GlobalVar.ScansPerOutput = int.Parse(options[name]);
                }
            }


        }
    }
}
