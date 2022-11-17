using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MealTimeMS.Tester.Junk
{
    class ReplacingStuffInPepXML
    {

        public static void DoJob()
        {
            String file = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\TestData\20200821K562300ng90min_1_Slot1-1_1_1638_nopd.pep.xml";
            String outputFile = @"D:\CodingLavaleeAdamCDriveBackup\APIO\MTMSWorkspace\TestData\20200821K562300ng90min_1_Slot1-1_1_1638_nopd_replaced.pep.xml";
            var sr = new StreamReader(file);
            var sw = new StreamWriter(outputFile);
            String line = sr.ReadLine();
            while(line != null)
            {
                String toWrite = line.Replace("ProLuCID", "SEQUEST");
                sw.WriteLine(toWrite);
                line = sr.ReadLine();

            }
            sw.Flush();
            sw.Close();


        }
    }
}
