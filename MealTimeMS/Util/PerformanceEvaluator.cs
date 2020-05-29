using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataTypes = Microsoft.Spark.Sql.Types;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data;
using MealTimeMS.ExclusionProfiles;

namespace MealTimeMS.Util
{

    public class PerformanceEvaluator
    {

        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private class BaselineComparison
        {
            private int ddaNum;
            private List<String> proteinsIdentifiedByNoExclusion;
            private int numProteinsIdentifiedNaiveExperiment;
            private int totalResourcesNaiveExperiment;

            public BaselineComparison(ProteinProphetResult ppr, int numMS2, int ddaNum)
            {
                proteinsIdentifiedByNoExclusion = ppr.getProteinsIdentified();
                numProteinsIdentifiedNaiveExperiment = proteinsIdentifiedByNoExclusion.Count;
                totalResourcesNaiveExperiment = numMS2;
                this.ddaNum = ddaNum;
            }

            public int getDdaNum()
            {
                return ddaNum;
            }

            public List<String> getProteinsIdentifiedByNoExclusion()
            {
                return proteinsIdentifiedByNoExclusion;
            }

            public int getNumProteinsIdentifiedNaiveExperiment()
            {
                return numProteinsIdentifiedNaiveExperiment;
            }

            public int getTotalResourcesNaiveExperiment()
            {
                return totalResourcesNaiveExperiment;
            }

            override
            public String ToString()
            {
                return "BaselineComparison [ddaNum=" + ddaNum + ", proteinsIdentifiedByNoExclusion="
                        + proteinsIdentifiedByNoExclusion + ", numProteinsIdentifiedNaiveExperiment="
                        + numProteinsIdentifiedNaiveExperiment + ", totalResourcesNaiveExperiment="
                        + totalResourcesNaiveExperiment + "]";
            }

        }



        private static Dictionary<Header, Object> data = new Dictionary<Header, Object>();

        private static int numProteinsIdentifiedOriginalExperiment;//original refers to original, fully-processed protein prophet result by using all MS data 
        private static Dictionary<int, BaselineComparison> baselineComparisonSet = new Dictionary<int, BaselineComparison>(); //baseline refers to stuff like top 12 DDA or top 6 DDA with no exclusion ...etc

        public PerformanceEvaluator()
        {
			clear();
        }

        /*
         * Pre-requisite: the peptide p was selected to be excluded from analysis, and
         * thus its mass is on the exclusion list Purpose: check if the sequence of
         * peptide p is truly on the exclusion list or not. If the sequence is not on
         * the exclusion list, then the peptide was incorrectly excluded. This is
         * because the mass is on the current exclusion list
         */
        public void evaluateExclusion(ExclusionList exclusionList, Peptide p)
        {
            
            log.Debug("Evaluating peptide mass exclusion...");

			//		// // sanity check for the pre-requisite
			//		if (!exclusionList.isExcluded(p.getMass())) {
			//			// log.Warn(
			//			// "ERROR: Theoretical peptide mass was found on the exclusion list, but was
			//			// excluded... the spectra mass does not match the mass of the theoretical
			//			// database.");
			//			numWarnings_EvaluateExclusion++;
			//			// return;
			//		}
#if WRITE_RT_TIME
			WriteRetention(String.Format("EX\t{0}\t{1}", exclusionList.getCurrentTime(), p.getRetentionTime().getRetentionTimeStart()));
#endif
			if (containsPeptideSequence(exclusionList.getExclusionList(), p))
            {
                if (isOnCurrentExclusionList(exclusionList, p))
                {
                    log.Debug("Peptide sequence was found on the exclusion list! This is what we want.");
                    incrementValue(Header.EvaluateExclusion_FoundOnCurrentExclusionList);
                }
                else if (isOnFutureExclusionList(exclusionList, p))
                {
                    log.Debug("Peptide sequence was found on the future exclusion list.");
                    incrementValue(Header.EvaluateExclusion_FoundOnFutureExclusionList);
                }
                else if (isOnPastExclusionList(exclusionList, p))
                {
                    log.Debug("Peptide sequence was found on the past exclusion list.");
                    incrementValue(Header.EvaluateExclusion_FoundOnPastExclusionList);
                }
                else if (isOnPastObservedExclusionList(exclusionList, p))
                {
                    log.Debug("Peptide sequence was found on the past observed exclusion list.");
                    // this means that this peptide sequence was excluded, but not anymore
                    incrementValue(Header.EvaluateExclusion_FoundOnPastObservedExclusionList);
                }
                else if (isOnCurrentObservedExclusionList(exclusionList, p))
                {
                    log.Debug("Peptide sequence was found on the observed exclusion list! This is what we want.");
                    incrementValue(Header.EvaluateExclusion_FoundOnCurrentObservedExclusionList);
                }
            }
            else
            {

                log.Debug("Peptide sequence was not found on any of the lists! It was wrongly excluded!");
                // TODO figure out why this is happening.........
				// Solved: if a peptide not on the ex list has a similar mass within ppm tolorence of a peptide that's supposed to be excluded, this will happen
                incrementValue(Header.EvaluateExclusion_NotFoundOnExclusionList);
            }
            incrementValue(Header.TotalNumEvaluateExclusion);
        }

    
        public void evaluateAnalysis(ExclusionList exclusionList, Peptide p)
        {
            log.Debug("Evaluating peptide analysis...");
#if WRITE_RT_TIME
            WriteRetention(String.Format("AN\t{0}\t{1}",exclusionList.getCurrentTime(),p.getRetentionTime().getRetentionTimeStart()));
#endif
            // // sanity check for the pre-requisite
            // if (exclusionList.isExcluded(p.getMass())) {
            // // log.Warn(
            // // "ERROR: Theoretical peptide mass was found on the exclusion list, but was
            // // analyzed... the spectra mass does not match the mass of the theoretical
            // // database.");
            // numWarnings_EvaluateAnalysis++;
            // }
			
            if ( containsPeptideSequence(exclusionList.getExclusionList(), p)){
                if (isOnCurrentExclusionList(exclusionList, p))
                {
                    WriteEvaluation("Peptide sequence was found in the current exclusion list, but was analyzed anyways");
                    log.Debug("Peptide sequence was found in the current exclusion list, but was analyzed anyways");
                    log.Debug("Mass of peptide: " + p.getMass());
                    log.Debug("ppmTolerance: " + exclusionList.getPPMTolerance());
                    // this means the ppm mass tolerance is not high enough
                    // it's on the EL but not excluded this is a ppm tolerance problem
                    incrementValue(Header.EvaluateAnalysis_FoundOnCurrentExclusionList);
                }
                else if (isOnFutureExclusionList(exclusionList, p))
                {
                    WriteEvaluation("Peptide sequence was found on the future exclusion list.");
                    log.Debug("Peptide sequence was found on the future exclusion list.");
                    log.Debug("Retention time of peptide: " + p.getRetentionTime());
                    log.Debug("current_time: " + exclusionList.getCurrentTime());
                    // this means that this peptide sequence is to be excluded, but not rn
                    // this is a retention time prediction problem
                    incrementValue(Header.EvaluateAnalysis_FoundOnFutureExclusionList);
                }
                else if (isOnPastExclusionList(exclusionList, p))
                {
                    WriteEvaluation("Peptide sequence was found on the past exclusion list.");
                    log.Debug("Peptide sequence was found on the past exclusion list.");
                    log.Debug("Retention time of peptide: " + p.getRetentionTime());
                    log.Debug("current_time: " + exclusionList.getCurrentTime());
                    // this means that this peptide sequence was excluded, but not anymore
                    // this is a retention time prediction problem
                    incrementValue(Header.EvaluateAnalysis_FoundOnPastExclusionList);
                }
                else if (isOnPastObservedExclusionList(exclusionList, p))
                {
                    WriteEvaluation("Peptide sequence was found on the past observed exclusion list.");
                    log.Debug("Peptide sequence was found on the past observed exclusion list.");
                    // this means that this peptide sequence was excluded, but not anymore
                    incrementValue(Header.EvaluateAnalysis_FoundOnPastObservedExclusionList);
                }
                else if (isOnCurrentObservedExclusionList(exclusionList, p))
                {
                    WriteEvaluation("Peptide sequence was found on the observed exclusion list but was analyzed anyways.");
                    log.Debug("Peptide sequence was found on the observed exclusion list but was analyzed anyways.");
                    log.Debug("Retention time of peptide: " + p.getRetentionTime());
                    log.Debug("current_time: " + exclusionList.getCurrentTime());
                    log.Debug("Mass of peptide: " + p.getMass());
                    log.Debug("ppmTolerance: " + exclusionList.getPPMTolerance());
                    incrementValue(Header.EvaluateAnalysis_FoundOnCurrentObservedExclusionList);
                }
            }
            else
            {
                WriteEvaluation("Peptide sequence was not found on any of the lists!");
                log.Debug("Peptide sequence was not found on any of the lists!");
                incrementValue(Header.EvaluateAnalysis_NotFoundOnExclusionList);
            }
            incrementValue(Header.TotalNumEvaluateAnalysis);
            count++;
        }
        int count = 0;
        private void WriteEvaluation(String str) {
            return;
            String output = this.count +": " + str;
            //WriterClass.writeln(output, 1);
            count++;
        }
        private void WriteRetention(String str)
        {
            WriterClass.writeln(str, writerClassOutputFile.peptideRTTime);
        }
        private bool isOnPastObservedExclusionList(ExclusionList exclusionList, Peptide p)
        {
            return exclusionList.retentionTense(p).Equals(ExclusionList.PAST_OBSERVED);
            List<Peptide> list = exclusionList.getPastObservedPeptides();
            return containsPeptideSequence(list, p);
        }

        private bool isOnCurrentObservedExclusionList(ExclusionList exclusionList, Peptide p)
        {
            exclusionList.retentionTense(p).Equals(ExclusionList.CURRENT_OBSERVED);
            List<Peptide> list = exclusionList.getCurrentObservedPeptides();
            return containsPeptideSequence(list, p);
        }

        private bool isOnCurrentExclusionList(ExclusionList exclusionList, Peptide p)
        {
            return exclusionList.retentionTense(p).Equals(ExclusionList.PRESENT);
            List<Peptide> list = exclusionList.getCurrentExcludedPeptides();
            return containsPeptideSequence(list, p);
        }

        private bool isOnFutureExclusionList(ExclusionList exclusionList, Peptide p)
        {
            return exclusionList.retentionTense(p).Equals(ExclusionList.FUTURE);
            List<Peptide> list = exclusionList.getFutureExcludedPeptides();
            return containsPeptideSequence(list, p);
        }

        private bool isOnPastExclusionList(ExclusionList exclusionList, Peptide p)
        {
            return exclusionList.retentionTense(p).Equals(ExclusionList.PAST);
            List<Peptide> list = exclusionList.getPastExcludedPeptides();
            return containsPeptideSequence(list, p);
        }

        private bool containsPeptideSequence(List<Peptide> list, Peptide p)
        {
            bool matched = false;
            foreach (Peptide pep in list)
            {
                if (pep.getSequence().Equals(p.getSequence()))
                {
                    // the peptide sequence was on the list
                    matched = true;
                    break;
                }
            }
            return matched;
        }

        public void massValidator(double spectra_mass, double id_mass, double database_mass, ExclusionList exclusionList)
        {
            // make sure each double falls ewithin ppmTolerance of each other
            bool spectraMassOnExclusionList = exclusionList.isExcluded(spectra_mass);
            bool idMassOnExclusionList = exclusionList.isExcluded(id_mass);
            bool databaseMassOnExclusionList = exclusionList.isExcluded(database_mass);

            if (!(spectraMassOnExclusionList && idMassOnExclusionList && databaseMassOnExclusionList))
            {
                log.Debug("$" + spectra_mass + "\t" + id_mass + "\t" + database_mass + "\t" + (spectra_mass - id_mass)
                        + "\t" + (database_mass - id_mass) + "\t" + (spectra_mass - database_mass));
                // spectra_mass id_mass database_mass spec-id db-id spec-db
            }

        }

        public bool massComparer(double spectra_mass, double id_mass, double database_mass, double ppm_tolerance,
                int scanNum)
        {
            bool db_vs_spec = BinarySearchUtil.withinPPMTolerance(database_mass, spectra_mass, ppm_tolerance);
            bool id_vs_spec = BinarySearchUtil.withinPPMTolerance(id_mass, spectra_mass, ppm_tolerance);
            bool id_vs_db = BinarySearchUtil.withinPPMTolerance(id_mass, database_mass, ppm_tolerance);
            log.Debug("Scan num: " + scanNum);
            log.Debug("(spectra_mass,id_mass,database_mass,ppm_tolerance) = (" + spectra_mass + "," + id_mass + ","
                    + database_mass + "," + ppm_tolerance + ")");
            log.Debug("(db_vs_spec,id_vs_spec,id_vs_db) = (" + db_vs_spec + "," + id_vs_spec + "," + id_vs_db
                    + ");\tdelta: " + (database_mass - spectra_mass) + "," + (id_mass - spectra_mass) + ","
                    + (id_mass - database_mass) + ")");

            if (db_vs_spec && id_vs_db && id_vs_spec)
            {
                return true;
            }
            else
            {
                log.Info("Scan num: " + scanNum);
                log.Info("(spectra_mass,id_mass,database_mass,ppm_tolerance) = (" + spectra_mass + "," + id_mass
                        + "," + database_mass + "," + ppm_tolerance + ")");
                log.Info("(db_vs_spec,id_vs_spec,id_vs_db) = (" + db_vs_spec + "," + id_vs_spec + "," + id_vs_db
                        + ");\tdelta: " + (database_mass - spectra_mass) + "," + (id_mass - spectra_mass) + ","
                        + (id_mass - database_mass) + ")");
                return false;
            }
        }


        public void finalizePerformanceEvaluator(String experimentName, String experimentType, double analysisTime,
                double totalRunTime, ExclusionList exclusionList, ProteinProphetResult ppr, int ddaNum, ExclusionProfile exclusionProfile)
        {
            setExperimentName(experimentName, experimentType);
            setExperimentDuration(analysisTime, totalRunTime);
			setExperimentParams();
            setExclusionList(exclusionList);
            postProcessingCalculations(ddaNum, ppr, exclusionProfile);
        }

        /*
         * This function returns the number of overlapping strings between the two sets
         * That is, how many Strings in set1 are also found in set2
         */
        private static int compareProteins(List<String> set1, List<String> set2)
        {
            int matches = 0;
            foreach (String s in set2)
            {
                if (set1.Contains(s))
                {
                    matches++;
                }
            }
            return matches;
        }

        private void setProteinsIdentified(ProteinProphetResult ppr, List<String> proteinsIdentifiedByNoExclusion)
        {
            if (ppr != null)
            {
                List<String> proteinsIdentified = ppr.getProteinsIdentified();
                int intersect = compareProteins(proteinsIdentifiedByNoExclusion, proteinsIdentified);
				ChangeValue(Header.NumProteinsIdentified, proteinsIdentified.Count);
				ChangeValue(Header.ProteinsIdentifiedInLimitedDDA, intersect);
            }
        }

        public void setExperimentName(String experimentName, String experimentType)
        {

			ChangeValue(Header.ExperimentName, experimentName);
			ChangeValue(Header.ExperimentType, experimentType);
			
		}
		public void setExperimentParams()
		{
			ChangeValue(Header.ppmTol, GlobalVar.ppmTolerance);
			ChangeValue(Header.rtWin, GlobalVar.retentionTimeWindowSize);
			ChangeValue(Header.xCorr, GlobalVar.XCorr_Threshold);
			ChangeValue(Header.numDB, GlobalVar.NumDBThreshold);
			ChangeValue(Header.prThr, GlobalVar.AccordThreshold);

			
			

			if (data[Header.ExperimentType].Equals(ExclusionProfileEnum.NORA_EXCLUSION_PROFILE.getDescription()))
			{
				ChangeValue(Header.prThr, -1);
			}
			if (data[Header.ExperimentType].Equals(ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE.getDescription()))
			{
				ChangeValue(Header.xCorr, -1);
				ChangeValue(Header.numDB, -1);
			}

		}
		private void setExperimentDuration(double analysisTime, double totalRunTime)
        {
			ChangeValue(Header.AnalysisTime, analysisTime);
			ChangeValue(Header.TotalRunTime, totalRunTime);
        }

        private void setExclusionList(ExclusionList exclusionList)
        {
            int pastSize = exclusionList.getPastExcludedPeptides().Count;
            int currentSize = exclusionList.getCurrentExcludedPeptides().Count;
            int futureSize = exclusionList.getFutureExcludedPeptides().Count;
            int pastObservedSize = exclusionList.getPastObservedPeptides().Count;
            int currentObservedSize = exclusionList.getCurrentObservedPeptides().Count;
            int totalExclusionListSize = pastSize + currentSize + futureSize + pastObservedSize + currentObservedSize;
			ChangeValue(Header.ExclusionListPastSize, pastSize);
			ChangeValue(Header.ExclusionListCurrentSize, currentSize);
			ChangeValue(Header.ExclusionListFutureSize, futureSize);
			ChangeValue(Header.ExclusionListPastObserved, pastObservedSize);
			ChangeValue(Header.ExclusionListCurrentObserved, currentObservedSize);
			ChangeValue(Header.ExclusionListFinalTotalSize, totalExclusionListSize);
        }

        private void postProcessingCalculations(int ddaNum, ProteinProphetResult ppr, ExclusionProfile exclusionProfile)
        {

            BaselineComparison bc = baselineComparisonSet[ddaNum];
            List<String> proteinsIdentifiedByNoExclusion = bc.getProteinsIdentifiedByNoExclusion();
            int totalResourcesNaiveExperiment = bc.getTotalResourcesNaiveExperiment();
            int numProteinsIdentifiedNaiveExperiment = bc.getNumProteinsIdentifiedNaiveExperiment();

            // set proteins identified first
            setProteinsIdentified(ppr, proteinsIdentifiedByNoExclusion);

            int correctlyExcluded = (int)data[Header.EvaluateExclusion_FoundOnCurrentExclusionList]
				+ (int)data[Header.EvaluateExclusion_FoundOnCurrentObservedExclusionList];
            int incorrectlyExcluded = (int)data[Header.EvaluateExclusion_NotFoundOnExclusionList];
            /*
             * found on past observed, found on past exclusion list, and found on future
             * exclusion list are not incorrect exclusions, they are retention time being
             * predicted incorrectly... 
             */
            double ratioIncorrectlyExcludedOverCorrectlyExcluded = takeRatio(incorrectlyExcluded, correctlyExcluded);
			ChangeValue(Header.CorrectlyExcluded, correctlyExcluded);
			ChangeValue(Header.IncorrectlyExcluded, incorrectlyExcluded);
			ChangeValue(Header.RatioIncorrectlyExcludedOverCorrectlyExcluded, ratioIncorrectlyExcludedOverCorrectlyExcluded);

            // Resources saved in total # available MS2 - # ms2 used foreach analysis
            int resourcesSaved = totalResourcesNaiveExperiment - (int)data[Header.NumMS2Analyzed];
            double percentResourcesSaved = takeRatio(resourcesSaved, totalResourcesNaiveExperiment);
            double percentResourcesUsed = 1 - percentResourcesSaved;
			ChangeValue(Header.PercentResourcesSaved, percentResourcesSaved);
			ChangeValue(Header.PercentResourcesUsed, percentResourcesUsed);

			/*-	
             * Protein Identification Sensitivity = # proteins identified / # proteins identified in whole experiment
             * Protein Identification Fold Change = # proteins identified / # proteins identified by naive approach
             * Protein Identification Sensitivity Limited DDA = # proteins identified also identified in naive approach / proteins identified by naive approach
             */
			ChangeValue(Header.ProteinIdentificationSensitivity,
                    takeRatio((int)data[Header.NumProteinsIdentified], numProteinsIdentifiedOriginalExperiment));
			ChangeValue(Header.ProteinIdentificationFoldChange,
                    takeRatio((int)data[Header.NumProteinsIdentified], numProteinsIdentifiedNaiveExperiment));
			ChangeValue(Header.ProteinIdentificationSensitivityLimitedDDA,
                    takeRatio((int)data[Header.ProteinsIdentifiedInLimitedDDA], numProteinsIdentifiedNaiveExperiment));

			List<String> inProgramExcludedProteins = exclusionProfile.getDatabase().getExcludedProteins();
			int proteinOverlap_inProgramExcluded_vs_NoExclusion = compareProteins(inProgramExcludedProteins, proteinsIdentifiedByNoExclusion);
			ChangeValue(Header.NumProteinOverlap_ExcludedProteinsAgainstNoExclusionProteins, proteinOverlap_inProgramExcluded_vs_NoExclusion);
			ChangeValue(Header.ProteinGroupsIdentified, ppr.getFilteredProteinGroups().Count);

		}

        /*
         * Prevents divide by zero and integer division errors
         */
        private static double takeRatio(int numerator, int denominator)
        {
            return (denominator > 0) ? (((double)numerator) / denominator) : 0.0;
        }

        public void countMS2Unidentified()
        {
            incrementValue(Header.NumMS2UnidentifiedByDBSearch);
        }

        public void countMS2Identified()
        {
            incrementValue(Header.NumMS2IdentifiedByDBSearch);
        }

        public void countPeptidesAdded()
        {
            incrementValue(Header.ExclusionDatabasePeptidesAdded);
        }

        public void countPeptidesIdentified()
        {
            incrementValue(Header.ExclusionDatabasePeptidesQueried);
        }

        public void countPeptideCalibration()
        {
            incrementValue(Header.NumPeptideRTUsedForCalibration);
        }

        public void countSpectra()
        {
            incrementValue(Header.NumSpectraQueried);
        }

        public void countMS2UnidentifiedExcluded()
        {
            incrementValue(Header.NumUnidentifiedMS2Excluded);
        }

        public void countMS1()
        {
            incrementValue(Header.NumMS1);
        }

        public void countMS2()
        {
            incrementValue(Header.NumMS2Queried);
        }

        public void countExcludedSpectra()
        {
            incrementValue(Header.NumMS2Excluded);
        }

        public void countAnalyzedSpectra()
        {
            incrementValue(Header.NumMS2Analyzed);
        }

        public void countRepurposedResource()
        {
            incrementValue(Header.NumMS2Repurposed);
        }

        private void incrementValue(Header h)
        {
            addValue(h, 1);
        }

        private void addValue(Header h, int valueToAdd)
        {
            if (h.getDataType().Equals(typeof(DataTypes.IntegerType)))
            {
                int value = (int)data[h];
                data[h]= value + valueToAdd;
            }
            else
            {
                log.Warn("Error. Trying to add a non-integer!");
            }
        }

        public Object getValue(String query)
        {
            foreach (Header h in Enum.GetValues(typeof(Header)))
            {
                String headerValue = h.getDescription();
                if (query.Equals(headerValue))
                {
                    return data[h];
                }
            }
            return null;
        }

		public Object getValue(Header h)
		{
			return data[h];
		}

        public  String getHeader()
        {
            return Header.AnalysisTime.getHeader();
        }

        public String outputPerformance()
        {

            String[] arr = new String[Enum.GetNames(typeof(Header)).Length];
            int count = 0;
            foreach(Header h in Enum.GetValues(typeof(Header)))
            {
                arr[count] = data[h].ToString();
                count++;
            }
            return String.Join("\t", arr);
        }

        public void clear()
        {
            if (data == null)
            {
                data = new Dictionary<Header, Object>();
            }
            else
            {
                data.Clear();
            }

            Array headerValues =  Enum.GetValues(typeof(Header));
            foreach(Header h in headerValues)
            {
                Type dataType = h.getDataType();
                Object value = null;
                if (dataType.Equals(typeof(DataTypes.StringType)))
                {
                    value = h.DEFAULT_STRING();
                }
                else if (dataType.Equals(typeof(DataTypes.DoubleType)))
                {
                    value = h.DEFAULT_DOUBLE();
                }
                else if (dataType.Equals(typeof(DataTypes.IntegerType)))
                {
                    value = h.DEFAULT_INT();
                }
                data.Add(h, value);
            }
            

        }

        public static void setOriginalExperiment(int numProteinsIdentified)
        {
            numProteinsIdentifiedOriginalExperiment = numProteinsIdentified;
        }

        public static void setBaselineComparison(ProteinProphetResult ppr, int numMS2, int ddaNum)
        {
            if (!baselineComparisonSet.ContainsKey(ddaNum))
            {
                BaselineComparison bc = new BaselineComparison(ppr, numMS2, ddaNum);
                baselineComparisonSet.Add(ddaNum, bc);
            }

        }

        public void countPeptidesExcluded()
        {
            incrementValue(Header.NumPeptidesAddedToExclusionList);
        }

        public void countProteinsExcluded()
        {
            incrementValue(Header.NumProteinsAddedToExclusionList);
        }

        public void countMS2UnidentifiedAnalyzed()
        {
            incrementValue(Header.NumUnidentifiedMS2Analyzed);
        }

		public void ChangeValue(Header h, Object value)
		{
			if (data.ContainsKey(h))
			{
				data[h] =value;
			}
			else
			{
				data.Add(h, value);
			}

		}

    }
    public static class HeaderExtension
    {
        private static Dictionary<Header, Type> dataTypesTable = new Dictionary<Header, Type>() {

            {Header.ExperimentName, typeof( DataTypes.StringType) },
            {Header.ExperimentType, typeof( DataTypes.StringType) },
            {Header.AnalysisTime, typeof( DataTypes.DoubleType) },
            {Header.TotalRunTime, typeof( DataTypes.DoubleType) },
            {Header.NumProteinsIdentified, typeof( DataTypes.IntegerType) },
            {Header.NumSpectraQueried, typeof( DataTypes.IntegerType) },
            {Header.NumMS1, typeof( DataTypes.IntegerType) },
            {Header.NumMS2Queried, typeof( DataTypes.IntegerType) },
            {Header.NumMS2Excluded, typeof( DataTypes.IntegerType) },
            {Header.NumMS2Analyzed, typeof( DataTypes.IntegerType) },
            {Header.NumMS2Repurposed, typeof( DataTypes.IntegerType) },
            {Header.NumMS2IdentifiedByDBSearch, typeof( DataTypes.IntegerType) },
            {Header.NumMS2UnidentifiedByDBSearch, typeof( DataTypes.IntegerType) },
            {Header.NumUnidentifiedMS2Analyzed, typeof( DataTypes.IntegerType) },
            {Header.NumUnidentifiedMS2Excluded, typeof( DataTypes.IntegerType) },
            {Header.ExclusionDatabasePeptidesQueried, typeof( DataTypes.IntegerType) },
            {Header.ExclusionDatabasePeptidesAdded, typeof( DataTypes.IntegerType) },
            {Header.NumPeptideRTUsedForCalibration, typeof( DataTypes.IntegerType) },
            {Header.NumPeptidesAddedToExclusionList, typeof( DataTypes.IntegerType) },
            {Header.NumProteinsAddedToExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateExclusion_FoundOnCurrentExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateExclusion_FoundOnFutureExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateExclusion_FoundOnPastExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateExclusion_FoundOnPastObservedExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateExclusion_FoundOnCurrentObservedExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateExclusion_NotFoundOnExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateAnalysis_FoundOnCurrentExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateAnalysis_FoundOnFutureExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateAnalysis_FoundOnPastExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateAnalysis_FoundOnPastObservedExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateAnalysis_FoundOnCurrentObservedExclusionList, typeof( DataTypes.IntegerType) },
            {Header.EvaluateAnalysis_NotFoundOnExclusionList, typeof( DataTypes.IntegerType) },
            {Header.ExclusionListFinalTotalSize, typeof( DataTypes.IntegerType) },
            {Header.ExclusionListPastSize, typeof( DataTypes.IntegerType) },
            {Header.ExclusionListCurrentSize, typeof( DataTypes.IntegerType) },
            {Header.ExclusionListFutureSize, typeof( DataTypes.IntegerType) },
            {Header.ExclusionListPastObserved, typeof( DataTypes.IntegerType) },
            {Header.ExclusionListCurrentObserved, typeof( DataTypes.IntegerType) },
            {Header.TotalNumEvaluateExclusion, typeof( DataTypes.IntegerType) },
            {Header.TotalNumEvaluateAnalysis, typeof( DataTypes.IntegerType) },
            {Header.CorrectlyExcluded, typeof( DataTypes.IntegerType) },
            {Header.IncorrectlyExcluded, typeof( DataTypes.IntegerType) },
            {Header.RatioIncorrectlyExcludedOverCorrectlyExcluded, typeof( DataTypes.DoubleType) },
            {Header.ResourcesSaved, typeof( DataTypes.IntegerType) },
            {Header.PercentResourcesSaved, typeof( DataTypes.DoubleType) },
            {Header.PercentResourcesUsed, typeof( DataTypes.DoubleType) },
            {Header.ProteinIdentificationSensitivity, typeof( DataTypes.DoubleType) },
            {Header.ProteinIdentificationFoldChange, typeof( DataTypes.DoubleType) },
            {Header.ProteinsIdentifiedInLimitedDDA, typeof( DataTypes.IntegerType) },
            {Header.ProteinIdentificationSensitivityLimitedDDA, typeof( DataTypes.DoubleType) },
			{Header.ppmTol, typeof( DataTypes.DoubleType) },
			{Header.rtWin, typeof( DataTypes.DoubleType) },
			{Header.xCorr, typeof( DataTypes.DoubleType) },
			{Header.numDB, typeof( DataTypes.DoubleType) },
			{Header.prThr, typeof( DataTypes.DoubleType) },
			{Header.NumProteinOverlap_ExcludedProteinsAgainstNoExclusionProteins, typeof( DataTypes.IntegerType) },
			{Header.ProteinGroupsIdentified, typeof( DataTypes.IntegerType) }

		};


        public  static String DEFAULT_STRING(this Header h) {
            return "NULL";
        }
        public static int DEFAULT_INT(this Header h)
        {
            return 0;
        }
        public static double DEFAULT_DOUBLE(this Header h)
        {
            return 0.0;
        }

        public static String getDescription(this Header h)
        {
            return Enum.GetName(typeof(Header),h);
        }

        public static Type getDataType(this Header h)
        {
            return dataTypesTable[h];
        }

        public static Object getInitialValue(this Header h)
        {
            Object value = null;
            Type dataType = dataTypesTable[h]; 
            if (dataType.Equals(typeof(DataTypes.StringType)))
            {
                value = h.DEFAULT_STRING();
            }
            else if (dataType.Equals(typeof(DataTypes.DoubleType)))
            {
                value = h.DEFAULT_DOUBLE();
            }
            else if (dataType.Equals(typeof(DataTypes.IntegerType)))
            {
                value = h.DEFAULT_INT();
            }
            return value;
        }


        public static String getHeader(this Header h)
        {
            
            String[] header = new String[Enum.GetNames(typeof(Header)).Length];
            String separator = "\t";
            for (int i = 0; i < header.Length; i++)
            {
                header[i] = Enum.GetName(typeof(Header), i);
            }
            return String.Join(separator, header);
        }

        public static String ToString(this Header h)
        {
            return h.getDescription();
        }
    }

    public enum Header
    {
        ExperimentName,
        ExperimentType,
        AnalysisTime,
        TotalRunTime,
        NumProteinsIdentified,
        NumSpectraQueried,
        NumMS1,
        NumMS2Queried,
        NumMS2Excluded,
        NumMS2Analyzed,
        NumMS2Repurposed,
        NumMS2IdentifiedByDBSearch,
        NumMS2UnidentifiedByDBSearch,
        NumUnidentifiedMS2Analyzed,
        NumUnidentifiedMS2Excluded,
        ExclusionDatabasePeptidesQueried,
        ExclusionDatabasePeptidesAdded,
        NumPeptideRTUsedForCalibration,
        NumPeptidesAddedToExclusionList,
        NumProteinsAddedToExclusionList,
        EvaluateExclusion_FoundOnCurrentExclusionList,
        EvaluateExclusion_FoundOnFutureExclusionList,
        EvaluateExclusion_FoundOnPastExclusionList,
        EvaluateExclusion_FoundOnPastObservedExclusionList,
        EvaluateExclusion_FoundOnCurrentObservedExclusionList,
        EvaluateExclusion_NotFoundOnExclusionList,
        EvaluateAnalysis_FoundOnCurrentExclusionList,
        EvaluateAnalysis_FoundOnFutureExclusionList,
        EvaluateAnalysis_FoundOnPastExclusionList,
        EvaluateAnalysis_FoundOnPastObservedExclusionList,
        EvaluateAnalysis_FoundOnCurrentObservedExclusionList,
        EvaluateAnalysis_NotFoundOnExclusionList,
        ExclusionListFinalTotalSize,
        ExclusionListPastSize,
        ExclusionListCurrentSize,
        ExclusionListFutureSize,
        ExclusionListPastObserved,
        ExclusionListCurrentObserved,
        TotalNumEvaluateExclusion,
        TotalNumEvaluateAnalysis,
        CorrectlyExcluded,
        IncorrectlyExcluded,
        RatioIncorrectlyExcludedOverCorrectlyExcluded,
        ResourcesSaved,
        PercentResourcesSaved,
        PercentResourcesUsed,
        ProteinIdentificationSensitivity,
        ProteinIdentificationFoldChange,
        ProteinsIdentifiedInLimitedDDA,
        ProteinIdentificationSensitivityLimitedDDA,
		ppmTol,
		rtWin,
		xCorr,
		numDB,
		prThr,
		NumProteinOverlap_ExcludedProteinsAgainstNoExclusionProteins,
		ProteinGroupsIdentified
    }

}
