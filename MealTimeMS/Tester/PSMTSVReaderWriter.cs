using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.Data;

namespace MealTimeMS.Tester
{	
	//meant to be used to read a PSM file in tsv format
	//comet result pepxml can be converted into .tsv using crux.exe psm-convert command
	static class PSMTSVReaderWriter
	{
		static StreamWriter IDWriter;

        

        //Deprecated function
        public static List<IDs> ParseTSV(String fileDir) {
			List<IDs> idList = new List<IDs>();
			StreamReader sr = new StreamReader(fileDir);
			List<String> header = sr.ReadLine().Split("\t".ToCharArray()).ToList();
			String line = sr.ReadLine();
			int lastScanNum = -1;
			while(line!= null)
			{
				String[] info = line.Split("\t".ToCharArray());
				int scanNum = int.Parse( info[header.IndexOf("scan")]);
				if (scanNum == lastScanNum)
				{
					line = sr.ReadLine();
					continue;
				}
				lastScanNum = scanNum;
				double startTime = double.Parse(info[header.IndexOf("start time")]);
				String pepSeq = info[header.IndexOf("sequence")];
				double pep_mass = double.Parse(info[header.IndexOf("peptide mass")]); 
				double x_Corr = double.Parse(info[header.IndexOf("xcorr score")]);
				double dCN = double.Parse(info[header.IndexOf("dCN")]);
				String parentProtein = info[header.IndexOf("protein id")];
				HashSet<String> accessions = new HashSet<string>( parentProtein.Split(",".ToCharArray()));
				IDs id = new IDs(startTime, scanNum, pepSeq, pep_mass, x_Corr, dCN, accessions);
				idList.Add(id);
				line = sr.ReadLine();
			}
			return idList;
		}
		static String[] headerArr = new String[] { "scan","start time","sequence","peptide mass","xcorr score","dCN", "protein id", "precursorMZ","precursorCharge", "CalculatedPreCursorMass","ChainSawPepMass" };
		public static void InitiatePSMWriter(String outputFile)
		{
			IDWriter = new StreamWriter(outputFile);
			IDWriter.WriteLine(String.Join("\t", headerArr));
		}

		public static void WritePSM(IDs id, Spectra spec, double dbPepMass)
		{
			IDWriter.WriteLine(String.Join("\t",id.getScanNum().ToString(),id.getScanTime().ToString(),id.getPeptideSequence_withModification().ToString(),
				id.getPeptideMass().ToString(), id.getXCorr().ToString(), id.getDeltaCN().ToString(), String.Join(",",id.getParentProteinAccessions()), spec.getPrecursorMz(),spec.getPrecursorCharge(),spec.getCalculatedPrecursorMass(), dbPepMass));
		}

		public static void ClosePSMWriter() {
			IDWriter.Close();
		}
		

		public static void TestParseTSV()
		{
			String file = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\MS_QC_120min.tsv\\MSQC120min.tsv";
			List<IDs> list = ParseTSV(file);
		}


	}
}
