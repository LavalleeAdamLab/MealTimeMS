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
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Models.Regression;

namespace MealTimeMS.ExclusionProfiles.MachineLearningGuided
{


	//// THIS PROFILE IS DEPRICATED, SUCCEEDED BY MLGESequenceExclusion.cs
	public class SequenceExclusion : ExclusionProfile
	{



		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		public Dictionary<String, double> peptideRT;
		ITransformer lrModel;
		LogisticRegression lrAccord;

		public SequenceExclusion(String logisticRegressionClassifierSaveFile, Database _database,
				 double _ppmTolerance, double _retentionTimeWindowSize) : this(Loader.loadLogisticRegressionModel(logisticRegressionClassifierSaveFile), _database,
					_ppmTolerance, _retentionTimeWindowSize)
		{
		}

		public SequenceExclusion(ITransformer _lrModel, Database _database,
				 double _ppmTolerance, double _retentionTimeWindowSize) : base(_database, _ppmTolerance)
		{
			lrModel = _lrModel;
			setRetentionTimeWindow(_retentionTimeWindowSize);
			lrAccord = new LogisticRegression();
			lrAccord.Weights = new double[] { -0.014101223713988448, 0.40498899120575244, -0.4050931006103277, -0.6514251562095439, -1.4199639211914807, -0.00154170434120518, -0.0017589165180070616, -0.001427050540781882, -0.006890591731651152, 0.23434955458842885, 0.24386505335051745, 0.25265687551174654, 0.34976191542247076, 0.17989186249395828, 0.15598728100439885 };
			lrAccord.Intercept = -2.0771355924182346;
			peptideRT = new Dictionary<String, double>();

		}

		public ITransformer getLogisticRegressionModel()
		{
			return lrModel;
		}

		//public double getProbabilityThreshold()
		//{
		//    return lrModel.getThreshold();
		//}

		public void setLogisticRegressionModel(ITransformer lrm)
		{
			lrModel = lrm;
		}

		public void setProbabilityThreshold(double d)
		{
			//lrModel.setThreshold(d);
		}
		override
		protected bool processMS2(Spectra spec)
		{
			performanceEvaluator.countMS2();

			log.Debug(spec);
			IDs id = performDatabaseSearch(spec);

			// check if mass is on exclusion list

			Boolean isExcluded = false;
			if (id != null)
			{
				Peptide pep = getPeptideFromIdentification(id);
				 isExcluded = exclusionList.containsPeptide(pep); //checks if the mass should've been excluded, 
																  //in a real experiment, this should never equal to true
																  //since the mass should not have been scanned in the first place 
																  //if MS exclusion table was updated correctly through API
				if (!peptideRT.Keys.Contains(pep.getSequence()))
				{
					peptideRT.Add(pep.getSequence(), spec.getStartTime());
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
					.assessProteinIdentificationConfidence(pep.getProteins(), lrAccord);

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
			return ExclusionProfileEnum.MLGE_SEQUENCE_EXCLUSION_PROFILE;
		}

	}

}



