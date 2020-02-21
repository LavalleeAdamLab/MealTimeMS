using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Simulation
{

/* 
 * ExclusionExplorer class
 * This class is used to systematically permute over a range of different operating variables.
 * This will give us insight into which variables we want to use in our experiment.
 */
public class ExclusionExplorer
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /* VARIABLES TO ITERATE OVER */
        private static readonly List<Double> XCORR_THRESHOLD_LIST = Collections.unmodifiableList(Arrays.asList(2.5, 3.0, 3.5));
        private static readonly List<int> NUM_DB_THRESHOLD_LIST = Collections.unmodifiableList(Arrays.asList(2, 3, 4));
        private static readonly List<Double> PPM_TOLERANCE_LIST = new List<Double>().AddRange(((3.0 / 1000000), (5.0 / 1000000), (10.0 / 1000000)).ToTuple);
        private static readonly List<Double> RETENTION_TIME_WINDOW_LIST = Collections
                .unmodifiableList(Arrays.asList(.5, .75, 1.5, 2.0));
        private static readonly List<Double> PROBABILITY_THRESHOLD_LIST = Collections
                .unmodifiableList(Arrays.asList(0.1, 0.3, 0.5, 0.7, 0.9));
        private static readonly List<String> LOGISTIC_REGRESSION_CLASSIFIER_LIST = Collections.unmodifiableList(Arrays
                .asList("output/2019-07-11IdentificationLogisticRegression"));
        private static readonly List<int> RT_ALIGNMENT_WINDOW_SIZE_LIST = Collections
                .unmodifiableList(Arrays.asList(RetentionTimeUtil.DEFAULT_WINDOW_SIZE));
        // specifies the maximum number of MS2 allowed per MS1 scan
        private static readonly List<int> MAX_NUM_MS2_SPEC_PER_MS1_LIST = Collections
                .unmodifiableList(Arrays.asList(12));

        //
        // /* VARIABLES TO ITERATE OVER */
        // private static readonly List<Double> XCORR_THRESHOLD_LIST =
        // Collections.unmodifiableList(Arrays.asList(2.5, 3.0, 3.5));
        // private static readonly List<int> NUM_DB_THRESHOLD_LIST =
        // Collections.unmodifiableList(Arrays.asList(2, 3, 4));
        // private static readonly List<Double> PPM_TOLERANCE_LIST = Collections
        // .unmodifiableList(Arrays.asList((3.0 / 1000000), (5.0 / 1000000), (10.0 /
        // 1000000)));
        // private static readonly List<Double> RETENTION_TIME_WINDOW_LIST = Collections
        // .unmodifiableList(Arrays.asList(.5, .75, 1.5, 2.0));
        // // private static readonly List<Double> PROBABILITY_THRESHOLD_LIST =
        // // setUpProbabilityThresholdList(0.05); // do 5
        // private static readonly List<Double> PROBABILITY_THRESHOLD_LIST = Collections
        // .unmodifiableList(Arrays.asList(0.1, 0.3, 0.5, 0.7, 0.9));
        // private static readonly List<String> LOGISTIC_REGRESSION_CLASSIFIER_LIST =
        // Collections.unmodifiableList(Arrays
        // .asList("output/2019-03-25IdentificationLogisticRegression_NEW_NEGATIVE_SET"));
        // // private static readonly List<Double> PROBABILITY_THRESHOLD_LIST =
        // // Collections.unmodifiableList(Arrays.asList(0.85));
        // // private static readonly List<String> LOGISTIC_REGRESSION_CLASSIFIER_LIST =
        // // Collections.unmodifiableList(Arrays
        // //
        // .asList("output/2019-03-25IdentificationLogisticRegression_NEW_NEGATIVE_SET"));
        // private static readonly List<int> RT_ALIGNMENT_WINDOW_SIZE_LIST =
        // Collections
        // .unmodifiableList(Arrays.asList(RetentionTimeUtil.DEFAULT_WINDOW_SIZE));
        // // specifies the maximum number of MS2 allowed per MS1 scan
        // private static readonly List<int> MAX_NUM_MS2_SPEC_PER_MS1_LIST =
        // Collections
        // .unmodifiableList(Arrays.asList(5,6,12));
        // // if true, uses half of the maximum MS2 spectra, otherwise uses all of it

        private static List<LogisticRegressionModel> lrModels;
        /*
         * Parameters we have tested beforeache in
         * 	- xCorrThreshold: 2.0, 2.5, 3.0, 3.5
         *  - numDBThreshold: 2, 3, 4, 5
         *  - ppmTolerance: (3.0 / 1000000), (5.0 / 1000000), (10.0 / 1000000), (15.0 / 1000000), (20.0 / 1000000)
         *  - retentionTimeWindow (minutes): .25, .5, .75, 1.0, 1.5, 2.0
         *  - probabilityThreshold: 0.01, 0.02, ..., 0.99
         */

        // Schedules multiple simulations to run one after another
        private static ExperimentScheduler experimentScheduler;
        // Handles the input/output of the explorer class
        private static ExclusionExplorerIOHandler experimentIOHandler;

        private static readonly bool saveRetentionTimeAlignmentFile = true;

        /* Random Exclusion stuff */
        private static readonly bool computeRandomExcluded = true;
        private static int numRandomReplicates = 10;

        private static List<Spectra> spectraArray;
        private static readonly int NUM_MISSED_CLEAVAGES = 1;
        private static Database database;
        private static ResultDatabase resultDatabase;

        private static void addExperiments()
        {
            log.Info("Adding experiments to scheduler...");
            experimentScheduler = new ExperimentScheduler(database, resultDatabase);
            // add the no exclusion first
            foreach (int maxNumMS2PerMS1 in MAX_NUM_MS2_SPEC_PER_MS1_LIST)
            {
                experimentScheduler.addNoExclusionExperiment(0.0, 0.0, maxNumMS2PerMS1);
            }
            // add the rest of the experiments (besides random)
            foreach (int maxNumMS2PerMS1 in MAX_NUM_MS2_SPEC_PER_MS1_LIST)
            {
                //			experimentScheduler.addNoraExclusionExperiments(XCORR_THRESHOLD_LIST, NUM_DB_THRESHOLD_LIST,
                //					PPM_TOLERANCE_LIST, RETENTION_TIME_WINDOW_LIST, RT_ALIGNMENT_WINDOW_SIZE_LIST, maxNumMS2PerMS1);
                experimentScheduler.addMachineLearningExclusionExperiments(lrModels, LOGISTIC_REGRESSION_CLASSIFIER_LIST,
                        PROBABILITY_THRESHOLD_LIST, PPM_TOLERANCE_LIST, RETENTION_TIME_WINDOW_LIST,
                        RT_ALIGNMENT_WINDOW_SIZE_LIST, maxNumMS2PerMS1);
            }
            log.Info("Added " + experimentScheduler.getSize() + " experiments to scheduler.");
        }

        private static void addSingleExperiment()
        {
            experimentScheduler = new ExperimentScheduler(database, resultDatabase);
            experimentScheduler.addNoExclusionExperiment(0.0, 0.0, 6);
            experimentScheduler.addNoraExclusionExperiments(XCORR_THRESHOLD_LIST, NUM_DB_THRESHOLD_LIST, PPM_TOLERANCE_LIST,
                    RETENTION_TIME_WINDOW_LIST, RT_ALIGNMENT_WINDOW_SIZE_LIST, 6);
            System.out.println("2 experiments added");
            log.Info("Added " + experimentScheduler.getSize() + " experiments to scheduler.");
        }

        /*
         * Returns a list of probability thresholds to be tested, between 0.0 and 1.0
         * (non-inclusive) with the specified step size
         */
        private static List<Double> setUpProbabilityThresholdList(double stepSize)
        {
            List<Double> probabilityThresholdList = new List<Double>();

            double probabilityThreshold = stepSize;
            while (probabilityThreshold < 1.0)
            {
                probabilityThresholdList.Add(probabilityThreshold);
                probabilityThreshold += stepSize;
            }

            return Collections.unmodifiableList(probabilityThresholdList);
        }


        private static void setUp()
        {
            readonly long startTime = System.nanoTime();
            experimentIOHandler = new ExclusionExplorerIOHandler();

            log.Debug("Setting up database...");
            database = databaseSetUp(InputFileOrganizer.FASTA_FILE);
            log.Debug("Done setting up database.");

            log.Debug("Loading the result database...");
            resultDatabase = Loader.parseResultDatabase(InputFileOrganizer.MS_QC_120_RESULT_DATABASE_FILE);
            log.Debug("Done loading the result database.");

            log.Debug("Selecting spectra...");
            MZMLFile mzml = Loader.parseMZML(InputFileOrganizer.MS_QC_120_MZML_FILE);
            spectraArray = mzml.getSpectraArray();
            log.Debug("Done selecting spectra.");

            log.Debug("Loading logistic regression model");
            lrModels = new List<LogisticRegressionModel>();
            foreach (String logisitic_regression_file in LOGISTIC_REGRESSION_CLASSIFIER_LIST)
            {
                LogisticRegressionModel lrm = Loader.loadLogisticRegressionModel(logisitic_regression_file);
                lrModels.Add(lrm);
            }
            addExperiments();

            log.Info("Determining number of proteins identified in original experiment");
            String originalCometFile = InputFileOrganizer.MS_QC_120_ORIGINAL_COMET_FILE;
            ProteinProphetResult ppr = experimentIOHandler.postProcessing(originalCometFile, false);
            PerformanceEvaluator.setOriginalExperiment(ppr.getNum_proteins_identified());
            log.Info("Original Protein Prophet results from comet file: " + originalCometFile);
            log.Info("Originally identified " + ppr.getNum_proteins_identified() + " proteins...");
            System.out.println("Originally identified " + ppr.getNum_proteins_identified() + " proteins...");

            readonly double duration = (System.nanoTime() - startTime) / 1000000000.0;

            experimentIOHandler.createUsedScansFile(resultDatabase, database);
            experimentIOHandler.createAllUsedScansFile(spectraArray, resultDatabase);
            if (saveRetentionTimeAlignmentFile)
            {
                experimentIOHandler.createRTAlignmentFolder();
            }

            log.Info("Set up took " + duration + " seconds to set up");
        }

        private static Database databaseSetUp(String fasta_file_name)
        {
            FastaFile f = Loader.parseFasta(fasta_file_name);
            DigestedFastaFile df = PerformDigestion.performDigest(f, NUM_MISSED_CLEAVAGES);

            int numberOfProteinsInFasta = f.getAccessionToFullSequence().Count;
            int numberOfPeptidesInDigestedFasta = df.getDigestedPeptideArray().Count;

            log.Info("Fasta file: " + fasta_file_name);
            log.Info("Num missed cleavages: " + NUM_MISSED_CLEAVAGES);
            log.Info("Number of proteins: " + numberOfProteinsInFasta);
            log.Info("Number of peptides: " + numberOfPeptidesInDigestedFasta);

            log.Debug("Constructing graph...");
            Database g = new Database(f, df);
            log.Debug(g);

            return g;
        }

        private static void runExclusionExplore()
        {
            System.out.println("Starting ExclusionExplorer...");

            readonly long programStartTime = System.nanoTime();

            setUp();

            while (experimentScheduler.hasNextExperiment())
            {
                String experimentName = experimentScheduler.getNextExperimentInfo();
                ExclusionProfile exclusionProfile = experimentScheduler.getNextExperiment();
                runExperiment(exclusionProfile, experimentName);
            }

            readonly long programEndTime = System.nanoTime();
            double duration = (programEndTime - programStartTime) / 1000000000.0;
            System.out.println("ExclusionExplorer finished. Program took " + duration / 60.0 + " minutes.");

            System.out.println("Making figures...");
            experimentIOHandler.makeFigures();

        }

        private static void runExperiment(ExclusionProfile exclusionProfile, String experimentName)
        {
            // run the simulation
            readonly long startTime = System.nanoTime();
            exclusionProfile = Simulator.runSimulation(spectraArray, exclusionProfile);
            readonly long endTime = System.nanoTime();
            double analysisTime = (endTime - startTime) / 1000000000.0;
            // do post processing
            bool keepPostProcessingFiles = false;
            if ((exclusionProfile.getAnalysisType() != ExclusionProfileEnum.RANDOM_EXCLUSION_PROFILE
                    && exclusionProfile.getAnalysisType() != ExclusionProfileEnum.NO_EXCLUSION_PROFILE))
            {

                // keep the post processing files if it's not random or no exclusion.
                keepPostProcessingFiles = true;

                // add the randomly excluded simulations to see how well it performs
                // comparatively
                if (computeRandomExcluded)
                {
                    int numExcluded = (int)exclusionProfile.getPerformanceEvaluator().getValue("NumMS2Excluded");
                    int numAnalyzed = (int)exclusionProfile.getPerformanceEvaluator().getValue("NumMS2Analyzed");
                    experimentScheduler.addRandomExclusionExperiment(spectraArray, numExcluded, numAnalyzed,
                            numRandomReplicates, experimentName, Simulator.getMaxNumMS2PerMS1());
                }
            }
            // Perform post processing analysis
            ProteinProphetResult ppr = experimentIOHandler.postProcessing(exclusionProfile, experimentName,
                    keepPostProcessingFiles);
            // Extract values from no exclusion info to compare our results to...
            if (exclusionProfile.getAnalysisType() == ExclusionProfileEnum.NO_EXCLUSION_PROFILE)
            {
                int numMS2 = (int)exclusionProfile.getPerformanceEvaluator().getValue("NumMS2Analyzed");
                PerformanceEvaluator.setBaselineComparison(ppr, numMS2, Simulator.getMaxNumMS2PerMS1());
            }

            readonly long postProcessingEndTime = System.nanoTime();
            double totalRunTime = (postProcessingEndTime - startTime) / 1000000000.0;

            // write output on table
            String result = exclusionProfile.getPerformanceVector(experimentName,
                    exclusionProfile.getAnalysisType().getDescription(), analysisTime, totalRunTime, ppr,
                    Simulator.getMaxNumMS2PerMS1());
            experimentIOHandler.appendResult(result);
            experimentIOHandler.appendUsedScans(experimentName, exclusionProfile.getSpectraUsed(),
                    exclusionProfile.getUnusedSpectra());
            experimentIOHandler.appendAllUsedScans(experimentName, exclusionProfile.getSpectraUsed());

            // save retention time alignment file for post processing figures
            if (saveRetentionTimeAlignmentFile)
            {
                List<Double> scanTimes = RetentionTimeUtil.getScanTimes();
                List<Double> errors = RetentionTimeUtil.getErrorsList();
                List<Double> offsets = RetentionTimeUtil.getOffsetList();
                experimentIOHandler.writeRTAlignmentFile(experimentName, scanTimes, errors, offsets);
            }
        }

        public static void main(String[] args)
        {
            runExclusionExplore();
        }

    }

}
