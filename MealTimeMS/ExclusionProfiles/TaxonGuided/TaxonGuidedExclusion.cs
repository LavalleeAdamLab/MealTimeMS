//using System;
//​
//​
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.ML;
//using MealTimeMS.Data;
//using MealTimeMS.Data.Graph;
//using MealTimeMS.Util;
//using MealTimeMS.IO;
//using MealTimeMS.ExclusionProfiles;
//using Accord.Statistics.Models.Regression;
//using MealTimeMS.ExclusionProfiles.TaxonGuided.TaxonClass;
//using static TaxonDB.Program;
//using TaxonDB;
//​
//namespace MealTimeMS.ExclusionProfiles.TaxonGuided.TaxonClass
//{
//    //taxon CLASS
//​
//​
////// 
///// <summary>
///// </summary>
//public class TaxonGuidedExclusion : ExclusionProfile
//    {
//​​
//        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
//        public List<object[]> excludedProteinFeatureList;
//        public List<double[]> peptideIDRT;
//        Dictionary<string, double> rtCalcPredictedRT;
//​
//        public List<Peptide> SamplePeptideList = new List<Peptide>();
//​
//​
//        //ITransformer lrModel; //deprecated for now, this is ML.NET, not currently in use, using Accord.Net
//        protected LogisticRegression lrAccord; // this is Accord.Net's classifier
//​
//        public object TaxonClass { get; private set; }
//        //internal IEnumerable<Taxon> TaxonList { get; private set; }
//​
        
//​
//        public TaxonGuidedExclusion(String AccordSavedWeight, Database _database,
//                 double _ppmTolerance, double _retentionTimeWindowSize) : this(Loader.LoadAccordNetLogisticRegressionModel(AccordSavedWeight), _database,
//                    _ppmTolerance, _retentionTimeWindowSize)
//        {
//        }
//​
//        public TaxonGuidedExclusion(LogisticRegression accordModel, Database _database,
//                 double _ppmTolerance, double _retentionTimeWindowSize) : base(_database, _ppmTolerance)
//        {
//            rtCalcPredictedRT = new Dictionary<string, double>();
//            peptideIDRT = new List<double[]>();
//            setRetentionTimeWindow(_retentionTimeWindowSize);
//            lrAccord = accordModel;
//            excludedProteinFeatureList = new List<object[]>();
//​
//        }
//​
       
//​
//​
//        override
//        protected void evaluateIdentification(IDs id)
//        {
           
//​
//            // check if the peptide is identified or not
//            if (id == null)
//            {
//                performanceEvaluator.countMS2UnidentifiedAnalyzed();
//                return;
//            }
//            String sequence = id.getPeptideSequence();
//​
//            Peptide pep = getPeptideFromIdentification(id); // id is null, it already returned
//​
            
//            /////////////TAXON
//            string[] TaxonsContainingThisPeptide = Taxon.peptide_taxon_db​[sequence];

//            foreach (Taxon taxon in TaxonList)
//            {
//                taxon.AddSamplePeptides(sequence, taxon); //checks if it is present and then adds and updates the count
//                if (taxon.taxonPresent() == true)
//                {
//                    foreach (string entry in taxon.getDatabasePeptides())
//                    {
//                        Peptide pept = database.getPeptide(entry);
//                        exclusionList.addPeptide(pept); //get the id from the sequences in the DB
//                    }
//                    //exclusionList.addPeptide(taxon.getDatabasePeptides()); //ISSUE I have a list of peptides in DB
//                }
//            }
//​
//​
//​
//            ////////////
//             // creating the list which contains the list of seqyuences
//​
//​
//​
//            Double xCorr = id.getXCorr();
//            double dCN = id.getDeltaCN();
//            pep.addScore(xCorr, dCN);
//#if (!DONTEVALUATE)
//            performanceEvaluator.evaluateAnalysis(exclusionList, pep);
//#endif
//​
//            // exclude this peptide for analysis if the xCorr score is above a threshold
//            const double XCORR_THRESHOLD = 2.5;
//            // add the peptide to the exclusion list if it is over the xCorr threshold
//            if ((xCorr > XCORR_THRESHOLD))
//            {
//                performanceEvaluator.countPeptidesExcluded();
//                log.Debug("xCorrThreshold passed. Peptide added to the exclusion list.");
//                exclusionList.addPeptide(pep);
//                // calibrates our retention time alignment if the observed time is different
//                // from the predicted only if it passes this threshold
//                calibrateRetentionTime(pep);
//            }
//​
//            //Need to have a list of peptides present so I can loop over it to assign taxons?
//​
//            // Add all the peptides corresponding to the parent protein, if the parent
//            // protein is deemed confidently identified by the logisitc regression
//            // classifier
//​
//            //Write a function that finds out which taxons are conf. identified
//            //Add them to exclusion list
//​
           
           
       
//​
////            List<Protein> proteinsToExclude = new List<Protein>();
////            foreach (Protein parentProtein in pep.getProteins())
////            {
////                // prevents repeated exclusion of a protein already excluded
////                if ((!parentProtein.IsExcluded()))
////                {
////                    // determine if parent protein is confidently identified
////                    bool isConfidentlyIdentified = identificationPredictions[parentProtein.getAccession()];
////                    if (isConfidentlyIdentified)
////                    {
////                        // exclude all peptides of that protein
////#if TRACKEXCLUDEDPROTEINFEATURE
////						excludedProteinFeatureList.Add(parentProtein.vectorize().ItemArray);
////#endif
////                        parentProtein.setExcluded(true);
////                        log.Debug("Parent protein " + parentProtein.getAccession() + " is identified confidently "
////                                + parentProtein.getNumDB() + " times!");
////                        performanceEvaluator.countProteinsExcluded();
////                        proteinsToExclude.Add(parentProtein);
////                    }
////                }
////            }
////            exclusionList.addProteins(proteinsToExclude);
//​
//​
//        }
//​
//​
//        override
//        public String ToString()
//        {
//            double retentionTimeWindow = database.getRetentionTimeWindow();
//            double ppmTolerance = exclusionList.getPPMTolerance();
//            double probabilityThreshold = GlobalVar.AccordThreshold;
//            return "MachineLearningGuidedExclusion[" + "RT_window: " + retentionTimeWindow + ";ppmTolerance: "
//                    + ppmTolerance + ";probabilityThreshold: " + probabilityThreshold + "]";
//        //}
//​
//        override
//        public ExclusionProfileEnum getAnalysisType()
//        {
//            return ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE;
//        }
//​
//    }
//​
//}
//​
//​
//​
///*
//foreach (String seq in SamplePeptideList)
//{
//    Peptide pep = database.getPeptide(seqeunce);
//    exlcusionList.AddPepetide(pep)
//            }
//*/