using System.IO;
using System.Collections.Generic;
using System;

namespace MealTimeMS.IO
{

	public class PartialPepXMLWriter
	{
		private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		/* CONSTANTS AND TAGS */
		private const string SUMMARY_XML_TAG = "summary_xml=\"";
		private const string MZML_FILE_TAG = "<msms_run_summary base_name=\"";
		private const string SEARCH_SUMMARY_XML_TAG = "<search_summary base_name=\"";
		private const string SEARCH_DB_LOCAL_TAG = "<search_database local_path=\"";
		private const string SEARCH_DB_PARAMETER_TAG = "<parameter name=\"database_name\" value=\"";
		private const string END_TAG = "\"";
		private const string SPECTRUM_QUERY_START_TAG = "<spectrum_query";
		private const string SPECTRUM_QUERY_END_TAG = "</spectrum_query>";

		/* NEW FILE NAMES */
		private static string mzml_file_path;
		private static string database_file_path;
		private static string output_pep_xml_file_path;
		private static string input_pep_xml_file_path;
		private static string pep_xml_file_path;
		private static string mzml_file_path_basename;
		private static string pep_xml_file_path_basename;

		private static bool changeFilePaths;
		private static bool includeAllSpectra;
		private static List<int> includeSpectra;

		public static void writeCompletePepXMLFile(string input_pep_xml, string output_file_name)
		{
			includeAllSpectra = true;
			changeFilePaths = false;
			setUpFilePaths(input_pep_xml, null, output_file_name, null, null, null);
			write();
		}

		public static void writeCompletePepXMLFile(string input_pep_xml, string output_pep_xml,
			string mzml_file_name, string fasta_file_name, string pep_xml_file_name)
		{
			includeAllSpectra = true;
			changeFilePaths = true;
			setUpFilePaths(input_pep_xml, null, output_pep_xml, mzml_file_name, fasta_file_name, pep_xml_file_name);
			write();

		}

		public static void writePartialPepXMLFile(string input_pep_xml, List<int> _includeSpectra,
			string output_pep_xml)
		{
			includeAllSpectra = false;
			changeFilePaths = false;
			setUpFilePaths(input_pep_xml, _includeSpectra, output_pep_xml, null, null, null);
			write();
		}

		public static void writePartialPepXMLFile(string input_pep_xml, List<int> _includeSpectra,
			string output_pep_xml, string mzml_file_name, string fasta_file_name, string pep_xml_file_name)
		{
			includeAllSpectra = false;
			changeFilePaths = true;
			setUpFilePaths(input_pep_xml, _includeSpectra, output_pep_xml, mzml_file_name, fasta_file_name,
				pep_xml_file_name);
			write();
		}

		private static void setUpFilePaths(string input_pep_xml, List<int> _includeSpectra, string output_pep_xml,
			string mzml_file_name, string fasta_file_name, string _pep_xml_file_name)
		{

			/*** absolute file paths ***/
			input_pep_xml_file_path = IOUtils.getAbsolutePath(input_pep_xml);
			output_pep_xml_file_path = IOUtils.getAbsolutePath(output_pep_xml);
			pep_xml_file_path = IOUtils.getAbsolutePath(_pep_xml_file_name);
			mzml_file_path = IOUtils.getAbsolutePath(mzml_file_name);
			database_file_path = IOUtils.getAbsolutePath(fasta_file_name);

			/*** basename ***/
			mzml_file_path_basename = IOUtils.removeFileExtention(mzml_file_path);
			pep_xml_file_path_basename = IOUtils.removeFileExtention(IOUtils.removeFileExtention(pep_xml_file_path));
			// you have to do it twice. once for .xml and another for .pep

			/*** spectra included ***/
			includeSpectra = _includeSpectra;
		}

		private static void write()
		{

			// counters
			int counter = 0;
			int total_counter = 0;

			log.Debug("Writing partial pepxml file...");
			//try
			//{
				StreamReader reader = new StreamReader(input_pep_xml_file_path);
				var writer = new StreamWriter(output_pep_xml_file_path);
				log.Debug("File name: " + output_pep_xml_file_path);

				// get the first line
				string line = reader.ReadLine();

				// Write everything that isn't a spectrum_query
				// Write only the spectrum_query whose scan number are in includedScans
				while (line != null)
				{

					if (line.Contains(SPECTRUM_QUERY_START_TAG))
					{
						int scanNum = parseScanNum(line);
						string spectralInformation = "";
						// keep reading lines until the end tag is found
						while (!line.Contains(SPECTRUM_QUERY_END_TAG))
						{
							spectralInformation += (line + "\n");
							line = reader.ReadLine();
						}

						spectralInformation += (line + "\n");

						// only write if the scan number is in includedScans
						// will write anyways, if includeAllSpectra == true
						if (includeAllSpectra || includeSpectra.Contains(scanNum))
						{
							writer.Write(spectralInformation);
							writer.Flush();
							counter++;
						}

						total_counter++;
					}
					else
					{

						// Re-write line accordingly if it has a tag
						// will not change the line if changeFilePaths == false
						string outputLine = line;
						if (changeFilePaths == true)
						{
							if (line.Contains(SUMMARY_XML_TAG))
							{
								// pepxml file ends in .pep.xml
								outputLine = IOUtils.replaceField(SUMMARY_XML_TAG, END_TAG, line, pep_xml_file_path);
							}
							else if (line.Contains(MZML_FILE_TAG))
							{
								// mzml file is without the .mzml file extension
								outputLine = IOUtils.replaceField(MZML_FILE_TAG, END_TAG, line,
									mzml_file_path_basename);
							}
							else if (line.Contains(SEARCH_SUMMARY_XML_TAG))
							{
								// search summary xml is without the .pep.xml file extension
								outputLine = IOUtils.replaceField(SEARCH_SUMMARY_XML_TAG, END_TAG, line,
									pep_xml_file_path_basename);
							}
							else if (line.Contains(SEARCH_DB_LOCAL_TAG))
							{
								// ends in .fasta
								outputLine = IOUtils.replaceField(SEARCH_DB_LOCAL_TAG, END_TAG, line,
									database_file_path);
							}
							else if (line.Contains(SEARCH_DB_PARAMETER_TAG))
							{
								// ends in .fasta
								outputLine = IOUtils.replaceField(SEARCH_DB_PARAMETER_TAG, END_TAG, line,
									database_file_path);
							}
						}

						writer.Write(outputLine + "\n");
						writer.Flush();
					}

					line = reader.ReadLine();

				}

				log.Debug(total_counter + " spectra in original file.");
				log.Debug("Wrote " + counter + " spectra to file");
				writer.Close();
				reader.Close();
			//}
			//catch (Exception e)
			//{
			//	Console.WriteLine(e.Message);
			//	log.Error("Writing partial pepxml file unsuccessful!!!");
			//	Console.ReadKey();
			//	Environment.Exit(1);
			//}

			log.Debug("Writing partial pepxml file successful.");
		}

		/*
		 * Parses the scan number from mzml file spectrum header 
		 * e.g. <spectrum index="0" id="controllerType=0 controllerNumber=1 scan=1" defaultArrayLength="531">
		 */
		private static int parseScanNum(string line)
		{
			const string SCAN = "scan=";
			int beginIndexOfScanNum = line.IndexOf(SCAN) + SCAN.Length+1;
			int endIndexOfScanNum = line.IndexOf("\"", beginIndexOfScanNum);
			string scanstring = line.Substring(beginIndexOfScanNum, endIndexOfScanNum-beginIndexOfScanNum);
			int scanNum = Int32.Parse(scanstring);
			return scanNum;
		}

	}

}