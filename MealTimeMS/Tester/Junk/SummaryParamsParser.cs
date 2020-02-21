using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.RunTime;
using MealTimeMS.ExclusionProfiles.TestProfile;

namespace MealTimeMS.Tester.Junk
{
	class SummaryParamsParser
	{
		public static void DoJob()
		{
			String resultFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\MLGESequentialRun_Full.txt_Summary.txt";
			String outputFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\Output\\ParamsParsed.txt";

			List<ExperimentResult> experiments = ExclusionExplorer.ParseExperimentResult(resultFile);
			
			StreamWriter sw = new StreamWriter(outputFile);
			String[] paramName = { "ppmTol", "rtWin", "xCorr", "numDB", "prThr"};

			sw.Write("expName\t");
			foreach(String param in paramName)
			{
				sw.Write(param+"\t");
			}
			sw.WriteLine();
			foreach(ExperimentResult exp in experiments)
			{
				String expName = exp.experimentName;
				sw.Write(expName + "\t");
				foreach (String param in paramName)
				{
					if (!expName.Contains(param))
					{
						sw.Write("-1\t");
						continue;
					}
					int startPos = expName.IndexOf(param) + param.Length + 1;
					int endPos = expName.IndexOf("_", startPos + 1);
					endPos = (endPos < 0) ? expName.Length : endPos;
					String sub = expName.Substring(startPos, endPos - startPos);
					double value = double.Parse(sub);
					String sss = param + "sdsd";
					sw.Write(value + "\t");
				}
				sw.WriteLine();
			}

			sw.Close();


		}

	}
}
