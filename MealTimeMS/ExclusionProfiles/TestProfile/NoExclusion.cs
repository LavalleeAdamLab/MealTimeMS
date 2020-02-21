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
		public List<double[]> peptideIDRT;
		public NoExclusion(Database _database, double _retentionTimeWindowSize) : base(_database)
		{
			rtCalcPredictedRT = new Dictionary<string, double>();
			peptideIDRT = new List<double[]>();
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

			// add decoy or non-existent protein connections
			// database.addProteinFromIdentification(pep, id.getParentProteinAccessions());

			Double xCorr = id.getXCorr();
			Double dCN = id.getDeltaCN();
			pep.addScore(xCorr, 0.0, dCN);
			performanceEvaluator.evaluateAnalysis(exclusionList, pep);

			

			RetentionTime rt = pep.getRetentionTime();
			//actual arrival time, xcorr, rtCalc predicted RT, corrected RT, offset
			
			if (!rtCalcPredictedRT.Keys.Contains(pep.getSequence()))
			{
				rtCalcPredictedRT.Add(pep.getSequence(), rt.getRetentionTimePeak());
			}
			double[] values = new double[] { id.getScanTime(), id.getXCorr(), rt.getRetentionTimePeak(), rt.getRetentionTimeStart() + GlobalVar.retentionTimeWindowSize, RetentionTime.getRetentionTimeOffset(), rtCalcPredictedRT[pep.getSequence()], rt.IsPredicted() ? 1 : 0 };
			

			if ((xCorr > 2.5))
			{
				performanceEvaluator.countPeptidesExcluded();
				log.Debug("xCorrThreshold passed. Peptide added to the exclusion list.");
				// calibrates our retention time alignment if the observed time is different
				// from the predicted only if it passes this threshold
				calibrateRetentionTime(pep);
			}
			values[4] = RetentionTime.getRetentionTimeOffset();
			peptideIDRT.Add(values);
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
}
