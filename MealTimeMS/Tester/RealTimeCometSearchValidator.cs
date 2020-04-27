using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Util;
using System.IO;
using MealTimeMS.IO;

namespace MealTimeMS.Tester
{
	class RealTimeCometSearchValidator
	{
		public static void TestValidity()
		{

			HashSet<String> set = new HashSet<String> { "sd","qq","sdd"};
			Console.WriteLine(set);
			String logOutput = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\Output\\CometValidationOutput.txt";
			StreamWriter sw = new StreamWriter(logOutput);
			String TSVFile = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\MS_QC_120min.tsv\\MSQC120min.tsv"; //obtained from standard comet search
			List<IDs> idList = PSMTSVReaderWriter.ParseTSV(TSVFile);
			sw.WriteLine("OriginalCometOutput has {0} PSMs", idList.Count);
			int sdDecoyCount = 0;
			foreach(IDs id in idList)
			{
				if (id.isDecoy())
				{
					sdDecoyCount++;
				}
			}
			Console.WriteLine("sdDecoy count: {0}",sdDecoyCount);
			sw.WriteLine("sdDecoy count: {0}", sdDecoyCount);

			String ms2File = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\MS_QC_120min.ms2";
			List<Spectra> spectra= Loader.parseMS2File(ms2File).getSpectraArray();

			String dbPath = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\PreComputedFiles\\uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx";
			String paramsPath = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\2019.comet.params";
			CometSingleSearch.InitializeComet(dbPath, paramsPath);
			List<IDs> realTimeIDList = new List<IDs>();
			foreach (Spectra spec in spectra)
			{
				IDs id = null;
				if (CometSingleSearch.Search(spec, out id))
				{
					realTimeIDList.Add(id);
				}
			}
			sw.WriteLine("RealTimeOutput has {0} PSMs", realTimeIDList.Count);
			sw.WriteLine(CometSingleSearch.ReportFailedStatistics());
			sw.WriteLine("ScanNum\tSeq_sd\tSeq_rt\txcorr_sd\txcorr_rt");
			sw.Flush();

			int sameSeq = 0;
			int diffSeq = 0;

			
			foreach (IDs standardID in idList)
			{
				Boolean matched = false;
				foreach (IDs realTimeID in realTimeIDList)
				{

					if(standardID.getScanNum() == realTimeID.getScanNum())
					{
						matched = true;
						String output = String.Format("{0}\t{1}\t{2}\t{3}\t{4}",standardID.getScanNum(),standardID.getPeptideSequence(),realTimeID.getPeptideSequence(),standardID.getXCorr(),realTimeID.getXCorr());
						sw.WriteLine(output);
						if (standardID.getPeptideSequence().Equals(realTimeID.getPeptideSequence()))
						{
							sameSeq++;
						}
						else
						{
							diffSeq++;
						}
						break;
					}
					

				}
				if (!matched&&!standardID.isDecoy())
				{
					String output = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", standardID.getScanNum(), standardID.getPeptideSequence(), "", standardID.getXCorr(), "", standardID.getParentProteinAccessionsAsString());
					sw.WriteLine(output);
				}
				
			}
			sw.WriteLine("sameSequence: {0}\t diffSeq: {1}", sameSeq, diffSeq);



		}


	}
}
