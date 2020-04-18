using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.Data;
using MealTimeMS.IO;
using System.IO;

namespace MealTimeMS.Util
{
	class DecoyConcacenatedDatabaseGenerator
	{
		//ignore the comments below
		//!!!
		//This class is only intended to be used in line with comet idx generation, and only supports Trypsin
		//Fixes the problem of openMS-CometIDX chain. 
		//in the said faulty method, real sequences such as ABCDKEFGR 
		//would be processed by openMS into DCBAKGFER
		//in comet idx that would turn into decoy peptide sequences: DCBAK, GFER, and DCBAKGFER
		//However, in the standard "offline" comet processing, the decoy sequences with 1 misscleavage should be: DCBAK, GFER, and GFEKDCBAR
		//This new method ensures that happens. 

		//The idea is, first you would generate all possible target sequence, then you would reverse all of the possible target sequence, include the miscleaved ones
		//So the target sequences for protein ABCDKEFGR with 1 miscleavage is:
		//ABCDK
		//EFGR
		//ABCDKEFGR
		//So you would get 3 decoy sequences by reversing each of those, keeping the C-terminus the same
		//DCBAK, GFER, and GFEKDCBAR
		//!!!

		public static String GenerateConcacenatedDecoyFasta(String fastaFile)
		{
			String outputFolder = IOUtils.getDirectory(fastaFile);
			return GenerateConcacenatedDecoyFasta(fastaFile, outputFolder);

		}
		

		public static String GenerateConcacenatedDecoyFasta(String fastaFile, String outputFolder)
		{
			String outputFilePath = Path.Combine(outputFolder, IOUtils.getBaseName(fastaFile) + "_decoyConcacenated.fasta");
			StreamWriter sw = new StreamWriter(outputFilePath);
			StreamReader reader = new StreamReader(fastaFile);
			int maxSequencePerLine = 0;
			String line = reader.ReadLine();
			while(line!= null)
			{
				String accession;
				if (line.StartsWith(">"))
				{
					accession = line;
					sw.WriteLine(accession);
					line = reader.ReadLine();
					maxSequencePerLine = Math.Max(line.Length, maxSequencePerLine);
					String sequence = "";
					while (line!=null&&!line.StartsWith(">"))
					{
						sequence = sequence + line+"\n";
						line = reader.ReadLine();
					}
					sw.Write(sequence);

					sw.WriteLine(">"+GlobalVar.DecoyPrefix + accession.Substring(1));
					String decoySequence = ReverseSequence(sequence.Replace("\n", ""));
					for (int i = 0; i < decoySequence.Length; i++)
					{
						if (i % maxSequencePerLine == 0&& i != 0 )
						{
							sw.WriteLine();
						}
						sw.Write(decoySequence[i]);
					}
					sw.WriteLine();
				}
			}
			sw.Close();
			return outputFilePath;
		}
		//depricated
		public static String GenerateConcacenatedDecoyFasta_MimicCometInternal(String fastaFile, String outputFilePath)
		{
			StreamWriter sw = new StreamWriter(outputFilePath);
			
			
			
			DigestedFastaFile df = PerformDigestion.performDigest(fastaFile, GlobalVar.NUM_MISSED_CLEAVAGES, false);
			List<DigestedPeptide> targetSequences = df.getDigestedPeptideArray();


			String line;
			foreach(DigestedPeptide targetSeq in targetSequences)
			{
				//write the target sequence
				line = ">" + targetSeq.getAccession();
				sw.WriteLine(line);

				line = targetSeq.getSequence();
				sw.WriteLine(line);

				//write the decoy sequence
				line = ">" + GlobalVar.DecoyPrefix + targetSeq.getAccession();
				sw.WriteLine(line);

				line = ReverseSequence_keepCTerm(targetSeq.getSequence());
				sw.WriteLine(line);
			}
			sw.Close();
			return outputFilePath;
		}
		private static String ReverseSequence_keepCTerm(String sequence)
		{
			//keeps the C-terminus peptide at the last position, and reverses everything
			
			String sequenceToBeReversed = sequence.Substring(0, sequence.Length - 1);
			String cTermAminoAcid = sequence[sequence.Length - 1].ToString();
			String reversedSequence = ReverseSequence(sequenceToBeReversed);
			reversedSequence = reversedSequence + cTermAminoAcid;
			return reversedSequence;
		}
		private static String ReverseSequence(String sequence)
		{
			//keeps the C-terminus peptide at the last position, and reverses everything
			String reversedSequence = "";
			foreach (char c in sequence)
			{
				reversedSequence = c + reversedSequence;
			}
			return reversedSequence;
		}



	}
}
