using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.Data;

namespace MealTimeMS.Util.PostProcessing
{
	public class ProteinProphetEvaluator
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

	/* CONSTANTS AND TAGS */
	private readonly static String PROTEIN_GROUP_START_TAG = "<protein_group ";
	private readonly static String PROTEIN_GROUP_END_TAG = "</protein_group>";
	private readonly static String PROTEIN_START_TAG = "<protein ";
	private readonly static String PROTEIN_END_TAG = "</protein>";
	private readonly static String PEPTIDE_START_TAG = "<peptide ";
	private readonly static String PEPTIDE_END_TAG = "</peptide>";
	private readonly static String PROBABILITY_TAG = "probability=\"";
	private readonly static String PROBABILITY_ErrorTable_TAG = "min_prob=\"";
	private readonly static String PEPTIDE_SEQUENCE_TAG = "peptide_sequence=\"";
	private readonly static String CONFIDENCE_TAG = "confidence=\"";
	private readonly static String NUM_DISTINCT_PEPTIDES_TAG = "total_number_distinct_peptides=\"";
	private readonly static String END_TAG = "\"";

		private readonly static String FDR_ErrorTable_TAG = "<error_point error=\"";     //"false_positive_error_rate=\"";
		private readonly static String FDR_TAG = "false_positive_error_rate=\"";     //;
	private readonly static String PROTEIN_NAME_TAG = "protein_name=\"";

	/* FILTERING THRESHOLDS */
	private  static double PROTEIN_GROUP_PROBABILITY_THRESHOLD = 0;//0.9
	private readonly static double DEFAULT_PROTEIN_PROBABILITY_THRESHOLD = 0.9;
	private static double protein_probablity_threshold = 0.0;
	// private readonly static double PROTEIN_CONFIDENCE_THRESHOLD = 0.9;
	private readonly static int NUM_DISTINCT_PEPTIDES_THRESHOLD = 1;

	/*
	 * Extracts the probability for protein group or protein
	 */
	private static double extractFDR(String line)
	{
		double value = 0.0;
		int beginIndex = line.IndexOf(FDR_TAG) + FDR_TAG.Length;
		int endIndex = line.IndexOf(END_TAG, beginIndex);
		String stringValue = line.Substring(beginIndex, endIndex-beginIndex);
		if(!Double.TryParse(stringValue, out value))
		{
				log.Error("Failed to parse FDR");
		}
		return value;
	}
		private static double extractFDR_ErrorTable(String line)
	{
		double value = 0.0;
		int beginIndex = line.IndexOf(FDR_ErrorTable_TAG) + FDR_ErrorTable_TAG.Length;
		int endIndex = line.IndexOf(END_TAG, beginIndex);
		String stringValue = line.Substring(beginIndex, endIndex-beginIndex);
		if(!Double.TryParse(stringValue, out value))
		{
				log.Error("Failed to parse FDR");
		}
		return value;
	}

	/*
	 * Extracts the name of the protein
	 */
	private static String extractProteinName(String line)
	{
		int beginIndex = line.IndexOf(PROTEIN_NAME_TAG) + PROTEIN_NAME_TAG.Length;
		int endIndex = line.IndexOf(END_TAG, beginIndex);
		String stringValue = line.Substring(beginIndex, endIndex - beginIndex);
		return stringValue;
	}

	/*
	 * Extracts the probability for protein group or protein
	 */
	private static double extractProbability(String line)
	{
		double value = 0.0;
		int beginIndex = line.IndexOf(PROBABILITY_TAG) + PROBABILITY_TAG.Length;
		int endIndex = line.IndexOf(END_TAG, beginIndex);
		String stringValue = line.Substring(beginIndex, endIndex - beginIndex);
		Double.TryParse(stringValue, out value);
		return value;
	}
		/*
	 * Extracts the probability from error table
	 */
		private static double extractProbability_ERRORTable(String line)
		{
			double value = 0.0;
			int beginIndex = line.IndexOf(PROBABILITY_ErrorTable_TAG) + PROBABILITY_ErrorTable_TAG.Length;
			int endIndex = line.IndexOf(END_TAG, beginIndex);
			String stringValue = line.Substring(beginIndex, endIndex - beginIndex);
			Double.TryParse(stringValue, out value);
			return value;
		}
		/*
		 * Extracts the confidence value of a protein identification
		 */
		private static double extractConfidence(String line)
	{
		double value = -1.0;
		int beginIndex = line.IndexOf(CONFIDENCE_TAG) + CONFIDENCE_TAG.Length;
		int endIndex = line.IndexOf(END_TAG, beginIndex);
		String stringValue = line.Substring(beginIndex, endIndex - beginIndex);
		if (!stringValue.Equals("-nan"))
		{
				Double.TryParse(stringValue, out value);
		}
		//TODO commented out due to high overhead
		//		try{
		//			value = Double.parseDouble(stringValue);
		//		}catch (Exception e){
		//			log.Debug("Confidence value failed to be parsed. Confidence is: "+ stringValue);
		//		}

		return value;
	}

	/*
	 * Extracts the number of distinct peptides of a protein identification
	 */
	private static int extractNumPeptides(String line)
	{
		int value = 0;
		int beginIndex = line.IndexOf(NUM_DISTINCT_PEPTIDES_TAG) + NUM_DISTINCT_PEPTIDES_TAG.Length;
		int endIndex = line.IndexOf(END_TAG, beginIndex);
		String stringValue = line.Substring(beginIndex, endIndex - beginIndex);
		int.TryParse(stringValue, out value);
		return value;
	}

	private static String extractPeptideSequence(String line)
	{
		int beginIndex = line.IndexOf(PEPTIDE_SEQUENCE_TAG) + PEPTIDE_SEQUENCE_TAG.Length;
		int endIndex = line.IndexOf(END_TAG, beginIndex);
		String stringValue = line.Substring(beginIndex, endIndex - beginIndex);
		return stringValue;
	}

	private static List<String> extractProteinNames(List<String> proteinData)
	{
		List<String> proteins = new List<String>();

		foreach (String s in proteinData)
		{
			String name = extractProteinName(s);
			proteins.Add(name);
		}
		return proteins;
	}

	private static List<String> extractProteinGroupsData(String prot_xml_file, double proteinGroupPrThrehold)
	{
		List<String> proteinGroups = new List<String>();

		// counter
		int numProteinGroups = 0;

		log.Debug("Reading prot xml file and extracting protein groups...");
		try
		{
			StreamReader reader = new StreamReader(prot_xml_file);

			// get the first line
			String line = reader.ReadLine();

			// Ignore everything that isn't within a protein group
			// Store protein groups into an array
			while (line != null)
			{

				if (line.Contains(PROTEIN_GROUP_START_TAG))
				{
					String proteinGroupInformation = "";
					double probabilityValue = extractProbability(line);
					// keep reading lines until the end tag is found
					while (!line.Contains(PROTEIN_GROUP_END_TAG))
					{
						proteinGroupInformation += (line + "\n");
						line = reader.ReadLine();
					}
					proteinGroupInformation += (line + "\n");

					// only keep if above probability threshold
					//TODO >=
					if (probabilityValue >= proteinGroupPrThrehold)
					{
						proteinGroups.Add(proteinGroupInformation);
					}
					numProteinGroups++;
				}
				line = reader.ReadLine();

			}
			log.Debug(numProteinGroups + " spectra in original file.");
			log.Debug("Wrote " + proteinGroups.Count + " spectra to file");
			reader.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
			log.Error("Reading prot xml file unsuccessful!!!");
			Environment.Exit(0);
		}
		log.Debug("Reading prot xml file successful.");
		return proteinGroups;
	}
	private static List<String> extractNegativeProteinGroupsData(String prot_xml_file)
	{
		List<String> proteinGroups = new List<String>();

		// counter
		int numProteinGroups = 0;

		log.Debug("Reading prot xml file and extracting protein groups...");
		try
		{
			StreamReader reader = new StreamReader(prot_xml_file);

			// get the first line
			String line = reader.ReadLine();

			// Ignore everything that isn't within a protein group
			// Store protein groups into an array
			while (line != null)
			{

				if (line.Contains(PROTEIN_GROUP_START_TAG))
				{
					String proteinGroupInformation = "";
					double probabilityValue = extractProbability(line);
					// keep reading lines until the end tag is found
					while (!line.Contains(PROTEIN_GROUP_END_TAG))
					{
						proteinGroupInformation += (line + "\n");
						line = reader.ReadLine();
					}
					proteinGroupInformation += (line + "\n");


					proteinGroups.Add(proteinGroupInformation);
					numProteinGroups++;
				}
				line = reader.ReadLine();

			}
			log.Debug(numProteinGroups + " spectra in original file.");
			log.Debug("Wrote " + proteinGroups.Count + " spectra to file");
			reader.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
			log.Error("Reading prot xml file unsuccessful!!!");
			Environment.Exit(0);
		}
		log.Debug("Reading prot xml file successful.");
		return proteinGroups;
	}

	// Extracts the peptides from the protein data information (from xml file text)
	private static Dictionary<String, List<String>> extractPeptides(List<String> filteredProteinsData)
	{
		Dictionary<String, List<String>> proteinsToPeptides = new Dictionary<String, List<String>>();

		log.Debug("Filtering the proteins from the protein groups...");
		foreach (String proteinInfo in filteredProteinsData)
		{
			List<String> peptides = new List<String>();

			// Get protein name
			String proteinAccession = extractProteinName(proteinInfo);

			// Get list of peptides
			String[] split = proteinInfo.Split("\n".ToCharArray());
				// working variable to store the peptide information
			String peptideInformation = "";
			foreach (String s in split)
			{
				if (s.Contains(PEPTIDE_START_TAG))
				{
					// overwrite old proteinInformation variable
					peptideInformation = s + "\n";
				}
				else if (s.Contains(PEPTIDE_END_TAG))
				{
					// add last line
					peptideInformation += s + "\n";
					// extract value
					String peptideSequence = extractPeptideSequence(peptideInformation);
					peptides.Add(peptideSequence);
				}
				else
				{
					peptideInformation += s + "\n";
				}
			}
			proteinsToPeptides.Add(proteinAccession, peptides);

		}
		return proteinsToPeptides;
	}

	private static List<String> filterProteinsData(List<String> proteinGroups, double prThreshold)
	{
		List<String> proteins = new List<String>();

		// counter
		int numProteinsTotal = 0;

		log.Debug("Filtering the proteins from the protein groups...");
		foreach (String proteinGroupInformation in proteinGroups)
		{
			// split by line
			String[] split = proteinGroupInformation.Split("\n".ToCharArray());
			// working variable to store the protein information
			String proteinInformation = "";

			foreach (String s in split)
			{
				if (s.Contains(PROTEIN_START_TAG))
				{
					// overwrite old proteinInformation variable
					proteinInformation = s + "\n";
				}
				else if (s.Contains(PROTEIN_END_TAG))
				{
					// add last line
					proteinInformation += s + "\n";

					// extract values
					double probability = extractProbability(proteinInformation);
					double confidence = extractConfidence(proteinInformation);

					int numPeptides = extractNumPeptides(proteinInformation);

						// only include if it passes these thresholds
						// if ((probability > PROTEIN_PROBABILITY_THRESHOLD) && (confidence >
						// PROTEIN_CONFIDENCE_THRESHOLD)&& (numPeptides >=
						// NUM_DISTINCT_PEPTIDES_THRESHOLD)) {
					
					//TODO probability >= protein_probablity_threshold
					if ((probability >= prThreshold)
							&& (numPeptides >= NUM_DISTINCT_PEPTIDES_THRESHOLD))
					{
						proteins.Add(proteinInformation);
						log.Debug(String.Format("Protein with probability of {0}, confidence of {1}, and numPeptides of {2} was added",probability, confidence, numPeptides));
					}
					else
					{
						log.Debug(String.Format(
								"Protein with probability of {0}, confidence of {1}, and numPeptides of {2} was filtered",
								probability, confidence, numPeptides));
					}
					numProteinsTotal++;

				}
				else
				{
					proteinInformation += s + "\n";
				}
			}

		}
		foreach (String ss in proteins)
		{
			//System.out.println(ss);
		}

		return proteins;
	}

	// Gives you the xml data from proteins which are above the protein probability
	// threshold (set in the previous step). These are proteins which are not
	// identified with high confidence
	private static List<String> filterNegativeTrainingSetProteinData(List<String> proteinGroups, double proteinProbabilityThreshold)
	{
		List<String> proteins = new List<String>();

		// counter
		int numProteinsTotal = 0;

		log.Debug("Filtering the proteins from the protein groups...");
		foreach (String proteinGroupInformation in proteinGroups)
		{
			// split by line
			String[] split = proteinGroupInformation.Split("\n".ToCharArray());

			// working variable to store the protein information
			String proteinInformation = "";

			foreach (String s in split)
			{
				if (s.Contains(PROTEIN_START_TAG))
				{
					// overwrite old proteinInformation variable
					proteinInformation = s + "\n";
				}
				else if (s.Contains(PROTEIN_END_TAG))
				{
					// add last line
					proteinInformation += s + "\n";

					// extract values
					double probability = extractProbability(proteinInformation);
					double confidence = extractConfidence(proteinInformation);
					int numPeptides = extractNumPeptides(proteinInformation);

					// only include if it passes these thresholds
					// if ((probability > PROTEIN_PROBABILITY_THRESHOLD) && (confidence >
					// PROTEIN_CONFIDENCE_THRESHOLD)&& (numPeptides >=
					// NUM_DISTINCT_PEPTIDES_THRESHOLD)) {
					if (probability < proteinProbabilityThreshold)
					{
						proteins.Add(proteinInformation);
						log.Debug(String.Format(
								"Protein with probability of {0}, confidence of {1}, and numPeptides of {2} was added",
								probability, confidence, numPeptides));
					}
					else
					{
						log.Debug(String.Format(
								"Protein with probability of {0}, confidence of {1}, and numPeptides of {2} was filtered",
								probability, confidence, numPeptides));
					}
					numProteinsTotal++;

				}
				else
				{
					proteinInformation += s + "\n";
				}
			}

		}
		return proteins;
	}

	private static double setFDRThreshold(String prot_xml_file,  double fdr_threshold, out double prThreshold)
	{
		double probability_threshold = Double.MaxValue;
		double max_fdr = 0.0;

		log.Debug("Determining appropriate probability for filtering at an FDR threshold of " + fdr_threshold);
		//try
		//{
			StreamReader reader = new StreamReader(prot_xml_file);

			// get the first line
			String line = reader.ReadLine();
			int count = 0;

			while (line != null)
			{

				if (line.Contains(FDR_ErrorTable_TAG))
				{
					count++;
					// you want the highest probability that is under the fdr threshold
					double fdr = extractFDR_ErrorTable(line);
					double prob = extractProbability_ERRORTable(line);
					//double prob = extractProbability(line);
					if (fdr < fdr_threshold && fdr > max_fdr)
					{ //josh changed this from < to <=
						max_fdr = fdr;
						probability_threshold = Math.Min(probability_threshold, prob);
					}
					// System.out.println(fdr + "\t" + prob);
				}
				line = reader.ReadLine();

			}
			if (count == 0)
			{
				reader.Close();
				Console.WriteLine("ProteinProphet result .prot.xml parsing error, no error table found in file:\n {0}",prot_xml_file);
				throw new Exception();
			}
			reader.Close();
		//}
		//catch (Exception e)
		//{
		//	Console.WriteLine(e.ToString());
		//	log.Error("Unable to determine FDR threshold!!! Setting minimum probability to "
		//			+ DEFAULT_PROTEIN_PROBABILITY_THRESHOLD);
		//	protein_probablity_threshold = DEFAULT_PROTEIN_PROBABILITY_THRESHOLD;
		//	Environment.Exit(0);
		//}
		log.Debug("Setting protein probability to " + probability_threshold + " with an fdr of " + max_fdr);
			prThreshold = probability_threshold;
		return max_fdr;
	}



		/*
		 * Returns the ProteinProphetFile... this gives us a query-able object which can
		 * determine if a protein accession was identified by an experiment, as well as
		 * specific peptides.
		 */
		private static ProteinProphetFile processProteinProphetFile(String protXMLFileName)
		{
			const double fdr_threshold = 0.01; // 1% false discovery rate
			double prThreshold = 0;
			double fdr = setFDRThreshold(protXMLFileName, fdr_threshold,out prThreshold);
			List<String> proteinGroupsData = extractProteinGroupsData(protXMLFileName,0);
			List<String> filteredProteinsData = filterProteinsData(proteinGroupsData, prThreshold);
			Dictionary<String, List<String>> proteinsToPeptides = extractPeptides(filteredProteinsData);
			ProteinProphetFile ppf = new ProteinProphetFile(protXMLFileName, proteinsToPeptides, fdr,
					prThreshold);

			ppf.getProteinProphetResult().SetProteinGroup(ProteinProphetEvaluator.ExtractPositiveProteinGroups(protXMLFileName));
			return ppf;
		}


		//TODO delete
		public static List<String> ExtractPositiveProteinGroups(String protXMLFileName)
		{
			const double fdr_threshold = 0.01; // 1% false discovery rate
			double proteinGroupPRThreshold = 0;
			double fdr = setFDRThreshold(protXMLFileName, fdr_threshold, out proteinGroupPRThreshold);

			List<String> proteinGroupsData = extractProteinGroupsData(protXMLFileName,proteinGroupPRThreshold);
			return proteinGroupsData;

		}

		/*
		 * Extracts the protein names from proteins not identified with high confidence.
		 * The fdr_threshold should be a high value (0.2 or higher) for this to be true.
		 * Extracts the proteins identified above this fdr threshold.
		 */
		public static List<String> extractNegativeTrainingSetProteinNames(String proteinProphetFile,
			 double pr_threshold)
	{
		// setting to an fdr of 0.2 didn't work, because the largest fdr is 0.173...
		// double fdr = setFDRThreshold(proteinProphetFile, fdr_threshold);
		List<String> proteinGroupsData = extractNegativeProteinGroupsData(proteinProphetFile);
		List<String> filteredProteinsData = filterNegativeTrainingSetProteinData(proteinGroupsData,pr_threshold);
		Dictionary<String, List<String>> proteinsToPeptides = extractPeptides(filteredProteinsData);
		ProteinProphetFile ppf = new ProteinProphetFile(proteinProphetFile, proteinsToPeptides, 1,
				pr_threshold);
		return ppf.getProteinNames();
	}

	/*
	 * Extract the number of proteins identified at a given FDR threshold and
	 * protein probability threshold
	 */
	public static ProteinProphetResult getProteinProphetResult(String protXMLFileName)
	{
		ProteinProphetFile ppf = processProteinProphetFile(protXMLFileName);

		return ppf.getProteinProphetResult();
	}

	/*
	 * Returns the list of proteins identified from a protein prophet experiment
	 */
	public static List<String> extractIdentifiedProteinNames(String protXMLFileName)
	{
		ProteinProphetFile ppf = processProteinProphetFile(protXMLFileName);
		return ppf.getProteinNames();
	}

	/*
	 * TODO fix this
	 */
	//public static int countWronglyExcludedScans(Dictionary<String, List<String>> setOfUnidentifiedPeptides,
	//		ResultDatabase rdb, List<int> unusedSpectra)
	//{
	//	int count = 0;
	//	foreach (int scanNum in unusedSpectra)
	//	{
	//		IDs id = rdb.getID(scanNum);
	//		HashSet<String> proteinAccessions = id.getParentProteinAccessions();
	//		String peptideSequence = id.getPeptideSequence();
	//		foreach (String accession in proteinAccessions)
	//		{
	//			bool foundInSetOfUnidentifiedPeptides = setOfUnidentifiedPeptides.ContainsKey(accession);
	//			if (foundInSetOfUnidentifiedPeptides)
	//			{
	//				List<String> sequences = setOfUnidentifiedPeptides[accession];
	//				bool sequenceFound = sequences.Contains(peptideSequence);
	//				if (sequenceFound)
	//				{
	//					count++;
	//				}
	//			}
	//		}
	//	}
	//	return count;
	//}

	//private static Dictionary<String, Double[]> extractProteinWithScore(List<String> proteinGroups)
	//{
	//	Dictionary<String, Double[]> proteins = new Dictionary<String, Double[]>();

	//	// counter
	//	int numProteinsTotal = 0;

	//	log.Debug("Filtering the proteins from the protein groups...");
	//	foreach (String proteinGroupInformation in proteinGroups)
	//	{
	//		// split by line
	//		String[] split = proteinGroupInformation.Split("\n".ToCharArray());
	//		// working variable to store the protein information
	//		String proteinInformation = "";

	//		foreach (String s in split)
	//		{
	//			if (s.Contains(PROTEIN_START_TAG))
	//			{
	//				// overwrite old proteinInformation variable
	//				proteinInformation = s + "\n";
	//			}
	//			else if (s.Contains(PROTEIN_END_TAG))
	//			{
	//				// add last line
	//				proteinInformation += s + "\n";

	//				// extract values
	//				double probability = extractProbability(proteinInformation);
	//				double confidence = extractConfidence(proteinInformation);
	//				int numPeptides = extractNumPeptides(proteinInformation);

	//				// only include if it passes these thresholds
	//				// if ((probability > PROTEIN_PROBABILITY_THRESHOLD) && (confidence >
	//				// PROTEIN_CONFIDENCE_THRESHOLD)&& (numPeptides >=
	//				// NUM_DISTINCT_PEPTIDES_THRESHOLD)) {
	//				if ((probability > protein_probablity_threshold)
	//						&& (numPeptides >= NUM_DISTINCT_PEPTIDES_THRESHOLD))
	//				{
	//					//proteins.Add(proteinInformation);
	//					log.Debug(String.Format(
	//							"Protein with probability of {0}, confidence of {1}, and numPeptides of {2} was added",
	//							probability, confidence, numPeptides));
	//				}
	//				else
	//				{
	//					log.Debug(String.Format(
	//							"Protein with probability of {0}, confidence of {1}, and numPeptides of {2} was filtered",
	//							probability, confidence, numPeptides));
	//				}
	//				numProteinsTotal++;

	//			}
	//			else
	//			{
	//				proteinInformation += s + "\n";
	//			}
	//		}

	//	}
	//	return proteins;
	//}

	public static void main(String[] args)
	{
		String original_protein_prophet_output = "/Users/apell035/workspace/RealTimeMS/data/processed_files/protein_prophet_output/Alex_Mac_MS_QC_60min_interact.prot.xml";
		String testFile = "/Users/apell035/workspace/RealTimeMS/data/processed_files/protein_prophet_output/Alex_Mac_MS_QC_240min_interact.prot.xml";
		// String rdb_file =
		// "data/tsv_files/MS_QC_60min_result_database_NEW_REPROCESSED.tsv";
		// ResultDatabase rdb = Loader.parseResultDatabase(rdb_file);
		ProteinProphetFile ogppf = processProteinProphetFile(original_protein_prophet_output);
		ProteinProphetFile ppf = processProteinProphetFile(testFile);

		Dictionary<String, List<String>> setOfUnidentifiedPeptides = ogppf.comparePeptides(ppf);

		List<int> unusedSpectra = new List<int>();

		// String temp =
		// "C:\\Users\\Alexander\\workspace\\tpp\\fromMac\\2018-10-29_explore_Experiment_480_xCorr_3.5_numDB_5_ppmTol_2.0E-6_rtWindow_2.0_comet_search_interact.prot.xml";

		// ProteinProphetResult numProtID =
		// numProteinsIdentified(original_protein_prophet_output);
		// List<String> proteins =
		// extractIdentifiedProteinNames(original_protein_prophet_output);
		// System.out.println(proteins);
	}
}
}
