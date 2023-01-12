using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.ExclusionProfiles.TestProfile;
using MealTimeMS.Data;
using MealTimeMS.Util;
using MealTimeMS.Data.Graph;

namespace MealTimeMS.Tester.Junk
{
    public  class ExclusionMSTester
    {
        public static void DoJob()
        {
            GlobalVar.ppmTolerance = 10.0 / 10000000.0;
            GlobalVar.retentionTimeWindowSize = 1.0;
            var exclusionList = new ExclusionMSWrapper_ExclusionList(GlobalVar.ppmTolerance, "http://192.168.0.29:8000");
            Peptide pep = new Peptide("EFDSFFEFK", 1200.0, true, 1);
            pep.setRetentionTime(new RetentionTime(15.0, GlobalVar.retentionTimeWindowSize, GlobalVar.retentionTimeWindowSize,true));
            Peptide pep2 = new Peptide("rsfgdsgfd", 1205.0, true, 3);
            pep2.setRetentionTime(new RetentionTime(85.0, GlobalVar.retentionTimeWindowSize, GlobalVar.retentionTimeWindowSize,true));
            exclusionList.ClearExclusionMS();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i =1;i <= 10000; i++)
            {
                pep = new Peptide(RandomString(15), random.NextDouble()*2000+5, true, i);
                pep.setRetentionTime(new RetentionTime(random.NextDouble()*60, GlobalVar.retentionTimeWindowSize, GlobalVar.retentionTimeWindowSize, true));

                exclusionList.addPeptide(pep);
            }
            Console.WriteLine("Elapsed time: {0}s for 10000 additions", watch.ElapsedMilliseconds / 1000);
            
            exclusionList.addPeptides(new List<Peptide>(){ pep,pep2});
            Console.WriteLine("Peptide Added: "+pep.ToString());
            System.Threading.Thread.Sleep(2000);

            //exclusionList.ClearExclusionMS();
            //exclusionList.RemovePeptide(pep);
            Console.WriteLine("Peptide removed");
            Program.ExitProgram(0);
        }
        private static System.Random random = new System.Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIKLMNPQRSTVWY";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
