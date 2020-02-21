using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.Data;
using MealTimeMS.Util;

namespace MealTimeMS.Tester
{
	public class PostProcessingTester
	{
		public static void DoJob()
		{
			String comet = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\MS_QC_120min.pep.xml";
			String output = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\";
			ProteinProphetResult ppr = PostProcessingScripts.RunProteinProphet(comet, output, true);
			Console.WriteLine(ppr.ToString());
		}
		
	}
}
