using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.Util;

namespace MealTimeMS.Tester
{
	class ProteinProphetResultTester
	{
		public static void DoJob()
		{




			//InputFileOrganizer.FASTA_FILE = "C:\\Coding\\2019LavalleeLab\\temp2\\ExampleDataSet\\uniprot_SwissProt_Human_1_11_2017.fasta";
			////String outputCometFile = "C:\\Coding\\2019LavalleeLab\\temp2\\ModifiedDBSearchFiles\\Result\\ModdedSearch_MLGEGolden_nonCheat_peptideSearchResultIncluded\\PartialCometFile\\1ModdedSearch_MLGE_nonCheat_peptideSearchResultIncluded_partial.pep.xml";
			//String outputCometFile = "C:\\Coding\\2019LavalleeLab\\temp2\\ModifiedDBSearchFiles\\Result\\Modded_MLGEGolden_rtCheatpeptideSearchResultIncluded\\PartialCometFile\\1Modded_MLGEGolden_rtCheat_partial.pep.xml";
			//ProteinProphetResult ppr = PostProcessingScripts.RunProteinProphet(outputCometFile, InputFileOrganizer.OutputFolderOfTheRun, true);
			String proteinProphetFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\PreComputedFiles\\MS_QC_120min_interact.prot.xml";
			ProteinProphetResult ppr = ProteinProphetEvaluator.getProteinProphetResult(proteinProphetFile);

			Console.WriteLine(ppr.ToString());
			Console.WriteLine("Protein groups " + ppr.getFilteredProteinGroups().Count);
		}
	}
}
