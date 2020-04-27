using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.ExclusionProfiles
{
	public class ExperimentScheduler
	{
		public ExclusionProfileEnum exclusionType;
		public double ppmTolerance;
		public double retentionTimeWindowSize;
		public double XCorr_Threshold;
		public int NumDBThreshold;
		public double ClassifierDecisionThreshold;
		ExclusionProfile exclusionProfile;
		
		public ExperimentScheduler(ExclusionProfileEnum _exclusionType, double ppmTol, double rtWin, double xCorr, int numDB, double prThr )
		{
			exclusionType = _exclusionType;
			ppmTolerance = ppmTol;
			retentionTimeWindowSize = rtWin;
			XCorr_Threshold = xCorr;
			NumDBThreshold = numDB;
			ClassifierDecisionThreshold = prThr;
		}
	}
}
