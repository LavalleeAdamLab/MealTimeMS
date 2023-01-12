using System;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Collections.Generic;

using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.IO;
using MealTimeMS.Util;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.RunTime;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
using MealTimeMS.ExclusionProfiles.TestProfile;
using MealTimeMS.ExclusionProfiles.Heuristic;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Simulation;
using IMsScan = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;


namespace MealTimeMS
{
	//A scan arrives in the format of IMsScan at the DataReceiver class, which is then passed to ParseIMsScan() of this class and 
	//  parsed into a lighter-weight Scan class object, and the spectra will be stored into the parsedSpectra queue

	//The StartProcessing() function of this class will be running in parallel with the DataReceiver or DataReceiverSimulation class.
	//	This thread will be dequeing the parsedSpectra queue to process the Scans using Exclusion profile.
	public static class DataProcessor
    {
		public static event EventHandler<MS2Evaluated> MS2EvaluatedEvent;
		static ConcurrentQueue<Spectra> parsedSpectra;
        static long taskCounter;
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private static int outputLevel = 1; //0 for detailed, 1 for simple, 2 for peptide only. Value changed by InputHandler Class taking console input
        private static bool running = true;
        static int scanIDCounter;

		public static List<double[]> scanArrivalAndProcessedTimeList;

        private static bool preExperimentSetupFinished = false;
        public static void StartProcessing(ExclusionProfile exclusionProfile)
        {
			log.Debug("Initiating up DataProcessor Variables");
			reset();
            preExperimentSetupFinished = true;
            while (running|| taskCounter > 0) //!parsedSpectra.IsEmpty
			{
				//Database searches any scan in processedScans FIFO queue as long as it's not empty
				
                Spectra processedSpectra;
                if (parsedSpectra.TryDequeue(out processedSpectra))
                {
                    bool analyzed = exclusionProfile.evaluate(processedSpectra);
					double[] scanArrivalProcessedTime = { processedSpectra.getScanNum(), processedSpectra.getArrivalTime(), getCurrentMiliTime() };
					scanArrivalAndProcessedTimeList.Add(scanArrivalProcessedTime);
                    Interlocked.Decrement(ref taskCounter);
                }
            }
            log.Info("DataProcessor finished, processed {0} scans", scanIDCounter);
			
        }
        static public List<double[]> spectraNotAdded;
		static private bool ignore = false;

        //parses the IMsScan into a spectra object so IMSscan object can be released to free memory
        public static void ParseIMsScan(IMsScan arrivedScan)
        {
			scanIDCounter++;
			
			
#if IGNORE
			if (Interlocked.Read(ref taskCounter)>=25)
			{
				ignore = true;
			}
			else if (Interlocked.Read(ref taskCounter) < 10)
			{
				ignore = false;
			}

			if (ignore)
			{
				String scanNum = "";
				arrivedScan.CommonInformation.TryGetValue(GlobalVar.ScanNumHeader, out scanNum);
				spectraNotAdded.Add(new double[] {Double.Parse(scanNum) , getCurrentMiliTime(), -1 });
				Console.WriteLine("ignoring scan "+ scanNum);
				return;
			}
#endif
			Interlocked.Increment(ref taskCounter);
			//Spectra spectra = IMsScanParser.Parse(arrivedScan, scanIDCounter, getCurrentMiliTime());//Parses the IMsScan into a Spectra object 
			Spectra spectra = IMsScanParser.Parse(arrivedScan, scanIDCounter, getCurrentMiliTime());
            parsedSpectra.Enqueue(spectra);
        }

        public static void EndProcessing() {  running = false;  }

        public static void SetOutputLevel(int i){   outputLevel = i;    }

        public static bool SetupFinished() { return preExperimentSetupFinished; }

		public static void reset()
		{
			preExperimentSetupFinished = false;
			running = true;
			taskCounter = 0;
			parsedSpectra = new ConcurrentQueue<Spectra>();
			scanIDCounter = 0;
			scanArrivalAndProcessedTimeList = new List<double[]>();
			spectraNotAdded = new  List<double[]>();
		}
		public static double getCurrentMiliTime()
		{
			//reports current tick in mili seconds
			return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		}

		//outputs the scan information, only used for testing ,
		//now pretty much depricated, wouldn't recommend using it
		private static void DisplayScan(Scan scan, IDs id)
        {

            StringBuilder writeOut = new StringBuilder();
            writeOut.Append(scan.getDetailedString());
            StringBuilder consoleOut = new StringBuilder();

            switch (outputLevel)
            {
                case 0:
                    consoleOut.Append(scan.getDetailedString());
                    break;
                case 1:
                    consoleOut.Append(scan.getSimpleString());
                    break;
                case 2:
                    if (scan.GetSpectra().getMSLevel() == 2)
                    {
                        if (id != null)
                        {
                            consoleOut.AppendLine(id.ToString());
                            writeOut.AppendLine(id.ToString());
                        }
                        else
                        {
                            consoleOut.Append(String.Format("scanNum {0} cannot be matched to a peptide", id.getScanNum()));
                        }
                    }
                    break;
                case 3:
                    consoleOut.Append("available info: ");
                    foreach (String name in scan.infoTable.Keys)
                    {
                        consoleOut.Append("\"" + name + "\" ");
                    }
                    break;
            }
            writeOut.AppendLine();
            consoleOut.AppendLine();
            WriterClass.writeln(writeOut.ToString());
            if (scan.ID % GlobalVar.ScansPerOutput == 0)
            {
                log.Info(consoleOut.ToString());
            }
        }
    }
	public class MS2Evaluated : EventArgs
	{
		public int scanNum;
		public bool analyzed;
		public MS2Evaluated(int _scanNum, bool _analyzed)
		{
			scanNum = _scanNum;
			analyzed = _analyzed;
		}

	}
}
