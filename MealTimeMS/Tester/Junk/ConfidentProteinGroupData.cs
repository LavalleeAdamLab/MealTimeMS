using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MealTimeMS.Util.PostProcessing;
namespace MealTimeMS.Tester.Junk
{
	public class ConfidentProteinGroupData
	{
		public static void DoJob()
		{
			String originalProt = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\PreComputedFiles\\MS_QC_120min_interact.prot.xml";
			var protGroups = ProteinProphetEvaluator.ExtractPositiveProteinGroups(originalProt);
			Console.WriteLine(protGroups.Count);

		}

	}
}
