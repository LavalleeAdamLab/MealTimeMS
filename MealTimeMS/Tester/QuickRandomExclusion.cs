using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.IO;
using MealTimeMS.Util;
using MealTimeMS.Util.PostProcessing;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
using MealTimeMS.ExclusionProfiles.TestProfile;
using MealTimeMS.Tester;
using MealTimeMS.ExclusionProfiles.Nora;

using MealTimeMS.ExclusionProfiles;
namespace MealTimeMS.Tester
{
    class QuickRandomExclusion
    {
        public static void DoJob(double[] ratioToKeep_list, int repeatPerRatio)
        {
            StreamWriter sw = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,"QuickRandomExclusion_results.txt"));
            sw.WriteLine(String.Join(separator: "\t", "PercentResourceUsed", "NumProteinsIdentified","ProteinGroups","NumSpectraAnalyzed","NumSpectraExcluded"));
            //Gets list of all scanNum
             var ms2SpectraList = Loader.parseMS2File(InputFileOrganizer.MS2SimulationTestFile).
                getSpectraArray().
                Select(x=>x.getScanNum()).
                ToList();
             int ExperimentTotalScans = ms2SpectraList.Count;
            foreach(double ratioToKeep in ratioToKeep_list)
            {
                for(int i =0; i < repeatPerRatio; i++)
                {
                    var rand = new Random();
                    var shuffledList = ms2SpectraList.OrderBy(_ => rand.Next()).ToList();
                    int numScansToInclude =(int)( ExperimentTotalScans * ratioToKeep);
                    var includedSpectra = shuffledList.
                        Take(numScansToInclude).
                        OrderBy(x => x).ToList();
                    
                    var ppr = PostProcessing(includedSpectra, "QuickRandomExclusion_"+i.ToString());
                    int proteinGroups = ppr.getFilteredProteinGroups().Count;
                    List<String> proteinsIdentified = ppr.getProteinsIdentified();
                    sw.WriteLine(String.Join(separator: "\t", ratioToKeep, proteinsIdentified.Count, proteinGroups,numScansToInclude, ExperimentTotalScans-numScansToInclude));
                }
            }
            sw.Close();
            Program.ExitProgram(0);


        }
        private static ProteinProphetResult PostProcessing(List<int> includedSpectra, String experimentName)
        {
            String partialCometFileOutputFolder = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "PartialCometFile");
            if (!Directory.Exists(partialCometFileOutputFolder))
            {
                Directory.CreateDirectory(partialCometFileOutputFolder);
            }
            String outputCometFile = Path.Combine(partialCometFileOutputFolder,
                   experimentName + "_partial" + InputFileOrganizer.PepXMLSuffix);

            PartialPepXMLWriter.writePartialPepXMLFile(InputFileOrganizer.OriginalCometOutput, includedSpectra,
                outputCometFile, InputFileOrganizer.MS2SimulationTestFile, InputFileOrganizer.FASTA_FILE, outputCometFile); //TODO was using MZML instead of MS2

            ProteinProphetResult ppr = PostProcessingScripts.RunProteinProphet(outputCometFile, InputFileOrganizer.OutputFolderOfTheRun, true);
            return ppr;
        }

    }
}
