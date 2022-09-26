using System;
using MealTimeMS.IO;
using MealTimeMS.Data;
using MealTimeMS.Util;
namespace MealTimeMS.Tester
{
    public class CometSingleSearchTester
    {
        public static void CometSingleSearchTest()
        {





            String idx = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\PreComputedFiles\\uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx";
			//String idx = "C:\\temp\\comet_2019015\\comet_source_2019015\\IDXMake\\uniprot_SwissProt_Human_1_11_2017_decoyConcacenated.fasta.idx";
			String param = "C:\\Coding\\2019LavalleeLab\\temp2\\ExampleDataSet\\2019.comet.params";

			CometSingleSearch.InitializeComet(idx, param);
			//	CometSingleSearch.QualityCheck();

			String ms2File = "C:\\temp\\comet_2019015\\temp\\MS_QC_120min_partial_modified.ms2";
			MZMLFile ms2 = Loader.parseMS2File(ms2File);
			var x = ms2.getSpectraArray()[0];
			IDs idd = null; 
			CometSingleSearch.Search(x,out idd);
			Console.WriteLine(idd.ToString());

			Program.ExitProgram(1);
            String dataRoot = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\MealTimeMS_APITestRun\\Data\\";
            String outputRoot = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\MealTimeMS_APITestRun\\Output\\";
            //String mzmlPath = dataRoot+"60minMZMLShrink.csv";
            String mzmlPath = dataRoot + "8001.ms2.txt";
            String dbPath = dataRoot + "tinyDB.fasta.idx"; //
            String outputPath = outputRoot+"output.txt";
            String paramsPath = dataRoot + "comet.params";
            MZMLFile mzml = Loader.parseMS2File(mzmlPath);
            //MZMLFile mzml = null;
            CometSingleSearch.InitializeComet(dbPath,paramsPath);
            WriterClass.initiateWriter(outputPath);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int counter = 0;
            Console.WriteLine("Starting comet search");
           

            for (int i = 0; i < 1; i++)
            {
                if (i % 1 == 0)
                {
                    Spectra spec = mzml.getSpectraArray()[i];
                    if (spec.getMSLevel() != 2)
                    {
                        continue;
                    }
                    Console.WriteLine("scanNum {0} RT {2} Mass {2} MSLevel {3}",spec.getScanNum(), spec.getStartTime(),
                    spec.getCalculatedPrecursorMass(), spec.getMSLevel());
                    IDs id = null;
                    if (CometSingleSearch.Search(spec, out id))
                    {
                        String outLine = String.Format("{0}\t{1}\txcorr\t{2}\tdcn\t{3}", id.getScanNum(), id.getPeptideSequence(), id.getXCorr(), id.getDeltaCN());
                        Console.WriteLine(outLine);
                        WriterClass.writeln(outLine);
                    }
                    else
                    {
                        Console.WriteLine("Spectrum cannot be matched\n");
                    }
                    counter++;

                }

            }
            watch.Stop();
            Console.WriteLine("Comet search of "+ counter +" spectra took "+watch.ElapsedMilliseconds +" milliseconds");
            WriterClass.writeln("Comet search of " + counter + " spectra took " + watch.ElapsedMilliseconds + " milliseconds");
            WriterClass.CloseWriter();

        }
        public static void TestSearch()
        {
            CometSingleSearch.InitializeComet(InputFileOrganizer.IDXDataBase, InputFileOrganizer.CometParamsFile);
            String ms2File = InputFileOrganizer.MS2SimulationTestFile;
            MZMLFile ms2 = Loader.parseMS2File(ms2File);
            foreach(Spectra spec in ms2.getSpectraArray())
            {
                IDs idd = null;
                if(CometSingleSearch.Search(spec, out idd))
                {
                    Console.WriteLine(idd.ToString());
                }
                else
                {
                    Console.WriteLine("Spectra {0} not found", spec.getScanNum());
                }

            }
            
           

            Program.ExitProgram(1);

        }
    }
}
