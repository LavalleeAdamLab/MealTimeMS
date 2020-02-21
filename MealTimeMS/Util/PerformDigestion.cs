using System;
using System.Collections.Generic;

using MealTimeMS.Data.InputFiles;
using MealTimeMS.Data.Graph;
using MealTimeMS.IO;

namespace MealTimeMS.Util
{

public class PerformDigestion
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static DigestedFastaFile performDigest(List<Protein> proteins, int numMissedCleavages)
        {
            Dictionary<String, Protein> protein_hash = new Dictionary<String, Protein>();
            foreach (Protein p in proteins)
            {
                protein_hash.Add(p.getAccession(), p);
            }
            return performDigest(protein_hash, numMissedCleavages);
        }

        public static DigestedFastaFile performDigest(Dictionary<String, Protein> protein_hash, int numMissedCleavages)
        {
            String tempOutput = "temp.tsv";
            Writer.writeFastaFile(tempOutput, new List<Protein>(protein_hash.Values));
            FastaFile f = new FastaFile(tempOutput, protein_hash);
            return performDigest(f, numMissedCleavages);
        }

        public static DigestedFastaFile performDigest(FastaFile f, int numMissedCleavages)
        {
            // The input fasta file path
            String fastaFilePath = f.getFileName();
            return performDigest(fastaFilePath, numMissedCleavages,true);
        }

        public static DigestedFastaFile performDigest(String fastaFilePath, int numMissedCleavages, Boolean keepFile)
        {
            if (GlobalVar.useChainsawComputedFile)
            {
                return Loader.parseDigestedFasta(InputFileOrganizer.ChainSawResult);
            }

            // These are the output file paths
            String outputTSVFile = fastaFilePath + "_digestedPeptides.tsv";
            String outputIndexFile = fastaFilePath + ".index";
			

            /* Peptide chainsaw parameters */
            String chainsaw = InputFileOrganizer.ChainSaw; // tool location
            //String enzyme = "Trypsin/P";
            String enzyme = "Trypsin";
            String specificity = "fully";
            int minimumPeptideLength = GlobalVar.MinimumPeptideLength;
            int maximumPeptideLength = 200;
            // minimum and maximum peptide length is what typically is analyzed well by the
            // mass spectrometer

            // This is the chainsaw command to be run on the terminal
            String chainsawCommand = chainsaw + " -c " + enzyme + " -s " + specificity + " -m " + minimumPeptideLength
                    + " -M " + maximumPeptideLength + " -n " + numMissedCleavages + " " + fastaFilePath;

            log.Info("Digesting database");
            // perform in-silico digestion
            String chainsawOutput = ExecuteShellCommand.executeCommand(chainsawCommand);
            log.Info(chainsawCommand);
            log.Info(chainsawOutput);

            // parse new tsv file
            DigestedFastaFile df = Loader.parseDigestedFasta(outputTSVFile);

			// this tool automatically outputs file in the same folder as the input database, move file to output folder, 
			if (keepFile)
			{
				ExecuteShellCommand.executeCommand("move " + outputTSVFile + " " + InputFileOrganizer.preExperimentFilesFolder);
				InputFileOrganizer.ChainSawResult = InputFileOrganizer.preExperimentFilesFolder + "\\" + IOUtils.getBaseName(outputTSVFile);
			}
			else
			{
				ExecuteShellCommand.executeCommand("rm " + outputTSVFile);
			}
			//ExecuteShellCommand.executeCommand("rm " + outputIndexFile);
			
            return df;
        }
    }

}
