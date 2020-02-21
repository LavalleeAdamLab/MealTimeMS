using System;
using MealTimeMS.IO;
using MealTimeMS.Data;
namespace MealTimeMS.Tester
{
    public static class MZMLParserTester
    {
        public static void parseMZML()
        {
            String mzmlPath = "/Users/lavalleelab/Desktop/JoshLab/Temp/60minMZML.csv";
            MZMLFile mzml= Loader.parseMZMLCSV(mzmlPath);

            for(int i = 0; i < 7000; i++)
            {
                if (i % 500 == 0)
                {
                    Spectra spec = mzml.getSpectraArray()[i];
                    Console.WriteLine("ID {0} scanNum{1} RT{2} Mass{3} MSLevel{4}", spec.getIndex(), spec.getScanNum(), spec.getStartTime(),
                        spec.getCalculatedPrecursorMass(), spec.getMSLevel());
                }
                
            }
           

        }
    }
}
