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
			new Parameter("CrucialFiles","FastaFileName",true,"uniprot_SwissProt_Human_1_11_2017.fasta",""),
			new Parameter("CrucialFiles","CometParamsFile",true,"2019.comet.params",""),
			new Parameter("CrucialFiles","LogisRegressionClassiferSavedWeights",true,"AccordWeight_DCN240Testing_prThr0.561.txt",""),
			new Parameter("SimulationFiles","MS2SimulationTestFile",true,"MS_QC_120min.ms2",""),
			new Parameter("SimulationFiles","MZMLSimulationTestFile",true,"MS_QC_120min.mzML",""),
			new Parameter("PrecomputedFiles","UsePrecomputedFiles",true,"false","true: use the files below instead of generating them with the program. false: generate them with the program automatically. Set to false if running the program for the first time"),
			new Parameter("PrecomputedFiles","ChainSawResult",true,"uniprot_SwissProt_Human_1_11_2017.fasta_digestedPeptides.tsv",""),
			new Parameter("PrecomputedFiles","RTCalcResult",true,"tempOutputPeptideList_rtOutput.txt",""),
			new Parameter("PrecomputedFiles","DecoyFasta",true,"uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta",""),
			new Parameter("PrecomputedFiles","IDXDataBase",true,"uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx",""),
			new Parameter("PrecomputedFiles","OriginalCometOutput",true,"MS_QC_120min.pep.xml",""),
			new Parameter("PrecomputedFiles","ProtXML",true,"MS_QC_120min_interact.prot.xml",""),
			new Parameter("ExperimentParameters","ppmTolerance",true,"5.0","in ppm, so a value of 5.0 would become 5.0/1000000.0"),
			new Parameter("ExperimentParameters","retentionTimeWindowSize",true,"1.0","The retention time window (+- value) allowed to deviate from predicted peptide retention time"),
			new Parameter("ExperimentParameters","MinimumPeptideLength",true,"6",""),
			new Parameter("ExperimentParameters","NUM_MISSED_CLEAVAGES",true,"1",""),
			new Parameter("ExperimentParameters","XCorr_Threshold",true,"2.0",""),
			new Parameter("ExperimentParameters","NumDBThreshold",true,"2",""),
			new Parameter("ExperimentParameters","LogisticRegressionDecisionThreshold",true,"0.5","")
		};

		public static Dictionary<String, String[]> CrucialFiles = new Dictionary<String, String[]>() {
			//These are the necessary files
			{ "FastaFileName",new String[]{"uniprot_SwissProt_Human_1_11_2017.fasta" } },
			{ "CometParamsFile", new String[]{"2019.comet.params" } },
			{ "LogisRegressionClassiferSavedWeights",new String[]{ "AccordWeight_DCN240Testing_prThr0.561.txt" } },
		};
		public static Dictionary<String, String[]> SimulationFiles = new Dictionary<String, String[]>() {
			//below are simulations only
			{ "MS2SimulationTestFile", new String[]{"MS_QC_120min.ms2" } },
			{ "MZMLSimulationTestFile", new String[]{"MS_QC_120min.mzML" } },
		};
		public static Dictionary<String, String[]> MLTrainingFiles = new Dictionary<String, String[]>() {
			//idFeatureTraining
		
		};
		public static Dictionary<String, String[]> PrecomputedFiles = new Dictionary<String, String[]>() {
			{ "UsePrecomputedFiles", new String[]{"false", "true: use the files below instead of generating them with the program. false: generate them with the program automatically. Set to false if running the program for the first time" } },
			{ "ChainSawResult", new String[]{ "uniprot_SwissProt_Human_1_11_2017.fasta_digestedPeptides.tsv" } },
			{ "RTCalcResult", new String[]{ "tempOutputPeptideList_rtOutput.txt" } },
			{ "DecoyFasta", new String[]{ "uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta" } },
			{ "IDXDataBase", new String[]{ "uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx" } },
			{ "OriginalCometOutput", new String[]{ "MS_QC_120min.pep.xml" } },
			{ "ProtXML", new String[]{ "MS_QC_120min_interact.prot.xml" } },
		};
		public static Dictionary<String, String[]> ExperimentParameters = new Dictionary<String, String[]>() {
			//idFeatureTraining
			{ "ppmTolerance", new String[]{"5.0","in ppm, so a value of 5.0 would become 5.0/1000000.0" } },
			{ "retentionTimeWindowSize", new String[]{"1.0", "The retention time window (+- value) allowed to deviate from predicted peptide retention time" } },
			{ "MinimumPeptideLength", new String[]{"6" } },
			{ "NUM_MISSED_CLEAVAGES", new String[]{"1", } },
			{ "XCorr_Threshold", new String[]{"2.0","double" } },
			{ "NumDBThreshold", new String[]{"2","Integer" } },
			{ "LogisticRegressionDecisionThreshold", new String[]{"0.50" } },
			
		};

		public static void ParseParamsFile(String paramsFile)
		{
			StreamReader sr = new StreamReader(paramsFile);
			bool UsePrecomputedFiles = false;
			String line = sr.ReadLine();
			while(line != null)
			{
				if (!line.StartsWith("#")||!line.Trim().Equals(""))
				{
					String ogLine = line;
					if (line.Contains("#"))
					{
						line = line.Substring(0, line.IndexOf("#"));
					}
					line = line.Trim();
					String[] splitedLine = line.Split(" ".ToCharArray());
					String name = splitedLine[0];
					String value = splitedLine[splitedLine.Length-1];
					if (value.Equals("="))
					{
						Console.WriteLine("Warning! Value for \"{0}\" missing", name);
					}

					switch (name)
					{
						case "FastaFileName":
							InputFileOrganizer.FASTA_FILE = value;
							InputFileOrganizer.dbFasta = value;
							break;
						case "CometParamsFile":
							InputFileOrganizer.CometParamsFile = value;
							break;
						case "LogisRegressionClassiferSavedWeights":
							InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile = value;
							break;
						case "MS2SimulationTestFile":
							InputFileOrganizer.MS2SimulationTestFile = value;
							break;
						case "MZMLSimulationTestFile":
							InputFileOrganizer.MZMLSimulationTestFile = value;
							break;
						case "UsePrecomputedFiles":
							UsePrecomputedFiles = Boolean.Parse(value);
							break;
						case "ChainSawResult":
							if (UsePrecomputedFiles)
							{
								InputFileOrganizer.ChainSawResult = value;
								GlobalVar.useChainsawComputedFile=true;
							}
							break;
						case "RTCalcResult":
							if (UsePrecomputedFiles)
							{
								InputFileOrganizer.RTCalcResult = value;
								GlobalVar.useRTCalcComputedFile = true;
							}
							break;
						case "DecoyFasta":
							if (UsePrecomputedFiles)
							{
								InputFileOrganizer.DecoyFasta = value;
								GlobalVar.useDecoyFastaComputedFile = true;
							}
							break;
						case "IDXDataBase":
							if (UsePrecomputedFiles)
							{
								InputFileOrganizer.IDXDataBase = value;
								GlobalVar.useIDXComputedFile = true;
							}
							break;
						case "OriginalCometOutput":
							if (UsePrecomputedFiles)
							{
								InputFileOrganizer.OriginalCometOutput = value;
								GlobalVar.usePepXMLComputedFile = true;
							}
							break;
						case "ProtXML":
							if (UsePrecomputedFiles)
							{
								InputFileOrganizer.ProtXML = value;
							}
							break;
						case "ppmTolerance":
							GlobalVar.ppmTolerance = double.Parse(value);
							break;
						case "retentionTimeWindowSize":
							GlobalVar.retentionTimeWindowSize= double.Parse(value);
							break;
						case "MinimumPeptideLength":
							GlobalVar.MinimumPeptideLength= int.Parse(value);
							break;
						case "NUM_MISSED_CLEAVAGES":
							GlobalVar.NUM_MISSED_CLEAVAGES = int.Parse(value);
							break;
						case "XCorr_Threshold":
							GlobalVar.XCorr_Threshold = double.Parse(value);
							break;
						case "NumDBThreshold":
							GlobalVar.NumDBThreshold = int.Parse(value);
							break;
						case "LogisticRegressionDecisionThreshold":
							GlobalVar.AccordThreshold = double.Parse(value);
							break;
						default:
							Console.WriteLine("Warning! Cannot identify this line in MealTime-MS params file: \"{0}\"",ogLine);
							Console.WriteLine("Params File at: " + paramsFile);
							break;
					}

				}
				line = sr.ReadLine();
			}

		}
		public static void WriteTemplateParamsFile(String directory)
		{
			String paramsFile = Path.Combine(directory, "MealTimeMS.template.params");
			StreamWriter sw = new StreamWriter(paramsFile);
			paramList.Sort((Parameter x, Parameter y) => String.Compare(x.category, y.category));
			String lastCategory = "";

			foreach(Parameter param in paramList)
			{
				if (!param.category.Equals(lastCategory))
				{
					sw.WriteLine("#\n#" + param.category + "\n#");
					lastCategory = param.category;
				}
				String description = param.description;
				if (!description.Equals(""))
				{
					description = "#"+ description;
				}
				String line = String.Format("{0} = {1}\t{2}", param.name, param.defaultValue, description);
				line = line.Trim();
				sw.WriteLine(line);

			}

			sw.Flush();
			sw.Close();
			Console.WriteLine("Template params file has been written to :\n{0}", paramsFile);
		}

		private static void WriteDictionary(StreamWriter sw, Dictionary<String,String[]> dic, String dictionaryDescriptoin)
		{
			
			sw.WriteLine("#"+dictionaryDescriptoin+"\n#\n#");
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

	}

	public class Parameter
	{
		public String category;
		public String name;
		public bool crucial;
		public String description;
		public String defaultValue;
		public Parameter(String _category, String _name, bool _crucial,  String _defaultVal, String _description)
		{
			category = _category;
			name = _name;
			crucial=_crucial;
			defaultValue = _defaultVal;
			description = _description;
		}
	}

}
