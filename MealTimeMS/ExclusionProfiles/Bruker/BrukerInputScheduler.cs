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


namespace MealTimeMS.Util
{
    //A scan arrives in the format of IMsScan at the DataReceiver class, which is then passed to ParseIMsScan() of this class and 
    //  parsed into a lighter-weight Scan class object, and the spectra will be stored into the parsedSpectra queue

    //The StartProcessing() function of this class will be running in parallel with the DataReceiver or DataReceiverSimulation class.
    //	This thread will be dequeing the parsedSpectra queue to process the Scans using Exclusion profile.
    public static class BrukerInputScheduler
    {
        static ConcurrentQueue<IDs> parsedItem;
        static long taskCounter;
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private static int outputLevel = 1; //0 for detailed, 1 for simple, 2 for peptide only. Value changed by InputHandler Class taking console input
        private static bool running = true;
        static int itemCounter;

        public static List<double[]> scanArrivalAndProcessedTimeList;

        private static bool preExperimentSetupFinished = false;
        public static void StartProcessing(ExclusionProfile exclusionProfile, CancellationToken ct)
        {
            log.Debug("Initiating input message scheduler variables");
            reset();
            preExperimentSetupFinished = true;
            Console.WriteLine("Input message scheduler running");
            while (true) //!parsedSpectra.IsEmpty
            {
                if (ct.IsCancellationRequested)
                {
                    Console.WriteLine("Ignored {0} scans in total", itemsNotAdded.Count);
                    return;
                }
                //Processes any psm in parsedItem FIFO queue as long as it's not empty
                IDs parsedID;
                if (parsedItem.TryDequeue(out parsedID))
                {
                    exclusionProfile.evaluateIdentificationAndUpdateCurrentTime(parsedID);
                    double[] scanArrivalProcessedTime = { parsedID.getScanNum(), parsedID.getScanTime(), getCurrentMiliTime() };
                    scanArrivalAndProcessedTimeList.Add(scanArrivalProcessedTime);
                    Interlocked.Decrement(ref taskCounter);
                }
                
            }
            log.Info("DataProcessor finished, processed {0} scans", itemCounter);

        }
        static public List<double[]> itemsNotAdded;
        static private bool ignore = false;

        //parses the psmProlucid object received from the psm stream into an IDs object that MTMS can interpret, and adds it to the queue to be processed
        public static void EnqueueProlucidPSM(IDs id)
        {
            itemCounter++;
            if (id == null || id.getXCorr() < 0.1)
            {
                //ignore unmatched or id with xcor less than 0.1
                return;
            }
#if IGNORE
            if (Interlocked.Read(ref taskCounter)>=200)
			{
				ignore = true;
			}
			else if (Interlocked.Read(ref taskCounter) < 10)
			{
				ignore = false;
			}

			if (ignore)
			{
                int scanNum = id.getScanNum();
				itemsNotAdded.Add(new double[] {scanNum , getCurrentMiliTime(), -1 });
				//Console.WriteLine("ignoring scan "+ scanNum);
				return;
			}
#endif
            Interlocked.Increment(ref taskCounter);
            parsedItem.Enqueue(id);
        }

        public static void EndProcessing() { running = false; }

        public static bool SetupFinished() { return preExperimentSetupFinished; }

        public static void reset()
        {
            preExperimentSetupFinished = false;
            running = true;
            taskCounter = 0;
            parsedItem = new ConcurrentQueue<IDs>();
            itemCounter = 0;
            scanArrivalAndProcessedTimeList = new List<double[]>();
            itemsNotAdded = new List<double[]>();
        }
        public static double getCurrentMiliTime()
        {
            //reports current tick in mili seconds
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static void WriteScanArrivalProcessedTime()
        {

            String header = "ScanNum\tArrival\tProcessed";
            WriterClass.writeln(header, writerClassOutputFile.scanArrivalAndProcessedTime);
            List<double[]> listOfAllItems = scanArrivalAndProcessedTimeList;
            listOfAllItems.AddRange(itemsNotAdded);
            foreach (double[] scan in listOfAllItems)
            {
                String data = "";
                foreach (double d in scan)
                {
                    data = data + d + "\t";
                }
                WriterClass.writeln(data, writerClassOutputFile.scanArrivalAndProcessedTime);
            }

        }


    }
   
}
