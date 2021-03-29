using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using MealTimeMS.Data;
using MealTimeMS.Util;
namespace MealTimeMS.Simulation
{
	//deprecated - designed to be used to simulate instrument broadcasting in real time but with TopN functionality. 
    // Replaced with QuickDDAInstrumentSimulation 
	public class DDAInstrumentSimulation : InstrumentSimulation
	{

		int TopN = 0;
		int ms2Processed = 0;
		int ms2Analyzed = 0;
		ConcurrentBag<int> ms2Sent;

		ConcurrentDictionary<int, int> broadcastedMS2; //<scanNum, status> status: 0:broadcasted but not evaluated by DataProcessor yet. 1:MS2 analyzed. -1: MS2 Excluded

		public DDAInstrumentSimulation(List<Spectra> _specList, int _maxMS2NumPerMS1)
		{
			TopN = _maxMS2NumPerMS1;
			//specList is passed in with MS2 only, adding MS1
			specList = AddingMS1SpectraToMS2OnlyList(_specList);
			//TestFunction_WriteNumMS2PerMS1(_specList);

		}


		
		private static List<List<Spectra>> GroupMS2(List<Spectra> fullSpecList)
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
				}else if(spec.getMSLevel() == 2)
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

		public static void TestFunction_WriteNumMS2PerMS1(List<Spectra> _specList)
		{
			StreamWriter sw = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "MS2PerMS1.txt"));
			int first = _specList[0].getScanNum();
			foreach(Spectra spec in _specList)
			{
				if (spec.getScanNum() == 1)
				{
					continue;
				}

				if (spec.getMSLevel() == 1)
				{
					int numMS2 = spec.getScanNum() - first;
					sw.WriteLine(numMS2);
					first = spec.getScanNum();
				}
			}
			sw.Close();
		}
	
		public static List<Spectra> AddingMS1SpectraToMS2OnlyList(List<Spectra> ms2OnlySpecList)
		{
			List<Spectra> specListMS1Included = new List<Spectra>();

			for (int i = 1; i <= ms2OnlySpecList[ms2OnlySpecList.Count-1].getScanNum(); i++)
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

		override
		public void StartInstrument()
		{
			broadcastedMS2 = new ConcurrentDictionary<int, int>();
			DataProcessor.MS2EvaluatedEvent += OnMS2Evaluated;

			List<List<Spectra>> groupedMS2List = GroupMS2(specList);

			//int miliSecondsPerScan = (int)((GlobalVar.ExperimentTotalTimeInMinutes*60000)/ GlobalVar.ExperimentTotalScans);
			OnAcquisitionStreamOpening();
		
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			watch.Start();

			ms2Sent = new ConcurrentBag<int>();
			int counter = 0; //total # of ms2 sent

			foreach(List<Spectra> workingMS2 in groupedMS2List)
			{
				if (counter > maxMS2ToSimulate)
				{
					break;
				}
				
				if (workingMS2.Count <= TopN)
				{
					foreach(Spectra ms2 in workingMS2)
					{
						OnMSScanArrived(ms2);
						counter++;
					}
				}
				else
				{
					
					int ms2ToSendIndex = 0;
					int numbersOfSentMS2 = 0;
					while (true)
					{
						
						Spectra ms2 = workingMS2[ms2ToSendIndex];
						OnMSScanArrived(ms2);
						counter++;
						ms2Sent.Add(ms2.getScanNum());
						ms2ToSendIndex++;
						numbersOfSentMS2++;
						
						while (!Interlocked.Equals(ms2Processed, numbersOfSentMS2))
						{
							//wait until Dataprocessor finishes processing all spectra sent
							Thread.CurrentThread.Join(1);
						}
						
						if (Interlocked.Equals(ms2Analyzed, TopN))
						{
							break;
						}
						if (numbersOfSentMS2 >= workingMS2.Count)
						{
							break;
						}
					}

					ms2Sent = new ConcurrentBag<int>();
					Interlocked.Exchange(ref ms2Processed ,0);
					Interlocked.Exchange(ref ms2Analyzed, 0);
					
				}

			}
			DataProcessor.MS2EvaluatedEvent -= OnMS2Evaluated;
			OnAcquisitionStreamClosing();
		}


		public  void OnMS2Evaluated(object sender, MS2Evaluated e)
		{
			bool analyzed = e.analyzed;

			if(ms2Sent.Contains(e.scanNum))
			{
				if (analyzed)
				{
					Interlocked.Increment(ref ms2Analyzed);
				}
				Interlocked.Increment(ref ms2Processed);
			}
			
		}


		//override
		//public void StartInstrument()
		//{
		//	broadcastedMS2 = new ConcurrentDictionary<int, int>();
		//	DataProcessor.MS2EvaluatedEvent += OnMS2Evaluated;

		//	List<List<Spectra>> groupedMS2List = GroupMS2(specList);

		//	//int miliSecondsPerScan = (int)((GlobalVar.ExperimentTotalTimeInMinutes*60000)/ GlobalVar.ExperimentTotalScans);
		//	OnAcquisitionStreamOpening();

		//	System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
		//	watch.Start();


		//	int counter = 0; //total # of ms2 sent

		//	foreach (List<Spectra> workingMS2 in groupedMS2List)
		//	{


		//		if (workingMS2.Count <= TopN)
		//		{
		//			foreach (Spectra ms2 in workingMS2)
		//			{
		//				OnMSScanArrived(ms2);
		//				counter++;
		//			}
		//		}
		//		else
		//		{
		//			int analyzedMS2 = 0;
		//			int ms2ToSendIndex = 0;
		//			int numbersOfSentMS2 = 0;
		//			while (true)
		//			{

		//				Spectra ms2 = workingMS2[ms2ToSendIndex];
		//				OnMSScanArrived(ms2);
		//				ms2ToSendIndex++;
		//				numbersOfSentMS2++;
		//				broadcastedMS2.TryAdd(ms2.getScanNum(), 0);
		//				while (CountMS2Processed(broadcastedMS2) < numbersOfSentMS2)
		//				{
		//					//wait until Dataprocessor finishes processing all spectra sent
		//				}
		//				analyzedMS2 = CountMS2Analyzed(broadcastedMS2);
		//				if (analyzedMS2 >= TopN)
		//				{
		//					break;
		//				}
		//				if (numbersOfSentMS2 >= workingMS2.Count)
		//				{
		//					break;
		//				}
		//			}
		//			broadcastedMS2.Clear();
		//		}

		//	}
		//	DataProcessor.MS2EvaluatedEvent -= OnMS2Evaluated;
		//	OnAcquisitionStreamClosing();
		//}

		//private int CountMS2Analyzed(ConcurrentDictionary<int, int> dic)
		//{
		//	int total = 0;
		//	foreach (int i in dic.Values)
		//	{
		//		if (i == 1)
		//		{
		//			total++;
		//		}
		//	}
		//	return total;

		//}
		//private int CountMS2Processed(ConcurrentDictionary<int, int> dic)
		//{
		//	int total = 0;
		//	foreach (int i in dic.Values)
		//	{
		//		if (i == 1 || i == -1)
		//		{
		//			total++;
		//		}
		//	}
		//	return total;

		//}

		//public void OnMS2Evaluated(object sender, MS2Evaluated e)
		//{

		//	int scanNum = e.scanNum;
		//	bool analyzed = e.analyzed;
		//	if (broadcastedMS2.ContainsKey(scanNum))
		//	{
		//		broadcastedMS2[scanNum] = analyzed ? 1 : -1;
		//	}
		//}

	}
}
