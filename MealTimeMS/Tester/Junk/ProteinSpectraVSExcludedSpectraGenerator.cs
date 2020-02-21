using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.Util;
using MealTimeMS.Util.PostProcessing;
namespace MealTimeMS.Tester.Junk
{
	class ProteinSpectraVSExcludedSpectraGenerator
	{
		const String scanBeginingTag = "<spectrum_query spectrum";
		const String scanNumTag = "start_scan=\"";
		const String searchHitTag = "<search_hit hit_rank=\"1";
		const String searchHitEndTag = "</search_hit>";
		const String accessionTag = "protein=\"";
		const String alternativeProteinTag = "<alternative_protein protein=\"";

		static String pepXMLFile = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\Temp\\ProteinSpectraCount\\MS_QC_120min.pep.xml";
		static String excludedScanNumFile = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\Temp\\ProteinSpectraCount\\RandomMLGEGolden\\ExcludedSpectra.txt";
		static String outputFile = "C:\\Users\\LavalleeLab\\Documents\\JoshTemp\\Temp\\ProteinSpectraCount\\ProteinTotalSpectraAgainstExcludedSpectra.txt";


		public static void DoJob()
		{

			pepXMLFile = InputFileOrganizer.OriginalCometOutput;

				excludedScanNumFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\Output\\MLGE_Golden_nonRTCheat_excludedSpectraIncluded.txt_output\\MLGE_Golden_nonRTCheat_excludedSpectraIncluded.txt_ExcludedSpectra.txt";
			outputFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\Output\\MLGE_Golden_nonRTCheat_excludedSpectraIncluded.txt_output\\spectravsProtein.txt";
				StreamReader sr = new StreamReader(pepXMLFile);
				String line = sr.ReadLine();
				Dictionary<String, ProteinFromPepXML> ls = new Dictionary<String, ProteinFromPepXML>();

				while (line != null)
				{
					if (line.Contains(scanBeginingTag))
					{
						int scanNum = int.Parse(ValueParser(line, scanNumTag));
						line = sr.ReadLine();
						List<String> proteins = new List<String>();
						while (line != null)
						{
							if (line.Contains(searchHitTag))
							{

								proteins.Add(ValueParser(line, accessionTag));

								line = sr.ReadLine();
								while (!line.Contains(searchHitEndTag))
								{
									if (line.Contains(alternativeProteinTag))
									{
										proteins.Add(ValueParser(line, alternativeProteinTag));
									}

									line = sr.ReadLine();
								}
								break;
							}

							line = sr.ReadLine();
						}

						foreach (String accession in proteins)
						{
							if (!ls.ContainsKey(accession))
							{
								ProteinFromPepXML pp = new ProteinFromPepXML();
								pp.AddSpectra(scanNum);
								ls.Add(accession, pp);
							}
							else
							{
								ls[accession].AddSpectra(scanNum);
							}

						}

					}


					line = sr.ReadLine();
				}

				sr.Close();
				sr = new StreamReader(excludedScanNumFile);

				line = sr.ReadLine();
				while (line != null)
				{
					if (line.Equals(""))
					{
						line = sr.ReadLine();
						continue;
					}
					int excludedScanNum = int.Parse(line);
					foreach (String accession in ls.Keys)
					{
						ls[accession].AddExcludedSpectra(excludedScanNum);
					}

					line = sr.ReadLine();
				}

				StreamWriter sw = new StreamWriter(outputFile);
				sw.WriteLine("Accession\tTotalSpectra\tExcludedSpectra");
				int counter = 0;
				foreach (String accession in ls.Keys)
				{
					if (accession.Contains("DECOY_"))
					{
						continue;
					}
					ProteinFromPepXML pp = ls[accession];
					String str = String.Format("{0}\t{1}\t{2}", accession, pp.totalSpectra.Count, pp.excludedSpectra.Count);
					sw.WriteLine(str);
					counter++;
				}
				sw.Close();
				Console.WriteLine("Results written to {0}, a total of {1} proteins", outputFile, counter);
			

		}

		public static void FilterForConfidentlyIdentifiedProteinOnly()
		{
			List<String> ppr= ProteinProphetEvaluator.extractIdentifiedProteinNames(InputFileOrganizer.ProtXML);
			String excludedSpectraPerProteinAll = Path.Combine(InputFileOrganizer.DataRoot, "ProteinTotalSpectraAgainstExcludedSpectra.txt");
			String outputFile = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "FilteredProteinWithExcludedSpectraCount.txt");
			StreamReader sr = new StreamReader(excludedSpectraPerProteinAll);
			StreamWriter sw = new StreamWriter(outputFile);
			String line = sr.ReadLine();
			sw.WriteLine(line);
			line = sr.ReadLine();
			int count = 0;
			while (line != null)
			{
				String protName = line.Split("\t".ToCharArray())[0];
				if (ppr.Contains(protName))
				{
					sw.WriteLine(line);
					count++;
				}
				line = sr.ReadLine();
			}
			sw.Close();
			Console.WriteLine(count);

		}

		public static String ValueParser(String line, String tag)
		{
			int startIndex = line.IndexOf(tag) + tag.Length;
			int endIndex = line.IndexOf("\"", startIndex + 1);

			return line.Substring(startIndex, endIndex - startIndex);
		}
	}



	class ProteinFromPepXML
	{
		//public String accession;
		public List<int> totalSpectra;
		public List<int> excludedSpectra;
		public ProteinFromPepXML()
		{
			totalSpectra = new List<int>();
			excludedSpectra = new List<int>();
		}
		public void AddSpectra(int i)
		{
			if (!totalSpectra.Contains(i))
			{
				totalSpectra.Add(i);
			}
		}

		public void AddExcludedSpectra(int i)
		{
			if (totalSpectra.Contains(i))
			{
				excludedSpectra.Add(i);
			}
		}

	}
}
