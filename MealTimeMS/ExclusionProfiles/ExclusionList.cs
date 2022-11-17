using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data;
using MealTimeMS.Util;
using System.IO;

namespace MealTimeMS.ExclusionProfiles
{


	// the list that keeps tracks of the excluded peptides
public class ExclusionList
    {

        public const String PAST = "Past";
        public const String PRESENT = "Current";
        public const String FUTURE = "Future";
        public const String ERROR = "Error";
        public const String PAST_OBSERVED = "Past_Observed";
        public const String CURRENT_OBSERVED = "Current_Observed";

        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private double ppmTolerance;
        protected double currentTime;

        // Peptides on the exclusion list, sorted by mass
        private List<Peptide> exclusionList;

        public ExclusionList(double _ppmTolerance)
        {
            currentTime = RetentionTime.MINIMUM_RETENTION_TIME;
            ppmTolerance = _ppmTolerance;
            exclusionList = new List<Peptide>();
        }

        /*
         * Determines if a peptide's retention time is in the past, future, or present
         * Returns PAST(-1) if the retention window has passed, PRESENT(0) if the
         * retention window is in the future, FUTURE(1) if the retention window is
         * active.
         */
        public String retentionTense(Peptide pep)
        {
            RetentionTime rt = pep.getRetentionTime();
            bool isObserved = !rt.IsPredicted();
            double rtStart = rt.getRetentionTimeStart();
            double rtEnd = rt.getRetentionTimeEnd();

            if (isObserved)
            {
                if (pep.isExcluded(currentTime))
                {
                    return CURRENT_OBSERVED;
                }
                else
                {
                    return PAST_OBSERVED;
                }
            }
            else if (pep.isExcluded(currentTime))
            {
                return PRESENT;
            }
            else if (rtStart > currentTime)
            {
                return FUTURE;
            }
            else if (rtEnd < currentTime)
            {
                return PAST;
            }
            return ERROR;
        }

        // UTILITY for if a given list contains the peptide... used to clean up my code
        private bool containsPeptide(List<Peptide> list, Peptide pep)
        {
            return list.Contains(pep);
        }
		// UTILITY for if a given list contains the peptide... used to clean up my code
		public virtual bool containsPeptide( Peptide pep)
		{
			return exclusionList.Contains(pep);
		}

		// Returns all peptides which match the specified tense (PAST,PRESENT,FUTURE)
		private List<Peptide> getPeptidesTense(String tense)
        {
            List<Peptide> peptides = new List<Peptide>();
            foreach (Peptide pep in exclusionList)
            {
                String peptideTense = retentionTense(pep);
                if (peptideTense.Equals(tense))
                {
                    peptides.Add(pep);
                }
            }
            return peptides;
        }

      

        private void UpdateMassSpectrometerExclusionTable()
        {
            //TODO update actual exclusion list
        }

        public void setPPMTolerance(double _ppmTolerance)
        {
            ppmTolerance = _ppmTolerance;
        }

        public void setCurrentTime(double d)
        {
            currentTime = d;
        }

        public double getPPMTolerance()
        {
            return ppmTolerance;
        }

        public double getCurrentTime()
        {
            return currentTime;
        }

        // Returns all the peptides which were previously excluded
        public List<Peptide> getPastExcludedPeptides()
        {
            return getPeptidesTense(PAST);
        }

        // Returns all the peptides which are currently excluded
        public List<Peptide> getCurrentExcludedPeptides()
        {
            return getPeptidesTense(PRESENT);
        }

        // Returns all the peptides which will be excluded at a later time
        public List<Peptide> getFutureExcludedPeptides()
        {
            return getPeptidesTense(FUTURE);
        }

        public List<Peptide> getPastObservedPeptides()
        {
            return getPeptidesTense(PAST_OBSERVED);
        }

        public List<Peptide> getCurrentObservedPeptides()
        {
            return getPeptidesTense(CURRENT_OBSERVED);
        }

        // Returns all the peptides that have been added to the exclusion list.
        public List<Peptide> getAllExcludedPeptides()
        {
            return exclusionList;
        }
#if TRACKEXCLUSIONLISTOPERATION
        private StreamWriter exclusionListOperationSW;
        private System.Diagnostics.Stopwatch stopWatch;
        public void StartExclusionListOperationSW()
        {
            exclusionListOperationSW = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,"exclusionListOperationLog"));
            exclusionListOperationSW.WriteLine(String.Join(separator: "\t", "Time_ms", "scanNum"));
            stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
        }
        public void EndExclusionListOperationSW()
        {
            exclusionListOperationSW.Close();
        }
        public void ExclusionListOperationSW_log(Peptide pep, int scanNum)
        {
            exclusionListOperationSW.WriteLine(String.Join(separator: "\t", stopWatch.ElapsedMilliseconds, scanNum));

        }

        public void addProtein(Protein p, int scanNum)
        {
            foreach (Peptide pep in p.getPeptides())
            {
                ExclusionListOperationSW_log(pep, scanNum);
            }
        }
#endif

        // Add a single peptide to the exclusion list and sort
        virtual public void addPeptide(Peptide pep)
        {
            if (!pep.isFromFastaFile())
            {
                return;
            }
            // Prevents adding peptides already on the list
            if (!containsPeptide(exclusionList, pep))
            {
				int position = BinarySearchUtil.findPositionToAdd(exclusionList, pep, BinarySearchUtil.SortingScheme.MASS);
				exclusionList.Insert(position, pep);
				//exclusionList.Add(pep);
				//SortListByMass();
               
            }
        }

		//Depricated, shouldn't be called, this is functional but just too computationally intensive when the list gets too large
		public void SortListByMass()
		{
			exclusionList.Sort((Peptide x, Peptide y) => (y.getMass()).CompareTo(x.getMass()));
		}

        public void observedPeptide(Peptide pep, double time, double rt_window)
        {
            // set a new RT
            RetentionTime newRT = new RetentionTime(time + rt_window, rt_window, rt_window, false);
            // excludes it for 2*rt_window time
            pep.setRetentionTime(newRT);

            //addPeptide(pep); //personally i dont think this line is necessary, since the peptide would have already be added in 
                                //MachineLearningGuidedExclusion EvaluateIdentification()
                                //Note that in the last line when re-setting the retention time of the peptide, isPredicted will be set to false
        }

        // Add all the peptides of that protein to the exclusion list
        public void addProtein(Protein p)
        {
			foreach(Peptide pep in p.getPeptides())
			{
				addPeptide(pep);
			}
        }

        public void addProteins(List<Protein> proteins)
        {
            foreach(Protein p in proteins)
            {
				addProtein(p);
            }
        }
        /*
         * Finds the list of peptides that match the query mass, within the
         * ppmTolerance. If any of these peptides are excluded in the specified time,
         * this function will return true.
         */
        virtual public bool isExcluded(double queryMass)
        {
            List<Peptide> matchedMassesPeptides = BinarySearchUtil.findPeptides(exclusionList, queryMass, ppmTolerance);
            foreach (Peptide p in matchedMassesPeptides)
            {
                if (p.isExcluded(currentTime))
                {
                    return true;
                }
            }
            return false;
        }
        public List<Peptide> getExclusionList()
        {
            return exclusionList;
        }

        public void reset()
        {
            exclusionList.Clear();
            currentTime = 0;
        }

        public String exclusionExplorerOutput()
        {
            int pastExcluded = 0;
            int currentlyExcluded = 0;
            int futureExcluded = 0;
            int pastObserved = 0;
            int currentObserved = 0;

            foreach (Peptide p in exclusionList)
            {
                String tense = retentionTense(p);
                switch (tense)
                {
                    case PAST:
                        pastExcluded++;
                        break;
                    case PRESENT:
                        currentlyExcluded++;
                        break;
                    case FUTURE:
                        futureExcluded++;
                        break;
                    case PAST_OBSERVED:
                        pastObserved++;
                        break;
                    case CURRENT_OBSERVED:
                        currentObserved++;
                        break;
                    case ERROR:
                        log.Error("Peptide tense was undetermined " + p);
                        break;
                }
            }

            if (pastExcluded + currentlyExcluded + futureExcluded != exclusionList.Count)
            {
                log.Error("Something is wrong with the exclusion list...");
                log.Info("pastExcluded: " + pastExcluded);
                log.Info("currentlyExcluded: " + currentlyExcluded);
                log.Info("futureExcluded: " + futureExcluded);
                log.Info("exclusionList.Count: " + exclusionList.Count);
            }

            return pastExcluded + "\t" + currentlyExcluded + "\t" + futureExcluded + "\t" + pastObserved + "\t"
                    + currentObserved + "\t";
        }

        override
        public String ToString()
        {
            return "ExclusionList{ExcludedPeptides[Past=" + getPastExcludedPeptides().Count + ",Present="
                    + getCurrentExcludedPeptides().Count + ",Future=" + getFutureExcludedPeptides().Count
                    + "]; currentTime=" + currentTime + "; ppmTolerance=" + ppmTolerance + "}";
        }

    }

}
