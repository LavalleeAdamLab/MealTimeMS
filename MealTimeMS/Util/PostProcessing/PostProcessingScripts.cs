using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.IO;
using MealTimeMS.Data;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.Util.PostProcessing;

namespace MealTimeMS.Util.PostProcessing
{
	class PostProcessingScripts
	{
		public static void deleteFile(String fileName)
		{
			String command = "rm " + fileName;
			ExecuteShellCommand.executeCommand(command);
		}

		//I think this tool looks into the file specified in the pepxml for the original mzML file, so make sure it's there
		private static String executePeptideProphet(String directory, String cometFile)
		{
			String programName = InputFileOrganizer.XInteract;
			String database = InputFileOrganizer.FASTA_FILE;
			String peptideProphetOutputDirectory = IOUtils.getAbsolutePath(directory) + "\\peptide_prophet_output\\";
			if (!Directory.Exists(peptideProphetOutputDirectory))
			{
				Directory.CreateDirectory(peptideProphetOutputDirectory);
			}
			String outputName = peptideProphetOutputDirectory+IOUtils.getBaseName(IOUtils.getBaseName(cometFile)) + "_interact.pep.xml";
			String cometFilePath = IOUtils.getAbsolutePath(cometFile);
			String command = programName + " -N" + outputName + " -D" + database+ " -PPM -OAp " + cometFilePath;

			// System.out.println(command);
			// String result = ExecuteShellCommand.executeCommand(command);
			// System.out.println(result);

			String XinteractOutput = ExecuteShellCommand.executeCommand(command);
			//Console.WriteLine(XinteractOutput);
			return outputName;
		}

		private static String executeProteinProphet(String directory, String peptideProphetFile)
		{
			String programName = InputFileOrganizer.ProteinProphet;
			String proteinProphetOutputDirectory = IOUtils.getAbsolutePath(directory) + "\\protein_prophet_output\\";
			if (!Directory.Exists(proteinProphetOutputDirectory))
			{
				Directory.CreateDirectory(proteinProphetOutputDirectory);
			}
			String outputName = proteinProphetOutputDirectory + IOUtils.getBaseName(IOUtils.getBaseName(peptideProphetFile)) + ".prot.xml";

			String command = programName + " " + peptideProphetFile + " " + outputName;

			// System.out.println(command);
			// String result = ExecuteShellCommand.executeCommand(command);
			// System.out.println(result);

			ExecuteShellCommand.executeCommand(command);

			return outputName;
		}

		public static ProteinProphetResult postProcessing(ExclusionProfile exclusionProfile, String experimentName,
			Boolean keepResults)
		{
			String partialCometFileOutputFolder = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "PartialCometFile");
			if (!Directory.Exists(partialCometFileOutputFolder))
			{
				Directory.CreateDirectory(partialCometFileOutputFolder);
			}
			String outputCometFile = Path.Combine(partialCometFileOutputFolder,
				   experimentName + "_partial" + InputFileOrganizer.PepXMLSuffix);
		
			PartialPepXMLWriter.writePartialPepXMLFile(InputFileOrganizer.OriginalCometOutput, exclusionProfile.getSpectraUsed(),
				outputCometFile, InputFileOrganizer.MS2SimulationTestFile, InputFileOrganizer.FASTA_FILE, outputCometFile); //TODO was using MZML instead of MS2

			ProteinProphetResult ppr =RunProteinProphet(outputCometFile, InputFileOrganizer.OutputFolderOfTheRun,keepResults);

			//PostProcessingScripts.deleteFile(outputCometFile);
			// delete these files if this flag is false
			//if (!keepResults)
			//{
				
			//}

			return ppr;
		}

		public static ProteinProphetResult RunProteinProphet(String cometFilePath, String outputFolder, Boolean keepResults)
		{
			Logger.debug("Post processing comet file: ");
            //Console.WriteLine("\nRunnign protein prophet\n!!!if program doesn't respond for a long time, try pressing a typing a few keys into the command line!!!\n");
            String proteinProphetOutput = ProteinProphetSearch(cometFilePath,outputFolder,keepResults);
			ProteinProphetResult ppr = ProteinProphetEvaluator.getProteinProphetResult(proteinProphetOutput);
			ppr.SetProteinGroup(ProteinProphetEvaluator.ExtractPositiveProteinGroups(proteinProphetOutput));
			
			return ppr;
		}
		public static String ProteinProphetSearch(String cometFilePath, String outputFolder, Boolean keepResults)
		{
            Console.WriteLine("\nRunnign protein prophet\n!!!if program doesn't respond for a long time, try pressing a typing a few keys into the command line!!!\n");
            String peptideProphetOutput = PostProcessingScripts.executePeptideProphet(outputFolder, cometFilePath);
			String proteinProphetOutput = PostProcessingScripts.executeProteinProphet(outputFolder,
					peptideProphetOutput);
			//PostProcessingScripts.deleteFile(peptideProphetOutput);
			// delete these files if this flag is false
			if (!keepResults)
			{
				
				PostProcessingScripts.deleteFile(proteinProphetOutput);
			}
			return proteinProphetOutput;
		}

		public static String CometStandardSearch (String ms2FilePath, String outputFolder, Boolean keepResults)
		{
			String database = InputFileOrganizer.DecoyFasta;
			String paramsFile = InputFileOrganizer.CometParamsFile;
			String cometExe = InputFileOrganizer.CometExe;
			String command = cometExe + " -P" + paramsFile + " -D" + database + " " + ms2FilePath;
			ExecuteShellCommand.executeCommand(command);
			String output =  Path.Combine( IOUtils.getDirectory(ms2FilePath), IOUtils.getBaseName(ms2FilePath)+InputFileOrganizer.PepXMLSuffix);
			ExecuteShellCommand.MoveFile(output, outputFolder);

			String outputFilePath = Path.Combine(outputFolder, IOUtils.getBaseName(IOUtils.getBaseName(output))+ InputFileOrganizer.PepXMLSuffix);

			return outputFilePath;
			

		}
	}
}
