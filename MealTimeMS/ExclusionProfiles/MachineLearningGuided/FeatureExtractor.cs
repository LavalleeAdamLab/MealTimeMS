using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.RunTime;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Util;
using MealTimeMS.IO;
using MealTimeMS.Util.PostProcessing;
using System.IO;

namespace MealTimeMS.ExclusionProfiles.MachineLearningGuided
{
	class FeatureExtractor
	{

		static String OutputFile_PositiveAndNegative = "";
		static String OutputFile_PositiveAndNonPositive = "";
		static String OutputFile_PositiveAndNonPositive_NoDecoy = "";

		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        //static string mzmlFileBaseName="MS_QC_240min";
        public static void ExtractFeatures_Bruker(String BrukerdotDFolder, out String extractedFeatureSavedFile_posAndNeg, out String extractedFeatureSavedFile_posAndNonPos)
        {
            Console.WriteLine("Extracting features from {0}", InputFileOrganizer.BrukerdotDFolder);
            //InputFileOrganizer.MZMLSimulationTestFile = mzmlFile;
            String dataFileBaseName = new DirectoryInfo(BrukerdotDFolder).Name ;
            OutputFile_PositiveAndNegative = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, dataFileBaseName + "_extractedFeatures_PositiveAndNegative.tsv");
            OutputFile_PositiveAndNonPositive = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, dataFileBaseName + "_extractedFeatures_positiveAndNonPositive.tsv");
            OutputFile_PositiveAndNonPositive_NoDecoy = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, dataFileBaseName + "_extractedFeatures_positiveAndNonPositive_NoDecoy.tsv");

            log.Info("Running No Exclusion Simulation");
            ExclusionProfile exclusionProfile = ExclusionExplorer.SimulationForFeatureExtraction();

            log.Info("Extracting identification feature from exclusion profile");
            List<IdentificationFeatures> idf = exclusionProfile.getFeatures();
            log.Info("Recalibrating stDev");
            idf = IdentificationFeatureExtractionUtil.recalibrateStDev(idf);

            writeFeatures(idf);
            extractedFeatureSavedFile_posAndNeg = OutputFile_PositiveAndNegative;
            extractedFeatureSavedFile_posAndNonPos = OutputFile_PositiveAndNonPositive;
            Console.WriteLine("Extracted Feature written to {0} and {1}", OutputFile_PositiveAndNegative, OutputFile_PositiveAndNonPositive);

        }
        public static void ExtractFeatures(String ms2File, out String extractedFeatureSavedFile_posAndNeg, out String extractedFeatureSavedFile_posAndNonPos)
		{
			Console.WriteLine("Extracting features from {0}", ms2File);
			
			InputFileOrganizer.MS2SimulationTestFile = ms2File;
			String ms2FileBaseName = Path.GetFileNameWithoutExtension(ms2File);
			OutputFile_PositiveAndNegative = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, ms2FileBaseName + "_extractedFeatures_PositiveAndNegative.tsv");
			OutputFile_PositiveAndNonPositive = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, ms2FileBaseName + "_extractedFeatures_positiveAndNonPositive.tsv");
			OutputFile_PositiveAndNonPositive_NoDecoy = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, ms2FileBaseName + "_extractedFeatures_positiveAndNonPositive_NoDecoy.tsv");

			//the current feature extraction will include decoy proteins in the database and testing set
			SimulationWithDecoyParamsSetUp();

			//placeholder values, dont matter
			GlobalVar.ppmTolerance = 1;
			GlobalVar.retentionTimeWindowSize = 1;
			GlobalVar.AccordThreshold = 1;
			GlobalVar.XCorr_Threshold = 1;
			GlobalVar.NumDBThreshold = 1;
			//

			log.Info("Running No Exclusion Simulation");
			ExclusionProfile exclusionProfile = ExclusionExplorer.SingleSimulationRun(ExclusionProfileEnum.NO_EXCLUSION_PROFILE);

			log.Info("Extracting identification feature from exclusion profile");
			List<IdentificationFeatures> idf = exclusionProfile.getFeatures();
			log.Info("Recalibrating stDev");
			idf = IdentificationFeatureExtractionUtil.recalibrateStDev(idf);
			
			writeFeatures(idf);
			extractedFeatureSavedFile_posAndNeg = OutputFile_PositiveAndNegative;
			extractedFeatureSavedFile_posAndNonPos = OutputFile_PositiveAndNonPositive;
			Console.WriteLine("Extracted Feature written to {0} and {1}", OutputFile_PositiveAndNegative,OutputFile_PositiveAndNonPositive);

		}

		private static void SimulationWithDecoyParamsSetUp()
		{
            //The run to extract protein features to train classifier has slightly different simulation paramaters than a normal run. 
            //Namely, the in-program database contains decoy proteins here but not in a normal run

            GlobalVar.isSimulationForFeatureExtraction = true;
			String decoyConcatDB = DecoyConcacenatedDatabaseGenerator.GenerateConcacenatedDecoyFasta(InputFileOrganizer.FASTA_FILE, InputFileOrganizer.OutputFolderOfTheRun);
			InputFileOrganizer.ExclusionDBFasta = decoyConcatDB; //sets the in-program databse to one that contains decoy protein - for feature generation only
			InputFileOrganizer.DecoyFasta = decoyConcatDB;

			//InputFileOrganizer.RTCalcResult = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\PreComputedFiles\\tempOutputPeptideList_rtOutput_240min.txt";
		}


		private static void writeFeatures(List<IdentificationFeatures> idf)
		{
			log.Info("Classifying positive and negative sets");
			// Extract which proteins were confidently identified at 0.01 FDR with protein prophet
			List<String> identifiedProteins = ProteinProphetEvaluator.extractIdentifiedProteinNames(InputFileOrganizer.OriginalProtXMLFile);
			// Extract which proteins were not confidently identified, with a specified FDR
			// threshold
			List<String> negativeTrainingSetProteins = ProteinProphetEvaluator.extractNegativeTrainingSetProteinNames(InputFileOrganizer.OriginalProtXMLFile, 0.25);
			// 2019-05-23 FOUND IT! Here is where we filter the negative training set with
			// above 20% FDR

			// Proteins identified with a 0.01 FDR with protein prophet
			List<IdentificationFeatures> positiveTrainingSet = new List<IdentificationFeatures>();
			// Proteins not identified with a 0.01 FDR protein prophet
			List<IdentificationFeatures> negativeTrainingSet = new List<IdentificationFeatures>();

			List<IdentificationFeatures> nonPositiveTrainingSet = new List<IdentificationFeatures>();
		

			// Determine which features are in positive or negative training set
			foreach (IdentificationFeatures i in idf)
			{
				String accession = i.getAccession();
				if (i.getCardinality() > 0)
				{
					if (!accession.StartsWith(GlobalVar.DecoyPrefix))
					{
						//if this is a real protein
						if (identifiedProteins.Contains(accession))
						{
							positiveTrainingSet.Add(i);
						}
						else
						{
							nonPositiveTrainingSet.Add(i);
						}

						if (negativeTrainingSetProteins.Contains(accession))
						{
							negativeTrainingSet.Add(i);
						}
					}
					else
					{
						//if it's a decoy protein
						negativeTrainingSet.Add(i);
						nonPositiveTrainingSet.Add(i);
					}
				}
			}
			WriteIdentificationFeaturesFile(OutputFile_PositiveAndNegative, positiveTrainingSet, negativeTrainingSet);
			WriteIdentificationFeaturesFile(OutputFile_PositiveAndNonPositive, positiveTrainingSet, nonPositiveTrainingSet);

			List<IdentificationFeatures> positiveSetNoDecoy= new List<IdentificationFeatures>();
			List<IdentificationFeatures> nonPositiveSetNoDecoy= new List<IdentificationFeatures>();
			foreach (IdentificationFeatures i in positiveTrainingSet)
			{
				if (!i.getAccession().Contains(GlobalVar.DecoyPrefix))
				{
					positiveSetNoDecoy.Add(i);
				}
			}
			foreach (IdentificationFeatures i in nonPositiveTrainingSet)
			{
				if (!i.getAccession().Contains(GlobalVar.DecoyPrefix))
				{
					nonPositiveSetNoDecoy.Add(i);
				}
			}
			
			WriteIdentificationFeaturesFile(OutputFile_PositiveAndNonPositive_NoDecoy, positiveSetNoDecoy,nonPositiveSetNoDecoy);

		}

		/*
	 * Write the identification features used for training the logistic regression
	 * classifier
	 */
		public static void WriteIdentificationFeaturesFile(String file_path,
				List<IdentificationFeatures> positiveTrainingSet,
				List<IdentificationFeatures> negativeTrainingSet)
		{
			log.Debug("Writing Identification Features to a file...");
			try
			{
				StreamWriter writer = new StreamWriter(file_path);
				log.Debug("File name: " + file_path);

				// Write header TODO remove
				String header = "label," + IdentificationFeatures.getHeader();
				writer.Write(header);

				// in the first column, 1 indicates positive training set
				foreach (IdentificationFeatures i in positiveTrainingSet)
				{
					writer.Write("\n" + "1," + i.writeToFile());
					writer.Flush();
				}
				// in the first column, 0 indicates negative training set
				foreach (IdentificationFeatures i in negativeTrainingSet)
				{
					writer.Write("\n" + "0," + i.writeToFile());
					writer.Flush();
				}
				writer.Flush();
				writer.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				log.Error("Writing file unsuccessful!!!");
				Console.ReadKey();
				Environment.Exit(0);
			}
			log.Debug("Writing file successful.");
		}


	}
}
