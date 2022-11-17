using System;
using System.Collections.Generic;
using System.Data;
using MealTimeMS.Data;
using MealTimeMS.Util;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.ML;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data.InputFiles;
using Accord.Statistics.Models.Regression;


//using InformedProteomics.Backend.MassSpecData;
//using InformedProteomics.Backend.Data.Spectrometry;
namespace MealTimeMS.IO
{
    public static class Loader
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();


        public static DataTable parseIdentificationFeature(String fileName, DataTable dt)
        {
            Console.WriteLine("Loading Identification Features File data from " + fileName);
            StreamReader sr = new StreamReader(fileName);
            string[] headers = sr.ReadLine().Split(',');
            String[] columnNames = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();
            Type[] columnTypes = dt.Columns.Cast<DataColumn>().Select(x => x.DataType).ToArray();
            int[] colIndex = new int[columnNames.Length];

    
            for (int i=0;i<headers.Length;i++)
            {
                String header = headers[i];
                int indexInTable = Array.IndexOf(columnNames, header);
                if (indexInTable==-1)
                {
                    Console.WriteLine("Unrecognized identification feature header name: {0}. \nSystem will now wxit", header);
                    Console.ReadKey();
                    Environment.Exit(1);
                }
                else
                {
                    colIndex[i] = indexInTable;
                }
            }
            
            while (!sr.EndOfStream)
            {
                string[] line = sr.ReadLine().Split(",".ToCharArray());
                DataRow dr = dt.NewRow();
                for (int i = 0; i < columnNames.Length; i++)
                {
                    int colInd = colIndex[i];
                    Type colType = columnTypes[colInd];
                    if (colType.Equals(typeof(String)))
                    {
                        dr[colInd] = (String)line[i];
                    }
                    else if (colType.Equals(typeof(int)))
                    {
                        dr[colInd] = int.Parse(line[i]);
                    }
                    else if (colType.Equals(typeof(double)))
                    {
                        dr[colInd] = double.Parse(line[i]);
                    }
                    
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }


        public static FastaFile parseFasta(String fileName)
        {
            // Stores the mapping of accession to its respective protein sequence
            Dictionary<String, Protein> proteins = new Dictionary<String, Protein>(); 
            log.Debug("Parsing fasta file...");
            try
            {
                log.Debug("File path: " + fileName);
                StreamReader reader = new StreamReader(fileName);
                String line = reader.ReadLine();

                // read lines
                while (line != null)
                {
                    String accession = line.Split(" ".ToCharArray())[0].Split(">".ToCharArray())[1];
                    String sequence = "";

                    line = reader.ReadLine();
                    while (line.Contains(">") == false)
                    {
                        sequence += line;

                        line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }
                    }
                    Protein prot = new Protein(accession, sequence);
                    proteins.Add(accession, prot);
                }
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                log.Error("Undigested FASTA file not correctly parsed.");
                Environment.Exit(0);
            }
            log.Debug("Done parsing the undigested FASTA file.");
            return new FastaFile(fileName, proteins);
        }
        public static DigestedFastaFile parseDigestedFasta(String fileName)
        {
            // List to store the objects of each in-silico digested peptide
            List<DigestedPeptide> ArrDigest = new List<DigestedPeptide>();

            log.Debug("Parsing the digested FASTA file.");
            try
            {
                log.Debug("File path: " + fileName);
                StreamReader reader = new StreamReader(fileName);
                String line = reader.ReadLine();

                // read lines
                while (line != null)
                {
                    /*-
                     * Reads the lines of the file into an array of Strings, after each "tab", text
                     * is stored in new cell. Header shown below:
                     * sequence/protein/mass/missedCleavages/specificity/nTerminusIsSpecific/cTerminusIsSpecific
                     */
                    String[] values = line.Split("\t".ToCharArray());

                    // Skip the header
                    if (values[0].Equals("sequence"))
                    {
                        // Move to the next line
                        line = reader.ReadLine();
                        values = line.Split("\t".ToCharArray());
                    }

                    String sequence = values[0]; // peptide sequence
                    String accession = values[1]; // accession of the parent protein
                    double mass = Double.Parse(values[2]);
                    DigestedPeptide digPeps = new DigestedPeptide(sequence, accession, mass);
                    ArrDigest.Add(digPeps);

                    line = reader.ReadLine();

                }
                reader.Close();

                log.Debug("Done parsing the digested FASTA file.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                log.Error("Digested FASTA file not correctly parsed.");
                Environment.Exit(0);
            }
            return new DigestedFastaFile(fileName, ArrDigest);
        }

        //public static ResultDatabase parseResultDatabase(String result_database_file_name)
        //{
        //    return ResultDatabaseUtil.parseResultDatabase(result_database_file_name);
        //}

        public static bool checkValidFilePath(String file_path)
        {
            return File.Exists(file_path);
        }

        /*
	 * Reads a fasta file and digests it, or reads a digested fasta file These files
	 * are then used to set up a database which will be used to exclude peptides
	 * from the experiment.
	 */
        public static Database loadExclusionDatabase(String fasta_file_name, String digested_fasta_file_name)
        {
            FastaFile fasta;
            DigestedFastaFile digestedFasta;

            // Load fasta file
            fasta = Loader.parseFasta(fasta_file_name);

            // Load digested fasta file
            digestedFasta = Loader.parseDigestedFasta(digested_fasta_file_name);

            return loadExclusionDatabase(fasta, digestedFasta);
        }

        public static Database loadExclusionDatabase(String fasta_file_name, int missedCleavages)
        {
            FastaFile fasta;
            DigestedFastaFile digestedFasta;

            // Load fasta file
            fasta = Loader.parseFasta(fasta_file_name);

            // Digest the fasta file using peptide chainsaw
            digestedFasta = PerformDigestion.performDigest(fasta, missedCleavages);

            return loadExclusionDatabase(fasta, digestedFasta);
        }

        public static Database loadExclusionDatabase(FastaFile fasta, DigestedFastaFile digestedFasta)
        {
            return new Database(fasta, digestedFasta);
        }

        //public static ResultDatabase loadResultDatabase(String mzml_file_name, String mzid_file_name)
        //{
        //    MZMLFile mzml = Loader.parseMZML(mzml_file_name);
        //    MZIDFile mzid = Loader.parseMZID(mzid_file_name);
        //    return loadResultDatabase(mzml, mzid);
        //}

        //public static ResultDatabase loadResultDatabase(MZMLFile mzml, String mzid_file_name)
        //{
        //    MZIDFile mzid = Loader.parseMZID(mzid_file_name);
        //    return loadResultDatabase(mzml, mzid);
        //}

        //public static ResultDatabase loadResultDatabase(MZMLFile mzml, MZIDFile mzid)
        //{
        //    return new ResultDatabase(mzml, mzid);
        //}

        //public static Set<String> parseSetString(String input)
        //{
        //    // match the inside of []
        //    Pattern pattern = Pattern.compile("\\[(.*)\\]");
        //    Matcher matcher = pattern.matcher(input);
        //    HashSet<String> returnSet = new HashSet<String>();

        //    while (matcher.find())
        //    {
        //        // handles whitespace
        //        List<String> items = matcher.group(1).Split("\\s*,\\s*");
        //        foreach (String number in items)
        //        {
        //            returnSet.Add(number.Trim());
        //        }
        //    }
        //    return returnSet;
        //}

        public static Dictionary<String, Double> parseSSRCalcOutput(String fileName)
        {
            Dictionary<String, Double> hydrophobicityIndexDatabase = new Dictionary<String, Double>();

            log.Debug("Parsing SSRCalc file...");
            try
            {
                log.Debug("File path: " + fileName);
                StreamReader reader = new StreamReader(fileName);
                reader.ReadLine(); // discard header
                                   // header: Sequence Length HI (pred) HI (DB) Slope
                String line = reader.ReadLine();

                // read lines
                while (line != null)
                {
                    String[] split = line.Split("\t".ToCharArray());
                    String peptideSequence = split[0];
                    if (peptideSequence == null || peptideSequence.Equals("") || split.Length < 1)
                    {
                        break;
                    }
                    String predictedHydrophobicityIndex = split[2];

                    hydrophobicityIndexDatabase.Add(peptideSequence, Double.Parse(predictedHydrophobicityIndex));
                    line = reader.ReadLine();
                }
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                log.Error("SSRCalc file not correctly parsed.");
                Environment.Exit(0);
            }
            log.Debug("Done parsing the SSRCalc file.");
            return hydrophobicityIndexDatabase;
        }

        public static Dictionary<String, Double> parseRTCalcOutput(String fileName)
        {
            Dictionary<String, Double> retentionTimeDatabase = new Dictionary<String, Double>();

            log.Debug("Parsing RTCalc file...");
            try
            {
                log.Debug("File path: " + fileName);
                StreamReader reader = new StreamReader(fileName);
                String line = reader.ReadLine();//skip header in auto-rt
                line = reader.ReadLine(); 
                // read lines
                while (line != null)
                {
                    String[] split = line.Split("\t".ToCharArray());
                    String peptideSequence = split[0];
                    String predictedRetentionTime = split[1];
                    if (peptideSequence == null || peptideSequence.Equals(""))
                    {
                        break;
                    }
                    double retentionTime = Double.Parse(predictedRetentionTime);
                    if (retentionTime < 0)
                    {
                        // SOME ARE NEGATIVE VALUES. WHY???
                        retentionTime = 0;
                    }
                    else
                    {
                        // CONVERT SECONDS INTO MINUTES
                        //retentionTime = retentionTime / 60.0;
                        
                    }

                    if (!retentionTimeDatabase.ContainsKey(peptideSequence))
                    {
                        retentionTimeDatabase.Add(peptideSequence, retentionTime);
                    }
                   

                    line = reader.ReadLine();
                }
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                log.Error("RTCalc file not correctly parsed.");
                Environment.Exit(0);
            }
            log.Debug("Done parsing the RTCalc file.");
            return retentionTimeDatabase;
        }
		public static LogisticRegression LoadAccordNetLogisticRegressionModel(String savedCoefficientFile)
		{
			StreamReader sr = new StreamReader(savedCoefficientFile);
	
			String[] weightsStr = sr.ReadLine().Trim().Split("\t".ToCharArray());
			double[] weights = new double[weightsStr.Length];
			for (int i = 0 ; i<weights.Length; i++)
			{
				weights[i] = double.Parse(weightsStr[i]);
			}
			double intercept = double.Parse(sr.ReadLine());
			LogisticRegression lrAccord = new LogisticRegression();
			lrAccord.Weights = weights;
			lrAccord.Intercept = intercept;
			return lrAccord;

		}

		public static ITransformer loadLogisticRegressionModel(String fileName)
        {
			//TODO delete
			ITransformer IT = null;
			return IT;

            log.Debug("Loading logistic regression model...");
            log.Debug("File: " + fileName);
            MLContext mlContext = new MLContext();

            //Define DataViewSchema for data preparation pipeline and trained model
            DataViewSchema modelSchema;

            // Load trained model
            ITransformer  lrModel = mlContext.Model.Load(fileName, out modelSchema);

            log.Debug("Finished loading logisitc regression model.");
            return lrModel;
        }
		public static MZMLFile parseMS2File(String fileName)
		{
			return parseMSFile(fileName, 2);
		}

		public static MZMLFile parseMSFile(String fileName, int _mslevel)
        {
            List<Spectra> spectraArray = new List<Spectra>();
            log.Info("Parsing the ms2 file: "+fileName);
            log.Debug("File path: " + fileName);
            StreamReader reader = new StreamReader(fileName);
            String line = reader.ReadLine();

            while (line != null)
            {
                if (!line.Substring(0, 1).Equals("H"))
                {
                    break;
                }
                line = reader.ReadLine();
            }
            int index = 1;
            while (!String.IsNullOrEmpty(line))
            {
                int scanNum = 0 ;
                int msLevel = _mslevel;
                int peakCount=0;
                double startTime=0;
                double precursorMz=0;
                int precursorCharge=0;
                //parse header
                while (line != null)
                {
                    String[] str= line.Split("\t".ToCharArray());
                    if (str[0].Equals("S"))
                    {
                        scanNum = int.Parse(str[1]);
                        precursorMz = double.Parse(str[3]); //isolation window
                    }else if (str[0].Equals("I"))
                    {
                        if (str[1].Equals("RTime"))
                        {
                            startTime = double.Parse(str[2]);
                        }
                    }else if (str[0].Equals("Z"))
                    {
                        precursorCharge = int.Parse(str[1]);
						//TODO Remove, only to test if using Accurate Monoisotopic M/z instead of isolation window
						//double precursorMH = double.Parse(str[2]);
						//precursorMz = MassConverter.MHPlusToMZ(precursorMH,precursorCharge);
                        //Remove end
						break;
                    }
                    line = reader.ReadLine();
                }

                line = reader.ReadLine();
                List<String[]> peaks = new List<String[]>();
                while (!line.Substring(0,1).Equals("S"))
                {
                    peaks.Add(line.Split(" ".ToCharArray()));
                    line = reader.ReadLine();
                    if (String.IsNullOrEmpty(line)){
                        break;
                    }
                }
                peakCount = peaks.Count;
                double[] peakMz = new double[peakCount];
                double[] peakIntensity = new double[peakCount];
                for(int i=0;i<peakCount;i++)
                {
                    peakMz[i] = double.Parse(peaks[i][0]);
                    peakIntensity[i] = double.Parse(peaks[i][1]);
                }
                Spectra spec = new Spectra(index, scanNum, msLevel, peakCount, peakMz, peakIntensity,
                    startTime, precursorMz, precursorCharge);
                spectraArray.Add(spec);
                index++;
            }


            MZMLFile mzml = new MZMLFile(fileName, spectraArray);
           
            return mzml;
                
        }


        public static MZMLFile parseMZMLCSV (String fileName, int num)
        {
            List<Spectra> spectraArray = new List<Spectra>();
            log.Info("Parsing the mzML file.");
            log.Debug("File path: " + fileName);
            StreamReader reader = new StreamReader(fileName);

            String[] header = reader.ReadLine().Split("\t".ToCharArray());

            String line = reader.ReadLine();
            int counter = 0;
            while (line != null)
            {
                

                String[] data = line.Split("\t".ToCharArray());
                int index = (int) double.Parse(data[0]);
                int scanNum = int.Parse(data[1]);
                int msLevel = int.Parse(data[2]);
                int peakCount = int.Parse(data[3]);
                double startTime = double.Parse(data[4]);
                double precursorMz = double.Parse(data[5]);
                double precursorCharge = double.Parse(data[6]);
                


                String[] mzArr= reader.ReadLine().Split("\t".ToCharArray());
                String[] intensityArr = reader.ReadLine().Split("\t".ToCharArray());
                double[] peakMz = new double[peakCount];
                double[] peakIntensity = new double[peakCount];
                for(int i = 0; i < peakCount; i++)
                {
                    peakMz[i] = double.Parse(mzArr[i]);
                    peakIntensity[i] = double.Parse(intensityArr[i]);
                }
                Spectra spec = new Spectra(index, scanNum, msLevel, peakCount, peakMz, peakIntensity,
                    startTime, precursorMz, precursorCharge);
                spectraArray.Add(spec);
                line = reader.ReadLine();
                counter++;
                if (counter >= num)
                {
                    break;
                }

            }
            Console.WriteLine("Finished Parsing MZML. Parsed " + counter + " spectra");
            return new MZMLFile(fileName, spectraArray);

        }
        public static MZMLFile parseMZMLCSV(String fileName)
        {
            return parseMZMLCSV(fileName, int.MaxValue);
        }
    }
}
