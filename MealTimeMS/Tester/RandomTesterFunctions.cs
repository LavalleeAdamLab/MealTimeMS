using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MealTimeMS.Util;


namespace MealTimeMS.Tester
{
	public static class RandomTesterFunctions
	{

		public static void LoadAndReplaceRT(ref Dictionary<string, double> pepRTList)
		{
			String file = Path.Combine(InputFileOrganizer.DataRoot, "NoExclusionWithRT.txt_PeptideRTTime.txt");
			StreamReader sr = new StreamReader(file);
			sr.ReadLine(); //reads the header then ignores it
			String line = sr.ReadLine();
			Random rnd = new Random();
			while (line != null)
			{
				String pep = line.Split("\t".ToCharArray())[0];
				double rtInMinutes = double.Parse(line.Split("\t".ToCharArray())[1]);
				if (pepRTList.ContainsKey(pep))
				{
					double randomizedDeviation = (-15.0 + rnd.NextDouble() * 30.0) / 60.0;
					//Console.WriteLine(randomizedDeviation);
					pepRTList[pep] = rtInMinutes + randomizedDeviation;
				}

				line = sr.ReadLine();
			}
		}

		

	}
}
