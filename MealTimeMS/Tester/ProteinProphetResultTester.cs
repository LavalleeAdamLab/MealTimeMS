using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.Util;
using System.IO;

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
            String proteinProphetFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\NoExclusion_WithIonMObility\protein_prophet_output\0NoExclusion_WithIonMObility_partial_interact.prot.xml";
             proteinProphetFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\Dat1630_NoExclusion\protein_prophet_output\0Dat1630_NoExclusion_partial_interact.prot.xml";
            proteinProphetFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\testRunnn\protein_prophet_output\1testRunnn_partial_interact.prot.xml";
            ProteinProphetResult ppr = ProteinProphetEvaluator.getProteinProphetResult(proteinProphetFile);
            using (StreamWriter sw = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "IdentifiedProteins.tsv")))
            {
                foreach (String prot in ppr.getProteinsIdentified())
                {
                    sw.WriteLine(prot);
                }

            }; 
			Console.WriteLine(ppr.ToString());
			Console.WriteLine("Protein groups " + ppr.getFilteredProteinGroups().Count);
		}
	}
}
