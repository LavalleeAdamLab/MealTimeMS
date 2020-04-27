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
	static class RealTestResultAnalyzer
	{
		static String ms2File = "C:\\Coding\\2019LavalleeLab\\RealTest_Results_20200219\\MSQC_QE_200ng_HEK_2hr_to_run_200219172225.ms2";
		static String excludedSpectraFile = "C:\\Coding\\2019LavalleeLab\\RealTest_Results_20200219\\120minRun_2.txt_ExcludedSpectra.txt";
		static String mzml = "C:\\Coding\\2019LavalleeLab\\RealTest_Results_20200219\\MSQC_QE_200ng_HEK_2hr_to_run_200219172225.mzML";//"C:\\Coding\\2019LavalleeLab\\GoldStandardData\\MZML_Files\\MS_QC_120min.mzml";
		public static void DoJob()
		{
			

			//comet
			Console.WriteLine("Performing Comet search on full ms2 data");
			String fullCometFile = PostProcessingScripts.CometStandardSearch(ms2File, InputFileOrganizer.OutputFolderOfTheRun, true);
			InputFileOrganizer.OriginalCometOutput = fullCometFile;


			//protein prophet
			Console.WriteLine("Perform a protein prophet search on full pepxml");
			String fullProteinProphetFile = PostProcessingScripts.ProteinProphetSearch(fullCometFile, InputFileOrganizer.OutputFolderOfTheRun, true);
			InputFileOrganizer.OriginalProtXMLFile =fullProteinProphetFile;

			ProteinProphetResult baseLinePpr = ProteinProphetEvaluator.getProteinProphetResult(InputFileOrganizer.OriginalProtXMLFile);

			//load spectra
			Console.WriteLine("loading spectra array");
			List<Spectra> ls = Loader.parseMS2File(ms2File).getSpectraArray();

			List<int> includedSpectra = new List<int>();
			List<int> excludedSpectra = new List<int>();

			StreamReader sr = new StreamReader(excludedSpectraFile);
			String line = sr.ReadLine();

			while (line != null)
			{
				int excluded = int.Parse(line);
				excludedSpectra.Add(excluded);
				line = sr.ReadLine();
			}

			foreach(Spectra sp in ls)
			{
				if (!excludedSpectra.Contains(sp.getScanNum())){

					includedSpectra.Add(sp.getScanNum());
				}
			}
			String outputCometFile = Path.Combine( InputFileOrganizer.OutputFolderOfTheRun ,"realTestpartialOut.pep.xml"); //"C:\\Coding\\2019LavalleeLab\\GoldStandardData\\pepxml\\MS_QC_120min_partial.pep.xml";
			String fastaFile = InputFileOrganizer.FASTA_FILE;//"C:\\Coding\\2019LavalleeLab\\GoldStandardData\\Database\\uniprot_SwissProt_Human_1_11_2017.fasta";
			
			PartialPepXMLWriter.writePartialPepXMLFile(fullCometFile, includedSpectra,
			   outputCometFile, mzml, fastaFile, outputCometFile);

			String partialProt = PostProcessingScripts.ProteinProphetSearch(outputCometFile, InputFileOrganizer.OutputFolderOfTheRun, true);
			ProteinProphetResult partialPpr = ProteinProphetEvaluator.getProteinProphetResult(partialProt);

			double partialNum = partialPpr.getNum_proteins_identified();
			double totalNum = baseLinePpr.getNum_proteins_identified();
			double idSens = partialNum / totalNum*100.0;

			double includedScanNum = includedSpectra.Count;
			double totalScanNum = ls.Count;
			double usedResource = includedScanNum / totalScanNum * 100;
			String line1 = String.Format("includedScans {0} \t totalScanNum {1} \tUsedResources {2}", includedScanNum, totalScanNum, usedResource);
			String line2 = String.Format("partialNum {0} \t totalNum {1} \tidsens {2}", partialNum, totalNum, idSens);
			Console.WriteLine(line1);
			Console.WriteLine(line2);
			WriterClass.writeln(line1);
			WriterClass.writeln(line2);
			WriterClass.CloseWriter();


		}
	}
}
