using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data;
using MealTimeMS.Util;
using MealTimeMS.Tester;
using MealTimeMS.ExclusionProfiles.TestProfile;
namespace MealTimeMS.ExclusionProfiles
{

    //  This is the class where all Alex's program is going on
    //  This is the base class which MachineLearningGuidedExclusion class implements
    //  It contains an ExclusionList object that keeps track of the stuff being excluded
    //  After DataProcessor class parses an incoming IMsScan into a Spectra object, this class processes the Spectra object in its evaluate() method - which is 
    //      the entry point to Alex's program
    public abstract class ExclusionProfile
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        protected readonly static double DEFAULT_PPMTOLERANCE = (5.0 / 1000000.0);

        public String ExperimentName;
        public double ExperimentNumber;
        protected PerformanceEvaluator performanceEvaluator;
        protected ExclusionList exclusionList;
        protected Database database;


        protected List<int> includedSpectra;
        protected List<int> excludedSpectra;

        protected double currentTime;

        public ExclusionProfile(Database _database, double _ppmTolerance)
        {
            database = _database;

            //exclusionList = new ExclusionList(_ppmTolerance);
            //exclusionList = new SimplifiedExclusionList_Key(_ppmTolerance);
            if (GlobalVar.isForBrukerRunTime)
            {
                exclusionList = new ExclusionMSWrapper_ExclusionList(_ppmTolerance, GlobalVar.exclusionMS_url);
            }
            else
            {
                exclusionList = new SimplifiedExclusionList_IM2(_ppmTolerance);
            }
            currentTime = 0.0;
            includedSpectra = new List<int>();
            excludedSpectra = new List<int>();
            reset();
        }

        public ExclusionProfile(Database _database) : this(_database, DEFAULT_PPMTOLERANCE)
        {

        }

        public double getPPMTolerance()
        {
            return exclusionList.getPPMTolerance();
        }

        public double getRetentionTimeWindow()
        {
            return database.getRetentionTimeWindow();
        }


        public Database getDatabase()
        {
            return database;
        }

        public ExclusionList getExclusionList()
        {
            return exclusionList;
        }

        public void setPPMTolerance(double _ppmTolerance)
        {
            exclusionList.setPPMTolerance(_ppmTolerance);
        }

        public void setRetentionTimeWindow(Double retentionTimeWindow)
        {
            database.changeRetentionTimeWindow(retentionTimeWindow);
        }

        public String getPerformanceVector(String experimentName, String experimentType, double analysisTime,
                double totalRunTime, ProteinProphetResult ppr, int ddaNum, ExclusionProfile exclusionProfile)
        {
            performanceEvaluator.finalizePerformanceEvaluator(experimentName, experimentType, analysisTime, totalRunTime,
                    exclusionList, ppr, ddaNum, exclusionProfile);
            return performanceEvaluator.outputPerformance();
        }

        public List<IdentificationFeatures> getFeatures()
        {
            return database.extractFeatures();
        }

        public List<int> getSpectraUsed()
        {
            return includedSpectra;
        }

        public List<int> getUnusedSpectra()
        {
            return excludedSpectra;
        }

        //Called by DataProcessor, entry point to Alex's program
        public virtual bool evaluate(Spectra spec)
        {


            updateCurrentTimeAndExclusionListTime(spec.getStartTime());
            if (spec.getMSLevel() == 1)
            {
                log.Debug("Evaluating ms1 scan");
                processMS1(spec);
            }
            else if (spec.getMSLevel() == 2)
            {
                log.Debug("evaluating ms2 scan");
                if (spec.getIndex() % GlobalVar.ScansPerOutput == 0)
                {
#if SIMULATION
                    double progressPercent = spec.getIndex() / GlobalVar.ExperimentTotalScans * 100;
                    log.Info("Progress: {0:F2}% Processing ID: {1}\t ScanNum: {2} \t Excluded spectra: {3} \t Excluded peptides: {4}",
                        progressPercent, spec.getIndex(), spec.getScanNum(),
                        excludedSpectra.Count, exclusionList.getAllExcludedPeptides().Count);
#else
					log.Info("Progress: {0}\t{1} excluded------------------------",spec.getIndex(),excludedSpectra.Count);
					log.Info("ExclusionListSize: {0}\tRTOffset: {1}",exclusionList.getExclusionList().Count, RetentionTime.getRetentionTimeOffset());
#endif
                }


                return processMS2(spec);
            }
            else
            {
                log.Debug("unrecognized msScan");
            }
            return true;

        }

        protected IDs performDatabaseSearch(Spectra spec)
        {
            IDs id = null;

            if (CometSingleSearch.Search(spec, out id))
            {
                log.Debug("MS2 scan was identified.");
                log.Debug(id);
                performanceEvaluator.countMS2Identified();
                //PSMTSVReaderWriter.WritePSM(id,spec, dbPepMass); //TODO turn this on in the future

            }
            else
            {
                // scan cannot be matched to a peptide
                log.Debug("MS2 scan {0} was not identified by a comet search", spec.getScanNum());
                performanceEvaluator.countMS2Unidentified();
            }

            return id;
        }

        protected Peptide getPeptideFromIdentification(IDs id)
        {
            String peptideSequence = id.getPeptideSequence();
            Peptide pep = database.getPeptide(peptideSequence);
            if (pep == null)
            {
                //log.Warn(String.Format("Peptide sequence {0} is reported by the search engine but " +
                //    "is not present in the fasta database digested with the given number of miscleavages," +
                //    "this psm will be ignored", peptideSequence));
                log.Debug("Adding peptide...");
                pep = database.addPeptideFromIdentification(id, currentTime);
                log.Debug("Added peptide " + peptideSequence + ".");
                performanceEvaluator.countPeptidesAdded();
            }
            else
            {
                log.Debug("Peptide found.");
                performanceEvaluator.countPeptidesIdentified();
            }
            return pep;
        }

        /*
         * Computes the difference between the predicted retention time and when we
         * actually observed this peptide. This difference is then used to calibrate the
         * retention time using the RetentionTimeCalibrator class by calculating a
         * retention time offset. This offset will shift all retention time windows by
         * this value.
         */
        protected void calibrateRetentionTime(Peptide pep)    //called when we observe a peptide that passes the xcorr threshold
        {

            bool isPredictedRT = pep.getRetentionTime().IsPredicted(); //       if isPredictedRT is true, then that means this is the first time you observed it
                                                                       //      if false, then you already have observed it and would have already 
                                                                       // readjusted the offset, so no need to to it again.
                                                                       //      isPredictedRT is false means that you have observed it before, excluded it, but then
                                                                       //it shows up again
            if (isPredictedRT)
            {
                // only calibrate RT prediction if it was a predicted peptide...
                // peptides which were observed should not affect RT prediction calibration
                double predictedRT = pep.getRetentionTime().getRetentionTimePeak();
                //The rt of some peptides (eg. length >60) cannot be predicted by autoRT, in which case 
                //its rt peak, start, end will all be set by default to RetentionTime.MINIMUM_RETENTION_TIME
                //We do NOT want to calibrate RT offset according to these
                if (predictedRT != RetentionTime.MINIMUM_RETENTION_TIME)
                {
                    double rtPredictionError = currentTime - predictedRT;
                    double newOffset = RetentionTimeUtil.computeRTOffset(rtPredictionError, currentTime);
                    RetentionTime.setRetentionTimeOffset(newOffset);
                    exclusionList.updateRetentionTimeOffset(newOffset);
                    performanceEvaluator.countPeptideCalibration();
                }
            }
            //changes the status of the peptide from isPredicted = true to false, because now you have observed it
            double observedTime = currentTime;
#if SIMPLIFIEDEXCLUSIONLIST
            observedTime = currentTime - RetentionTime.getRetentionTimeOffset(); // In the SIMPLIFIEDEXCLUSIONLIST and ExclusionMS, the rtOffset cannot differentiate between an observed peptide rt or predicted peptide rt, so we need to nullify that offset by subtracting it here first
#endif
            exclusionList.UpdateObservedPeptide(pep, observedTime, database.getRetentionTimeWindow());
        }


        private void updateCurrentTimeAndExclusionListTime(double spectra_startTime)
        {
            //rn with the thermo data it's in minutes
            this.currentTime = spectra_startTime;
            exclusionList.setCurrentTime(spectra_startTime);
            performanceEvaluator.countSpectra();
        }

        protected virtual void evaluateExclusion(IDs id)
        {
            // performanceEvaluator.countExcludedSpectra();
            // check if the peptide is identified or not
            if (id == null)
            {
                performanceEvaluator.countMS2UnidentifiedExcluded();
                return;
            }

            Peptide pep = getPeptideFromIdentification(id);
            if (pep == null)
            {

                return;
            }
#if (!DONTEVALUATE)
			performanceEvaluator.evaluateExclusion(exclusionList, pep);

#endif
            // no peptide retention time calibration since it was excluded
        }



        protected void processMS1(Spectra spec)
        {

            log.Debug("MS1 scan, unused");

            performanceEvaluator.countMS1();
            includedSpectra.Add(spec.getScanNum());
        }

        protected virtual bool processMS2(Spectra spec)
        {
            performanceEvaluator.countMS2();

            log.Debug(spec);


            // check if mass is on exclusion list
            double spectraMass = spec.getCalculatedPrecursorMass();
            Boolean isExcluded = exclusionList.isExcluded(spec);
            //Boolean isExcluded = exclusionList.isExcluded(spectraMass); //checks if the mass should've been excluded, 
            //in a real experiment, this should never equal to true
            //since the mass should not have been scanned in the first place 
            //if MS exclusion table was updated correctly through API
            RecordSpecInfo(spec);
            if (isExcluded)
            {
                performanceEvaluator.countExcludedSpectra();
                excludedSpectra.Add(spec.getScanNum());
#if (SIMULATION)
                IDs id = performDatabaseSearch(spec);
                WriterClass.LogScanTime("Excluded", (int)spec.getIndex());

                log.Debug("Mass " + spectraMass + " is on the exclusion list. Scan " + spec.getScanNum() + " excluded.");
                evaluateExclusion(id);
                if (id == null || id.getXCorr() < 0.5)
                {
                    return false;
                }
                var pep = getPeptideFromIdentification(id);
                if (pep == null)
                {
                    performanceEvaluator.countPepUnmatchedID();
                    return false;
                }
                if (exclusionList is SimplifiedExclusionList_IM2 & pep.isFromFastaFile())
                {
                    var simplifiedExclusionList = (SimplifiedExclusionList_IM2)exclusionList;
                    if (simplifiedExclusionList.EvaluateExclusion(performanceEvaluator, spec, pep))
                    {
                        performanceEvaluator.incrementValue(Header.SimplifiedExclusionList_correct);
                    }
                    else
                    {
                        performanceEvaluator.incrementValue(Header.SimplifiedExclusionList_incorrect);
                        //includedSpectra.Add(spec.getScanNum());
                    }

                }

#endif


                return false;
            }
            else
            {
                IDs id = performDatabaseSearch(spec);
                performanceEvaluator.countAnalyzedSpectra();
                log.Debug("Mass " + spectraMass + " was not on the exclusion list. Scan " + spec.getScanNum() + " analyzed.");
                if (id != null && id.getXCorr() > 0.00001)
                {
                    var pep = getPeptideFromIdentification(id);
                    if (pep == null)
                    {
                        performanceEvaluator.countPepUnmatchedID();
                    }
                    else
                    {
                        if (exclusionList is SimplifiedExclusionList_IM2 & pep.isFromFastaFile())
                        {
                            var simplifiedExclusionList = (SimplifiedExclusionList_IM2)exclusionList;
                            if (!simplifiedExclusionList.EvaluateAnalysis(performanceEvaluator, spec, pep))
                            {
                                //includedSpectra.Remove(spec.getScanNum());
                            }
                        }
                    }
                }
                if (id != null && id.getXCorr() > 0.1)
                {
                    includedSpectra.Add(spec.getScanNum());
                    evaluateIdentification(id);
                }
                //WriterClass.LogScanTime("Processed", (int)spec.getIndex());
                return true;
            }


        }

        public virtual void RecordSpecInfo(Spectra spec)
        {
        }
        //This function looks at the bruker ms2 spectra, decides if it's supposed to be
        //excluded, then adds the scanNumber to either included spectra or excluded spectra
        //returns whether the ms2 is supposed to be excluded.
        public bool process_bruker_ms2(double spectra_rt_seconds, double precursorMass, int scanNum)
        {

            updateCurrentTimeAndExclusionListTime(spectra_rt_seconds);
            Boolean isExcluded = exclusionList.isExcluded(precursorMass);
            if (isExcluded)
            {
                excludedSpectra.Add(scanNum);
            }
            else
            {
                includedSpectra.Add(scanNum);
            }
            return isExcluded;
        }
        public void evaluateIdentificationAndUpdateCurrentTime(IDs id)
        {
            if (id == null)
            {
                return;
            }
            if (id.getXCorr() < 0.1)
            {
                return;
            }
            //Note the process bruker ms2 function also updates the current time
            //Need to fix this because if both threads are not synchronized the current time is 
            //updated by both the psm and ms2 which is an issue when one thread lags behind the other
            currentTime = id.getScanTime();
            updateCurrentTimeAndExclusionListTime(currentTime);
            includedSpectra.Add(id.getScanNum());
            evaluateIdentification(id);
        }
        protected abstract void evaluateIdentification(IDs id);

        public abstract ExclusionProfileEnum getAnalysisType();

        /*-
         * Reset the exclusion list and any scores in the database
         */
        public virtual void reset()
        {
            log.Info("Resetting exclusion profile, exclusion list, and exclusion database");
            database.reset();
            exclusionList.reset();
            performanceEvaluator = new PerformanceEvaluator();
            includedSpectra.Clear();
            excludedSpectra.Clear();
            RetentionTimeUtil.resetRTOffset();
        }

        // Bad practice, but it saves me headache...
        public PerformanceEvaluator GetPerformanceEvaluator()
        {
            return performanceEvaluator;
        }

    }

}
