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
//SIMULATION,EVALUATE,STDEVINCLUDED,WRITE_RT_TIME,IGNORE,CHEATINGRTTIME,TRACKEXCLUDEDPROTEINFEATURE
//for a real time test, use either 
	//IGNORE,EVALUATE   or
	//IGNORE
using System;
using System.Globalization;
using System.Threading;
using MealTimeMS.Tester;
using MealTimeMS.IO;
using MealTimeMS.Util;
using MealTimeMS.RunTime;
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
        static void Tester(String str)
        {
			//JunkTester.DoJob();
			//Console.WriteLine("Logistic Regression Training");
			//InputFileOrganizer.SetWorkDir(str);
			//RealTimeCometSearchValidator.TestValidity();
			//PostProcessingTester.DoJob();
			//PartialPepXMLWriterTester.DoJob();
			//ProteinProphetResultTester.DoJob();
			//DecoyConcacenatedGeneratorTester.DoJob();
			//FeatureExtractor.ExtractFeatures(true);
			//IdentificationLogisticRegressionTrainer.DoJob();
			ProteinSpectraVSExcludedSpectraGenerator.FilterForConfidentlyIdentifiedProteinOnly();
			Console.WriteLine("program finished, press any key to continute");
            Console.ReadKey();
            Environment.Exit(0);
        }

        static void Main(string[] args)
		{

			//CommandLine.Parser.Default.ParseArguments<PrintParams,Options>(args)
			//.WithParsed<PrintParams>(PrintParameters)
			//.WithParsed<Options>(RunOptions)
			//.WithNotParsed(HandleParseError);


			//if (args ==null || args[0].Contains("-help"))
			//{
			//	Console.WriteLine("Usage: ");
			//	Console.WriteLine("MealTimeMS.exe <Workplace directory> <paramsFileFullPath> [options]");
			//	Console.WriteLine("\n Optional arguments:");
			//	Console.WriteLine("[--report]\n\tnumber of scans processed for every info output (default 1)");
			//	Console.WriteLine("-p To generate a params file template");

			//}
			SetUpOptions(args);
			
			//Sets up the output directory and creates the output files
			//WriterClass responsible for writing the any output to a file
			WriterClass.ExperimentOutputSetUp();

			//parses other options, only used when hooked on to actual machine
			

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			//Tester(args[0]);

			Console.WriteLine("Bring the system to On mode and/or start an acquisition to see results.");
			//ExclusionExplorer.RunExclusionExplorer(ExclusionProfiles.ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE);
			//ExclusionExplorer.SingleSimulationRun(ExclusionProfiles.ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE);
			//ExclusionExplorer.RunRealTimeExperiment();
			ExclusionExplorer.RunRandomExclusion("C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\Workplace\\TestData\\DataForRandomExclusion - Sheet1.tsv");
			//ExclusionExplorer.RunRandomExclusion("C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\Workplace\\TestData\\randomTest.tsv");


			Thread.CurrentThread.Join(2000); // waits x seconds for DataProcessor to finish
            WriterClass.CloseWriter();
            Console.WriteLine("Program finished");

			ExitProgram(0);
        }
		static void RunOptions(Options opts)
		{
			//handle options
			if (opts.paramsFile == null)
			{
				Console.WriteLine("MealTime-MS param file missing, use option -p to generate a template param file in your workplace directory");
				ExitProgram(3);
			}
			InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(opts.workPlaceDir) + "\\");
			GlobalVar.IsSimulation = opts.isSimulation;
			GlobalVar.ScansPerOutput = opts.scansPerOutput;
			SetNLogLevel(opts.logLevel);

			


		}
		static void PrintParameters(PrintParams pr)
		{
			String workDir = Path.GetFullPath(pr.workPlaceDir);
			Console.WriteLine("Writing template params file to: {0}", workDir);
			MealTimeMSParamsParser.WriteTemplateParamsFile(workDir);
			ExitProgram(2);
		}
		static void HandleParseError(IEnumerable<Error> errs)
		{
			//handle errors
			Console.WriteLine("Incorrect Usage Format, program will now exit");
			ExitProgram(3);
		}
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
