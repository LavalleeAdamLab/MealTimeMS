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
            //debug--
           // DumpTop20Proteins(exclusionProfile, 0, 1.0);

            List<double> dropPoints = new List<double>() { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18 };
            double dropWindow = 1.5;
            
            //--debug end
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
                    if (GlobalVar.DebugIntervals < 1)
                    {
                        exclusionProfile.evaluateIdentificationAndUpdateCurrentTime(parsedID);
                        double[] scanArrivalProcessedTime = { parsedID.getScanNum(), parsedID.getScanTime(), getCurrentMiliTime() };
                        scanArrivalAndProcessedTimeList.Add(scanArrivalProcessedTime);
                        Interlocked.Decrement(ref taskCounter);
                    }
                    else if(GlobalVar.DebugIntervals == 1)
                    {
                        Interlocked.Decrement(ref taskCounter);
                        if (dropPoints.Count < 1)
                        {
                            continue;
                        }
                        if (parsedID.getScanTime() >= dropPoints[0])
                        {
                            DumpTop20Proteins(exclusionProfile,dropPoints[0],dropWindow);
                            dropPoints.RemoveAt(0);
                        }
                        continue;
                    }else if (GlobalVar.DebugIntervals == 2)
                    {
                        PostDummyIntervals(exclusionProfile);
                        while (true)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                Console.WriteLine("Ignored {0} scans in total", itemsNotAdded.Count);
                                return;
                            }
                        }
                    }
                    
                }
                
            }
            log.Info("DataProcessor finished, processed {0} scans", itemCounter);

        }
        
        static public List<double[]> itemsNotAdded;
        static private bool ignore = false;
        public static void DumpTop20Proteins(ExclusionProfile exclusionProfile, double startRT, double rtWin)
        {
            List<String> accessionsToExclude = new List<String>() { "sp|P07900|HS90A_HUMAN", "sp|P08238|HS90B_HUMAN", "sp|P13639|EF2_HUMAN", "sp|P21333|FLNA_HUMAN", "sp|P35579|MYH9_HUMAN", "sp|P35580|MYH10_HUMAN", "sp|P42704|LPPRC_HUMAN", "sp|P49327|FAS_HUMAN", "sp|P49792|RBP2_HUMAN", "sp|P60709|ACTB_HUMAN", "sp|P63261|ACTG_HUMAN", "sp|P78527|PRKDC_HUMAN", "sp|Q00610|CLH1_HUMAN", "sp|Q14204|DYHC1_HUMAN", "sp|Q14315|FLNC_HUMAN", "sp|Q15149|PLEC_HUMAN", "sp|Q5T4S7|UBR4_HUMAN", "sp|Q6P2Q9|PRP8_HUMAN", "sp|Q9Y490|TLN1_HUMAN" };
            Database db = exclusionProfile.getDatabase();
            List<Protein> proteinsToExclude = new List<Protein>();
            foreach (String accession in accessionsToExclude)
            {
                Protein prot = db.getProtein(accession);
                if(prot == null)
                {
                    Console.WriteLine("Abundant protein {0} not found in the mtms database", accession);
                    continue;
                }
                foreach(Peptide pep in prot.getPeptides())
                {
                    pep.setRetentionTime(new RetentionTime(startRT,startRT+(rtWin*2) ));
                    pep.updateIntervalJson(GlobalVar.ppmTolerance, rtWin, -100);
                }
                proteinsToExclude.Add(prot);
            }
            Console.WriteLine("Dumping the following abundant proteins to exclusionMS: "+
                String.Join(separator:",",accessionsToExclude));
            ((ExclusionMSWrapper_ExclusionList)exclusionProfile.getExclusionList()).pepSeqToIntervalID.Clear();
            exclusionProfile.getExclusionList().addProteins(proteinsToExclude);
        }

        //posts intervals to test if exclusionMS is working.
        public static void PostDummyIntervals(ExclusionProfile exclusionProfile)
        {
           var exclusionMSWrapper = ((ExclusionMSWrapper_ExclusionList)exclusionProfile.getExclusionList());
            String interval1 = (new ExclusionMSInterval(1, 100, 1500, 0, 5, true)).toJSONString();
            String interval2 = (new ExclusionMSInterval(2, 1500, 6000, 5, 10, true)).toJSONString();
            List<String> intervals = new List<string>() { interval1, interval2 };
            exclusionMSWrapper.PostIntervals(intervals);
        }

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
