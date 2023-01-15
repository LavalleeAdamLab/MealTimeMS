#region legal notice
// Copyright(c) 2016 - 2018 Thermo Fisher Scientific - LSMS
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion legal notice

//possible pre-processor directives
//SIMULATION,DONTEVALUATE,STDEVINCLUDED,WRITE_RT_TIME,IGNORE,CHEATINGRTTIME,TRACKEXCLUDEDPROTEINFEATURE, DDA, EXTRACT_SPECTRAL_COUNT, LINUX, COMETOFFLINESEARCH
//for a real time test, use either 
//IGNORE,DONTEVALUATE   or
//IGNORE
//For bruker, use BRUKERACQUISITIONSIMULATOR
using System;
using System.Globalization;
using System.Threading;
using MealTimeMS.Tester;
using MealTimeMS.IO;
using MealTimeMS.Util;
using MealTimeMS.RunTime;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Tester.Junk;
using System.Collections.Generic;
using CommandLine;
using NLog;
using System.IO;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
namespace MealTimeMS
{
	//Entry point of the program
	//Run the program with option format like this:
	//	[WorkDirectory (while developping, use the directory of this project)] [True or False (true if running a simulation, false if hooked on to actual MS)] [int (number of spectra for each display, not important, just enter a positive number)] 
	static class Program
    {        
        static void Tester()
        {
           
            BrukerInstrumentConnection.TestConnection();
            return;
            //AcquisitionSimulatorTester.DoJob();
            //ExclusionMSTester.DoJob();
            //ProteinProphetResultTester.DoJob();
            //ProteinProphetResultTester.DoJob();
            //ConfidentProteinGroupData.DoJob();
           // ReplacingStuffInPepXML.DoJob();
            //PostProcessingTester.DoJob();
            //Program.ExitProgram(0);
            String workDir = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace";
			InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(workDir) + "\\");
			WriterClass.ExperimentOutputSetUp();
            //ProteinProphetResultTester.DoJob();
            //Program.ExitProgram(1);
            //String savedClassifierCoeff = IdentificationLogisticRegressionTrainer.TraingAndWriteAccordModel(@"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\Training_test_id0.1xCorFilter\20200821K562300ng90min_1_Slot1-1_1_1638.d_extractedFeatures_positiveAndNonPositive.tsv", InputFileOrganizer.OutputFolderOfTheRun);
            //Program.ExitProgram(1);
            GlobalVar.exclusionMS_ip = "http://192.168.0.29";
            BrukerInstrumentConnection.PrintAllProlucidPSM(
                "D:\\CodingLavaleeAdamCDriveBackup\\APIO\\APIO_testData\\20200821K562200ng90min_1_Slot1-1_1_1630.d",
                "D:\\CodingLavaleeAdamCDriveBackup\\APIO\\APIO_testData\\20200821K562200ng90min_1_Slot1-1_1_1630.d\\20200821K562200ng90min_1_Slot1-1_1_1630_nopd.sqt");
            Program.ExitProgram(0);

			CometSingleSearchTester.CometSingleSearchTest();
			ProteinProphetResultTester.DoJob();

			//ProteinSpectraVSExcludedSpectraGenerator.DoJob();
			//ExtractPPRFromProteinProphetResult.DoJob();
			//JunkTester.DoJob();
			//Console.WriteLine("Logistic Regression Training");
			//InputFileOrganizer.SetWorkDir(str);
			//RealTimeCometSearchValidator.TestValidity();
			//PostProcessingTester.DoJob();
			//PartialPepXMLWriterTester.DoJob();
			//ProteinProphetResultTester.DoJob();
			//DecoyConcacenatedGeneratorTester.DoJob();
			//FeatureExtractor.ExtractFeatures(true);
			//IdentificationLogisticRegressionTrainer.TraingAndWriteAccordModel();
			//IdentificationLogisticRegressionTrainer.DoJob();
			//ExcludedProteinOverlapAnalyzer.DoJob();
			//IdentificationLogisticRegressionTrainer.TestLRModel("C:\\Coding\\2019LavalleeLab\\temp2\\Output\\ModdedTraining_PreExFile_output\\MS_QC_240min_extractedFeatures_positiveAndNonPositive_ClassifierCoefficient.txt", 
			//"C:\\Coding\\2019LavalleeLab\\temp2\\Output\\ModdedTraining_PreExFile_output\\MS_QC_240min_extractedFeatures_positiveAndNonPositive.tsv",
			//"C:\\Coding\\2019LavalleeLab\\temp2\\Output\\Modded_120FeatureExtraction_output\\MS_QC_120min_extractedFeatures_positiveAndNonPositive_NoDecoy.tsv");
			//ProteinSpectraVSExcludedSpectraGenerator.FilterForConfidentlyIdentifiedProteinOnly();
			Console.WriteLine("program finished, press any key to continute");
            Console.ReadKey();
            Environment.Exit(0);
        }

        static void Main(string[] args)
		{
            //!!! use pre-compile directives "SIMULATION,DDA,EXTRACT_SPECTRAL_COUNT" to build

            Tester();
            //Program.ExitProgram(0);

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //Parse Command Line options, and reads the MealTimeMS.param file to populate GlobalVar and InputFileOrganizer variables
            CommandLine.Parser.Default.ParseArguments<SimulationOptions, PrintParams, TrainClassifier, BrukerRuntimeCoreOptions>(args)
			.WithParsed<SimulationOptions>(RunSimulation)
			.WithParsed<PrintParams>(RunPrintParameters)
			.WithParsed<TrainClassifier>(RunTrainClassifier)
            .WithParsed<BrukerRuntimeCoreOptions>(RunBrukerRuntimeCore)
            .WithNotParsed(HandleParseError);

			//Sets up the output directory and creates the output files
			//WriterClass responsible for writing the any output to a file
			WriterClass.ExperimentOutputSetUp();
            //Copies the user-specified MealTimeMS param file to the output directory
			ExecuteShellCommand.CopyFile(InputFileOrganizer.MealTimeMSParamsFile, InputFileOrganizer.OutputFolderOfTheRun);
			//ExclusionExplorer.RunRealTimeExperiment();
            
            //Entry point to simulation
			ExclusionExplorer.RunExclusionExplorer(GlobalVar.ExclusionMethod);

			Thread.CurrentThread.Join(2000); // waits x seconds for DataProcessor to finish
            WriterClass.CloseWriter(); //Closes all the writerclass StreamWriters
            Console.WriteLine("Program finished");
			ExitProgram(0);
        }
		static void RunSimulation(SimulationOptions opts)
		{
			//handle options
			if (!File.Exists(opts.paramsFile))
			{
				Console.WriteLine("MealTime-MS param file: \"{0}\"\nis missing or cannot be located, use option -p to generate a template param file in your workplace directory",opts.paramsFile);
				ExitProgram(3);
			}
			InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(opts.workPlaceDir) + "\\");
			GlobalVar.IsSimulation = opts.isSimulation;
			//for now, end program if it's not simulation
			if (opts.isSimulation == false)
			{
				Console.WriteLine("Real-time experiment is not enabled in this version of the program, program will now exit.");
				Console.WriteLine("Please set the simulation parameter to true to proceed with the in silico simulation");
				Program.ExitProgram(1);
			}


			GlobalVar.ScansPerOutput = opts.scansPerOutput;
			SetNLogLevel(opts.logLevel);
			InputFileOrganizer.MealTimeMSParamsFile = opts.paramsFile; 
			MealTimeMSParamsParser.ParseParamsFile(opts.paramsFile);
			GlobalVar.isSimulationForFeatureExtraction = false;

			Console.WriteLine("Running data acquisition simulation:");
			Console.WriteLine("Exclusion Method: {0}", GlobalVar.ExclusionMethod.getDescription());
			Console.WriteLine("Simulation spectral file: {0}", InputFileOrganizer.MS2SimulationTestFile);
			var paramsRequired = ExclusionProfileEnumExtension.getParamsRequired( GlobalVar.ExclusionMethod);
			foreach (ExclusionTypeParamEnum e in paramsRequired)
			{
				String variableName = e.getShortDescription();
				String variableInfo = "";
				switch (e)
				{
					case ExclusionTypeParamEnum.ppmTol:
						variableInfo = String.Format("{0}: {1}", variableName, String.Join(",",GlobalVar.PPM_TOLERANCE_LIST));
						break;
					case ExclusionTypeParamEnum.rtWin:
						variableInfo = String.Format("{0}: {1}", variableName, String.Join(",", GlobalVar.RETENTION_TIME_WINDOW_LIST));
						break;
                    case ExclusionTypeParamEnum.imWin:
						variableInfo = String.Format("{0}: {1}", variableName, String.Join(",", GlobalVar.ION_MOBILITY_WINDOW_LIST));
						break;
					case ExclusionTypeParamEnum.xCorr:
						variableInfo = String.Format("{0}: {1}", variableName, String.Join(",", GlobalVar.XCORR_THRESHOLD_LIST));
						break;
					case ExclusionTypeParamEnum.numDB:
						variableInfo = String.Format("{0}: {1}", variableName, String.Join(",", GlobalVar.NUM_DB_THRESHOLD_LIST));
						break;
					case ExclusionTypeParamEnum.prThr:
						variableInfo = String.Format("{0}: {1}", variableName, String.Join(",", GlobalVar.LR_PROBABILITY_THRESHOLD_LIST));
						break;

				}
				Console.WriteLine(variableInfo);
			}
			if (!InputFileOrganizer.MeasuredPeptideRetentionTime.Equals(""))
			{
				Console.WriteLine("Using measured peptide retention time with {0} seconds of perturbation from file:\n  {1}",GlobalVar.amountPerturbationAroundMeasuredRetentionTimeInSeconds, InputFileOrganizer.MeasuredPeptideRetentionTime);
			}

		}

		static void RunPrintParameters(PrintParams pr)
		{
			String workDir = Path.GetFullPath(pr.workPlaceDir);
			Console.WriteLine("Writing template params file to: {0}", workDir);
			MealTimeMSParamsParser.WriteTemplateParamsFile(workDir);
			ExitProgram(2);
		}

		static void RunTrainClassifier(TrainClassifier tc)
		{
			InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(tc.workPlaceDir) + "\\");
			WriterClass.ExperimentOutputSetUp();
            GlobalVar.isSimulationForFeatureExtraction = true;
            GlobalVar.NUM_MISSED_CLEAVAGES = tc.misCleavage;
            GlobalVar.MinimumPeptideLength = tc.minPepLength;
            GlobalVar.includeIonMobility = false;
            InputFileOrganizer.OriginalCometOutput = tc.DatabaseSearchResult;
            //Debug
            //InputFileOrganizer.OriginalProtXMLFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\dfgdfg32423\preExperimentFiles\20200821K562300ng90min_1_Slot1-1_1_1638_nopd_replaced_interact.prot.xml";
            //GlobalVar.useComputedProteinProphet = true;
//
            InputFileOrganizer.FASTA_FILE = tc.fasta;
            InputFileOrganizer.ExclusionDBFasta = tc.fasta;
            InputFileOrganizer.BrukerdotDFolder = tc.BrukerDotDFolder;
            string[] sqtfiles = Directory.GetFiles(tc.BrukerDotDFolder, "*.sqt");
            InputFileOrganizer.ProlucidSQTFile = sqtfiles[0];
            string[] dotMS2files = Directory.GetFiles(tc.BrukerDotDFolder, "*.ms2");
            InputFileOrganizer.MS2SimulationTestFile = dotMS2files[0];
            if (sqtfiles.Length > 1 | dotMS2files.Length > 1)
            {
                Console.WriteLine("Multiple .sqt or .ms2 files found in the .d folder provided. Program will now exit");
                Program.ExitProgram(2);
            }
            
            Console.WriteLine("Num missed cleavages: {0}\nMin peptide length: {1}\nSimulating data from: {2}",
                GlobalVar.NUM_MISSED_CLEAVAGES, GlobalVar.MinimumPeptideLength, InputFileOrganizer.BrukerdotDFolder);

            String extractedFeatureSavedFile_posAndNeg;
			String extractedFeatureSavedFile_posAndNonPos;
            FeatureExtractor.ExtractFeatures_Bruker(InputFileOrganizer.BrukerdotDFolder, out extractedFeatureSavedFile_posAndNeg, out extractedFeatureSavedFile_posAndNonPos);
			
			String savedClassifierCoeff = IdentificationLogisticRegressionTrainer.TraingAndWriteAccordModel(extractedFeatureSavedFile_posAndNonPos, InputFileOrganizer.OutputFolderOfTheRun);
			Console.WriteLine("Classifier training successful, coefficient written to {0}", savedClassifierCoeff);
			Program.ExitProgram(0);

		}
        static void RunBrukerRuntimeCore(BrukerRuntimeCoreOptions brcOptions)
        {
            String kafka_url = String.Concat(brcOptions.kafka_ip , ":" , brcOptions.kafka_port);
            String schemaReg_url = String.Concat("http://" + brcOptions.schema_ip, ":", brcOptions.schema_port);
            String exclusionMS_url = String.Concat("http://"+brcOptions.exclusionMS_ip, ":", brcOptions.exclusionMS_port);
            
            GlobalVar.kafka_url = kafka_url;
            GlobalVar.schemaRegistry_url = schemaReg_url;
            GlobalVar.exclusionMS_url = exclusionMS_url;
            GlobalVar.exclusionMS_ip = "http://"+brcOptions.exclusionMS_ip;

            BrukerRuntimeCore.BrukerRuntimeCore_Main();
            Program.ExitProgram(0);
        }

        static void HandleParseError(IEnumerable<Error> errs)
		{
			//handle errors

			Console.WriteLine("Incorrect Usage Format, program will now exit");
			Console.WriteLine("Usage_Create a MealTimeMS param template file:\n\tMealTimeMS.exe -p <Directory To Create the param file>\n");
			Console.WriteLine("Usage_Training Classifier:\n\tMealTimeMS.exe -train <workPlaceDirectory> <MS2 file> <Protein Fasta database> <Comet parameter file>\n");
			Console.WriteLine("Usage_Running Simulation:\n\tMealTimeMS.exe -run [options] <workPlaceDirectory> <paramsFile>\n");

			ExitProgram(3);
		}

		[Verb("-run", HelpText = "Runs the program")]
		class SimulationOptions
		{
			[Value(0, MetaName = "workPlaceDir",Required =true, HelpText = "Directory of the output folder")]
			public String workPlaceDir { get; set; }

			[Value(1, MetaName = "paramsFile", Required = false,Default =null, HelpText = "Full path of the parameters file for MealTime MS, use option -p to generate")]
			public String paramsFile { get; set; }

			[Option('r', "report", Required = false, Default =1, HelpText = "Number of scans processed for every info output (default 1)")]
			public int scansPerOutput { get; set; }

			[Option('s', "simulation", Required = false, Default = true, HelpText = "Is this a simulation? Set to [false] if it's actually hooked up to a mass spec. Default: [true]")]
			public bool isSimulation{ get; set; }

			// Omitting long name, defaults to name of property, ie "--verbose"
			[Option('l', "logLevel", Required = false,
			  Default = "Info",
			  HelpText = "Log Level of the logger, options: Info, Debug")]
			public String logLevel { get; set; }
			//[Option('p', "params", Required = false,  HelpText = "Writes a template params file to the working directory")
		}

		[Verb("-p", HelpText = "Writes a template params file to the working directory")]
		class PrintParams
		{ //normal options here

			[Value(0, MetaName = "workPlaceDir", Required = true,Default ="", HelpText = "Directory of the output folder to write the template params file")]
			public String workPlaceDir { get; set; }
		}
        [Verb("-c", HelpText = "Launches MTMS as a standalone application that takes prolucid stream as input and makes http post calls to exclusionMS webserver")]
		class BrukerRuntimeCoreOptions
        { //normal options here

			//[Value(0, MetaName = "kafka_ip", Required = true,Default ="", HelpText = "ip of the kafka broker")]
            [Option( "kafka_ip", Required = true, HelpText = "ip of the kafka broker")]
            public String kafka_ip { get; set; }
            [Option( "kafka_port", Required = true, HelpText = "port number of the kafka broker")]
            public int kafka_port { get; set; }
            [Option( "schema_ip", Required = true, HelpText = "ip of the schema registry")]
            public String schema_ip { get; set; }
            [Option( "schema_port", Required = true, HelpText = "port number of the schema registry")]
            public String schema_port { get; set; }
            [Option("exclusionMS_ip", Required = true, HelpText = "ip of the exclusionMS webserver")]
            public String exclusionMS_ip { get; set; }
            [Option("exclusionMS_port", Required = true, HelpText = "port number of the exclusionMS webserver")]
            public String exclusionMS_port { get; set; }


        }

		[Verb("-train", HelpText = "Trains the logistic regression classifier from a MS experiment spectral data and generates a coefficient file\n" +
			"Usage:\n" +
			"-train <Work Place Directory> <Bruker .d folder path> <Fasta Database> <pep.xml Database search result>")]
		class TrainClassifier
		{ //normal options here

			
			//[Value(1, MetaName = "MZMLFile", Required = true, Default = "", HelpText = "The MZML file from the experiment")]
			//public String MZML_ClassifierTraining { get; set; }
			[Value(0, MetaName = "misCleavage", Required = true, Default = "", HelpText = "Number of miscleavages for protein digestion")]
			public int misCleavage { get; set; }
            [Value(1, MetaName = "minPepLength", Required = true, Default = "", HelpText = "Minimum peptide length")]
			public int minPepLength { get; set; }
            [Value(2, MetaName = "workPlaceDir", Required = true, Default = "", HelpText = "Directory of the output folder to write the template params file")]
            public String workPlaceDir { get; set; }
            [Value(3, MetaName = "BrukerDotDFolder", Required = true, Default = "", HelpText = "The MS2 file from the experiment, converted from the MZML file")]
			public String BrukerDotDFolder { get; set; }
			[Value(4, MetaName = "FastaDatabase", Required = true, Default = "", HelpText = "The protein database in .fasta format")]
			public String fasta{ get; set; }
            [Value(5, MetaName = "DatabaseSearchResult", Required = true, Default = "", HelpText = "A .pep.xml search result file")]
            public String DatabaseSearchResult { get; set; }
            //[Value(3, MetaName = "RetentionTimePredictions", Required = true, Default = "", HelpText = "Predicted peptide retention time (minutes)")]
            //public String rtPredictionsFile{ get; set; }
            //         [Value(4, MetaName = "IonMobilityPredictions", Required = true, Default = "", HelpText = "Predicted ion mobility")]
            //         public String IonMobilityPredictionFile { get; set; }



        }
		
		public static void SetNLogLevel(String _logLevel)
		{
			LogLevel logLevel;
			if (_logLevel.ToLower().Equals("debug"))
			{
				logLevel = LogLevel.Debug;
			}else if (_logLevel.ToLower().Equals("info"))
			{
				logLevel = LogLevel.Info;
			}
			else
			{
				Console.WriteLine("Error in parsing log level, setting to default LogLevel.Info");
				logLevel = LogLevel.Info;
			}
			foreach (var rule in LogManager.Configuration.LoggingRules)
			{
				rule.EnableLoggingForLevel(logLevel);
			}

			//Call to update existing Loggers created with GetLogger() or 
			//GetCurrentClassLogger()
			LogManager.ReconfigExistingLoggers();
		}

		public static void ExitProgram(int exitCode)
		{
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
			Environment.Exit(exitCode);
		}


	}
}
