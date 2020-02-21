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


namespace MealTimeMS.Util
{
	public static class CometSingleSearch
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		static CometSearchManagerWrapper SearchMgr;
		static double dPeptideMassLow;
		static double dPeptideMassHigh;
		static Regex rgx = new Regex("[^A-Z -]");

		static private Dictionary<String, int> failedStatistic;
		static String paramsFile;
		static String dbPath;
		public static void InitializeComet(String _dbPath, String paramsPath)
		{

			Console.WriteLine("Initializing Comet Search Manager Wrapper");
			dbPath = _dbPath;
			paramsFile = paramsPath;

			SearchMgr = new CometSearchManagerWrapper(paramsPath);
			Console.WriteLine("Comet Search Manager initialized");
			SearchSettings searchParams = new SearchSettings();


			string sDB = dbPath;
			dPeptideMassLow = 0;
			dPeptideMassHigh = 0;


			// Configure search parameters here
			// Will also read the index database and return dPeptideMassLow/dPeptideMassHigh mass range

			searchParams.ConfigureInputSettings(SearchMgr, ref dPeptideMassLow, ref dPeptideMassHigh, ref sDB);

			SearchMgr.InitializeSingleSpectrumSearch();

			Console.WriteLine("Comet parameters configured");
			failedStatistic = new Dictionary<String, int>();
			failedStatistic.Add("NoMatch", 0);
			failedStatistic.Add("LengthTooShort", 0);
			failedStatistic.Add("DecoyNoParent", 0);


		}
		
		public static void reset()
		{
			failedStatistic.Clear();
			failedStatistic.Add("NoMatch", 0);
			failedStatistic.Add("LengthTooShort", 0);
			failedStatistic.Add("DecoyNoParent", 0);
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

		//public CometSingleSearch(String dbPath)
		//{
		//    Console.WriteLine("Initializing Comet Search Manager Wrapper");
		//    SearchMgr = new CometSearchManagerWrapper();
		//    Console.WriteLine("Comet Search Manager initialized");
		//    SearchSettings searchParams = new SearchSettings();


		//    string sDB = dbPath;
		//    dPeptideMassLow = 0;
		//    dPeptideMassHigh = 0;

		//    // Configure search parameters here
		//    // Will also read the index database and return dPeptideMassLow/dPeptideMassHigh mass range
		//    searchParams.ConfigureInputSettings(SearchMgr, ref dPeptideMassLow, ref dPeptideMassHigh, ref sDB);
		//    Console.WriteLine("Comet parameters configured");
		//    SearchMgr.InitializeSingleSpectrumSearch();
		//}
		public static bool Search(Spectra spec, out IDs id)
		{


			int iNumPeaks = spec.getPeakCount();
			int iPrecursorCharge = spec.getPrecursorCharge();
			double dPrecursorMZ = spec.getPrecursorMz();
			double[] pdMass = spec.getPeakMz();
			double[] pdInten = spec.getPeakIntensity();

			//double dExpPepMass = (iPrecursorCharge * dPrecursorMZ) - (iPrecursorCharge - 1) * 1.00727646688;
			//if (dExpPepMass < dPeptideMassLow || dExpPepMass > dPeptideMassHigh)
			//{
			//    log.Debug("cannot match the spectra to a peptide: precursor mass out of db range");
			//    id = null;
			//    return false;
			//}


			ScoreWrapper score;
			List<FragmentWrapper> matchingFragments;
			string peptide; //"R.M[15.9949]MM[15.9949]QSGR.K"
			string protein; //output example: 
							//"sp|P12235|ADT1_HUMAN ADP/ATP translocase 1 OS=Homo sapiens GN=SLC25A4 PE=1 SV=4 : sp|P05141|ADT2_HUMAN ADP/ATP translocase 2 OS=Homo sapiens GN=SLC25A5 PE=1 SV=7 : sp|P12236|ADT3_HUMAN ADP/ATP translocase 3 OS=Homo sapiens GN=SLC25A6 PE=1 SV=4"


			//int iNumPeaks -> total number of peaks
			//double[] pdInten -> double[] of peak intensity
			//double[] pdMass -> double[] of peak mass

			SearchMgr.DoSingleSpectrumSearch(iPrecursorCharge, dPrecursorMZ, pdMass, pdInten, iNumPeaks,
			   out peptide, out protein, out matchingFragments, out score);
			if (peptide.Length == 0)
			{
				log.Debug("cannot match the spectra to a peptide, length 0");
				failedStatistic["NoMatch"]++;
				id = null;
				return false;
			}
		
			//parses parent protein into a list and remove all decoys
			HashSet<String> accessions = ParseAccession(protein);

			if (!IsNonDecoyPSM(accessions))
			{
				log.Debug("decoy peptide");
				failedStatistic["DecoyNoParent"]++;
				id = null;
				return false;
			}

			//remove modification brackets from sequence
			//if (peptide.Contains("["))
			//{
			//	Console.WriteLine();
			//}
			String cleanedPeptide = peptide.Substring(2, peptide.Length - 4);
			cleanedPeptide = rgx.Replace(cleanedPeptide, String.Empty);
			if (cleanedPeptide.Length < GlobalVar.MinimumPeptideLength)
			{
				log.Debug("Peptide too short");
				failedStatistic["LengthTooShort"]++;
				id = null;
				return false;
			}

			//if (cleanedPeptide.Equals("ECEDVDMCMTQDQSAR"))
			//{
			//	String skdsd = protein;
			//	skdsd = skdsd + "";
			//}


			double xcorr = score.xCorr;
			int iIonsMatch = score.MatchedIons;
			int iIonsTotal = score.TotalIons;
			double mass = score.mass;
			double dCn = score.dCn;

			//sw.WriteLine("{0}\t{1}\t{2}", spec.getScanNum(),cleanedPeptide,xcorr);
			id = new IDs(spec.getStartTime(), spec.getScanNum(), cleanedPeptide, mass, xcorr, dCn, accessions);
			return true;

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
				if (!acc.StartsWith(GlobalVar.DecoyString))
				{
					//if has a single real parent protein
					return true;
				}
			}
			return false;
		}

		public static String ReportFailedStatistics()
		{
			String str = "";
			foreach(String key in failedStatistic.Keys)
			{
				str = str + key + "\t" + failedStatistic[key] + "\n";
			}
			return str;
		}

		class SearchSettings
		{
			public bool ConfigureInputSettings(CometSearchManagerWrapper SearchMgr,
			   ref double dPeptideMassLow,
			   ref double dPeptideMassHigh,
			   ref string sDB)
			{

				String sTmp;
				int iTmp;
				double dTmp;



				SearchMgr.SetParam("database_name", sDB, sDB);

				//dTmp = 20.0; //ppm window
				//sTmp = dTmp.ToString();
				//SearchMgr.SetParam("peptide_mass_tolerance", sTmp, dTmp);

				//iTmp = 2; // 0=Da, 2=ppm
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("peptide_mass_units", sTmp, iTmp);

				//iTmp = 1;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("mass_type_parent", sTmp, iTmp);

				//iTmp = 1;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("mass_type_fragment", sTmp, iTmp);

				//iTmp = 1; // m/z tolerance
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("precursor_tolerance_type", sTmp, iTmp);

				//iTmp = 1; // 0=off, 1=0/1 (C13 error), 2=0/1/2, 3=0/1/2/3, 4=-8/-4/0/4/8 (for +4/+8 labeling)
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("isotope_error", sTmp, iTmp);

				//search enzyme 
				//iTmp = 1;
				// sTmp = iTmp.ToString();
				//SearchMgr.SetParam("search_enzyme_number", sTmp, iTmp);

				//iTmp = 1;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("allowed_missed_cleavage", sTmp, iTmp);

				//iTmp = 2;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("num_enzyme_termini", sTmp, iTmp);

				//iTmp = 5;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("max_variable_mods_in_peptide", sTmp, iTmp);

				//iTmp = 0;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("require_variable_mod", sTmp, iTmp);

				////ions
				//dTmp = 0.05; // fragment bin width 
				//sTmp = dTmp.ToString();
				//SearchMgr.SetParam("fragment_bin_tol", sTmp, dTmp);
				//double bintol = 0;
				//SearchMgr.GetParamValue("fragment_bin_tol",ref bintol);
				//Console.WriteLine(bintol + "  sdadasad");
				//Console.WriteLine("\n\n\n\n");

				//dTmp = 0.0; // fragment bin offset
				//sTmp = dTmp.ToString();
				//SearchMgr.SetParam("fragment_bin_offset", sTmp, dTmp);

				//iTmp = 0; // 0=use flanking peaks, 1=M peak only
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("theoretical_fragment_ions", sTmp, iTmp);



				//iTmp = 0;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("use_A_ions", sTmp, iTmp);

				//iTmp = 1;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("use_B_ions", sTmp, iTmp);

				//iTmp = 0;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("use_C_ions", sTmp, iTmp);

				//iTmp = 0;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("use_X_ions", sTmp, iTmp);

				//iTmp = 1;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("use_Y_ions", sTmp, iTmp);

				//iTmp = 0;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("use_Z_ions", sTmp, iTmp);

				//iTmp = 0;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("use_NL_ions", sTmp, iTmp);

				//iTmp = 1;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("sample_enzyme_number", sTmp, iTmp);

				//iTmp = 2;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("ms_level", sTmp, iTmp);

				//SearchMgr.SetParam("activation_method", "HCD", "HCD");

				//iTmp = 50; 
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("num_results", sTmp, iTmp);

				//iTmp = 3; // maximum fragment charge
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("max_fragment_charge", sTmp, iTmp);

				//iTmp = 6; // maximum precursor charge
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("max_precursor_charge", sTmp, iTmp);

				//iTmp = 1;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("clip_nterm_methionine", sTmp, iTmp);

				//iTmp = 5000;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("spectrum_batch_size", sTmp, iTmp);

				//SearchMgr.SetParam("decoy_prefix", "DECOY_", "DECOY_");

				//iTmp = 10;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("minimum_peaks", sTmp, iTmp);

				//iTmp = 0;
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("remove_precursor_peak", sTmp, iTmp);

				//dTmp = 1.5;
				//sTmp = dTmp.ToString();
				//SearchMgr.SetParam("remove_precursor_tolerance", sTmp, dTmp); 

				//dTmp = 57.021464;
				//sTmp = dTmp.ToString();
				//SearchMgr.SetParam("add_C_cysteine", sTmp, dTmp);





				//iTmp = 0; // 0=I and L are different, 1=I and L are same
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("equal_I_and_L", sTmp, iTmp);

				//iTmp = 30; // search time cutoff in milliseconds
				//sTmp = iTmp.ToString();
				//SearchMgr.SetParam("max_index_runtime", sTmp, iTmp);







				// Now actually open the .idx database to read mass range from it
				//int iLineCount = 0;
				//            bool bFoundMassRange = false;
				//            string strLine;
				//            System.IO.StreamReader dbFile = new System.IO.StreamReader(@sDB);

				//            while ((strLine = dbFile.ReadLine()) != null)
				//            {
				//                string[] strParsed = strLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				//                if (strParsed[0].Equals("MassRange:"))
				//                {
				//                    dPeptideMassLow = double.Parse(strParsed[1]);
				//                    dPeptideMassHigh = double.Parse(strParsed[2]);

				//                    var digestMassRange = new DoubleRangeWrapper(dPeptideMassLow, dPeptideMassHigh);
				//                    string digestMassRangeString = dPeptideMassLow.ToString() + " " + dPeptideMassHigh.ToString();
				//                    SearchMgr.SetParam("digest_mass_range", digestMassRangeString, digestMassRange);

				//                    bFoundMassRange = true;
				//                }
				//                iLineCount++;

				//                if (iLineCount > 6)  // header information should only be in first few lines
				//                    break;
				//            }
				//            dbFile.Close();

				//            if (!bFoundMassRange)
				//            {
				//                Console.WriteLine(" Error with indexed database format; missing MassRange header.\n");
				//                System.Environment.Exit(1);
				//            }

				return true;
			}
		}




	}
}
