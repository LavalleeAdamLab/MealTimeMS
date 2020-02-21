using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;


using MealTimeMS.Util;

namespace MealTimeMS.ExclusionProfiles.TestProfile
{
	class CombinedExclusion:MachineLearningGuidedExclusion
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		private double XCORR_THRESHOLD;
		private int numDBThreshold;
		public CombinedExclusion(String logisticRegressionClassifierSaveFile, Database _database,
				 double _ppmTolerance, double _retentionTimeWindowSize, double _XCORR_THRESHOLD, 
				 int _numDBThreshold) : base(logisticRegressionClassifierSaveFile, _database,
				  _ppmTolerance, _retentionTimeWindowSize)
		{
			XCORR_THRESHOLD = _XCORR_THRESHOLD;
			numDBThreshold = _numDBThreshold;
		}

		override
		protected void evaluateIdentification(IDs id)
		{

			// check if the peptide is identified or not
			if (id == null)
			{
				performanceEvaluator.countMS2UnidentifiedAnalyzed();
				return;
			}

			Peptide pep = getPeptideFromIdentification(id); // id is null, it already returned

			// add decoy or non-existent protein connections
			// database.AddProteinFromIdentification(pep, id.getParentProteinAccessions());

			Double xCorr = id.getXCorr();
			Double dCN = id.getDeltaCN();
			pep.addScore(xCorr, XCORR_THRESHOLD, dCN);
#if SIMULATION
			performanceEvaluator.evaluateAnalysis(exclusionList, pep);
#endif

			// exclude this peptide for analysis if the xCorr score is above a threshold
	
			// add the peptide to the exclusion list if it is over the xCorr threshold
			if ((xCorr > XCORR_THRESHOLD))
			{
				performanceEvaluator.countPeptidesExcluded();
				log.Debug("xCorrThreshold passed. Peptide added to the exclusion list.");
				exclusionList.addPeptide(pep);
				// calibrates our retention time alignment if the observed time is different
				// from the predicted only if it passes this threshold
				calibrateRetentionTime(pep);
			}

			// Add all the peptides corresponding to the parent protein, if the parent
			// protein is deemed confidently identified by the logisitc regression
			// classifier
			Dictionary<String, Boolean> identificationPredictions = IdentificationFeatureExtractionUtil
					.assessProteinIdentificationConfidence(pep.getProteins(), lrAccord);

			List<Protein> proteinsToExclude = new List<Protein>();
			foreach (Protein parentProtein in pep.getProteins())
			{
				// prevents repeated exclusion of a protein already excluded
				if ((!parentProtein.IsExcluded()))
				{
					// determine if parent protein is confidently identified
					bool isConfidentlyIdentified = identificationPredictions[parentProtein.getAccession()];
					if (isConfidentlyIdentified||parentProtein.getNumDB()>=numDBThreshold)
					{
						// exclude all peptides of that protein
						//excludedProteinFeatureList.Add(parentProtein.vectorize().ItemArray);
						parentProtein.setExcluded(true);
						log.Debug("Parent protein " + parentProtein.getAccession() + " is identified confidently "
								+ parentProtein.getNumDB() + " times!");
						performanceEvaluator.countProteinsExcluded();
						proteinsToExclude.Add(parentProtein);
					}
				}
			}
			exclusionList.addProteins(proteinsToExclude);

		}


		override
		public ExclusionProfileEnum getAnalysisType()
		{
			return ExclusionProfileEnum.COMBINED_EXCLUSION;
		}


	}
}
