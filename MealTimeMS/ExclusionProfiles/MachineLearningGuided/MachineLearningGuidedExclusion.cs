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

namespace MealTimeMS.ExclusionProfiles.MachineLearningGuided
{


    //// Uses logistic regression and training data to guide MS data acquisition
	//// Implements ExclusionProfile
	//Optimal values: 5 ppm, 1 rtWin, 0.5 prThr
	/// <summary>
	/// </summary>
    public class MachineLearningGuidedExclusion : ExclusionProfile
    {

		
		
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		public List<object[]> excludedProteinFeatureList;
		public List<double[]> peptideIDRT;
		Dictionary<string, double> rtCalcPredictedRT;
		//ITransformer lrModel; //deprecated for now, this is ML.NET, not currently in use, using Accord.Net
		protected  LogisticRegression lrAccord; // this is Accord.Net's classifier
		
		public MachineLearningGuidedExclusion(String AccordSavedWeight, Database _database,
                 double _ppmTolerance, double _retentionTimeWindowSize) : this(Loader.LoadAccordNetLogisticRegressionModel(AccordSavedWeight), _database,
                    _ppmTolerance, _retentionTimeWindowSize)
        {
        }

        public MachineLearningGuidedExclusion(LogisticRegression accordModel ,Database _database,
                 double _ppmTolerance, double _retentionTimeWindowSize):base(_database,  _ppmTolerance)
        {
			rtCalcPredictedRT = new Dictionary<string, double>();
			peptideIDRT = new List<double[]>();
            setRetentionTimeWindow(_retentionTimeWindowSize);
			lrAccord = accordModel;
			//lrAccord = new LogisticRegression();
			//lrAccord.Weights = new double[] { -0.014101223713988448, 0.40498899120575244, -0.4050931006103277, -0.6514251562095439, -1.4199639211914807, -0.00154170434120518, -0.0017589165180070616, -0.001427050540781882, -0.006890591731651152, 0.23434955458842885, 0.24386505335051745, 0.25265687551174654, 0.34976191542247076, 0.17989186249395828, 0.15598728100439885 };
			//lrAccord.Intercept = -2.0771355924182346;
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
            if(pep == null)
            {
                return;
            }
			//log.Info("Peptide Observed Time: {0}\tPredicted Time: {1} -----------------", id.getScanTime(),pep.getRetentionTime().getRetentionTimeStart());


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
                // calibrates our retention time alignment if the observed time is different
                // from the predicted only if it passes this threshold
                calibrateRetentionTime(pep);
                exclusionList.addPeptide(pep);
                log.Debug("xCorrThreshold passed. Peptide added to the exclusion list.");
                performanceEvaluator.countPeptidesExcluded();
            }
			
			// Add all the peptides corresponding to the parent protein, if the parent
			// protein is deemed confidently identified by the logisitc regression
			// classifier
			Dictionary<String, Boolean> identificationPredictions = IdentificationFeatureExtractionUtil
                    .assessProteinIdentificationConfidence( pep.getProteins(), lrAccord);

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
#if TRACKEXCLUDEDPROTEINFEATURE
						excludedProteinFeatureList.Add(parentProtein.vectorize().ItemArray);
#endif
                        parentProtein.setExcluded(true);
                        log.Debug("Confidence for parent protein " + parentProtein.getAccession()+"has crossed the probability threshold," +
                            "adding to exclusion list.");
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
            return ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE;
        }

    }

}



