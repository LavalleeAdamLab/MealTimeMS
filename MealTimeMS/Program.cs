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

        public static bool isSimulation = true;

        private static bool isListening = true;
        
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
			// ReadLessLines.DoJob("C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\MealTimeMS_APITestRun\\Data\\60minMZMLTemp.csv", "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\MealTimeMS_APITestRun\\Data\\60minMZMLShrink.csv", 24000, 100);
			//Environment.Exit(0);
			//CometSingleSearchTester.CometSingleSearchTest();
			//Thread.Sleep(30000);
			//Console.ReadKey();
			//Environment.Exit(0);

			
			
			InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(args[0]) + "\\");

			GlobalVar.IsSimulation = bool.Parse(args[1]);
            GlobalVar.ScansPerOutput = int.Parse(args[2]);
           
			//Sets up the output directory and creates the output files
			//WriterClass responsible for writing the any output to a file
            WriterClass.ExperimentOutputSetUp();

			//parses other options, only used when hooked on to actual machine
            SetUpOptions(args);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			//Tester(args[0]);

			Console.WriteLine("Bring the system to On mode and/or start an acquisition to see results.");
			//ExclusionExplorer.SingleSimulationRun(ExclusionProfiles.ExclusionProfileEnum.NORA_EXCLUSION_PROFILE);
			//ExclusionExplorer.RunExclusionExplorer(ExclusionProfiles.ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE);
			ExclusionExplorer.SingleSimulationRun(ExclusionProfiles.ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE);
			//ExclusionExplorer.RunRealTimeExperiment();
			//ExclusionExplorer.RunRandomExclusion(InputFileOrganizer.ExperimentResultFile);
			

           
            
            
			//new EasyDataReceiver().DoJob();
			

			
            Thread.CurrentThread.Join(2000); // waits x seconds for DataProcessor to finish
            //InputHandler.StopRunning();
            WriterClass.CloseWriter();
            Console.WriteLine("Program finished");


            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

      

        public static void SetUpOptions(String[] args)
        {
            for(int i = 3; i < args.Length; i++)
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
        }




    }
}
