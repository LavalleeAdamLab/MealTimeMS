using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using MealTimeMS.Util;

namespace MealTimeMS.RunTime
{
    public class BrukerRuntimeCore
    {
        public static void BrukerRuntimeCore_Main()
        {


        }



        public static void PopulateHardCodedContentFilePaths()
        {
            String TestDataDirectory = Path.Combine(InputFileOrganizer.AssemblyDirectory, "TestData");
            String datasetFolderName = "K562Lysate";
            InputFileOrganizer.ChainSawResult = Path.Combine(TestDataDirectory, datasetFolderName, "uniprot-R_20210810_UP000005640_human.fasta_digestedPeptides_2missedCleavage.tsv");
            InputFileOrganizer.RTCalcResult = Path.Combine(TestDataDirectory, datasetFolderName, "AutoRT_fastaPrediction_misCleaved2.tsv");
            InputFileOrganizer.ExclusionDBFasta = Path.Combine(TestDataDirectory, datasetFolderName, "uniprot-R_20210810_UP000005640_human.fasta");
            InputFileOrganizer.FASTA_FILE = Path.Combine(TestDataDirectory, datasetFolderName, "uniprot-R_20210810_UP000005640_human.fasta");
            InputFileOrganizer.AccordNet_LogisticRegressionClassifier_WeightAndInterceptSavedFile = Path.Combine(TestDataDirectory, datasetFolderName, "20200821K562300ng90min_1_Slot1-1_1_1638.d_extractedFeatures_positiveAndNonPositive.ClassifierCoefficient.txt");
        }

    }
}
