using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Util;
using System.IO;
using MealTimeMS.IO;

namespace MealTimeMS.Tester.Junk
{
    class CometSingleSearchTester_v2
    {
        //!!Change this
        static String WorkingDirectory = "C:\\Coding\\2019LavalleeLab\\Temp\\Output\\de123e1e2";
        public static void DoJob()
        {
            //!!Change this
            String paramsPath = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\2019.comet.params";


            String LogFile = Path.Combine(WorkingDirectory, "logOutput.txt");
            StreamWriter sw = new StreamWriter(LogFile);

            //You'll need to convert the .mzML or .RAW file to a .ms2 file using proteowizard msconvert
            String ms2File = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\MS_QC_60min.ms2"; 
            List<Spectra> spectra = Loader.parseMS2File(ms2File).getSpectraArray(); //Parse the ms2 file to in-program spectral data structure

            //You will need to generate a .idx from your .fasta database(decoy included) using the Comet.exe -i command  
            //Or you can just pass in a .fasta file (without decoy) and un-comment the chunk of code below and add the appropriate path
            String dbPath;
            dbPath = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\PreComputedFiles\\uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx";
            //InputFileOrganizer.CometExe = "";
            //InputFileOrganizer.CometParamsFile = paramsPath;
            //dbPath = GenerateIDXFromFastaForMe("C:\\Users\\database.fasta");


            CometSingleSearch.InitializeComet(dbPath, paramsPath);
            List<IDs> realTimeIDList = new List<IDs>();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine("Starting Search- Timer started");
            int counter = 0;
            foreach (Spectra spec in spectra)
            {
                IDs id = null;
                if (CometSingleSearch.Search(spec, out id)) //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! The most important line you're probably looking for !!!!!!!
                {
                    counter++;
                    realTimeIDList.Add(id);
                    Console.WriteLine("Comet Search successful for Scan Num: {0}", spec.getScanNum()); //You might want to disable this line if you want to test out 
                                                                                                       //how fast the program can run since any time you write something to console it slows the program a bit
                    Console.WriteLine(id.ToDetailedString()); //Displays the search result
                }
                else
                {
                    Console.WriteLine("Comet Search failed to match a peptide to Scan Num: {0}", spec.getScanNum()); //Disable this line too to run fast
                }
            }
            watch.Stop();
            Console.WriteLine("Comet search of " + counter + " spectra took " + watch.ElapsedMilliseconds + " milliseconds");

            //Outputing the search result to a log file, not necessary
            sw.WriteLine("Comet search of " + counter + " spectra took " + watch.ElapsedMilliseconds + " milliseconds");
            sw.WriteLine("RealTimeOutput has {0} PSMs", realTimeIDList.Count);
            sw.WriteLine(CometSingleSearch.ReportFailedStatistics());
            sw.WriteLine("ScanNum\tSeq\txcorr");
            sw.Flush();
            foreach (IDs realTimeID in realTimeIDList)
            {
                String output = String.Format("{0}\t{1}\t{2}", realTimeID.getScanNum(), realTimeID.getPeptideSequence(),realTimeID.getXCorr());
                sw.WriteLine(output);
            }
            sw.Close();

        }

        static String GenerateIDXFromFastaForMe(String fastaFilePath)
        {

            String decoyFasta = DecoyConcacenatedDatabaseGenerator.GenerateConcacenatedDecoyFasta(fastaFilePath, WorkingDirectory);
            String idxDB = CommandLineProcessingUtil.FastaToIDXConverter(decoyFasta, WorkingDirectory);
            return idxDB;
        }

    }
}
