using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.Util;

namespace MealTimeMS.IO
{
	static class MealTimeMSParamsParser
	{
		public static List<Parameter> paramList = new List<Parameter>()
		{
			new Parameter("CrucialFiles","FastaFileName",true,"uniprot_SwissProt_Human_1_11_2017.fasta","",true),
			new Parameter("CrucialFiles","CometParamsFile",true,"2019.comet.params","",true),
			new Parameter("SimulationParams","MS2SimulationTestFile",true,"MS_QC_120min.ms2","",true),
			new Parameter("SimulationParams","MZMLSimulationTestFile",true,"MS_QC_120min.mzML","",true),
			new Parameter("PreExperimentSetup","UsePrecomputedFiles",true,"false","true: use the files in the PrecomputedFiles section instead of generating them with the program. false: generate them with the program automatically. Set to false if running the program for the first time"),
			new Parameter("PreExperimentSetup","DecoyPrefix",true,"DECOY_","Decoy prefix used in the Comet params"),
			new Parameter("ClassifierTrainingFiles","MS2_forClassifierTraining",true,"MS_QC_240min.ms2","",true),
			new Parameter("ClassifierTrainingFiles","MZML_forClassifierTraining",true,"MS_QC_240min.mzML","",true),

			new Parameter("ExperimentParameters","ppmTolerance",true,"5.0","in ppm, so a value of 5.0 would become 5.0/1000000.0. Separate by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","retentionTimeWindowSize",true,"1.0","The retention time window allowed to deviate (beyond or below) from predicted peptide retention time. Separate by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","MinimumPeptideLength",true,"6",""),
			new Parameter("ExperimentParameters","NUM_MISSED_CLEAVAGES",true,"1",""),
			new Parameter("ExperimentParameters","XCorr_Threshold",true,"2.0","Separate by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","NumDBThreshold",true,"2","Separate intergers by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","LogisticRegressionDecisionThreshold",true,"0.5","Separate by comma if multiple values are provided"),
			new Parameter("PrecomputedFiles","LogisRegressionClassiferSavedWeights",false,"AccordWeight_DCN240Testing_prThr0.561.txt","",true),
			new Parameter("PrecomputedFiles","ChainSawResult",false,"uniprot_SwissProt_Human_1_11_2017.fasta_digestedPeptides.tsv","",true),
			new Parameter("PrecomputedFiles","RTCalcResult",false,"tempOutputPeptideList_rtOutput.txt","",true),
			new Parameter("PrecomputedFiles","DecoyFasta",false,"uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta","",true),
			new Parameter("PrecomputedFiles","IDXDataBase",false,"uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx","",true),
			new Parameter("PrecomputedFiles","OriginalCometOutput",false,"MS_QC_120min.pep.xml","",true),
			new Parameter("PrecomputedFiles","ProtXML",false,"MS_QC_120min_interact.prot.xml","",true)
		};
		
		//public static Dictionary<String, String[]> CrucialFiles = new Dictionary<String, String[]>() {
		//	//These are the necessary files
		//	{ "FastaFileName",new String[]{"uniprot_SwissProt_Human_1_11_2017.fasta" } },
		//	{ "CometParamsFile", new String[]{"2019.comet.params" } },
		//	{ "LogisRegressionClassiferSavedWeights",new String[]{ "AccordWeight_DCN240Testing_prThr0.561.txt" } },
		//};
		//public static Dictionary<String, String[]> SimulationFiles = new Dictionary<String, String[]>() {
		//	//below are simulations only
		//	{ "MS2SimulationTestFile", new String[]{"MS_QC_120min.ms2" } },
		//	{ "MZMLSimulationTestFile", new String[]{"MS_QC_120min.mzML" } },
		//};
		//public static Dictionary<String, String[]> MLTrainingFiles = new Dictionary<String, String[]>() {
		//	//idFeatureTraining

		//};
		//public static Dictionary<String, String[]> PrecomputedFiles = new Dictionary<String, String[]>() {
		//	{ "UsePrecomputedFiles", new String[]{"false", "true: use the files below instead of generating them with the program. false: generate them with the program automatically. Set to false if running the program for the first time" } },
		//	{ "ChainSawResult", new String[]{ "uniprot_SwissProt_Human_1_11_2017.fasta_digestedPeptides.tsv" } },
		//	{ "RTCalcResult", new String[]{ "tempOutputPeptideList_rtOutput.txt" } },
		//	{ "DecoyFasta", new String[]{ "uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta" } },
		//	{ "IDXDataBase", new String[]{ "uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx" } },
		//	{ "OriginalCometOutput", new String[]{ "MS_QC_120min.pep.xml" } },
		//	{ "ProtXML", new String[]{ "MS_QC_120min_interact.prot.xml" } },
		//};
		//public static Dictionary<String, String[]> ExperimentParameters = new Dictionary<String, String[]>() {
		//	//idFeatureTraining
		//	{ "ppmTolerance", new String[]{"5.0","in ppm, so a value of 5.0 would become 5.0/1000000.0" } },
		//	{ "retentionTimeWindowSize", new String[]{"1.0", "The retention time window (+- value) allowed to deviate from predicted peptide retention time" } },
		//	{ "MinimumPeptideLength", new String[]{"6" } },
		//	{ "NUM_MISSED_CLEAVAGES", new String[]{"1", } },
		//	{ "XCorr_Threshold", new String[]{"2.0","double" } },
		//	{ "NumDBThreshold", new String[]{"2","Integer" } },
		//	{ "LogisticRegressionDecisionThreshold", new String[]{"0.50" } },

		//};


		//Parses the params file for MealTimeMS
		public static void ParseParamsFile(String paramsFile)
		{

			StreamReader sr = new StreamReader(paramsFile);
			//version check
			String line = sr.ReadLine();
			String expectedHeader = GenerateParamsFileHeader();
			if (!line.Equals(expectedHeader))
			{
				Console.WriteLine("Params file should start with \"{0}\", check if the params file is the correct version or format.\n " +
					"use option -p to get a template of a params file", expectedHeader);
				Environment.Exit(5);
			}

			bool UsePrecomputedFiles = false; //default value, might be changed from params file value
			while (line != null)
			{
				if (!line.StartsWith("#") && !line.Trim().Equals(""))
				{
					String ogLine = line;
					if (line.Contains("#"))
					{
						line = line.Substring(0, line.IndexOf("#"));
					}
					line = line.Trim();
					String[] splitedLine = line.Split(" ".ToCharArray());
					String name = splitedLine[0];
					String value = splitedLine[splitedLine.Length - 1];
					Parameter param = GetParamContractFromName(name);
					if (param.crucial && value.Equals("="))
					{
						Console.WriteLine("Error! Value for \"{0}\" is missing in the MealTimeMS param file", name);
						Program.ExitProgram(6);
					}
					value = value.Trim("\"".ToCharArray());

					if (!param.category.Equals("PrecomputedFiles"))
					{
						//CheckParamValid(name, value);
						switch (name)
						{
							case "FastaFileName":
								InputFileOrganizer.FASTA_FILE = value;
								InputFileOrganizer.dbFasta = value;
								break;
							case "CometParamsFile":
								InputFileOrganizer.CometParamsFile = value;
								break;
							case "MS2SimulationTestFile":
								InputFileOrganizer.MS2SimulationTestFile = value;
								break;
							case "MZMLSimulationTestFile":
								InputFileOrganizer.MZMLSimulationTestFile = value;
								break;
							case "MS2_forClassifierTraining":
								InputFileOrganizer.MS2_ClassifierTraining = value;
								break;
							case "MZML_forClassifierTraining":
								InputFileOrganizer.MZML_ClassifierTraining = value;
								break;
							case "UsePrecomputedFiles":
								UsePrecomputedFiles = Boolean.Parse(value);
								break;
							case "DecoyPrefix":
								GlobalVar.DecoyPrefix = value;
								break;
							case "ppmTolerance":
								GlobalVar.PPM_TOLERANCE_LIST = new List<double>();
								List<double> userInputLS= ParseDoubleListFromCSV(value);
								foreach(double ppm in userInputLS)
								{
									GlobalVar.PPM_TOLERANCE_LIST.Add((double)((double)ppm / (double)1000000.0));	
								}
								Console.WriteLine("first ppm set to: {0}", GlobalVar.PPM_TOLERANCE_LIST[0]);
								//GlobalVar.ppmTolerance = double.Parse(value);
								break;
							case "retentionTimeWindowSize":
								GlobalVar.RETENTION_TIME_WINDOW_LIST = ParseDoubleListFromCSV(value);
								break;
							case "MinimumPeptideLength":
								GlobalVar.MinimumPeptideLength = int.Parse(value);
								break;
							case "NUM_MISSED_CLEAVAGES":
								GlobalVar.NUM_MISSED_CLEAVAGES = int.Parse(value);
								break;
							case "XCorr_Threshold":
								GlobalVar.XCORR_THRESHOLD_LIST = ParseDoubleListFromCSV(value);
								break;
							case "NumDBThreshold":
								GlobalVar.NUM_DB_THRESHOLD_LIST = ParseIntListFromCSV(value);
								break;
							case "LogisticRegressionDecisionThreshold":
								GlobalVar.LR_PROBABILITY_THRESHOLD_LIST = ParseDoubleListFromCSV(value);
								break;
							default:
								Console.WriteLine("Warning! Cannot identify this line in MealTime-MS params file: \"{0}\"", ogLine);
								Console.WriteLine("Params File at: " + paramsFile);
								break;
						}
					}
					else
					{
						if (UsePrecomputedFiles)
						{
							
							CheckParamValid(name, value);
							switch (name)
							{
								case "LogisRegressionClassiferSavedWeights":
									InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile = value;
									GlobalVar.useLogisticRegressionTrainedFile = true;
									break;

								case "ChainSawResult":
									InputFileOrganizer.ChainSawResult = value;
									GlobalVar.useChainsawComputedFile = true;
									break;

								case "RTCalcResult":
									InputFileOrganizer.RTCalcResult = value;
									GlobalVar.useRTCalcComputedFile = true;

									break;
								case "DecoyFasta":
									InputFileOrganizer.DecoyFasta = value;
									GlobalVar.useDecoyFastaComputedFile = true;

									break;
								case "IDXDataBase":
									InputFileOrganizer.IDXDataBase = value;
									GlobalVar.useIDXComputedFile = true;

									break;
								case "OriginalCometOutput":
									InputFileOrganizer.OriginalCometOutput = value;
									GlobalVar.usePepXMLComputedFile = true;

									break;
								case "ProtXML":
									InputFileOrganizer.ProtXML = value;

									break;
								default:
									Console.WriteLine("Warning! Cannot identify this line in MealTime-MS params file: \"{0}\"", ogLine);
									Console.WriteLine("Params File at: " + paramsFile);
									break;
							}
						}
					}


				}
				line = sr.ReadLine();
			}
		}

		public static void WriteTemplateParamsFile(String directory)
		{
			String paramsFile = Path.Combine(directory, "MealTimeMS.template.params");
			StreamWriter sw = new StreamWriter(paramsFile);
			sw.WriteLine(GenerateParamsFileHeader());
			//paramList.Sort((Parameter x, Parameter y) => String.Compare(x.category, y.category));
			String lastCategory = "";

			foreach (Parameter param in paramList)
			{
				if (!param.category.Equals(lastCategory))
				{
					if (param.category.Equals("PrecomputedFiles"))
					{
						sw.WriteLine("#\n#" + param.category + "\t*Ignore this section when you run the program for the first time, only used to skip the overhead and speed up future simulations\n#");
					}
					else
					{
						sw.WriteLine("#\n#" + param.category + "\n#");
					}
					lastCategory = param.category;
				}
				String description = param.description;
				if (!description.Equals(""))
				{
					description = "#" + description;
				}
				String line = String.Format("{0} = {1}\t{2}", param.name, param.defaultValue, description);
				line = line.Trim();
				sw.WriteLine(line);
			}

			sw.Flush();
			sw.Close();
			Console.WriteLine("Template params file has been written to :\n{0}", paramsFile);
		}


		private static String GenerateParamsFileHeader()
		{
			return "#" + GlobalVar.programName + "_" + GlobalVar.programVersion;
		}
		private static void WriteDictionary(StreamWriter sw, Dictionary<String, String[]> dic, String dictionaryDescriptoin)
		{

			sw.WriteLine("#" + dictionaryDescriptoin + "\n#\n#");
			foreach (String name in dic.Keys)
			{
				String defaultValue = dic[name][0];
				String description = "";
				if (dic[name].Length > 1)
				{
					description = "#" + dic[name][1];
					description = description.Trim();
				}
				String line = String.Format("{0} = {1}\t{2}", name, defaultValue, description);
				line = line.Trim();
				sw.WriteLine(line);
			}
		}
		private static void CheckFileExists(String file)
		{
			if (!File.Exists(file))
			{
				Console.WriteLine("File {0} in params file cannot be found", file);
				Program.ExitProgram(6);
			}
		}
		private static bool CheckParamValid(String name, String value)
		{
			Parameter param = GetParamContractFromName(name);
			if (param.thisIsAFile)
			{
				CheckFileExists(value); //if the file does not exist, system would exit
			}
			return true;
		}
		private static Parameter GetParamContractFromName(String name)
		{
			foreach (Parameter param in paramList)
			{
				if (param.name.Equals(name))
				{
					return param;
				}
			}
			return null;
		}

		private static List<double> ParseDoubleListFromCSV(String str)
		{
			String[] values = str.Split(",".ToCharArray());
			List<double> doubleVal = new List<double>();
			foreach (String val in values)
			{
				doubleVal.Add(double.Parse(val));
			}
			return doubleVal;
		}

		private static List<int> ParseIntListFromCSV(String str)
		{
			List<int> ls = new List<int>();
			var doubleList = ParseDoubleListFromCSV(str);
			foreach(double val in doubleList)
			{
				ls.Add((int)val);
			}
			return ls;
		}

		private static bool IsParamCrucial(String name)
		{
			Parameter param = GetParamContractFromName(name);
			return param.crucial;
		}
	}

	public class Parameter
	{
		public String category;
		public String name;
		public bool crucial;
		public String description;
		public String defaultValue;
		public bool thisIsAFile;
		public Parameter(String _category, String _name, bool _crucial, String _defaultVal, String _description)
		{
			category = _category;
			name = _name;
			crucial = _crucial;
			defaultValue = _defaultVal;
			description = _description;
			thisIsAFile = false;
		}
		public Parameter(String _category, String _name, bool _crucial, String _defaultVal, String _description, bool _thisIsAFile)
		{
			category = _category;
			name = _name;
			crucial = _crucial;
			defaultValue = _defaultVal;
			description = _description;
			thisIsAFile = _thisIsAFile;
		}
	}

}
