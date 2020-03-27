using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.IO;
using MealTimeMS.Util;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
using MealTimeMS.ExclusionProfiles.TestProfile;
using MealTimeMS.ExclusionProfiles.Nora;
using MealTimeMS.ExclusionProfiles;

namespace MealTimeMS.RunTime
{
	class ExclusionExplorer
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		private static Database database;
		private static ProteinProphetResult baseLinePpr;
		static List<Spectra> ms2SpectraList;
		static double startTime,analysisTime;

		//MLGE parameters:
		static readonly List<double> PPM_TOLERANCE_LIST = new List<double>(new double[]{10.0 / 1000000.0 });
		static readonly List<double> RETENTION_TIME_WINDOW_LIST = new List<double>(new double[] {0.75, 1.0, 2.0});
		static readonly List<double> PROBABILITY_THRESHOLD_LIST = new List<double>(new double[] {0.3,0.5,0.7,0.9});
		

		//Nora parameters:
		static readonly List<double> XCORR_THRESHOLD_LIST = new List<double>(new double[] {1.5, 2, 2.5});
		//static readonly List<double> XCORR_THRESHOLD_LIST = new List<double>(new double[] {1.5, 2.0 });
		static readonly List<int> NUM_DB_THRESHOLD_LIST = new List<int>(new int[] {2});

		public static void RunExclusionExplorer(ExclusionProfileEnum exclusionType)
		{
			PreExperimentSetUp();
			int experimentNumber = 0;

			if(exclusionType == ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE)
			{
				foreach (double ppmTol in PPM_TOLERANCE_LIST)
					foreach (double rtWin in RETENTION_TIME_WINDOW_LIST)
						foreach (double prThr in PROBABILITY_THRESHOLD_LIST)
						{
							experimentNumber++;
							startTime = getCurrentTime();
							GlobalVar.ppmTolerance = ppmTol;
							GlobalVar.retentionTimeWindowSize = rtWin;
							GlobalVar.AccordThreshold = prThr;


							ExclusionProfile exclusionProfile = new MachineLearningGuidedExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);
							//ExclusionProfile exclusionProfile = new NoraExclusion(database, GlobalVar.XCorr_Threshold, GlobalVar.ppmTolerance, GlobalVar.NumDBThreshold, GlobalVar.retentionTimeWindowSize);
							//ExclusionProfile exclusionProfile = new RandomExclusion( database, ms2SpectraList, 9871, 27636, 12);
							if (experimentNumber == 1)
							{
								WriterClass.writeln(exclusionProfile.GetPerformanceEvaluator().getHeader());
							}
							String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_MLGE:ppmTol_{0}_rtWin_{1}_prThr_{2}", ppmTol, rtWin, prThr);

							if (GlobalVar.IsSimulation)
							{
								new DataReceiverSimulation().DoJob(exclusionProfile, ms2SpectraList);
							}
							else
							{
								new DataReceiver().DoJob(exclusionProfile);
							}
							analysisTime = getCurrentTime() - startTime;
							PostExperimentProcessing(exclusionProfile, experimentName);
							exclusionProfile.reset();
							reset();
						}

			}
			else if (exclusionType == ExclusionProfileEnum.NORA_EXCLUSION_PROFILE)
			{
				foreach (double xCorr in XCORR_THRESHOLD_LIST)
					foreach (int numDB in NUM_DB_THRESHOLD_LIST)
						foreach (double ppmTol in PPM_TOLERANCE_LIST)
							foreach (double rtWin in RETENTION_TIME_WINDOW_LIST)
							{
								experimentNumber++;
								startTime = getCurrentTime();
								GlobalVar.ppmTolerance = ppmTol;
								GlobalVar.retentionTimeWindowSize = rtWin;
								GlobalVar.XCorr_Threshold = xCorr;
								GlobalVar.NumDBThreshold = numDB;


								ExclusionProfile exclusionProfile = new NoraExclusion(database, GlobalVar.XCorr_Threshold, GlobalVar.ppmTolerance, GlobalVar.NumDBThreshold, GlobalVar.retentionTimeWindowSize);

								if (experimentNumber == 1)
								{
									WriterClass.writeln(exclusionProfile.GetPerformanceEvaluator().getHeader());
								}
								String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_Nora:xCorr_{0}_numDB_{1}_ppmTol_{2}_rtWin_{3}", xCorr, numDB, ppmTol, rtWin) + "_expNum" + experimentNumber;

								if (GlobalVar.IsSimulation)
								{
									new DataReceiverSimulation().DoJob(exclusionProfile, ms2SpectraList);
								}
								else
								{
									new DataReceiver().DoJob(exclusionProfile);
								}
								analysisTime = getCurrentTime() - startTime;
								PostExperimentProcessing(exclusionProfile, experimentName);
								exclusionProfile.reset();
								reset();
							}
			}else if(exclusionType == ExclusionProfileEnum.COMBINED_EXCLUSION)
			{
				foreach (double rtWin in RETENTION_TIME_WINDOW_LIST)
					foreach (double prThr in PROBABILITY_THRESHOLD_LIST)
						foreach (int numDB in NUM_DB_THRESHOLD_LIST)
					foreach ( double xCorr in XCORR_THRESHOLD_LIST)
								foreach (double ppmTol in PPM_TOLERANCE_LIST)
								{
							experimentNumber++;
							startTime = getCurrentTime();
									GlobalVar.ppmTolerance = ppmTol;
									GlobalVar.retentionTimeWindowSize = rtWin;

						GlobalVar.AccordThreshold =prThr;

						GlobalVar.XCorr_Threshold = xCorr;
						GlobalVar.NumDBThreshold = numDB;


						ExclusionProfile exclusionProfile = new CombinedExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize, xCorr,numDB);
							//ExclusionProfile exclusionProfile = new NoraExclusion(database, GlobalVar.XCorr_Threshold, GlobalVar.ppmTolerance, GlobalVar.NumDBThreshold, GlobalVar.retentionTimeWindowSize);
							//ExclusionProfile exclusionProfile = new RandomExclusion( database, ms2SpectraList, 9871, 27636, 12);
							if (experimentNumber == 1)
							{
								WriterClass.writeln(exclusionProfile.GetPerformanceEvaluator().getHeader());
							}
							String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_Combined:xCorr_{0}_numDB_{1}_ppmTol_{2}_rtWin_{3}_prThr_{4}", xCorr,numDB, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize, GlobalVar.AccordThreshold);

							if (GlobalVar.IsSimulation)
							{
								new DataReceiverSimulation().DoJob(exclusionProfile, ms2SpectraList);
							}
							else
							{
								new DataReceiver().DoJob(exclusionProfile);
							}
							analysisTime = getCurrentTime() - startTime;
							PostExperimentProcessing(exclusionProfile, experimentName);
							exclusionProfile.reset();
							reset();
						}
			}else if (exclusionType == ExclusionProfileEnum.SVMEXCLUSION)
			{
				foreach (double ppmTol in PPM_TOLERANCE_LIST)
					foreach (double rtWin in RETENTION_TIME_WINDOW_LIST)
						{
							experimentNumber++;
							startTime = getCurrentTime();
							GlobalVar.ppmTolerance = ppmTol;
							GlobalVar.retentionTimeWindowSize = rtWin;


							ExclusionProfile exclusionProfile = new SVMExclusion(InputFileOrganizer.SVMSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);
						if (experimentNumber == 1)
							{
								WriterClass.writeln(exclusionProfile.GetPerformanceEvaluator().getHeader());
							}
							String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_SVM:ppmTol_{0}_rtWin_{1}", ppmTol, rtWin);

							if (GlobalVar.IsSimulation)
							{
								new DataReceiverSimulation().DoJob(exclusionProfile, ms2SpectraList);
							}
							else
							{
								new DataReceiver().DoJob(exclusionProfile);
							}
							analysisTime = getCurrentTime() - startTime;
							PostExperimentProcessing(exclusionProfile, experimentName);
							exclusionProfile.reset();
							reset();
						}

			}


		}
		
		public static ExclusionProfile SingleSimulationRun(ExclusionProfileEnum expType)
		{
			PreExperimentSetUp();
			int experimentNumber = 1;
			startTime = getCurrentTime();
			
			//parameters:
			GlobalVar.ppmTolerance = 5.0 / 1000000.0;
			GlobalVar.retentionTimeWindowSize = 1.0;

			GlobalVar.AccordThreshold = 0.5;

			GlobalVar.XCorr_Threshold = 1.5;
			GlobalVar.NumDBThreshold = 2;
			//random
			int numExcluded = 14826;
			int numAnalyzed = 22681;

			//end parameters

			ExclusionProfile exclusionProfile = null;
			switch (expType)
			{
				case ExclusionProfileEnum.NORA_EXCLUSION_PROFILE:
					exclusionProfile = new NoraExclusion(database, GlobalVar.XCorr_Threshold, GlobalVar.ppmTolerance, GlobalVar.NumDBThreshold, GlobalVar.retentionTimeWindowSize);
					break;
				case ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE:
					exclusionProfile = new MachineLearningGuidedExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);
					break;

				case ExclusionProfileEnum.RANDOM_EXCLUSION_PROFILE:
					
					exclusionProfile = new RandomExclusion_Fast(database, ms2SpectraList, numExcluded, numAnalyzed, 12);

					break;
				case ExclusionProfileEnum.NO_EXCLUSION_PROFILE:
					exclusionProfile = new NoExclusion(database,GlobalVar.retentionTimeWindowSize);
					break;
				case ExclusionProfileEnum.MLGE_SEQUENCE_EXCLUSION_PROFILE:
					exclusionProfile = new MLGESequenceExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);
					break;

				case ExclusionProfileEnum.NORA_SEQUENCE_EXCLUSION_PROFILE:
					exclusionProfile = new NoraSequenceExclusion(database, GlobalVar.XCorr_Threshold, GlobalVar.ppmTolerance, GlobalVar.NumDBThreshold, GlobalVar.retentionTimeWindowSize);
					break;
				case ExclusionProfileEnum.SVMEXCLUSION:
					exclusionProfile = new SVMExclusion(InputFileOrganizer.SVMSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);
					break;
			}

			WriterClass.writeln(exclusionProfile.GetPerformanceEvaluator().getHeader());
			String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName;
			new DataReceiverSimulation().DoJob(exclusionProfile, ms2SpectraList);
			analysisTime = getCurrentTime() - startTime;

			WriteScanArrivalProcessedTime(DataProcessor.scanArrivalAndProcessedTimeList);

#if IGNORE
			WriteScanArrivalProcessedTime(DataProcessor.spectraNotAdded);
			
			foreach(double[] ignoredSpectra in DataProcessor.spectraNotAdded)
			{
				int scanNum = ms2SpectraList[(int)ignoredSpectra[0]-1].getScanNum();
				exclusionProfile.getSpectraUsed().Add(scanNum);
			}
#endif   

			if (expType == ExclusionProfileEnum.NO_EXCLUSION_PROFILE)
			{
				List<double[]> peptideIDRT = ((NoExclusion)exclusionProfile).peptideIDRT;
				
				//actual arrival time, xcorr, rtCalc predicted RT, corrected RT, offset
				WriterClass.writeln("arrivalTime\txcorr\trtPeak\tcorrectedRT\toffset\trtCalcPredicted\tisPredicted1", writerClassOutputFile.peptideRTTime);
				foreach(double[] id in peptideIDRT)
				{
					String str = "";
					foreach(double d in id)
					{
						str = str + "\t" + d;
					}
					str= str.Trim();
					WriterClass.writeln(str, writerClassOutputFile.peptideRTTime);
				}
			}
			//if (expType == ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE)
			//{
			//	List<double[]> peptideIDRT = ((MachineLearningGuidedExclusion)exclusionProfile).peptideIDRT;
				
			//	//actual arrival time, xcorr, rtCalc predicted RT, corrected RT, offset
			//	WriterClass.writeln("arrivalTime\txcorr\trtPeak\tcorrectedRT\toffset\trtCalcPredicted\tisPredicted1", writerClassOutputFile.peptideRTTime);
			//	foreach(double[] id in peptideIDRT)
			//	{
			//		String str = "";
			//		foreach(double d in id)
			//		{
			//			str = str + "\t" + d;
			//		}
			//		str= str.Trim();
			//		WriterClass.writeln(str, writerClassOutputFile.peptideRTTime);
			//	}
			//}

#if TRACKEXCLUDEDPROTEINFEATURE
			if(expType == ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE)
			{
				List<object[]> excludedProteinFeatures= ((MachineLearningGuidedExclusion)exclusionProfile).excludedProteinFeatureList;
				WriterClass.writeln("Accession\tCardinality\tHighestXCorr\tMeanXCorr\tMedianXCorr\tStDev", writerClassOutputFile.ExcludedSpectraScanNum);
				foreach (object[] feature in excludedProteinFeatures)
				{
					String featureStr = "";
					foreach(object o in feature)
					{
						featureStr = featureStr + o.ToString()+ "\t";
					}
					featureStr = featureStr.Trim();
					WriterClass.writeln(featureStr, writerClassOutputFile.ExcludedSpectraScanNum);
					
				}
			}
#endif
			WriteUnusedSpectra(exclusionProfile);
			WriteUsedSpectra(exclusionProfile);
			PostExperimentProcessing(exclusionProfile, experimentName);
			//WriteUnusedSpectra(exclusionProfile);

			return exclusionProfile;
		}
		
		//actual experiment hooked up to the mass spec
		public static void RunRealTimeExperiment()
		{
			PreExperimentSetUp();
			Console.WriteLine("Running real time experiment");

			startTime = getCurrentTime();
			GlobalVar.ppmTolerance = 5.0/1000000.0;
			GlobalVar.retentionTimeWindowSize = 1.0;
			GlobalVar.AccordThreshold = 0.5;

			Console.WriteLine("Creating exclusion profile");
			ExclusionProfile exclusionProfile = new MachineLearningGuidedExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);

			
			
			String experimentName = GlobalVar.experimentName + String.Format("_MLGE:ppmTol_{0}_rtWin_{1}_prThr_{2}", GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize, GlobalVar.AccordThreshold);

			
			new DataReceiver().DoJob(exclusionProfile);
			
			analysisTime = getCurrentTime() - startTime;
			try
			{
				WriteUnusedSpectra(exclusionProfile);
				WriteScanArrivalProcessedTime(DataProcessor.scanArrivalAndProcessedTimeList);
				WriteScanArrivalProcessedTime(DataProcessor.spectraNotAdded);
				WriteUsedSpectra(exclusionProfile);
				WriterClass.writeln("ExclusionList size: "+exclusionProfile.getExclusionList().getExclusionList().Count);
				WriterClass.writeln("Number of exclusion: "+exclusionProfile.getUnusedSpectra().Count);
				WriterClass.writeln("Final rtoffset: "+RetentionTime.getRetentionTimeOffset());

			}
			catch(Exception e)
			{
				Console.WriteLine("Writing exception catched at end of experiment");
			}
			finally
			{
				WriterClass.CloseWriter();
			}
			exclusionProfile.reset();
			reset();
		}

		public static void RunRandomExclusion(String usedResourcesFile)
		{
			PreExperimentSetUp();
			int experimentNumber = 0;
			List<ExperimentResult> resultList = ParseExperimentResult(usedResourcesFile);


			foreach (ExperimentResult expResult in resultList)
			{

				//Do 5 random experiments per normal experiment
				for(int i = 0 ; i < 10; i++)
				{
					experimentNumber++;
					startTime = getCurrentTime();


					String originalExperimentName = expResult.experimentName;
					int numExcluded = expResult.numSpectraExcluded;
					int numAnalyzed = expResult.numSpectraAnalyzed;

					ExclusionProfile exclusionProfile = new RandomExclusion_Fast(database, ms2SpectraList, numExcluded, numAnalyzed, 12);
					if (experimentNumber == 1)
					{
						WriterClass.writeln(exclusionProfile.GetPerformanceEvaluator().getHeader());
					}
					String experimentName = "EXP_" + experimentNumber + String.Format("Random:originalExperiment_{0}", originalExperimentName);

					if (GlobalVar.IsSimulation)
					{
						new DataReceiverSimulation().DoJob(exclusionProfile, ms2SpectraList);
					}
					else
					{
						new DataReceiver().DoJob(exclusionProfile);
					}
					analysisTime = getCurrentTime() - startTime;
					PostExperimentProcessing(exclusionProfile, experimentName);
					exclusionProfile.reset();
					reset();
				}
				
			}

		}

		//Parse Spectra usage information in past experiments for random exclusion
		public static List<ExperimentResult> ParseExperimentResult(params String[] resultFiles)
		{
			List<ExperimentResult> resultList = new List<ExperimentResult>();
			foreach(String resultFile in resultFiles)
			{
				StreamReader reader = new StreamReader(resultFile);
				String header= reader.ReadLine();
				while (!header.StartsWith("ExperimentName")){
					header = reader.ReadLine();
				}
				 
				String line = reader.ReadLine();
				while(line != null)
				{
					if (line.Equals("")|| !line.Contains("\t"))
					{
						line = reader.ReadLine();
						continue;
					}
					ExperimentResult expResult = new ExperimentResult(line, header);
					resultList.Add(expResult);
					line = reader.ReadLine();
				}
				reader.Close();
			}
			return resultList;

		}

		static void PreExperimentSetUp()
		{
			
			if (GlobalVar.IsSimulation)
			{
				ms2SpectraList = Loader.parseMS2File(InputFileOrganizer.MS2SimulationTestFile).getSpectraArray();
				GlobalVar.ExperimentTotalScans = ms2SpectraList.Count;
				FullPepXMLAndProteinProphetSetup();
				baseLinePpr = ProteinProphetEvaluator.getProteinProphetResult(InputFileOrganizer.ProtXML);

				//so in alex's original code, "original experiment" refers to original experiment without any exclusion or manipulation with this program
				//"baseline comparison" refers to the results after "NoExclusion" run, which is a top 6 or top 12 DDA run, which is not implemented in this program
				//So the two are the same in thie program
				
				int numMS2Analyzed = (int)GlobalVar.ExperimentTotalScans;
				PerformanceEvaluator.setBaselineComparison(baseLinePpr, numMS2Analyzed, 12);
				PerformanceEvaluator.setOriginalExperiment(baseLinePpr.getNum_proteins_identified());

				
			}
			log.Debug("Setting up Database");
			database = databaseSetUp(InputFileOrganizer.dbFasta);
			log.Debug("Done setting up database.");
			ConstructDecoyFasta();
			ConstructIDX();
			CometSingleSearch.InitializeComet(InputFileOrganizer.IDXDataBase, InputFileOrganizer.CometParamsFile);
			CometSingleSearch.QualityCheck();
			Console.WriteLine("preexperimental setup finishes");

		}
		private static void FullPepXMLAndProteinProphetSetup()
		{
			if (GlobalVar.IsSimulation)
			{

				if (!GlobalVar.usePepXMLComputedFile)
				{
					//comet
					log.Info("Performing Comet search on full ms2 data");
					String fullCometFile = PostProcessingScripts.CometStandardSearch(InputFileOrganizer.MS2SimulationTestFile, InputFileOrganizer.preExperimentFilesFolder, true);
					InputFileOrganizer.OriginalCometOutput = fullCometFile;


					//protein prophet
					log.Info("Perform a protein prophet search on full pepxml");
					String fullProteinProphetFile = PostProcessingScripts.ProteinProphetSearch(fullCometFile, InputFileOrganizer.OutputFolderOfTheRun, true);
					ExecuteShellCommand.MoveFile(fullProteinProphetFile, InputFileOrganizer.preExperimentFilesFolder);
					InputFileOrganizer.ProtXML = Path.Combine(InputFileOrganizer.preExperimentFilesFolder, IOUtils.getBaseName(IOUtils.getBaseName(fullProteinProphetFile)) + ".prot.xml");
				}
			}
		}

		private static void ConstructDecoyFasta()
		{
			if (!GlobalVar.useDecoyFastaComputedFile)
			{
				log.Debug("Generating decoy database for comet search validation");
				InputFileOrganizer.DecoyFasta = DecoyConcacenatedDatabaseGenerator.GenerateConcacenatedDecoyFasta(InputFileOrganizer.FASTA_FILE, InputFileOrganizer.preExperimentFilesFolder);
				log.Debug("Concacenated decoy database generated.");
			}
		}

		//Construct IDX file required for real time comet
		private static void ConstructIDX()
		{
			
			if (!GlobalVar.useIDXComputedFile)
			{
				log.Debug("Converting concacenated decoy database to idx file.");
				String idxDB = CommandLineProcessingUtil.FastaToIDXConverter(InputFileOrganizer.DecoyFasta, InputFileOrganizer.preExperimentFilesFolder);
				InputFileOrganizer.IDXDataBase = idxDB;
				log.Debug("idx Database generated");
				GlobalVar.useIDXComputedFile = true;
			}
		}

		private static Database databaseSetUp(String fasta_file_name)
		{
			FastaFile f = Loader.parseFasta(fasta_file_name);
			DigestedFastaFile df = PerformDigestion.performDigest(f, GlobalVar.NUM_MISSED_CLEAVAGES);

			int numberOfProteinsInFasta = f.getAccessionToFullSequence().Count;
			int numberOfPeptidesInDigestedFasta = df.getDigestedPeptideArray().Count;

			log.Info("Fasta file: " + fasta_file_name);
			log.Info("Num missed cleavages: " + GlobalVar.NUM_MISSED_CLEAVAGES);
			log.Info("Number of proteins: " + numberOfProteinsInFasta);
			log.Info("Number of peptides: " + numberOfPeptidesInDigestedFasta);

			log.Debug("Constructing graph...");
			Database g = new Database(f, df);
			log.Debug(g);

			return g;
		}

		private static void PostExperimentProcessing(ExclusionProfile exclusionProfile, String experimentName)
		{


			//WriterClass.writeln(exclusionProfile.ReportFailedCometSearchStatistics());
			WriterClass.Flush();
			
			if (GlobalVar.IsSimulation)
			{
				ProteinProphetResult ppr = PostProcessingScripts.postProcessing(exclusionProfile, GlobalVar.experimentName, true);
				
				
				

				double totalTime = getCurrentTime() - startTime;
				String result = exclusionProfile.getPerformanceVector(experimentName, exclusionProfile.getAnalysisType().getDescription()
					, analysisTime, totalTime, ppr, 12);
				Console.WriteLine(result);
				WriterClass.writeln(result);
			}
			else
			{
				WriterClass.writeln(exclusionProfile.GetPerformanceEvaluator().outputPerformance());
			}

			
			
			

		}
		private static void WriteUsedSpectra(ExclusionProfile exclusionProfile)
		{
			foreach (int scanInd in exclusionProfile.getSpectraUsed())
			{
				WriterClass.writeln(scanInd + "", writerClassOutputFile.IncludedSpectraScanNum);
			}
		}
		private static void WriteUnusedSpectra(ExclusionProfile exclusionProfile)
		{
			foreach (int scanInd in exclusionProfile.getUnusedSpectra())
			{
				WriterClass.writeln(scanInd + "",writerClassOutputFile.ExcludedSpectraScanNum);
			}
		}
		private static void WriteScanArrivalProcessedTime(List<double[]> list)
		{
			
			String header = "ScanNum\tArrival\tProcessed";
			WriterClass.writeln(header, writerClassOutputFile.scanArrivalAndProcessedTime);
			foreach (double[] scan in list)
			{
				String data = "";
				foreach(double d in scan)
				{
					data = data+ d + "\t";
				}
				WriterClass.writeln(data,writerClassOutputFile.scanArrivalAndProcessedTime);
			}

		}

		private static double getCurrentTime()
		{
			//reports current tick in seconds
			long nano = DateTime.Now.Ticks;
			nano /= TimeSpan.TicksPerMillisecond;
			return nano / 1000;
		}

		public static bool isListening = true;
		
		public static void EndMealTimeMS()
		{
			isListening = false;
		}
		public static bool IsListening()
		{
			return isListening;
		}
		public static void reset()
		{
			isListening = true;
			CometSingleSearch.reset();
		}

	}
}
