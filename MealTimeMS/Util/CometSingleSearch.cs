using System;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using CometWrapper;
using MealTimeMS.Data;
using MealTimeMS.IO;
using MealTimeMS.Properties;
using MealTimeMS.Tester;

namespace MealTimeMS.Util
{
    //Class responsible for comet search (i.e. ms2-to-peptide match) in real-time. 
    // Use pre-processor directive "COMETOFFLINESEARCH" to do an offline search. 
    //In this case you would need to include the comet search result table (in a tsv output format) in the MealTimeMS.params file under the parameter "CometOfflineSearchResultTable"
    public static class CometSingleSearch
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		static CometSearchManagerWrapper SearchMgr;
		static double dPeptideMassLow;
		static double dPeptideMassHigh;
		static Regex rgx = new Regex("[^A-Z -]");
		static String paramsFile;
		static String dbPath;
		public static void InitializeComet(String _dbPath, String paramsPath)
		{
            InitializeComet_NonRealTime(InputFileOrganizer.CometOfflineSearchResultTable);
            return;
		}


		//Reasons unclear, but sometimes comet returns all searches with xCorr of 9.9E-9 and with the wrong sequence
		//returns true if qc passed
		//The program should attempt to reinitialize the comet until it becomes functional again
		public static bool QualityCheck()
		{
			String qcFile = InputFileOrganizer.CometQualityCheckFile;
			
			StreamReader sr = new StreamReader(qcFile);
			String correctSeq = sr.ReadLine().Split("\t".ToCharArray())[2];
			double correctXCorr= double.Parse(sr.ReadLine().Split("\t".ToCharArray())[2]);
			sr.Close();
			Spectra spec = Loader.parseMS2File(qcFile).getSpectraArray()[0];
			IDs id = null;

			int reinitializationAttempt = 1;
			while (reinitializationAttempt < 5)
			{
				if (Search(spec, out id))
				{
					if (id.getPeptideSequence().Equals(correctSeq))
					{
						if (Math.Abs(id.getXCorr() - correctXCorr) <= 0.001)
						{
							Console.WriteLine("Comet QC passed, initialization successful");
							return true; //only return true if: sucessfully matches to a peptide with the right seq and right xCorr
						}
					}

				}
				Console.WriteLine("Comet QC Failed, reinitializing Comet for the {0} time", reinitializationAttempt);
				log.Error("Comet QC Failed");
				InitializeComet(dbPath, paramsFile);
				reinitializationAttempt++;
			}

			Console.WriteLine("Comet qc failed, Reinitialization attemps unsuccessful, program will now exit");
			Console.ReadKey();
			Environment.Exit(2);
			return false;
		}


		public static bool Search(Spectra spec, out IDs id)
		{
            return Search_NonRealTime(spec, out id);
		}
		private static HashSet<String> ParseAccession(String protein)
		{
			if (protein.Equals(""))
			{
				return new HashSet<String>();
;
			}
			//parses parent protein into a list and remove all decoys
			String[] splitRegex = { " : " };
			HashSet<String> accessions = new HashSet<string>();
			String[] proteinsDescription = protein.Split(splitRegex, StringSplitOptions.None);
			foreach (String prot in proteinsDescription)
			{
				String[] split = prot.Trim().Split(" ".ToCharArray());
				String acc = split[0];
				accessions.Add(acc);
			}
			return accessions;
		}

		//a PSM is bad if it isn't at least matched to a single real protein, i.e. only matched to decoys
		private static bool IsNonDecoyPSM(HashSet<String> accessions)
		{
			//TODO remove this line
			//return true;
			foreach(String acc in accessions)
			{
				if (!acc.StartsWith(GlobalVar.DecoyPrefix))
				{
					//if has at least a single real parent protein
					return true;
				}
			}
			return false;
		}

		static Dictionary<int, IDs> searchResultTable;//Only in use if comet offline search is enabled

		public static void InitializeComet_NonRealTime(String resultTable)
		{
			//List<IDs> IDList = ParseCometTSVOutput(resultTable);
			List<IDs> IDList = ParseProlucidTSVOutput(resultTable);
			searchResultTable = new Dictionary<int, IDs>();
			foreach (IDs id in IDList)
			{
				searchResultTable.Add(id.getScanNum(), id);
			}
		}

		public static bool Search_NonRealTime(Spectra spec, out IDs id)
		{
			if (searchResultTable.ContainsKey(spec.getScanNum()))
			{
                //Matches the scan num of the spectra to the scan num in the comet full search result
				id = searchResultTable[spec.getScanNum()];

                //Check if it is a decoy peptide (i.e. all parent proteins are decoys)
                if(id.getPeptideSequence().Length==0|| id.getPeptideSequence().Length<GlobalVar.MinimumPeptideLength||
                    !IsNonDecoyPSM(id.getParentProteinAccessions()))
                {
                    //if this is a decoy peptide or not matched to a peptide
                    id = null;
                    return false;
                }

                id.setScanTime(spec.getStartTime());
                if (id.getXCorr() < 0.0001)
                {
                    id = null;
                    return false;
                }
                return true;
			}
			else
			{
				id = null;
				return false;
			}

		}
		


        //For offline comet search, parses full comet search result from a tsv format
        private static List<IDs> ParseCometTSVOutput(String fileDir)
        {
            List<IDs> idList = new List<IDs>();
            StreamReader sr = new StreamReader(fileDir);
            sr.ReadLine();//ignore first line
            List<String> header = sr.ReadLine().Split("\t".ToCharArray()).ToList();
            String line = sr.ReadLine();
            int lastScanNum = -1;
            while (line != null)
            {
                String[] info = line.Split("\t".ToCharArray());
                int scanNum = int.Parse(info[header.IndexOf("scan")]);
                if (scanNum == lastScanNum)
                {
                    line = sr.ReadLine();
                    continue;
                }
                lastScanNum = scanNum;
                double startTime = -1;
                String pepSeq = info[header.IndexOf("plain_peptide")];
                String pepSeq_withModification = info[header.IndexOf("modified_peptide")];
                double pep_mass = double.Parse(info[header.IndexOf("calc_neutral_mass")]); //TODO not sure if we should use exp_neutral_mass or calc_neutral_mass
                double x_Corr = double.Parse(info[header.IndexOf("xcorr")]);
                double dCN = double.Parse(info[header.IndexOf("delta_cn")]);
                String parentProtein = info[header.IndexOf("protein")];
                HashSet<String> accessions = new HashSet<string>(parentProtein.Split(",".ToCharArray()));
                IDs id = new IDs(startTime, scanNum, pepSeq, pep_mass, x_Corr, dCN, accessions);
                id.setPeptideSequence_withModification(pepSeq_withModification);

                idList.Add(id);
                line = sr.ReadLine();
            }
            return idList;
        }

        private static List<IDs> ParseProlucidTSVOutput(String fileDir)
        {
            List<IDs> idList = new List<IDs>();
            StreamReader sr = new StreamReader(fileDir);
            List<String> header = sr.ReadLine().Split("\t".ToCharArray()).ToList();
            String line = sr.ReadLine();
            int lastScanNum = -1;
            while (line != null)
            {
                String[] info = line.Split("\t".ToCharArray());
                int scanNum = int.Parse(info[header.IndexOf("ms2_id")]);
                if (scanNum == lastScanNum)
                {
                    line = sr.ReadLine();
                    continue;
                }
                lastScanNum = scanNum;
                double startTime = -1;
                String pepSeq = info[header.IndexOf("peptide_stripped")];
                String pepSeq_withModification = info[header.IndexOf("peptide_modified")];
                double pep_mass = double.Parse(info[header.IndexOf("calc_mass")]); //TODO not sure if we should use exp_neutral_mass or calc_neutral_mass
                double x_Corr = double.Parse(info[header.IndexOf("xcorr")]);
                double dCN = double.Parse(info[header.IndexOf("dCN")]);
                String parentProtein = info[header.IndexOf("accessions")];
                HashSet<String> accessions = new HashSet<string>(parentProtein.Split(",".ToCharArray()));
                IDs id = new IDs(startTime, scanNum, pepSeq, pep_mass, x_Corr, dCN, accessions);
                id.setPeptideSequence_withModification(pepSeq_withModification);

                idList.Add(id);
                line = sr.ReadLine();
            }
            return idList;
        }

	}
}
