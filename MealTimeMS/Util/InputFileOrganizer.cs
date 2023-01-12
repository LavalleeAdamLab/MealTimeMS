using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.Properties;
using MealTimeMS.IO;

namespace MealTimeMS.Util
{
	public static class InputFileOrganizer
	{
		//contains all the files, pretty self-explanatory

		//Directories
		public static String WorkingDirectory = "";
		public static String OutputRoot = WorkingDirectory + "Output\\";
		public static String OutputFolderOfTheRun;
		public static String preExperimentFilesFolder;
		public static String AssemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

		//simulation files
		public static String MS2SimulationTestFile;
        public static String BrukerdotDFolder;
        public static String ProlucidSQTFile;

		//Files that are absolutely required
		public static String MealTimeMSParamsFile;
		public static String FASTA_FILE; //the original protein database fasta file
		public static String CometParamsFile;
        public static String AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile;

        public static String ExclusionDBFasta; //the fasta used to construct the in-program database graph. in most circumstances will be set to the same as FASTA_FILE
											   //quality check files
		public static String CometQualityCheckFile = "";

		//Tools
		public static String RTCalc = "";
		public static String RTCalcCoeff = "";
		//public const String RTCalc = "Tools\\RTCalc_2019.exe";
		//public const String RTCalcCoeff = "Tools\\rtcalc_120minTraining.coeff";
		public static String ChainSaw = "";
        //public static String ChainSaw = "Tools\\chainsaw.exe";
        public static String CometExe = "";
		public static String TPPBinFolder = "C:\\TPP\\bin\\";
		public static String XInteract = TPPBinFolder + "xinteract.exe";
		public static String ProteinProphet = TPPBinFolder + "ProteinProphet.exe";

		//pre computed files
		public static String RTCalcResult = "";
		public static String ChainSawResult = "";
		public static String OriginalCometOutput = "";
		public static String OriginalProtXMLFile = "";
		public static String DecoyFasta = "";
		public static String IDXDataBase = "";
		public static String MeasuredPeptideRetentionTime="";
        public static String PredictedIonMobility = "";

        //suffixes
        public static String PepXMLSuffix = ".pep.xml";

		//test files/ supplementary files
		public static String SVMSavedFile = "";

		public static String SummaryFileForRandomExclusion = "";
        public static String CometOfflineSearchResultTable = ""; // a .tsv of the comet search on the full .ms2 or mzml

        public static String CheatingMonoPrecursorMass = "";


        public static void SetWorkDir(String workDir)
		{
			WorkingDirectory = IOUtils.FilePathOSConverter(workDir);
			
			OutputRoot = IOUtils.FilePathOSConverter( WorkingDirectory + "Output\\");
			if (!Directory.Exists(OutputRoot))
			{
				Directory.CreateDirectory(OutputRoot);
			}
			CometQualityCheckFile = Path.Combine(AssemblyDirectory, "EmbeddedDataFiles", "CometQualityCheck.txt");

            RTCalc = Path.Combine(AssemblyDirectory,"Tools","RTCalc.exe");

#if WIN32
			CometExe = Path.Combine(AssemblyDirectory, "Tools", "comet.2019011.win32.exe");
			ChainSaw = Path.Combine(AssemblyDirectory, "Tools", "chainsaw.exe");
#elif LINUX
            CometExe = Path.Combine(AssemblyDirectory, "Tools", "comet.2019011.linux.exe");
			ChainSaw = Path.Combine(AssemblyDirectory, "Tools", "chainsaw");
            RTCalc = Path.Combine(AssemblyDirectory,"Tools","RTCalc");

#else
            ChainSaw = Path.Combine(AssemblyDirectory, "Tools", "chainsaw_x64.exe");
			CometExe = Path.Combine(AssemblyDirectory, "Tools", "comet.2019011.win64.exe");
#endif

		}
		
        

	}


}
