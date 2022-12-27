using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.IO;
using MealTimeMS.Data;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.Util.PostProcessing;

namespace MealTimeMS.Util
{
	public class CommandLineProcessingUtil
	{


		////Uses OpenMS to create a concacenated reversed database, reversed at each peptide segment with C terminus kept
		//public static String CreateConcacenatedDecoyDB(String fastaFilePath, String outputFolder)
		//{
		//	String DecoyDBGenerator= IOUtils.getAbsolutePath( InputFileOrganizer.DecoyDatabaseOpenMSEXE);
		//	String enzyme = "Trypsin";
		//	String outputFilePath = Path.Combine( outputFolder , IOUtils.getBaseName(fastaFilePath)+"_decoyConcacenated.fasta");
		//	String method = "reverse";
		//	String decoyString = GlobalVar.DecoyPrefix;

		//	String command = DecoyDBGenerator+" -in "+ fastaFilePath + " -out " +outputFilePath + " -enzyme "+ enzyme + " -decoy_string " + decoyString +" -decoy_string_position prefix "
		//		+" -method " +method  +" -Decoy:keepPeptideNTerm false -Decoy:keepPeptideCTerm true";
		//	Logger.debug("Command: " +command);
			

		//	//Logger.debug("running OpenMS DecoyDatabase.exe to generate concacenated decoy database");
		//	ExecuteShellCommand.executeCommand(command);
		//	Logger.debug("DecoyDatabase at: "+outputFilePath);

		//	return outputFilePath;
		//}

		//public static String CreateConcacenatedDecoyDB_TPP(String fastaFilePath, String outputFolder)
		//{
		//	String DecoyDBGenerator = IOUtils.getAbsolutePath(InputFileOrganizer.TPPDecoyGenerator);
		//	String outputFilePath = Path.Combine(outputFolder, IOUtils.getBaseName(fastaFilePath) + "_TPPDECOY.fasta");
		//	String decoyString = GlobalVar.DecoyPrefix;

		//	String command = "\""+DecoyDBGenerator + "\" -t \"" + decoyString + "\" \"" + fastaFilePath + "\" \"" + outputFilePath + "\"";
		
		//	Logger.debug("Command: " + command);

		//	//Logger.debug("running OpenMS DecoyDatabase.exe to generate concacenated decoy database");
		//	ExecuteShellCommand.executeCommand(command);
		//	Logger.debug("DecoyDatabase at: " + outputFilePath);

		//	return outputFilePath;
		//}

		//public static String CreateConcacenatedDecoyDB_improved(String fastaFilePath, String outputFolder)
		//{
	
		//	//Fixes the problem of openMS-CometIDX chain. 
		//	//in the faulty method, real sequences such as ABCDKEFGR 
		//	//would be processed by openMS into DCBAKGFER
		//	//in comet idx that would turn into decoy peptide sequences: DCBAK, GFER, and DCBAKGFER
		//	//However, in an "offline" comet processing, the sequences with 1 misscleavage should be: DCBAK, GFER, and GFEKDCBAR
		//	//This new method ensures that happens. 
		//	String outputFilePath = Path.Combine(outputFolder, IOUtils.getBaseName(fastaFilePath) + "_decoyConcacenated.fasta");
		//	outputFilePath = DecoyConcacenatedDatabaseGenerator.GenerateConcacenatedDecoyFasta(fastaFilePath, outputFilePath); //pretty redundant to re-set the outputFilePath, but it is what it is
		//	return outputFilePath;

		//}

		//Uses Comet to generate an IDX database file from a fasta file
		public static String FastaToIDXConverter (String fastaFilePath, String outputFolder)
		{
			Logger.debug("Converting fasta to idx");
			String cometEXE = InputFileOrganizer.CometExe;
			String cometParams = InputFileOrganizer.CometParamsFile;
			String command = cometEXE + " -D" + fastaFilePath + " -P" + cometParams + " -i";
			Logger.debug("Command: " + command);

			ExecuteShellCommand.executeCommand(command);
			String outputfileName = IOUtils.getBaseName(fastaFilePath) + ".fasta.idx";
			String currentOutputDirectory = Path.Combine(IOUtils.getDirectory(fastaFilePath), outputfileName); //because comet just generates the idx wherever the fasta originally is located
			String targetOutputDirectory = Path.Combine(outputFolder, outputfileName);
			if (File.Exists(targetOutputDirectory))
			{
				return targetOutputDirectory;
			}
			else
			{
				ExecuteShellCommand.MoveFile(currentOutputDirectory, outputFolder);

			}
			Logger.debug("DecoyDatabase at: " + targetOutputDirectory);

			return targetOutputDirectory;
		}

        public static void RunBrukerAcquisitionSimulator(String BrukerDotDFolder, String sqtFile)
        {

            //String command = "python -m bdal.paser.acquisitionsimulator -q --ip 127.0.0.1 --pid -1 --wid -1 --no_ms1 -d 30 -l 3 -r 0 --kafka-port 9092 --schema-port 8083 --sqt_file D:\\CodingLavaleeAdamCDriveBackup\\APIO\\APIO_testData\\200ngHeLaPASEF_2min.d\\200ngHeLaPASEF_2min_PaSER2023.sqt --input D:\\CodingLavaleeAdamCDriveBackup\\APIO\\APIO_testData\\200ngHeLaPASEF_2min.d";
            String command = String.Format("python -m bdal.paser.acquisitionsimulator -q --ip 127.0.0.1 " +
                "--pid -1 --wid -1 --no_ms1 -d 15 -l 3 -r 0 --kafka-port 9092 --schema-port 8083 " +
                "--sqt_file {0} --input {1}", sqtFile, BrukerDotDFolder);
            var runBrukerAcquisitionSimulatorSThread = Task.Run(() =>
            {
                Console.WriteLine("Run acquisition simulator");
                ExecuteShellCommand.executeCommand(command);
            });
        }

	}
}
