using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data;
using MealTimeMS.Util;
using MealTimeMS.Tester;
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
           
            exclusionList = new ExclusionList(_ppmTolerance);
			performanceEvaluator = new PerformanceEvaluator();
            currentTime = 0.0;
            includedSpectra = new List<int>();
            excludedSpectra = new List<int>();
            log.Debug("Setting up Comet");
            
            //CometDecoy = new CometSingleSearch(InputFileOrganizer.DecoyIDXDatabase, InputFileOrganizer.CometParamsFile);
            reset();
        }

        public ExclusionProfile(Database _database) : this(_database,  DEFAULT_PPMTOLERANCE)
        {
           
        }
		public String ReportFailedCometSearchStatistics()
		{
			return CometSingleSearch.ReportFailedStatistics();
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
        public bool evaluate(Spectra spec)
        {
			
			currentTime = spec.getStartTime();
            updateExclusionList(spec);
            if (spec.getMSLevel() == 1)
            {
				log.Debug("Evaluating ms1 scan");
                processMS1(spec);
            }
            else if(spec.getMSLevel()==2)
            {
				log.Debug("evaluating ms2 scan");
				if (spec.getIndex() % GlobalVar.ScansPerOutput == 0)
				{
#if SIMULATION
					double progressPercent = spec.getIndex() / GlobalVar.ExperimentTotalScans * 100;
					log.Info("Progress: {0:F2}% Processing ID: {1}\t ScanNum: {2} \t Excluded: {3}", progressPercent, spec.getIndex(), spec.getScanNum(),
						excludedSpectra.Count);
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
        protected  void calibrateRetentionTime(Peptide pep)    //called when we observe a peptide that passes the xcorr threshold
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
                double rtPredictionError = currentTime - predictedRT;
                double newOffset = RetentionTimeUtil.computeRTOffset(rtPredictionError, currentTime);
                RetentionTime.setRetentionTimeOffset(newOffset);
                performanceEvaluator.countPeptideCalibration();
            }
            //changes the status of the peptide from isPredicted = true to false, because now you have observed it
            exclusionList.observedPeptide(pep, currentTime, database.getRetentionTimeWindow());
        }

        private void updateExclusionList(Spectra spec)
        {
            currentTime = spec.getStartTime(); // in minutes
            exclusionList.setCurrentTime(currentTime);
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
            Boolean isExcluded = exclusionList.isExcluded(spectraMass); //checks if the mass should've been excluded, 
                                                                        //in a real experiment, this should never equal to true
                                                                        //since the mass should not have been scanned in the first place 
                                                                        //if MS exclusion table was updated correctly through API
           
			
            if (isExcluded)
            {
#if (SIMULATION)
				IDs id = performDatabaseSearch(spec);
				
                log.Debug("Mass " + spectraMass + " is on the exclusion list. Scan " + spec.getScanNum() + " excluded.");
                evaluateExclusion(id);
                WriterClass.LogScanTime("Excluded",(int)spec.getIndex());
#endif
				performanceEvaluator.countExcludedSpectra();
				excludedSpectra.Add(spec.getScanNum());
				return false;
            }
            else
            {
				IDs id = performDatabaseSearch(spec);
				performanceEvaluator.countAnalyzedSpectra();
                log.Debug("Mass " + spectraMass + " was not on the exclusion list. Scan " + spec.getScanNum() + " analyzed.");
                evaluateIdentification(id);
                includedSpectra.Add(spec.getScanNum());
                // calibrate peptide if the observed retention time doesn't match the predicted
                //WriterClass.LogScanTime("Processed", (int)spec.getIndex());
				return true;
            }
            

        }

        protected abstract void evaluateIdentification(IDs id);

        public abstract ExclusionProfileEnum getAnalysisType();

        /*-
         * Reset the exclusion list and any scores in the database
         */
        public void reset()
        {
            database.reset();
            exclusionList.reset();
            performanceEvaluator.clear();
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
