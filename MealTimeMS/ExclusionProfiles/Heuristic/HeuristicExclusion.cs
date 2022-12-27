using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Util;
using MealTimeMS.IO;

namespace MealTimeMS.ExclusionProfiles.Heuristic
{
	public class HeuristicExclusion : ExclusionProfile
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

	// Thresholds for exclusion
	private double xCorrThreshold;
	private int numDBThreshold;

	public HeuristicExclusion(Database _database, double _xCorrThreshold,
			double _ppmTolerance, int _numDBThreshold, double _retentionTimeWindowSize): this(_database, _xCorrThreshold, _ppmTolerance, _numDBThreshold)
		{
		
		setRetentionTimeWindow(_retentionTimeWindowSize);
	}

	public HeuristicExclusion(Database _database, double _xCorrThreshold,
			double _ppmTolerance, int _numDBThreshold):base(_database, _ppmTolerance)
	{
		
		xCorrThreshold = _xCorrThreshold;
		numDBThreshold = _numDBThreshold;
	}

	public double getXCorrThreshold()
	{
		return xCorrThreshold;
	}

	public double getNumDBThreshold()
	{
		return numDBThreshold;
	}

	public void setXCorrThreshold(double _xCorrThreshold)
	{
		xCorrThreshold = _xCorrThreshold;
	}

	public void setNumDBThreshold(int _numDBThreshold)
	{
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
		pep.addScore(xCorr, xCorrThreshold, dCN); // updates the peptide score and numDB for each parent protein of the peptide
		performanceEvaluator.evaluateAnalysis(exclusionList, pep);

		// add the peptide to the exclusion list if it is over the xCorr threshold
		if ((xCorr > 2.5))
		{
			
			// calibrates our retention time alignment if the observed time is different
			// from the predicted only if it passes this threshold
			calibrateRetentionTime(pep);
            exclusionList.addPeptide(pep);
                log.Debug("xCorrThreshold passed. Peptide added to the exclusion list.");
                performanceEvaluator.countPeptidesExcluded();

            }


            // add all of the other peptides belonging to the parent protein(s) if numDB
            // threshold is passed
            List<Protein> proteinsToExclude = new List<Protein>();
            foreach (Protein parentProtein in pep.getProteins())
		    {
			    if ((parentProtein.getNumDB() >= numDBThreshold) && (!parentProtein.IsExcluded()))
			    {
				    parentProtein.setExcluded(true);
				    log.Debug("Parent protein " + parentProtein.getAccession() + " is identified confidently "
				     + parentProtein.getNumDB() + " times!");
				    performanceEvaluator.countProteinsExcluded();
#if TRACKEXCLUSIONLISTOPERATION
                     exclusionList.addProtein(parentProtein, id.getScanNum());
#else
                    proteinsToExclude.Add(parentProtein);
#endif
                }
			    log.Debug(parentProtein);
		    }
            exclusionList.addProteins(proteinsToExclude);
		    log.Debug(pep);

	}

	override
	public String ToString()
	{
		double retentionTimeWindow = database.getRetentionTimeWindow();
		double ppmTolerance = exclusionList.getPPMTolerance();
		return "HeuristicExclusion[" + "RT_window: " + retentionTimeWindow + ";ppmTolerance: " + ppmTolerance
				+ ";xCorrThreshold: " + xCorrThreshold + ";numDBThreshold: " + numDBThreshold + "]";
	}

	override
	public ExclusionProfileEnum getAnalysisType()
	{
		return ExclusionProfileEnum.HEURISTIC_EXCLUSION_PROFILE;
	}
}
}
