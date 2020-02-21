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

		static String OutputFile_Training = "";
		static String OutputFile_Testing = "";
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		static string mzmlFileBaseName="MS_QC_240min";
		public static void ExtractFeatures(bool withDecoy)
		{
			InputFileOrganizer.MS2SimulationTestFile = InputFileOrganizer.DataRoot + mzmlFileBaseName+".ms2";
			InputFileOrganizer.MZMLSimulationTestFile = InputFileOrganizer.DataRoot + mzmlFileBaseName+".mzML";

			InputFileOrganizer.ProtXML = InputFileOrganizer.PreComputedFilesRoot + mzmlFileBaseName+"_interact.prot.xml";
			InputFileOrganizer.OriginalCometOutput = InputFileOrganizer.PreComputedFilesRoot + mzmlFileBaseName+".pep.xml";



			OutputFile_Training = mzmlFileBaseName + "_TrainingSet.tsv";
			OutputFile_Testing = mzmlFileBaseName+"_TestingSet.tsv";

			OutputFile_Training = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,OutputFile_Training);
			OutputFile_Testing = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, OutputFile_Testing);
			if (withDecoy)
			{
				SimulationWithDecoyParamsSetUp();
			}
			else
			{
				Environment.Exit(45);
				regularSetUp();
			}
			



			log.Info("Running No Exclusion Simulation");
			ExclusionProfile exclusionProfile = ExclusionExplorer.SingleSimulationRun(ExclusionProfileEnum.NO_EXCLUSION_PROFILE);

			log.Info("Extracting identification feature from exclusion profile");
			List<IdentificationFeatures> idf = exclusionProfile.getFeatures();
			log.Info("Recalibrating stDev");
			idf = IdentificationFeatureExtractionUtil.recalibrateStDev(idf);
			
			writeFeatures(idf);

		}

		private static void SimulationWithDecoyParamsSetUp()
		{
			String decoyConcatDB = DecoyConcacenatedDatabaseGenerator.GenerateConcacenatedDecoyFasta(InputFileOrganizer.FASTA_FILE, InputFileOrganizer.OutputFolderOfTheRun);
			GlobalVar.useIDXComputedFile = true;
			GlobalVar.useChainsawComputedFile = false;

			GlobalVar.usePepXMLComputedFile = true;
			

			InputFileOrganizer.dbFasta = decoyConcatDB; //sets the in-program databse to one that contains decoy protein - for feature generation only
			InputFileOrganizer.DecoyFasta = decoyConcatDB;
			GlobalVar.useDecoyFastaComputedFile = true;
			//remove these 2 lines
			GlobalVar.useRTCalcComputedFile = true;
			InputFileOrganizer.RTCalcResult = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\PreComputedFiles\\tempOutputPeptideList_rtOutput_240min.txt";

		}

		private static void regularSetUp()
		{
			GlobalVar.useDecoyFastaComputedFile = true;
			GlobalVar.useIDXComputedFile = true;
			GlobalVar.useChainsawComputedFile = true;
			GlobalVar.useRTCalcComputedFile = true;
			GlobalVar.usePepXMLComputedFile = true; //add something
		}

		private static void writeFeatures(List<IdentificationFeatures> idf)
		{
			log.Info("Classifying positive and negative sets");
			// Extract which proteins were confidently identified at 0.01 FDR with protein prophet
			List<String> identifiedProteins = ProteinProphetEvaluator.extractIdentifiedProteinNames(InputFileOrganizer.ProtXML);
			// Extract which proteins were not confidently identified, with a specified FDR
			// threshold
			List<String> negativeTrainingSetProteins = ProteinProphetEvaluator.extractNegativeTrainingSetProteinNames(InputFileOrganizer.ProtXML, 0.25);
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
					if (!accession.StartsWith(GlobalVar.DecoyString))
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
			WriteIdentificationFeaturesFile(OutputFile_Training, positiveTrainingSet, negativeTrainingSet);
			WriteIdentificationFeaturesFile(OutputFile_Testing, positiveTrainingSet, nonPositiveTrainingSet);
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
