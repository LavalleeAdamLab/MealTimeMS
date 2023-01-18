using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using MealTimeMS.Util;
using MealTimeMS.Data;
using MealTimeMS.Simulation;
using MealTimeMS.Data.Graph;
using MealTimeMS.IO;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.ExclusionProfiles.Heuristic;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
using MealTimeMS.ExclusionProfiles.TestProfile;
using MealTimeMS.Data.InputFiles;
using System.Threading;


namespace MealTimeMS.RunTime
{
    public class BrukerRuntimeCore
    {
        static Database database;
        public static void BrukerRuntimeCore_Main(bool isSimulation = false)
        {
            if (isSimulation)
            {
                SetUpResultTrackerParams();
            }
            PopulateHardCodedContentFilePaths();
            SetUpExperimentParameters();
            //RunResultTracker();
            //return;
            PreexperimentSetup();
            RunExperimentOrSimulation(isSimulation);
        }

        private static void RunExperimentOrSimulation(bool isSimulation)
        {
            ExclusionProfile exclusionProfile = CreateExclusionProfile();
            if (isSimulation)
            {
                Task MTMS = Task.Run(()=>RunExperiment(exclusionProfile, repeatListening: false) );
                int numSpectraAnalyzed = RunResultTracker();
                MTMS.Wait();
                //String proteinProphetResultFileName = "ProteinProphetResult";
                //ProteinProphetResult ppr = PostProcessingScripts.postProcessing(exclusionProfile, proteinProphetResultFileName, true);
                //String outputMssg = String.Format("Analyzed_scans\tIdentifiedProteins\tIdentifiedProteinGroups\n" +
                //    "{0}\t{1}\t{2}", numSpectraAnalyzed, ppr.getNum_proteins_identified(), ppr.getFilteredProteinGroups().Count);
                //WriterClass.QuickWrite(outputMssg, "BrukerAnalyzedScanTracker.txt");
                //Experiment e = new Experiment(exclusionProfile, GlobalVar.experimentName, 0, exclusionProfile.getAnalysisType());
                //ExclusionExplorer.WriteUsedSpectra(e);
            }
            else
            {
                RunExperiment(exclusionProfile, repeatListening: true);
            }
        }

        private static void SetUpExperimentParameters()
        {
            GlobalVar.isForBrukerRunTime = true;
            GlobalVar.includeIonMobility = false;
            GlobalVar.ppmTolerance = 10.0 / 1000000.0;
            GlobalVar.retentionTimeWindowSize = 0.6;
            //GlobalVar.IMWindowSize = imWin;
            GlobalVar.AccordThreshold = 0.95;
            GlobalVar.XCorr_Threshold = 3.5;
            GlobalVar.NumDBThreshold = 2;
            int experimentNumber = 1;



        }
        private static void PreexperimentSetup()
        {
            database = ExclusionExplorer.databaseSetUp(InputFileOrganizer.ExclusionDBFasta);
        }
        private static ExclusionProfile CreateExclusionProfile()
        {
            ExclusionProfile exclusionProfile = new CombinedExclusion(InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile,
                database, GlobalVar.ppmTolerance, GlobalVar.retentionTimeWindowSize,
                GlobalVar.XCorr_Threshold, GlobalVar.NumDBThreshold);
            //ExclusionProfile exclusionProfile = new HeuristicExclusion(database, GlobalVar.XCorr_Threshold,
            //     GlobalVar.ppmTolerance, GlobalVar.NumDBThreshold, GlobalVar.retentionTimeWindowSize);
            Console.WriteLine("Exclusion Profile:\n\t" + exclusionProfile.ToString());
            return exclusionProfile;
        }
        private static void RunExperiment(ExclusionProfile exclusionProfile, bool repeatListening)
        {
            while (true)
            {
                BrukerInstrumentConnection.ConnectRealTime(exclusionProfile);
                Thread.Sleep(3000);
                Console.Write("Resetting MealTimeMS exclusion environment");
                if (!repeatListening)
                    break;
                exclusionProfile = CreateExclusionProfile();
            }
            
        }

        private static void SetUpResultTrackerParams()
        {
            InputFileOrganizer.SetWorkDir(IOUtils.getAbsolutePath(@"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace") + "\\");
            WriterClass.ExperimentOutputSetUp();
            InputFileOrganizer.OriginalCometOutput = @"D:\CodingLavaleeAdamCDriveBackup\APIO\APIO_testData\20200821K562200ng90min_1_Slot1-1_1_1630.d\20200821K562200ng90min_1_Slot1-1_1_1630_nopd_replaced.pep.xml";
            InputFileOrganizer.BrukerdotDFolder = @"D:\CodingLavaleeAdamCDriveBackup\APIO\APIO_testData\20200821K562200ng90min_1_Slot1-1_1_1630.d\";
            InputFileOrganizer.ProlucidSQTFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\APIO_testData\20200821K562200ng90min_1_Slot1-1_1_1630.d\20200821K562200ng90min_1_Slot1-1_1_1630_nopd.sqt";
            InputFileOrganizer.MS2SimulationTestFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\APIO_testData\20200821K562200ng90min_1_Slot1-1_1_1630.d\20200821K562200ng90min_1_Slot1-1_1_1630_nopd.ms2";
        }
        
        private static int RunResultTracker()
        {
            ExclusionProfile includedSpectraTrackingProfile = new BrukerNoExclusion();
            BrukerInstrumentConnection.Connect(includedSpectraTrackingProfile, InputFileOrganizer.BrukerdotDFolder,
                InputFileOrganizer.ProlucidSQTFile, BrukerInstrumentConnection.BrukerConnectionEnum.MS2ConnectionOnly, startAcquisitionSimulator: true, _group_id: "recordMS2");
            int numSpectraAnalyzed =((BrukerNoExclusion)includedSpectraTrackingProfile).getAnalyzedSpectraCount();

            String proteinProphetResultFileName = "ProteinProphetResult";
            ProteinProphetResult ppr = PostProcessingScripts.postProcessing(includedSpectraTrackingProfile, proteinProphetResultFileName, true);
            String outputMssg = String.Format("Analyzed_scans\tIdentifiedProteins\tIdentifiedProteinGroups\n" +
                "{0}\t{1}\t{2}", numSpectraAnalyzed, ppr.getNum_proteins_identified(), ppr.getFilteredProteinGroups().Count);
            WriterClass.QuickWrite(outputMssg, "BrukerAnalyzedScanTracker.txt");
            Experiment e = new Experiment(includedSpectraTrackingProfile, GlobalVar.experimentName, 0, includedSpectraTrackingProfile.getAnalysisType());
            ExclusionExplorer.WriteUsedSpectra(e);
            return numSpectraAnalyzed;
        }
        private static void PopulateHardCodedContentFilePaths()
        {
            String TestDataDirectory = Path.Combine(InputFileOrganizer.AssemblyDirectory, "TestData");
            String datasetFolderName = "K562Lysate";
            InputFileOrganizer.ChainSawResult = Path.Combine(TestDataDirectory, datasetFolderName, "uniprot-R_20210810_UP000005640_human.fasta_digestedPeptides_2missedCleavage.tsv");
            InputFileOrganizer.RTCalcResult = Path.Combine(TestDataDirectory, datasetFolderName, "AutoRT_fastaPrediction_misCleaved2.tsv");
            InputFileOrganizer.ExclusionDBFasta = Path.Combine(TestDataDirectory, datasetFolderName, "uniprot-R_20210810_UP000005640_human.fasta");
            InputFileOrganizer.FASTA_FILE = Path.Combine(TestDataDirectory, datasetFolderName, "uniprot-R_20210810_UP000005640_human.fasta");
            InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile = Path.Combine(TestDataDirectory, datasetFolderName, "20200821K562300ng90min_1_Slot1-1_1_1638.d_extractedFeatures_positiveAndNonPositive.ClassifierCoefficient.txt");
        }

    }
}
