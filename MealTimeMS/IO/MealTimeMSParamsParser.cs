using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Util;

namespace MealTimeMS.IO
{
	static class MealTimeMSParamsParser
	{
		public static List<Parameter> paramList = new List<Parameter>()
		{
			new Parameter("CrucialFiles","FastaFileName",true,"","File path to the Protein sequence database Fasta file",true),
			new Parameter("CrucialFiles","CometParamsFile",true,"","File path to the comet parameters (2019 version). Make sure to set \"decoy_search\" to 0.",true),
			new Parameter("DirectorySetUp","TPPBinFolder",true,"C:\\TPP\\bin\\","Directory of the bin folder of the Trans-Proteomic Pipeline installation",false),
			new Parameter("SimulationParams","MS2SimulationSpectraFile",true,"","Spectral data in .ms2 format, can be converted from .mzML (or .raw) to .ms2 using ProteoWizard's msconvert",true),
			//new Parameter("SimulationParams","MZMLSimulationTestFile",true,"","",true),
			//new Parameter("PreExperimentSetup","UsePrecomputedFiles",true,"false","true: use the files in the PrecomputedFiles section instead of generating them with the program. false: generate them with the program automatically. Set to false if running the program for the first time"),
			new Parameter("PreExperimentSetup","DecoyPrefix",true,"DECOY_","Decoy prefix used in the Comet params"),
			new Parameter("PreExperimentSetup","RTCalcCoefficient",true,"","Trained RTCalc model .coeff file, used by RTCalc to predict peptide retention time. The training should be done in seconds.",true),
			new Parameter("PreExperimentSetup","NUM_MISSED_CLEAVAGES",true,"1","This version of the MealTime MS is limited to a Trypsin digestion experiment. This number specifies the number of missed cleavage of the digestion"),
			new Parameter("PreExperimentSetup","MinimumPeptideLength",true,"6","Minimum peptide length of the trypsin digestion"),
			//new Parameter("ClassifierTrainingFiles","MS2_forClassifierTraining",true,"MS_QC_240min.ms2","",true),
			//new Parameter("ClassifierTrainingFiles","MZML_forClassifierTraining",true,"MS_QC_240min.mzML","",true),
			new Parameter("ExperimentParameters","ExclusionMethod",true,"1","0: No Exclusion. 1: MealTimeMS. 2: Heuristic exclusion. 3: CombinedExclusion"),
			new Parameter("ExperimentParameters","ppmTolerance",true,"5.0","in ppm, so a value of 5.0 would become 5.0/1000000.0. Separate by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","retentionTimeWindowSize",true,"1.0","The retention time window (minutes) allowed to deviate (beyond or below) from predicted peptide retention time. Separate by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","XCorr_Threshold",true,"2.0","Separate by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","NumDBThreshold",true,"2","Separate intergers by comma if multiple values are provided"),
			new Parameter("ExperimentParameters","LogisticRegressionDecisionThreshold",true,"0.5","Separate by comma if multiple values are provided"),
			new Parameter("LogisticRegressionClassifier","LogisRegressionClassiferSavedCoefficient",false,"","File path to the saved coefficient file of a trained LR classifier model. " +
				"To generate a traiend logistic regression classifier model saved coefficient file, use command: \"MealTimeMS.exe -train\" option",true),
			//new Parameter("PrecomputedFiles","ChainSawResult",false,"","",true),
			new Parameter("PrecomputedFiles","RTCalcPredictedPeptideRT",false,"","RTCalc predicted peptide retention time result file, in seconds",true),
			//new Parameter("PrecomputedFiles","DecoyFasta",false,"","",true),
			new Parameter("PrecomputedFiles","IDXDataBase",false,"","",true),
			new Parameter("PrecomputedFiles","OriginalCometOutput",false,"","",true),
			//new Parameter("PrecomputedFiles","ProtXML",false,"","",true),
			new Parameter("SpecialSimulation","MeasuredPeptideRetentionTime",false,"","A file with empirically measured peptide retention time in minutes. The first line should be a number (double >= 0.0)in seconds specifing the " +
				"amount of perturbation around the retention time. The second line should contain a header \"peptide\tRT\", the rest of the file should contain" +
				"one peptide in each line with their respective retention time in minutes separated by tab: \"VSEFYEETK\t3.983788\". These will be used to replace the some RT values in the RTCalcPredictedPeptideRT file, with the perturbation specified",true)

		};
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
					String[] splitedLine = line.Split("=".ToCharArray());
					String name = splitedLine[0].Trim();
					String value = splitedLine[1].Trim();
					Parameter param = GetParamContractFromName(name);
					if (param.crucial && value.Equals(""))
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
								InputFileOrganizer.ExclusionDBFasta = value;
								break;
							case "CometParamsFile":
								InputFileOrganizer.CometParamsFile = value;
								break;
							case "TPPBinFolder":
								InputFileOrganizer.TPPBinFolder = value;
								InputFileOrganizer.ProteinProphet = Path.Combine(value, "ProteinProphet.exe");
								InputFileOrganizer.XInteract = Path.Combine(value, "xinteract.exe");
								break;
							case "MS2SimulationSpectraFile":
								InputFileOrganizer.MS2SimulationTestFile = value;
								break;
							case "LogisRegressionClassiferSavedCoefficient":
								InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile = value;
								GlobalVar.useLogisticRegressionTrainedFile = true;
								break;
							case "MeasuredPeptideRetentionTime":
								InputFileOrganizer.MeasuredPeptideRetentionTime = value;
								GlobalVar.useMeasuredRT = true;
								break;
							//case "MZMLSimulationTestFile":
							//InputFileOrganizer.MZMLSimulationTestFile = value;
							//	break;
							//case "MS2_forClassifierTraining":
							//	InputFileOrganizer.MS2_ClassifierTraining = value;
							//	break;
							//case "MZML_forClassifierTraining":
							//	InputFileOrganizer.MZML_ClassifierTraining = value;
							//	break;
							//case "UsePrecomputedFiles":
							//	UsePrecomputedFiles = Boolean.Parse(value);
							//	break;
							case "ExclusionMethod":
								GlobalVar.ExclusionMethod = ParseExclusionMethod(int.Parse(value));
								break;
							case "DecoyPrefix":
								GlobalVar.DecoyPrefix = value;
								break;
							case "RTCalcCoefficient":
								InputFileOrganizer.RTCalcCoeff = value;
								break;
							case "ppmTolerance":
								GlobalVar.PPM_TOLERANCE_LIST = new List<double>();
								List<double> userInputLS = ParseDoubleListFromCSV(value);
								foreach (double ppm in userInputLS)
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
						//if (UsePrecomputedFiles)
						//{

							if (CheckParamValid(name, value))
							{
								switch (name)
								{
									

									//case "ChainSawResult":
									//	InputFileOrganizer.ChainSawResult = value;
									//	GlobalVar.useChainsawComputedFile = true;
									//	break;

									case "RTCalcPredictedPeptideRT":
										InputFileOrganizer.RTCalcResult = value;
										GlobalVar.useRTCalcComputedFile = true;

										break;
									//case "DecoyFasta":
									//	InputFileOrganizer.DecoyFasta = value;
									//	GlobalVar.useDecoyFastaComputedFile = true;

									//	break;
									case "IDXDataBase":
										InputFileOrganizer.IDXDataBase = value;
										GlobalVar.useIDXComputedFile = true;

										break;
									case "OriginalCometOutput":
										InputFileOrganizer.OriginalCometOutput = value;
										GlobalVar.usePepXMLComputedFile = true;

										break;
									//case "ProtXML":
									//	InputFileOrganizer.OriginalProtXMLFile = value;
									//	break;
								
									default:
										Console.WriteLine("Warning! Cannot identify this line in MealTime-MS params file: \"{0}\"", ogLine);
										Console.WriteLine("Params File at: " + paramsFile);
										break;
								}
							}
						//}
					}


				}
				line = sr.ReadLine();
			}

			if(GlobalVar.ExclusionMethod.Equals(ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE)|| GlobalVar.ExclusionMethod.Equals(ExclusionProfileEnum.COMBINED_EXCLUSION))
			{
				if(!File.Exists(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile))
				{
					Console.WriteLine("LogisRegressionClassiferSavedCoefficient in the MealTimeMS.params is required to run the MealTime-MS exclusion or Combined exclusion methods, please provide a valid file");
					Console.WriteLine("To generate a traiend logistic regression classifier model saved coefficient file, use command: \n\"MealTimeMS.exe -train\" option");
					Program.ExitProgram(7);
				}

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
						sw.WriteLine("#\n\n###" + param.category + "\t*Leave all the parameters below blank when you run the program for the first time, only used to skip the overhead and speed up future simulations");
					}
					else
					{
						sw.WriteLine("#\n\n###" + param.category + "");
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

		private static ExclusionProfileEnum ParseExclusionMethod(int exclusionMethod)
		{
			switch (exclusionMethod)
			{
				case 0:
					return ExclusionProfileEnum.NO_EXCLUSION_PROFILE;
					break;
				case 1:
					return ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE;
					break;
				case 2:
					return ExclusionProfileEnum.NORA_EXCLUSION_PROFILE;
					break;
				case 3:
					return ExclusionProfileEnum.COMBINED_EXCLUSION;
					break;
			}
			Console.WriteLine("Specified ExclusionMethod param \"{0}\" is invalid, select between 1~3", exclusionMethod);
			Program.ExitProgram(6);

			return ExclusionProfileEnum.NO_EXCLUSION_PROFILE;
		}
		private static bool CheckFileExists(String file)
		{
			if (!File.Exists(file))
			{
				Console.WriteLine("File \"{0}\" in params file cannot be found", file);
				return false;
			}
			return true;
		}
		private static bool CheckParamValid(String name, String value)
		{
			Parameter param = GetParamContractFromName(name);
			if (param.thisIsAFile)
			{
				if (value.Trim().Equals(""))
				{
					return false;
				}
				if (!CheckFileExists(value))//if the file does not exist, ignore 
				{
					return false;
				}
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
			foreach (double val in doubleList)
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
