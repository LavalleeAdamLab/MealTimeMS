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
//SIMULATION,DONTEVALUATE,STDEVINCLUDED,WRITE_RT_TIME,IGNORE,CHEATINGRTTIME,TRACKEXCLUDEDPROTEINFEATURE, DDA, EXTRACT_SPECTRAL_COUNT
//for a real time test, use either 
//IGNORE,DONTEVALUATE   or
//IGNORE
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

using DataType = Microsoft.Spark.Sql.Types.DataType;
using DataTypes = Microsoft.Spark.Sql.Types;
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
			ProteinProphetResultTester.DoJob();
			//ProteinProphetResultTester.DoJob();
			//ConfidentProteinGroupData.DoJob();
			String workDir = "C:\\Coding\\2019LavalleeLab\\temp2";
			InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(workDir) + "\\");
			WriterClass.ExperimentOutputSetUp();
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
            //!!!!To Alona: use pre-compile directives "SIMULATION,DDA,EXTRACT_SPECTRAL_COUNT" to build


            //CometSingleSearchTester.CometSingleSearchTest();
            //Tester();
            //CometSingleSearchTester_v2.DoJob();
            //Program.ExitProgram(0);


            CommandLine.Parser.Default.ParseArguments<Options, PrintParams, TrainClassifier>(args)
			.WithParsed<Options>(RunSimulation)
			.WithParsed<PrintParams>(RunPrintParameters)
			.WithParsed<TrainClassifier>(RunTrainClassifier)
			.WithNotParsed(HandleParseError);

			//SetUpOptions(args);

			//Sets up the output directory and creates the output files
			//WriterClass responsible for writing the any output to a file
			WriterClass.ExperimentOutputSetUp();


			//Console.WriteLine("Bring the system to On mode and/or start an acquisition to see results.");

			ExecuteShellCommand.CopyFile(InputFileOrganizer.MealTimeMSParamsFile, InputFileOrganizer.OutputFolderOfTheRun);
			ExclusionExplorer.RunExclusionExplorer(GlobalVar.ExclusionMethod);

			Thread.CurrentThread.Join(2000); // waits x seconds for DataProcessor to finish
            WriterClass.CloseWriter();
            Console.WriteLine("Program finished");

			ExitProgram(0);
        }
		static void RunSimulation(Options opts)
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
			if (GlobalVar.useMeasuredRT)
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
			InputFileOrganizer.FASTA_FILE = tc.fasta;
			InputFileOrganizer.CometParamsFile = tc.cometParams;
			String extractedFeatureSavedFile_posAndNeg;
			String extractedFeatureSavedFile_posAndNonPos;
			FeatureExtractor.ExtractFeatures(tc.MS2_ClassifierTraining, out extractedFeatureSavedFile_posAndNeg, out extractedFeatureSavedFile_posAndNonPos);
			
			String savedClassifierCoeff = IdentificationLogisticRegressionTrainer.TraingAndWriteAccordModel(extractedFeatureSavedFile_posAndNonPos, InputFileOrganizer.OutputFolderOfTheRun);
			Console.WriteLine("Classifier training successful, coefficient written to {0}", savedClassifierCoeff);
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
		class Options
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

		[Verb("-train", HelpText = "Trains the logistic regression classifier from a MS experiment spectral data and generates a coefficient file\n" +
			"Usage:\n" +
			"-train <Work Place Directory> <MS2 File Directory> <Fasta Database> <Comet Parameter File>")]
		class TrainClassifier
		{ //normal options here

			[Value(0, MetaName = "workPlaceDir", Required = true, Default = "", HelpText = "Directory of the output folder to write the template params file")]
			public String workPlaceDir { get; set; }
			//[Value(1, MetaName = "MZMLFile", Required = true, Default = "", HelpText = "The MZML file from the experiment")]
			//public String MZML_ClassifierTraining { get; set; }
			[Value(1, MetaName = "MS2File", Required = true, Default = "", HelpText = "The MS2 file from the experiment, converted from the MZML file")]
			public String MS2_ClassifierTraining { get; set; }
			[Value(2, MetaName = "FastaDatabase", Required = true, Default = "", HelpText = "The protein database in .fasta format")]
			public String fasta{ get; set; }
			[Value(3, MetaName = "CometParams", Required = true, Default = "", HelpText = "Comet parameters file version 2019")]
			public String cometParams{ get; set; }

				
			
		}
		public static void SetUpOptions(String[] args)
		{
			for (int i = 3; i < args.Length; i++)
			{
				String option = args[i];
				if (option.Equals("E"))
				{
					//see exclusion format
					GlobalVar.SeeExclusionFormat = true;
					WriterClass.writePrintln("Exclusion table detail set to true, details will be written");
				}

				else if (option.Equals("SE"))
				{
					// set exclusion table
					GlobalVar.SetExclusionTable = true;
					WriterClass.writePrintln("Set Table set to true, app will attempt to set exclusion table");

				}
			}
			InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(args[0]) + "\\");
			GlobalVar.IsSimulation = bool.Parse(args[1]);
			GlobalVar.ScansPerOutput = int.Parse(args[2]);
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
