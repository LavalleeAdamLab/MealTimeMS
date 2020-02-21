using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MealTimeMS.Util;
namespace MealTimeMS.Tester
{
	class JunkTester
	{
		public static void DoJob()
		{
			System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			double startTime = getCurrentNanoTime();
			Console.WriteLine(startTime);
			for(int i = 0; i < 20; i++)
			{
				Thread.Sleep(500);
				Console.WriteLine(getCurrentNanoTime());
			}
			double endTime = getCurrentNanoTime();
			Console.WriteLine(endTime);

			watch.Stop();
			Console.WriteLine("elapsed: "+ (endTime-startTime));
			//String outputFolder = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\Output";
			//String decoy = CommandLineProcessingUtil.CreateConcacenatedDecoyDB(InputFileOrganizer.FASTA_FILE, outputFolder);
			//String output = CommandLineProcessingUtil.FastaToIDXConverter(decoy,InputFileOrganizer.OutputRoot);
			//Console.WriteLine(output);
		}

		private static double getCurrentNanoTime()
		{
			long nano = DateTime.Now.Ticks;
			nano /= TimeSpan.TicksPerMillisecond;
			return nano/1000;
		}

	}
}
