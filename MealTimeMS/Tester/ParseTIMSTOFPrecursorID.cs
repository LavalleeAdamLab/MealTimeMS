using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MealTimeMS.Tester
{
    public static class ParseTIMSTOFPrecursorID
    {

        public static Dictionary<int, int> getTIMSTOFFPrecursorID_to_ms2ID(String dotms2filePath)
        {
            var converter = new Dictionary<int, int>();
            var sr = new StreamReader(dotms2filePath);
            String line = sr.ReadLine();
            while (line != null)
            {
                if (line.StartsWith("S"))
                {
                    int scanNum = int.Parse(line.Split("\t".ToCharArray())[1]);
                    line = sr.ReadLine();
                    line = sr.ReadLine();
                    string[] sp = line.Split("\t".ToCharArray());
                    if (!sp[1].Equals("TIMSTOF_Precursor_ID"))
                    {
                        Console.WriteLine("WARNINGGGG");
                        Console.ReadLine();

                    }
                    int timsTOFPrecID = int.Parse(line.Split("\t".ToCharArray())[2]);
                    converter.Add(timsTOFPrecID, scanNum);
                }
                line = sr.ReadLine();
            }
            return converter;

        }
        //returns a <scanNum, corrected mono precursor mass> dictionary
        public static Dictionary<int, double> getCheatingMonoPrecursorMassTable(String cheatingPMfilePath)
        {
            if (cheatingPMfilePath.Equals("")){
                return null;
            }
            var PMTable = new Dictionary<int, double>();
            var sr = new StreamReader(cheatingPMfilePath);
            String line = sr.ReadLine();
            line = sr.ReadLine();
            while (line != null)
            {
               
                String[] content = line.Split("\t".ToCharArray());
                int scanNum = int.Parse(content[0]);
                double correctedPM = double.Parse(content[1]);
                PMTable.Add(scanNum, correctedPM);
                
                line = sr.ReadLine();
            }
            return PMTable;
        }
    }
}
