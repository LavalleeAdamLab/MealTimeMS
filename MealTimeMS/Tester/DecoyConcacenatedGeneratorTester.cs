using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;
namespace MealTimeMS.Tester
{
	class DecoyConcacenatedGeneratorTester
	{
		public static void DoJob()
		{
			String fastaFile = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\uniprot_SwissProt_Human_1_11_2017.fasta";
			String outputFile = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\RealTimeMS\\TestData\\";
			DecoyConcacenatedDatabaseGenerator.GenerateConcacenatedDecoyFasta(fastaFile);

		}
	}
}
