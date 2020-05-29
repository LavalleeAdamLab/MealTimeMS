using System;
using System.Text;
using System.IO;
using System.Threading;
using MealTimeMS.Util;
namespace MealTimeMS
{
    public static class WriterClass
    {
		static ReaderWriterLock rwl = new ReaderWriterLock();
        static StreamWriter sw;
        static StreamWriter sw2;
		static StreamWriter sw3;
		static StreamWriter sw4;
		static StreamWriter sw5;
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
		static System.Diagnostics.Stopwatch watch;

		public static void initiateWriter(String filePath)
        {
           
            Console.WriteLine("Writing results to: {0}\\",Path.GetDirectoryName( filePath));
          

            sw = new StreamWriter(filePath+"_Summary.txt");
			sw2 = new StreamWriter(filePath+"_ProcessTime_output.txt");
			//sw3 = new StreamWriter(filePath + "_ExcludedSpectra.txt");
			//sw4 = new StreamWriter(filePath + "_IncludedSpectra.txt");
			sw5 = new StreamWriter(filePath + "_PeptideRTTime.txt");
        }

        public static void ExperimentOutputSetUp()
        {
            String name = "";
            bool outputInvalid = true;
            String outputFolder = "";
            while (outputInvalid)
            {
                Console.WriteLine("Enter an output file name");
                name = Console.ReadLine();
                //if (!name.Contains(".txt"))
                //{
                //    name = name + ".txt";
                //}

                outputFolder = Path.Combine(InputFileOrganizer.OutputRoot, name);
               
                
                if (Directory.Exists(outputFolder))
                {
                    Console.WriteLine("File already exists: {0} \n would you like to overwrite? (y/n)", name);
                    if (Console.ReadKey().KeyChar.Equals('y'))
                    {
                        Directory.Delete(outputFolder);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            GlobalVar.experimentName = name;
            Directory.CreateDirectory(outputFolder);
            InputFileOrganizer.OutputFolderOfTheRun = outputFolder;
			Directory.CreateDirectory(outputFolder + "\\preExperimentFiles");
			InputFileOrganizer.preExperimentFilesFolder = outputFolder + "\\preExperimentFiles";

			String summaryOutputFile = Path.Combine(outputFolder, name); 
            initiateWriter(summaryOutputFile);
            writeln(DateTime.Now.ToString() + "\t"+name);
            sw2.WriteLine(DateTime.Now.ToString() + "\t" + name);
			watch = System.Diagnostics.Stopwatch.StartNew();
		}
        public static void writeln(String str, writerClassOutputFile writerNum)
        {
            switch (writerNum)
            {
                case writerClassOutputFile.scanArrivalAndProcessedTime:
                    sw2.WriteLine(str);
                    break;
				case writerClassOutputFile.ExcludedSpectraScanNum:
					sw3.WriteLine(str);
					break;
				case writerClassOutputFile.IncludedSpectraScanNum:
					sw4.WriteLine(str);
					break;
				case writerClassOutputFile.peptideRTTime:
					sw5.WriteLine(str);
					break;
			}
            
        }
		public static void LogScanTime(String scanStatus, int scanID)
		{
			//writeln(watch.ElapsedMilliseconds+"\t"+scanStatus+"\t"+ scanID);
		}


		public static void writeln(String str)
        {
			//using read-write lock since this program is multithreaded
			rwl.AcquireWriterLock(100000);
			try
			{
				// It's safe for this thread to access from the shared resource.
				sw.WriteLine(str);
				sw.Flush();
			}
			finally
			{
				// Ensure that the lock is released.
				rwl.ReleaseWriterLock();
			}
        }
		//Creates an instant of streamWriter and writer a string
		public static void QuickWrite(String str, String outputFileName)
		{
			String outputDirectory = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, outputFileName);
			StreamWriter tempSw = new StreamWriter(outputDirectory);
			tempSw.WriteLine(str);
			tempSw.Close();

		}

        public static void CloseWriter()
        {
            sw.Close();
            sw2.Close();
			//sw3.Close();
			//sw4.Close();
			sw5.Close();

		}

        public static void writePrintln(String str)
        {
            sw.WriteLine(str);
            logger.Info(str);
        }

        public static void Flush()
        {
            sw.Flush();
        }
        

    }


	public enum writerClassOutputFile
	{
		summary,
		scanArrivalAndProcessedTime,
		ExcludedSpectraScanNum,
		IncludedSpectraScanNum,
		peptideRTTime
	}
}
