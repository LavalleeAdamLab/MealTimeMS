using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Util;
using MealTimeMS.IO;
using Accord.Statistics.Models.Regression;
using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using Accord.IO;

namespace MealTimeMS.ExclusionProfiles.MachineLearningGuided
{


	//// Uses logistic regression and training data to guide MS data acquisition
	//// Implements ExclusionProfile
	public class SVMExclusion : ExclusionProfile
	{



		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		public List<object[]> excludedProteinFeatureList;


		SupportVectorMachine<Gaussian> svmModel;



		public SVMExclusion(String SVMSavedFile, Database _database,
				 double _ppmTolerance, double _retentionTimeWindowSize) : base(_database, _ppmTolerance)
		{
			setRetentionTimeWindow(_retentionTimeWindowSize);
			svmModel = Serializer.Load(SVMSavedFile, out svmModel);
			excludedProteinFeatureList = new List<object[]>();


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
			double dCN = id.getDeltaCN();
			pep.addScore(xCorr, dCN);
#if (!DONTEVALUATE)
			performanceEvaluator.evaluateAnalysis(exclusionList, pep);
#endif

			// exclude this peptide for analysis if the xCorr score is above a threshold
			const double XCORR_THRESHOLD = 2.5;
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
					.assessProteinIdentificationConfidence(pep.getProteins(), svmModel);

			List<Protein> proteinsToExclude = new List<Protein>();
			foreach (Protein parentProtein in pep.getProteins())
			{
				// prevents repeated exclusion of a protein already excluded
				if ((!parentProtein.IsExcluded()))
				{
					// determine if parent protein is confidently identified
					bool isConfidentlyIdentified = identificationPredictions[parentProtein.getAccession()];
					if (isConfidentlyIdentified)
					{
						// exclude all peptides of that protein
						excludedProteinFeatureList.Add(parentProtein.vectorize().ItemArray);
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
		public String ToString()
		{
			double retentionTimeWindow = database.getRetentionTimeWindow();
			double ppmTolerance = exclusionList.getPPMTolerance();
			double probabilityThreshold = GlobalVar.AccordThreshold;
			return "MachineLearningGuidedExclusion[" + "RT_window: " + retentionTimeWindow + ";ppmTolerance: "
					+ ppmTolerance + ";probabilityThreshold: " + probabilityThreshold + "]";
		}

		override
		public ExclusionProfileEnum getAnalysisType()
		{
			return ExclusionProfileEnum.SVMEXCLUSION;
		}

	}

}



