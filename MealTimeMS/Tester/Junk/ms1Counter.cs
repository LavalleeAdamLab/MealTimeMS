using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.IO;
using MealTimeMS.Util;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
using MealTimeMS.ExclusionProfiles.TestProfile;
using MealTimeMS.ExclusionProfiles.Heuristic;
using MealTimeMS.ExclusionProfiles;
using System.IO;
namespace MealTimeMS.Tester.Junk
{
	class ms1Counter
	{
		public static void DoJob()
		{
			String file = "C:\\Users\\LavalleeLab\\MS_QC_120min.ms1";
			StreamReader reader = new StreamReader(file);
			String line = reader.ReadLine();
			List<int> ms1ls = new List<int>();
			while (line != null)
			{
				String[] str = line.Split("\t".ToCharArray());
				if (str[0].Equals("S")){
					ms1ls.Add(int.Parse(str[1]));
				}
				line = reader.ReadLine();
			}
			List<int> childms2 = new List<int>();

			for(int i=1; i<ms1ls.Count; i++)
			{
				int scanNum2 = ms1ls[i];
				int scanNum1 = ms1ls[i - 1];
				childms2.Add(scanNum2 - scanNum1-1);

			}
			childms2.Sort();
			int count = 0;
			foreach(int k in childms2)
			{
				if (k == 12)
				{
					count++;
				}
				Console.WriteLine(k);
			}

			Console.WriteLine(count);

		}
	}
}
