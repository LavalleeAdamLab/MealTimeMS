namespace MealTimeMS.Tester
{
    using System;
    using System.Collections.Generic;
    using MealTimeMS.IO;
    using MealTimeMS.Util;
    
    public class PartialPepXMLWriterTester
    {
        public static void DoJob()
        {

            List<int> ls = new List<int>();
            ls.Add(679);
            ls.Add(883);
            ls.Add(897);
            ls.Add(2533);
            ls.Add(3230);
			String originalComet = InputFileOrganizer.OriginalCometOutput;//"C:\\Coding\\2019LavalleeLab\\GoldStandardData\\pepxml\\MS_QC_120min.pep.xml";
			String outputCometFile = InputFileOrganizer.OutputRoot + "300partialOut.pep.xml"; //"C:\\Coding\\2019LavalleeLab\\GoldStandardData\\pepxml\\MS_QC_120min_partial.pep.xml";
			String fastaFile = InputFileOrganizer.FASTA_FILE;//"C:\\Coding\\2019LavalleeLab\\GoldStandardData\\Database\\uniprot_SwissProt_Human_1_11_2017.fasta";
            String mzml = "";// InputFileOrganizer.MZMLSimulationTestFile;//"C:\\Coding\\2019LavalleeLab\\GoldStandardData\\MZML_Files\\MS_QC_120min.mzml";
            PartialPepXMLWriter.writePartialPepXMLFile(originalComet, ls,
                outputCometFile, mzml, fastaFile, outputCometFile);
        }
    }
}