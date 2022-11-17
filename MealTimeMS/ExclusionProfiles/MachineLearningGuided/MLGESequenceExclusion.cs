using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.ExclusionProfiles.TestProfile;
using MealTimeMS.Util;
using MealTimeMS.IO;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Models.Regression;


namespace MealTimeMS.ExclusionProfiles.MachineLearningGuided
{
	class MLGESequenceExclusion: MachineLearningGuidedExclusion
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		public MLGESequenceExclusion(String logisticRegressionClassifierSaveFile, Database _database,
				 double _ppmTolerance, double _retentionTimeWindowSize):base( logisticRegressionClassifierSaveFile,  _database,
				  _ppmTolerance,  _retentionTimeWindowSize)
		{
            exclusionList = new SequenceExclusionList(-1);

		}
		override
			protected bool processMS2(Spectra spec)
		{
			performanceEvaluator.countMS2();
			log.Debug(spec);
			IDs id = performDatabaseSearch(spec);

			Boolean isExcluded = false;
			if (id != null)
			{
				Peptide pep = getPeptideFromIdentification(id);
                if(pep != null)
                {
				isExcluded = exclusionList.containsPeptide(pep);

                }
			}
			if (isExcluded)
			{
#if SIMULATION
				//log.Debug("Mass " + spectraMass + " is on the exclusion list. Scan " + spec.getScanNum() + " excluded.");
				evaluateExclusion(id);
				WriterClass.LogScanTime("Excluded", (int)spec.getIndex());
#endif
				performanceEvaluator.countExcludedSpectra();
				excludedSpectra.Add(spec.getScanNum());
				return false;
			}
			else
			{
				performanceEvaluator.countAnalyzedSpectra();
				//log.Debug("Mass " + spectraMass + " was not on the exclusion list. Scan " + spec.getScanNum() + " analyzed.");
				evaluateIdentification(id);
				includedSpectra.Add(spec.getScanNum());
				// calibrate peptide if the observed retention time doesn't match the predicted
				//WriterClass.LogScanTime("Processed", (int)spec.getIndex());
				return true;
			}


		}
		override
		public ExclusionProfileEnum getAnalysisType()
		{
			return ExclusionProfileEnum.MLGE_SEQUENCE_EXCLUSION_PROFILE;
		}


	}
}
