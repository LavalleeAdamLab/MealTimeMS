using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;

namespace MealTimeMS.Tester.Junk
{
    class AcquisitionSimulatorTester
    {
        public static void DoJob()
        {
            InputFileOrganizer.BrukerdotDFolder = @"D:\CodingLavaleeAdamCDriveBackup\APIO\APIO_testData\20200821K562200ng90min_1_Slot1-1_1_1630.d\";
            InputFileOrganizer.ProlucidSQTFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\APIO_testData\20200821K562200ng90min_1_Slot1-1_1_1630.d\20200821K562200ng90min_1_Slot1-1_1_1630_nopd.sqt";
            GlobalVar.exclusionMS_ip = "http://192.168.0.29";
            CommandLineProcessingUtil.RunBrukerAcquisitionSimulator(InputFileOrganizer.BrukerdotDFolder, InputFileOrganizer.ProlucidSQTFile, GlobalVar.exclusionMS_ip);
        }
    }
}
