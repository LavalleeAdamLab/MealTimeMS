using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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

using MealTimeMS.Simulation;
using System.Diagnostics;

namespace MealTimeMS.Tester
{
    public static class QuickSimulation
    {
        public static void RunSimulation()
        {
            
            string ms2filePath = InputFileOrganizer.MS2SimulationTestFile;
            string originalCometOutput = InputFileOrganizer.OriginalCometOutput;
            String outputPartialCometFilePath = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,"partialCometFile");
            //parse .ms2 file
            List<Spectra> ms2SpectraList = Loader.parseMS2File(ms2filePath).getSpectraArray();

            List<int> includedSpectraScanNum = new List<int>();
            List<int> excludedSpectraScanNum = new List<int>();

            //loop through spectra list
            foreach(Spectra spec in ms2SpectraList)
            {
                bool exclude = spec.getPrecursorMz() + spec.getPrecursorCharge() +
                    spec.getIonMobility() + spec.getStartTime() <= 1000;
                if (exclude)
                {
                    excludedSpectraScanNum.Add(spec.getScanNum());
                }
                else
                {
                    includedSpectraScanNum.Add(spec.getScanNum());
                }

            }

            
            PartialPepXMLWriter.writePartialPepXMLFile(originalCometOutput, includedSpectraScanNum,
                outputPartialCometFilePath, ms2filePath, InputFileOrganizer.FASTA_FILE, outputPartialCometFilePath);

        }

    }
}
