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
using MealTimeMS.Tester;
using MealTimeMS.ExclusionProfiles.Nora;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Simulation;

namespace MealTimeMS.RunTime
{
	class ExclusionExplorer
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		private static Database database;
		
		static List<Spectra> ms2SpectraList;
		static double startTime, analysisTime;

		////MLGE parameters:
		//static readonly List<double> PPM_TOLERANCE_LIST = new List<double>(new double[] { 10.0 / 1000000.0 });
		//static readonly List<double> RETENTION_TIME_WINDOW_LIST = new List<double>(new double[] { 0.75, 1.0, 2.0 });
		//static readonly List<double> LR_PROBABILITY_THRESHOLD_LIST = new List<double>(new double[] { 0.3, 0.5, 0.7, 0.9 });

		////Nora parameters:
		//static readonly List<double> XCORR_THRESHOLD_LIST = new List<double>(new double[] { 1.5, 2, 2.5 });
		////static readonly List<double> XCORR_THRESHOLD_LIST = new List<double>(new double[] {1.5, 2.0 });
		//static readonly List<int> NUM_DB_THRESHOLD_LIST = new List<int>(new int[] { 2 });

		public static void RunExclusionExplorer(ExclusionProfileEnum exclusionType)
		{
			

			PreExperimentSetUp();
			WriterClass.writeln(new PerformanceEvaluator().getHeader());
			int experimentNumber = 0;
#if DDA
			if (true)
#else
			if (exclusionType == ExclusionProfileEnum.NO_EXCLUSION_PROFILE)
#endif

			{
				startTime = getCurrentTime();
				GlobalVar.ppmTolerance = 0;
				GlobalVar.retentionTimeWindowSize = GlobalVar.RETENTION_TIME_WINDOW_LIST[0];

				ExclusionProfile exclusionProfile = new NoExclusion(database, GlobalVar.retentionTimeWindowSize);
				String experimentName = GlobalVar.experimentName + String.Format("_Baseline_NoExclusion:ppmTol_rtWin_{0}", GlobalVar.retentionTimeWindowSize);
				RunSimulationAndPostProcess(exclusionProfile, experimentName, startTime, 0);

#if !DDA
				if (exclusionType == ExclusionProfileEnum.NO_EXCLUSION_PROFILE)
				{
					List<ObservedPeptideRtTrackerObject> peptideIDRT = ((NoExclusion)exclusionProfile).peptideIDRT;
					WriterClass.writeln("peptideSequence\tObservedRetentionTime", writerClassOutputFile.peptideRTTime);
					foreach (ObservedPeptideRtTrackerObject observedPeptracker in peptideIDRT)
					{
						WriterClass.writeln(String.Format("{0}\t{1}", observedPeptracker.peptideSequence, observedPeptracker.arrivalTime), writerClassOutputFile.peptideRTTime);
					}
				}
#endif
			}

			if (exclusionType == ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE)
			{
				foreach (double ppmTol in GlobalVar.PPM_TOLERANCE_LIST)
					foreach (double rtWin in GlobalVar.RETENTION_TIME_WINDOW_LIST)
						foreach (double prThr in GlobalVar.LR_PROBABILITY_THRESHOLD_LIST)
						{
							experimentNumber++;
							startTime = getCurrentTime();
							GlobalVar.ppmTolerance = ppmTol;
							GlobalVar.retentionTimeWindowSize = rtWin;
							GlobalVar.AccordThreshold = prThr;

							ExclusionProfile exclusionProfile = new MachineLearningGuidedExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);
							String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_MachineLearningGuidedExclusion:ppmTol_{0}_rtWin_{1}_prThr_{2}", ppmTol, rtWin, prThr);
							RunSimulationAndPostProcess(exclusionProfile, experimentName, startTime, experimentNumber);
						}
			}
			else if (exclusionType == ExclusionProfileEnum.NORA_EXCLUSION_PROFILE)
			{
				foreach (double xCorr in GlobalVar.XCORR_THRESHOLD_LIST)
					foreach (int numDB in GlobalVar.NUM_DB_THRESHOLD_LIST)
						foreach (double ppmTol in GlobalVar.PPM_TOLERANCE_LIST)
							foreach (double rtWin in GlobalVar.RETENTION_TIME_WINDOW_LIST)
							{
								experimentNumber++;
								startTime = getCurrentTime();
								GlobalVar.ppmTolerance = ppmTol;
								GlobalVar.retentionTimeWindowSize = rtWin;
								GlobalVar.XCorr_Threshold = xCorr;
								GlobalVar.NumDBThreshold = numDB;

								ExclusionProfile exclusionProfile = new NoraExclusion(database, GlobalVar.XCorr_Threshold, GlobalVar.ppmTolerance, GlobalVar.NumDBThreshold, GlobalVar.retentionTimeWindowSize);
								String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_HeuristicExclusion:xCorr_{0}_numDB_{1}_ppmTol_{2}_rtWin_{3}", xCorr, numDB, ppmTol, rtWin) + "_expNum" + experimentNumber;
								RunSimulationAndPostProcess(exclusionProfile, experimentName, startTime, experimentNumber);
							}
			}
			else if (exclusionType == ExclusionProfileEnum.COMBINED_EXCLUSION)
			{
				foreach (double rtWin in GlobalVar.RETENTION_TIME_WINDOW_LIST)
					foreach (double prThr in GlobalVar.LR_PROBABILITY_THRESHOLD_LIST)
						foreach (int numDB in GlobalVar.NUM_DB_THRESHOLD_LIST)
							foreach (double xCorr in GlobalVar.XCORR_THRESHOLD_LIST)
								foreach (double ppmTol in GlobalVar.PPM_TOLERANCE_LIST)
								{
									experimentNumber++;
									startTime = getCurrentTime();
									GlobalVar.ppmTolerance = ppmTol;
									GlobalVar.retentionTimeWindowSize = rtWin;
									GlobalVar.AccordThreshold = prThr;
									GlobalVar.XCorr_Threshold = xCorr;
									GlobalVar.NumDBThreshold = numDB;

									ExclusionProfile exclusionProfile = new CombinedExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize, xCorr, numDB);
									String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_CombinedExclusion:xCorr_{0}_numDB_{1}_ppmTol_{2}_rtWin_{3}_prThr_{4}",
										xCorr, numDB, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize, GlobalVar.AccordThreshold);
									RunSimulationAndPostProcess(exclusionProfile, experimentName, startTime, experimentNumber);
								}
			}
			else if (exclusionType == ExclusionProfileEnum.SVMEXCLUSION)
			{
				foreach (double ppmTol in GlobalVar.PPM_TOLERANCE_LIST)
					foreach (double rtWin in GlobalVar.RETENTION_TIME_WINDOW_LIST)
					{
						experimentNumber++;
						startTime = getCurrentTime();
						GlobalVar.ppmTolerance = ppmTol;
						GlobalVar.retentionTimeWindowSize = rtWin;

						ExclusionProfile exclusionProfile = new SVMExclusion(InputFileOrganizer.SVMSavedFile, database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize);
						String experimentName = "EXP_" + experimentNumber + GlobalVar.experimentName + String.Format("_SVM:ppmTol_{0}_rtWin_{1}", ppmTol, rtWin);
						RunSimulationAndPostProcess(exclusionProfile, experimentName, startTime, experimentNumber);
					}
			}
			else if (exclusionType == ExclusionProfileEnum.RANDOM_EXCLUSION_PROFILE)
			{
				List<ExperimentResult> resultList = ParseExperimentResult(InputFileOrganizer.SummaryFileForRandomExclusion);
				foreach (ExperimentResult expResult in resultList)
				{

					//Do 5 random experiments per normal experiment
					for (int i = 0; i < 10; i++)
					{
						experimentNumber++;
						startTime = getCurrentTime();
						String originalExperimentName = expResult.experimentName;
						int numExcluded = expResult.numSpectraExcluded;
						int numAnalyzed = expResult.numSpectraAnalyzed;

						ExclusionProfile exclusionProfile = new RandomExclusion_Fast(database, ms2SpectraList, numExcluded, numAnalyzed, GlobalVar.ddaNum);
						String experimentName = "EXP_" + experimentNumber + String.Format("Random:originalExperiment_{0}", originalExperimentName);

						RunSimulationAndPostProcess(exclusionProfile, experimentName, startTime, experimentNumber);
					}

				}
			}
		}

		public static void RunSimulationAndPostProcess(ExclusionProfile exclusionProfile, String experimentName, double startTime, double experimentNumber)
		{
			Console.WriteLine("\nSimulating \"{0}\"",experimentName);
#if DDA
			new QuickDDAInstrumentSimulation(exclusionProfile, ms2SpectraList, GlobalVar.ddaNum);
#else
			new DataReceiverSimulation().DoJob(exclusionProfile, ms2SpectraList);
#endif

			analysisTime = getCurrentTime() - startTime;
			PostExperimentProcessing(exclusionProfile, experimentName, experimentNumber);
			


			exclusionProfile.reset();
			reset();
		}


		public static ExclusionProfile SingleSimulationRun(ExclusionProfileEnum expType)
		{
			PreExperimentSetUp();
			int experimentNumber = 1;
			startTime = getCurrentTime();

			//parameters:
			//GlobalVar.ppmTolerance = 5.0 / 1000000.0;
			//GlobalVar.retentionTimeWindowSize = 1.0;
			//GlobalVar.AccordThreshold = 0.5;
			//GlobalVar.XCorr_Threshold = 1.5;
			//GlobalVar.NumDBThreshold = 2;
			if (GlobalVar.isSimulationForFeatureExtraction == false)
			{
			GlobalVar.ppmTolerance = GlobalVar.PPM_TOLERANCE_LIST[0];
			GlobalVar.retentionTimeWindowSize = GlobalVar.RETENTION_TIME_WINDOW_LIST[0];
			GlobalVar.AccordThreshold = GlobalVar.LR_PROBABILITY_THRESHOLD_LIST[0];
			GlobalVar.XCorr_Threshold = GlobalVar.XCORR_THRESHOLD_LIST[0];
			GlobalVar.NumDBThreshold = GlobalVar.NUM_DB_THRESHOLD_LIST[0];

			}
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

					exclusionProfile = new RandomExclusion_Fast(database, ms2SpectraList, numExcluded, numAnalyzed, GlobalVar.ddaNum);

					break;
				case ExclusionProfileEnum.NO_EXCLUSION_PROFILE:
					exclusionProfile = new NoExclusion(database, GlobalVar.retentionTimeWindowSize);
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
			WriteExcludedProteinList(exclusionProfile.getDatabase().getExcludedProteins());

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
				List<ObservedPeptideRtTrackerObject> peptideIDRT = ((NoExclusion)exclusionProfile).peptideIDRT;

				//actual arrival time, xcorr, rtCalc predicted RT, corrected RT, offset
				WriterClass.writeln("pepSeq\tarrivalTime\txcorr\trtPeak\tcorrectedRT\toffset\trtCalcPredicted\tisPredicted1", writerClassOutputFile.peptideRTTime);
				foreach (ObservedPeptideRtTrackerObject observedPeptracker in peptideIDRT)
				{

					WriterClass.writeln(observedPeptracker.ToString(), writerClassOutputFile.peptideRTTime);
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
			PostExperimentProcessing(exclusionProfile, experimentName, experimentNumber);
			//WriteUnusedSpectra(exclusionProfile);

			return exclusionProfile;
		}

		//actual experiment hooked up to the mass spec
		public static void RunRealTimeExperiment()
		{
			PreExperimentSetUp();
			Console.WriteLine("Running real time experiment");

			startTime = getCurrentTime();
			GlobalVar.ppmTolerance = 5.0 / 1000000.0;
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
				WriterClass.writeln("ExclusionList size: " + exclusionProfile.getExclusionList().getExclusionList().Count);
				WriterClass.writeln("Number of exclusion: " + exclusionProfile.getUnusedSpectra().Count);
				WriterClass.writeln("Final rtoffset: " + RetentionTime.getRetentionTimeOffset());

			}
			catch (Exception e)
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
				for (int i = 0; i < 10; i++)
				{
					experimentNumber++;
					startTime = getCurrentTime();


					String originalExperimentName = expResult.experimentName;
					int numExcluded = expResult.numSpectraExcluded;
					int numAnalyzed = expResult.numSpectraAnalyzed;

					ExclusionProfile exclusionProfile = new RandomExclusion_Fast(database, ms2SpectraList, numExcluded, numAnalyzed, GlobalVar.ddaNum);
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
					PostExperimentProcessing(exclusionProfile, experimentName, experimentNumber);
					exclusionProfile.reset();
					reset();
				}

			}

		}

		//Parse Spectra usage information in past experiments for random exclusion
		public static List<ExperimentResult> ParseExperimentResult(params String[] resultFiles)
		{
			List<ExperimentResult> resultList = new List<ExperimentResult>();
			foreach (String resultFile in resultFiles)
			{
				
				StreamReader reader = new StreamReader(resultFile);
				String header = reader.ReadLine();
				while (!header.StartsWith("ExperimentName"))
				{
					header = reader.ReadLine();
				}

				String line = reader.ReadLine();
				while (line != null)
				{
					if (line.Equals("") || !line.Contains("\t"))
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
			ConstructDecoyFasta();
			ConstructIDX();
			if (GlobalVar.IsSimulation)
			{
				ms2SpectraList = Loader.parseMS2File(InputFileOrganizer.MS2SimulationTestFile).getSpectraArray();
			
				GlobalVar.ExperimentTotalMS2 = ms2SpectraList.Count;
				FullPepXMLAndProteinProphetSetup();
				

				//so in alex's original code, "original experiment" refers to original experiment without any exclusion or manipulation with this program
				//"baseline comparison" refers to the results after "NoExclusion" run, which is a top 6 or top 12 DDA run, which is not implemented in this program
				//So the two are the same in this program

				
			}
			log.Debug("Setting up Database");
			database = databaseSetUp(InputFileOrganizer.ExclusionDBFasta);
			log.Debug("Done setting up database.");
			
			
			CometSingleSearch.InitializeComet(InputFileOrganizer.IDXDataBase, InputFileOrganizer.CometParamsFile);
			CometSingleSearch.QualityCheck();
			Console.WriteLine("pre-experimental setup finished");

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


				}
					//protein prophet
					log.Info("Perform a protein prophet search on full pepxml");
					String fullProteinProphetFile = PostProcessingScripts.ProteinProphetSearch(InputFileOrganizer.OriginalCometOutput, InputFileOrganizer.OutputFolderOfTheRun, true);
					ExecuteShellCommand.MoveFile(fullProteinProphetFile, InputFileOrganizer.preExperimentFilesFolder);
					InputFileOrganizer.OriginalProtXMLFile = Path.Combine(InputFileOrganizer.preExperimentFilesFolder, IOUtils.getBaseName(IOUtils.getBaseName(fullProteinProphetFile)) + ".prot.xml");
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
			Database g;
			if (GlobalVar.isSimulationForFeatureExtraction == true)
			{
				g = new Database(f, df, true,false);
			}
			else
			{
				g = new Database(f, df,true,true);
			}
			log.Debug(g);

			return g;
		}

		private static void PostExperimentProcessing(ExclusionProfile exclusionProfile, String experimentName, double experimentNumber)
		{


			//WriterClass.writeln(exclusionProfile.ReportFailedCometSearchStatistics());
			WriterClass.Flush();

			if (GlobalVar.IsSimulation)
			{
				ProteinProphetResult ppr;
				if (GlobalVar.isSimulationForFeatureExtraction)
				{
					ppr = ProteinProphetEvaluator.getProteinProphetResult(InputFileOrganizer.OriginalProtXMLFile);
				}
				else
				{
					ppr = PostProcessingScripts.postProcessing(exclusionProfile, GlobalVar.experimentName, true);	

				}

#if DDA
				if (exclusionProfile.getAnalysisType() == ExclusionProfileEnum.NO_EXCLUSION_PROFILE)
				{
					ProteinProphetResult baseLinePpr = ppr;
					int numMS2Analyzed = (int) exclusionProfile.GetPerformanceEvaluator().getValue(Header.NumMS2Analyzed);
					PerformanceEvaluator.setBaselineComparison(baseLinePpr, numMS2Analyzed, GlobalVar.ddaNum);
					PerformanceEvaluator.setOriginalExperiment(baseLinePpr.getNum_proteins_identified());
				}
#else
				if (exclusionProfile.getAnalysisType() == ExclusionProfileEnum.NO_EXCLUSION_PROFILE)
				{
					ProteinProphetResult baseLinePpr = ProteinProphetEvaluator.getProteinProphetResult(InputFileOrganizer.OriginalProtXMLFile);
					int numMS2Analyzed = (int)GlobalVar.ExperimentTotalMS2;
					PerformanceEvaluator.setBaselineComparison(baseLinePpr, numMS2Analyzed, GlobalVar.ddaNum);
					PerformanceEvaluator.setOriginalExperiment(baseLinePpr.getNum_proteins_identified());
				}
#endif

				double totalTime = getCurrentTime() - startTime;
				String result = exclusionProfile.getPerformanceVector(experimentName, exclusionProfile.getAnalysisType().getDescription()
					, analysisTime, totalTime, ppr, GlobalVar.ddaNum, exclusionProfile);
				Console.WriteLine(result);
				WriterClass.writeln(result);
				//ExtractNumberOfPeptidePerIdentifiedProtein.DoJob(ppr, exclusionProfile, experimentNumber);

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
				WriterClass.writeln(scanInd + "", writerClassOutputFile.ExcludedSpectraScanNum);
			}
		}
		private static void WriteScanArrivalProcessedTime(List<double[]> list)
		{

			String header = "ScanNum\tArrival\tProcessed";
			WriterClass.writeln(header, writerClassOutputFile.scanArrivalAndProcessedTime);
			foreach (double[] scan in list)
			{
				String data = "";
				foreach (double d in scan)
				{
					data = data + d + "\t";
				}
				WriterClass.writeln(data, writerClassOutputFile.scanArrivalAndProcessedTime);
			}

		}

		private static void WriteExcludedProteinList(List<string> excludedProteins)
		{
			String outputFile = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,"ExcludedProteinList.txt");
			StreamWriter sw = new StreamWriter(outputFile);
			foreach(String prot in excludedProteins)
			{
				sw.WriteLine(prot);
			}
			sw.Close();
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
