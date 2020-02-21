using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Util.PostProcessing;

namespace MealTimeMS.Tester
{
	class ProteinProphetResultTester
	{
		public static void DoJob()
		{
			String proteinProphetFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\Output\\Temp\\protein_prophet_output\\MS_QC_120min_interact.prot.xml";
			ProteinProphetResult ppr = ProteinProphetEvaluator.getProteinProphetResult(proteinProphetFile);
			Console.WriteLine(ppr.ToString());
		}
	}
}
