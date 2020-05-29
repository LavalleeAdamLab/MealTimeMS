using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.Data;

namespace MealTimeMS.Tester.Junk
{
	public static class ExtractPPRFromProteinProphetResult
	{

		public static void DoJob()
		{

			StreamWriter sw = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "CorrectedProteinProphetResultParsing.txt"));
			String noExclusionPPF = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\PreComputedFiles\\MS_QC_120min_interact.prot.xml";
			ProteinProphetResult baseLineppr = ProteinProphetEvaluator.getProteinProphetResult(noExclusionPPF);

			sw.WriteLine("FileName\tNumberofProteins\tNumberOfProteinGroups\tProteinIDSense\tProteinGroupIDSense");
			for(int i = 1; i <= 36;i++) {
				String proteinProphetResultFile = String.Format("C:\\Coding\\2019LavalleeLab\\ProteinProphetNewFilter\\ExclusionExplorer\\ConfidentlyIdentifiedProteinAndProteinGroupNotUpToDateSoOverlapWillBeWrongAsWell\\Heuristic_rtCheat_ProtKept\\protein_prophet_output\\{0}Heuristic_rtCheat_ProtKept_partial_interact.prot.xml", i);
				Console.WriteLine(proteinProphetResultFile);
				ProteinProphetResult ppr=ProteinProphetEvaluator.getProteinProphetResult(proteinProphetResultFile);
				double proteinIDSense = (double)ppr.getNum_proteins_identified() / (double)baseLineppr.getNum_proteins_identified();
				double proteinGroupIDSense = (double)ppr.getFilteredProteinGroups().Count / (double)baseLineppr.getFilteredProteinGroups().Count;
				sw.WriteLine(String.Join("\t",proteinProphetResultFile,ppr.getNum_proteins_identified(), ppr.getFilteredProteinGroups().Count,proteinIDSense,proteinGroupIDSense));
			}

			sw.Close();
			
			


		}
	}
}
