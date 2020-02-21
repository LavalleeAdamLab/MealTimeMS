using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data;
using MealTimeMS.Util;

namespace MealTimeMS.IO
{


public class Writer
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /*
         * This takes an array list of proteins and writes it in fasta format
         */
        public static void writeFastaFile(String file_path, List<Protein> proteins)
        {
            log.Debug("Writing List<Protein> to fasta...");
            try
            {
                StreamWriter writer = new StreamWriter(file_path);
                log.Debug("File name: " + file_path);

                foreach (Protein p in proteins)
                {
                    String header = ">" + p.getAccession();
                    String sequence = p.getSequence();
                    writer.Write(header + "\n" + sequence + "\n");
                    writer.Flush();
                }
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                log.Error("Writing fasta file unsuccessful!!!");
                Environment.Exit(0);
            }
            log.Debug("Writing fasta file successful.");
        }

        /*
         * This writes the peptides in a format which is usable by peptide chainsaw and
         * is a temporary file
         */
        public static String writePeptideList(List<Peptide> peptideList)
        {
            String outputFileName = InputFileOrganizer.OutputRoot+ "tempOutputPeptideList.txt";
            log.Debug("Writing peptide list to a file...");
            try
            {
                StreamWriter writer = new StreamWriter(outputFileName);
                log.Debug("File name: " + outputFileName);
                int count = 0;
                foreach (Peptide pep in peptideList)
                {
                    String sequence = pep.getSequence();
                    if (sequence.Length >= 6)
                    {
                        count++;
                        writer.Write(sequence + "\n");
                        writer.Flush();
                    }
                    else
                    {
                        log.Debug(sequence + " length < 6 AA. Unable to calculate retention time");
                    }
                }
                log.Debug(count + " peptide sequences written to file");
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                log.Error("Writing file unsuccessful!!!");
                Environment.Exit(0);
            }
            log.Debug("Writing file successful.");
            return outputFileName;
        }

        /*
         * Writes the result database to a file, to make it faster to re-initialize the
         * software
         */
        //public static void writeResultDatabaseToFile(String file_path, ResultDatabase rd)
        //{
        //    log.Debug("Writing Result Database to a file...");
        //    try
        //    {
        //        StreamWriter writer = new StreamWriter(file_path);
        //        log.Debug("File name: " + file_path);
        //        // Get all IDs
        //        List<IDs> ids = new List<IDs>(rd.getIDs());
        //        // Sort by scan number
        //        ids.Sort((IDs x, IDs y) => (x.getScanNum()).CompareTo(y.getScanNum()));

        //        // Write header
        //        String[] header = new String[] { "scan", "scan_t", "peptide_mass", "peptide_sequence", "parent_proteins",
        //            "peptide_evidence", "peptide_reference", "database_sequence_id", "xCorr", "deltaCN", "deltaCNStar",
        //            "spscore", "sprank", "evalue" };
        //        writer.Write(String.Join("\t", header));
        //        foreach (IDs id in ids)
        //        {
        //            writer.Write("\n" + outputIDToTSVFormat(id));
        //            writer.Flush();
        //        }
        //        writer.Flush();
        //        writer.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //        log.Error("Writing file unsuccessful!!!");
        //        Environment.Exit(0);
        //    }
        //    log.Debug("Writing file successful.");
        //}

        /*
         * Write the identification features used for training the logistic regression
         * classifier
         */
        //public static void writeIdentificationFeaturesFile(String file_path,
        //        List<IdentificationFeatures> positiveTrainingSet,
        //        List<IdentificationFeatures> negativeTrainingSet)
        //{
        //    log.Debug("Writing Identification Features to a file...");
        //    try
        //    {
        //        StreamWriter writer = new StreamWriter(file_path);
        //        log.Debug("File name: " + file_path);

        //        // Write header TODO remove
        //        String header = "label," + IdentificationFeatures.getHeader();
        //        writer.Write(header);

        //        // in the first column, 1 indicates positive training set
        //        foreach (IdentificationFeatures i in positiveTrainingSet)
        //        {
        //            writer.Write("\n" + "1," + i.WriteToFile());
        //            writer.Flush();
        //        }
        //        // in the first column, 0 indicates negative training set
        //        foreach (IdentificationFeatures i in negativeTrainingSet)
        //        {
        //            writer.Write("\n" + "0," + i.WriteToFile());
        //            writer.Flush();
        //        }
        //        writer.Flush();
        //        writer.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        e.printStackTrace();
        //        log.Error("Writing file unsuccessful!!!");
        //        System.exit(0);
        //    }
        //    log.Debug("Writing file successful.");
        //}

        /*
         * Useful for outputting the IDs object in the correct order for
         * writeResultDatabaseToFile
         */
        private static String outputIDToTSVFormat(IDs id)
        {
            return id.getScanNum() + "\t" + id.getScanTime() + "\t" + id.getPeptideMass() + "\t" + id.getPeptideSequence()
                    + "\t" + id.getParentProteinAccessions() + "\t" + id.getPepEvid() + "\t" + id.getPepRef() + "\t"
                    + id.getDBSeqID() + "\t" + id.getXCorr() + "\t" + id.getDeltaCN() + "\t" + id.getDeltaCNStar() + "\t"
                    + id.getSPScore() + "\t" + id.getSPRank() + "\t" + id.getEValue();
        }


    }

}
