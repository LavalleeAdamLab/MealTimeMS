using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Util;
using MealTimeMS.IO;

namespace MealTimeMS.ExclusionProfiles.TestProfile
{
	public class NoExclusion : ExclusionProfile
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		public Dictionary<String, double> rtCalcPredictedRT;
		public List<ObservedPeptideRtTrackerObject> peptideIDRT;

		public NoExclusion(Database _database, double _retentionTimeWindowSize) : base(_database)
		{
			rtCalcPredictedRT = new Dictionary<string, double>();
			peptideIDRT = new List<ObservedPeptideRtTrackerObject>();
			setRetentionTimeWindow(_retentionTimeWindowSize);
		}

		override
		protected void evaluateIdentification(IDs id)
		{
			log.Debug("NoExclusion. Scores added, but nothing added to the exclusion list");

			// check if the peptide is identified or not
			if (id == null)
			{
				performanceEvaluator.countMS2UnidentifiedAnalyzed();
				return;
			}

			Peptide pep = getPeptideFromIdentification(id); // if it was going to be null, it already returned
                                                            // is fragmented
            if (pep == null)
            {
                return;
            }
			// add decoy or non-existent protein connections
			// database.addProteinFromIdentification(pep, id.getParentProteinAccessions());

			Double xCorr = id.getXCorr();
			Double dCN = id.getDeltaCN();
			pep.addScore(xCorr, 0.0, dCN);
			performanceEvaluator.evaluateAnalysis(exclusionList, pep);
            


			


			if ((xCorr > 2.5))
			{
				// calibrates our retention time alignment if the observed time is different
				// from the predicted only if it passes this threshold
				calibrateRetentionTime(pep);
			}
			
		}

        override
        public void RecordSpecInfo(Spectra spec)
        {
            IDs id = performDatabaseSearch(spec);
            if (id == null)
                return;
           
            var pep = getPeptideFromIdentification(id);
            if (pep == null)
                return;
            RetentionTime rt = pep.getRetentionTime();

            if (!rtCalcPredictedRT.Keys.Contains(pep.getSequence()))
            {
                rtCalcPredictedRT.Add(pep.getSequence(), rt.getRetentionTimePeak());
            }

            ObservedPeptideRtTrackerObject observedPep = new ObservedPeptideRtTrackerObject(id.getScanNum(), pep.getSequence(), id.getPeptideSequence_withModification(), id.getScanTime(), id.getXCorr(), id.getDeltaCN(),
                rt.getRetentionTimePeak(), rt.getRetentionTimeStart() + GlobalVar.retentionTimeWindowSize,
                RetentionTime.getRetentionTimeOffset(), rtCalcPredictedRT[pep.getSequence()], (rt.IsPredicted() ? 1 : 0),
                pep.getMass(), spec.getCalculatedPrecursorMass(), id.getPeptideMass(), String.Join(separator: ",", id.getParentProteinAccessions()));

            observedPep.offset = RetentionTime.getRetentionTimeOffset();
            peptideIDRT.Add(observedPep);
        }

        override
		public String ToString()
		{
			double retentionTimeWindow = database.getRetentionTimeWindow();
			double ppmTolerance = exclusionList.getPPMTolerance();
			return "NoExclusion[" + "RT_window: " + retentionTimeWindow + ";ppmTolerance: " + ppmTolerance + "]";
		}

		override
		public ExclusionProfileEnum getAnalysisType()
		{
			return ExclusionProfileEnum.NO_EXCLUSION_PROFILE;
		}


	}


	public class ObservedPeptideRtTrackerObject{
		public String peptideSequence;
		public String peptideSequence_withModification;
		public double arrivalTime;
		public double xcorr;
		public double dCN;
		public double rtPeak;
		public double correctedRT;
		public double offset;
		public double originalRTCalcPredictedValue;
		public int isPredicted;
        public int scanNum;
        public double chainsawMass;
        public double ms2CalcPrecursorNeutralMass;
        public double sqtCalcMass;

        public String parentProteins;
        public ObservedPeptideRtTrackerObject(int _scanNum, String _peptideSequence,String _peptideSequence_withModification, 
            double _arrivalTime, double _xcorr, double _dCN,double _rtPeak, double _correctedRT, 
            double _offset, double _originalRTCalcPredictedValue, int _isPredicted,
            double _chainSawMass, double _ms2CalcNeutralMass, double _sqtCalcMass, String _parentProteins)
		{
            scanNum = _scanNum;
			peptideSequence = _peptideSequence;
			peptideSequence_withModification = _peptideSequence_withModification;
			arrivalTime = _arrivalTime;
			xcorr = _xcorr;
			dCN = _dCN;
			rtPeak = _rtPeak;
			correctedRT = _correctedRT;
			offset = _offset;
			originalRTCalcPredictedValue = _originalRTCalcPredictedValue;
			isPredicted = _isPredicted;
            chainsawMass = _chainSawMass;
            ms2CalcPrecursorNeutralMass = _ms2CalcNeutralMass;
            sqtCalcMass = _sqtCalcMass;
            parentProteins = _parentProteins;
		}
		public ObservedPeptideRtTrackerObject() {
		}
		public static String getHeader()
		{
			return String.Join("\t", "scanNum","pepSeq","pepSeqWithMod", "arrivalTime", "xCorr", "dCN", "rtPeak", "correctedRT", "offset", "originalRTCalcPredictedValue", "isPredicted",
                "chainsawMass","ms2CalcPrecursorNeutralMass","sqtCalcMass","parentProteins");
		}

		override
		public String ToString()
		{
			String str = String.Join("\t", scanNum, peptideSequence, peptideSequence_withModification, arrivalTime, xcorr,dCN, rtPeak, correctedRT, offset, originalRTCalcPredictedValue, isPredicted,
                chainsawMass, ms2CalcPrecursorNeutralMass, sqtCalcMass, parentProteins);
			return str;
		}
	}
}
