using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.Data;
using MealTimeMS.IO;

namespace MealTimeMS.Tester.Junk
{
	
	static class ProcessedScanMS2Parser
	{
		static String ms2File = "C:\\Coding\\2019LavalleeLab\\RealTest_Results_20200219\\MSQC_QE_200ng_HEK_2hr_to_run_200219172225.ms2";
		static String outputFile= "";
		static String processInfo = "C:\\Coding\\2019LavalleeLab\\RPlot\\IgnoreProcessTime\\Data for RealTimeMS paper - RealTimeProcessTime.tsv";
		public static void DoJob()
		{
			outputFile = Path.Combine (InputFileOrganizer.OutputFolderOfTheRun, "ms2Only.txt");
			List<Spectra> allms2 = Loader.parseMS2File(ms2File).getSpectraArray();
			StreamWriter sw = new StreamWriter(outputFile);
			StreamReader sr = new StreamReader(processInfo);
			List<int> ms2 = new List<int>();
			String line = sr.ReadLine();
			sw.WriteLine(line);
			line = sr.ReadLine();
			foreach(Spectra spec in allms2)
			{
				ms2.Add(spec.getScanNum());
			}
			while (line != null)
			{
					if (ms2.Contains(int.Parse(line.Split("\t".ToCharArray())[0])))
					{
						sw.WriteLine(line);
					}	
				line = sr.ReadLine();
			}
			
			sw.Close();
			sr.Close();


		}
	}
}
