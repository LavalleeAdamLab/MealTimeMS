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
			//String comet = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\90minNoExclusion_3\PartialCometFile\090minNoExclusion_3_partial.pep.xml";
			String comet = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\TestData\20200821K562300ng90min_1_Slot1-1_1_1638_nopd_replaced.pep.xml";
			String output = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\ProteinProphetTesterOutput";

            ProteinProphetResult ppr = PostProcessingScripts.RunProteinProphet(comet, output, true);
            String proteinProphetOutput = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\Output\ProteinProphetTesterOutput\protein_prophet_output\20200821K562300ng90min_1_Slot1-1_1_1638_nopd_replaced_interact.prot.xml";
            //ProteinProphetResult ppr = ProteinProphetEvaluator.getProteinProphetResult(proteinProphetOutput);
            ppr.SetProteinGroup(ProteinProphetEvaluator.ExtractPositiveProteinGroups(proteinProphetOutput));

            Console.WriteLine(ppr.ToString());
		}
		
	}
}
