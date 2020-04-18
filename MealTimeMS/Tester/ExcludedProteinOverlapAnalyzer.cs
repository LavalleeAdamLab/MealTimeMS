using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;
using System.IO;
using MealTimeMS.Data;
using MealTimeMS.Util.PostProcessing;

namespace MealTimeMS.Tester
{
	public static class ExcludedProteinOverlapAnalyzer
	{

		public static void DoJob()
		{
			StreamWriter sw = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "ExcludedProteinComparison.txt"));
			String ExcludedProteinFile = "C:\\Coding\\2019LavalleeLab\\temp2\\Output\\Gold_MLGE_nonCheat.txt_output\\ExcludedProteinList.txt";
			StreamReader sr = new StreamReader(ExcludedProteinFile);
			List<String> inProgramConfidentlyIdentified = new List<String>();
			String line = sr.ReadLine();
			while (line != null)
			{
				inProgramConfidentlyIdentified.Add(line);
				line = sr.ReadLine();
			}
			String proteinProphetFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\PreComputedFiles\\MS_QC_120min_interact.prot.xml";
			ProteinProphetResult ppr = ProteinProphetEvaluator.getProteinProphetResult(proteinProphetFile);
			List<String> realConfidentIdentified = ppr.getProteinsIdentified();

			List<String> intersection = ListUtil.FindIntersection(inProgramConfidentlyIdentified, realConfidentIdentified);

			sw.WriteLine("In-Program excluded: {0}", inProgramConfidentlyIdentified.Count);
			sw.WriteLine("Real confidently identified: {0}", realConfidentIdentified.Count);
			sw.WriteLine("Intersection: {0}", intersection.Count);
			sw.Close();


		}
	}
}
