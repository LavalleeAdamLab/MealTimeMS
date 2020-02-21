using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Util;
using MealTimeMS.IO;

namespace MealTimeMS.ExclusionProfiles.Nora
{
	public class NoraExclusion : ExclusionProfile
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

	// Thresholds for exclusion
	private double xCorrThreshold;
	private int numDBThreshold;

	public NoraExclusion(Database _database, double _xCorrThreshold,
			double _ppmTolerance, int _numDBThreshold, double _retentionTimeWindowSize): this(_database, _xCorrThreshold, _ppmTolerance, _numDBThreshold)
		{
		
		setRetentionTimeWindow(_retentionTimeWindowSize);
	}

	public NoraExclusion(Database _database, double _xCorrThreshold,
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

		// add decoy or non-existent protein connections
		// database.addProteinFromIdentification(pep, id.getParentProteinAccessions());

		Double xCorr = id.getXCorr();
			Double dCN = id.getDeltaCN();
		pep.addScore(xCorr, xCorrThreshold, dCN);
		performanceEvaluator.evaluateAnalysis(exclusionList, pep);

		// add the peptide to the exclusion list if it is over the xCorr threshold
		if ((xCorr > xCorrThreshold))
		{
			performanceEvaluator.countPeptidesExcluded();
			log.Debug("xCorrThreshold passed. Peptide added to the exclusion list.");
			exclusionList.addPeptide(pep);

			// calibrates our retention time alignment if the observed time is different
			// from the predicted only if it passes this threshold
			calibrateRetentionTime(pep);
		}
		// add all of the other peptides belonging to the parent protein(s) if numDB
		// threshold is passed
		foreach (Protein parentProtein in pep.getProteins())
		{
			if ((parentProtein.getNumDB() >= numDBThreshold) && (!parentProtein.IsExcluded()))
			{
				parentProtein.setExcluded(true);
				log.Debug("Parent protein " + parentProtein.getAccession() + " is identified confidently "
				 + parentProtein.getNumDB() + " times!");
				performanceEvaluator.countProteinsExcluded();
				exclusionList.addProtein(parentProtein);
			}
			log.Debug(parentProtein);
		}
		log.Debug(pep);

	}

	override
	public String ToString()
	{
		double retentionTimeWindow = database.getRetentionTimeWindow();
		double ppmTolerance = exclusionList.getPPMTolerance();
		return "NoraExclusion[" + "RT_window: " + retentionTimeWindow + ";ppmTolerance: " + ppmTolerance
				+ ";xCorrThreshold: " + xCorrThreshold + ";numDBThreshold: " + numDBThreshold + "]";
	}

	override
	public ExclusionProfileEnum getAnalysisType()
	{
		return ExclusionProfileEnum.NORA_EXCLUSION_PROFILE;
	}
}
}
