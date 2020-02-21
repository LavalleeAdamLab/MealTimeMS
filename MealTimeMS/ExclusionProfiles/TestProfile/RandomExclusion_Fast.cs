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

	// This class was created to see how well our guided exclusion compares to randomly excluding MS2 spectra
	public class RandomExclusion_Fast : ExclusionProfile
	{

		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		private List<int> randomlyExcludedScans;

		public RandomExclusion_Fast(Database _database, List<Spectra> spectraArray,
			int numExcluded, int numAnalyzed, int maxNumMS2PerMS1) : base(_database)
		{
			setUpRandomlyExcludedMS2_NoneDDA(spectraArray, numExcluded, numAnalyzed, maxNumMS2PerMS1);
			//setUpRandomlyExcludedMS2(spectraArray, numExcluded, numAnalyzed, maxNumMS2PerMS1);
		}


		private void setUpRandomlyExcludedMS2_NoneDDA(List<Spectra> ms2SpectraArray, int numExcluded, int numAnalyzed,
					int maximumNumberOfMS2Spectra)
		{
			ms2SpectraArray = CloneList(ms2SpectraArray); //just removes the reference to the original variable, so any operation to the new list doesn't mess with the original list up outside of this function
			randomlyExcludedScans = new List<int>();
			Random r = new Random();
			while (ms2SpectraArray.Count > numAnalyzed)
			{
				int pos = r.Next(ms2SpectraArray.Count);
				Spectra spectraToBeExcluded = ms2SpectraArray[pos];
				randomlyExcludedScans.Add(spectraToBeExcluded.getScanNum());
				ms2SpectraArray.RemoveAt(pos);
			}
		}
		private List<Spectra> CloneList(List<Spectra> ogList)
		{
			List<Spectra> newList = new List<Spectra>();
			foreach (Spectra spec in ogList)
			{
				newList.Add(spec);
			}
			return newList;
		}

		/*
		 * Alex Stuff: Accounted for DDA
		 * Goes through the spectra array and randomly excludes numExcluded of these
		 * scans
		 */
		private void setUpRandomlyExcludedMS2(List<Spectra> spectraArray, int numExcluded, int numAnalyzed,
				int maximumNumberOfMS2Spectra)
		{

			List<List<Spectra>> groupedByMS1 = GroupedByMS1(spectraArray);
			Dictionary<int, List<Spectra>> ms1ScanNumberToRemainingMS2 = new Dictionary<int, List<Spectra>>();
			List<Spectra> spectraEligibleForExclusion = new List<Spectra>();
			foreach (List<Spectra> groupedSpectra in groupedByMS1)
			{
				int ms1ScanNum = groupedSpectra[0].getScanNum();

				// remove the MS1 spectra
				groupedSpectra.RemoveAt(0);

				// keep adding MS2 until max number of MS2 added or no more MS2
				int numMS2Added = 0;
				while (groupedSpectra.Count != 0)
				{
					Spectra s = groupedSpectra[0];
					groupedSpectra.RemoveAt(0);
					spectraEligibleForExclusion.Add(s);
					if (++numMS2Added >= maximumNumberOfMS2Spectra)
					{
						break;
					}
				}

				ms1ScanNumberToRemainingMS2.Add(ms1ScanNum, groupedSpectra);
			}

			// Keep adding randomly selected spectra to randomly excluded list
			randomlyExcludedScans = new List<int>();
			Random r = new Random();
			while (spectraEligibleForExclusion.Count > numAnalyzed)
			{
				int pos = r.Next(spectraEligibleForExclusion.Count);
				Spectra randomlyExcludedSpectra = spectraEligibleForExclusion[pos];
				spectraEligibleForExclusion.RemoveAt(pos);
				randomlyExcludedScans.Add(randomlyExcludedSpectra.getScanNum());

				/* update spectraEligibleForExclusion */
				int specArrIndex = spectraArray.IndexOf(randomlyExcludedSpectra);
				Spectra parentMS1 = null;
				// find the parent MS1 spectra by backtracking in spectraArray
				while (specArrIndex >= 0)
				{
					Spectra prevSpectra = spectraArray[specArrIndex];
					if (prevSpectra.getMSLevel() == 1)
					{
						parentMS1 = prevSpectra;
						break;
					}
				}
				// add newly eligible MS2 to candidate list
				List<Spectra> ms2RemainingToAdd = ms1ScanNumberToRemainingMS2[parentMS1.getScanNum()];
				if (ms2RemainingToAdd.Count != 0)
				{
					Spectra specToAdd = ms2RemainingToAdd[0];
					spectraEligibleForExclusion.Add(specToAdd);
					ms2RemainingToAdd.RemoveAt(0);
				}

			}

		}

		private static List<List<Spectra>> GroupedByMS1(List<Spectra> spectraArray)
		{
			List<List<Spectra>> groupedSpectra = new List<List<Spectra>>();
			List<Spectra> workingArray = new List<Spectra>();
			foreach (Spectra s in spectraArray)
			{
				int msLevel = s.getMSLevel();
				if (msLevel == 1)
				{
					if (workingArray.Count != 0)
					{
						groupedSpectra.Add((workingArray));
					}
					workingArray = new List<Spectra>();
					workingArray.Add(s);
				}
				else
				{
					workingArray.Add(s);
				}
			}
			groupedSpectra.Add((workingArray));
			return groupedSpectra;
		}

		override
		protected void evaluateIdentification(IDs id)
		{
			
		}

		override
		protected void evaluateExclusion(IDs id)
		{

		}

		override
		protected bool processMS2(Spectra spec)
		{
			log.Debug(spec);
			performanceEvaluator.countMS2();
			//IDs id = performDatabaseSearch(spec);
			bool isExcluded = randomlyExcludedScans.Contains(spec.getScanNum());
			if (isExcluded)
			{
				log.Debug("MS2 spectra was randomly excluded");
				performanceEvaluator.countExcludedSpectra();
				//evaluateExclusion(id);
				excludedSpectra.Add(spec.getScanNum());
				return false;
			}
			else
			{
				log.Debug("MS2 spectra was analyzed");
				performanceEvaluator.countAnalyzedSpectra();
				//evaluateIdentification(id);
				includedSpectra.Add(spec.getScanNum());
				return true;
			}
		}

		override
		public String ToString()
		{
			double retentionTimeWindow = database.getRetentionTimeWindow();
			double ppmTolerance = exclusionList.getPPMTolerance();
			return "RandomExclusion[" + "RT_window: " + retentionTimeWindow + ";ppmTolerance: " + ppmTolerance + "]";
		}

		override
		public ExclusionProfileEnum getAnalysisType()
		{
			return ExclusionProfileEnum.RANDOM_EXCLUSION_PROFILE;
		}

	}
}
