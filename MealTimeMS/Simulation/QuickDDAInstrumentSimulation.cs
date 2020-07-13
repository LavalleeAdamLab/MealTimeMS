using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MealTimeMS.Data;
using MealTimeMS.ExclusionProfiles;

namespace MealTimeMS.Simulation
{
	//bypasses dataprocessor and receiver
	public class QuickDDAInstrumentSimulation
	{
		static int maxMS2ToSimulate = 1000000000;

		public QuickDDAInstrumentSimulation(Experiment e, List<Spectra> _specList, int _maxMS2NumPerMS1)
		{
			int TopN = _maxMS2NumPerMS1;
			//specList is passed in with MS2 only, adding MS1
			List<Spectra> MS1IncludedSpecList = AddingMS1SpectraToMS2OnlyList(_specList);
			List<List<Spectra>> groupedMS2 = GroupMS2(MS1IncludedSpecList);

			StartSimulation(e.exclusionProfile, groupedMS2, TopN);
			//TestFunction_WriteNumMS2PerMS1(_specList);

		}
		private static void StartSimulation(ExclusionProfile exclusionProfile, List<List<Spectra>> groupedMS2, int TopN)
		{
			int totalMS2SentCounter = 0;
			foreach (List<Spectra> workingMS2 in groupedMS2)
			{
				if (totalMS2SentCounter > maxMS2ToSimulate)
				{
					break;
				}

				int ms2ToSendIndex = 0;
				int numMS2Analyzed = 0;
				while (ms2ToSendIndex < workingMS2.Count)
				{

					Spectra ms2 = workingMS2[ms2ToSendIndex];
					bool analyzed = exclusionProfile.evaluate(ms2);
					if (analyzed)
					{
						numMS2Analyzed++;
					}
					totalMS2SentCounter++;
					ms2ToSendIndex++;

					if (numMS2Analyzed>=TopN)
					{
						break;
					}
				}				

			}

		}

		//From a full list of only ms2, groups them by their ms1 according to the missing scan nums in between the ms2
		public static List<List<Spectra>> GroupMS2(List<Spectra> fullSpecList)
		{
			List<List<Spectra>> groupedList = new List<List<Spectra>>();
			List<Spectra> group = new List<Spectra>();
			for (int i = 0; i < fullSpecList.Count; i++)
			{
				Spectra spec = fullSpecList[i];
				if (spec.getMSLevel() == 1)
				{
					if (group.Count > 0)
					{
						groupedList.Add(group);
						group = new List<Spectra>();
					}
					continue;
				}
				else if (spec.getMSLevel() == 2)
				{
					group.Add(spec);
				}
			}

			if (group.Count > 0)
			{
				groupedList.Add(group);
			}
			return groupedList;

		}
		public static List<Spectra> AddingMS1SpectraToMS2OnlyList(List<Spectra> ms2OnlySpecList)
		{
			List<Spectra> specListMS1Included = new List<Spectra>();

			for (int i = 1; i <= ms2OnlySpecList[ms2OnlySpecList.Count - 1].getScanNum(); i++)
			{
				//creating empty MS1 scans to populate full specList
				specListMS1Included.Add(Spectra.CreateEmptyMS1(i));
			}

			foreach (Spectra spec in ms2OnlySpecList)
			{
				//replacing MS1 spectra with MS2 at the correct scanNum
				if (spec.getMSLevel() == 2)
				{
					specListMS1Included[(spec.getScanNum() - 1)] = spec;
				}
			}
			return specListMS1Included;
		}
	}
}
