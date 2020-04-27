using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data;
using MealTimeMS.IO;

namespace MealTimeMS.Util
{


public class RetentionTimeUtil
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        // private static readonly double RETENTION_TIME_START_OFFSET = 60.0;
        // private static readonly double RETENTION_TIME_END_OFFSET = 60.0;
        public static int DEFAULT_WINDOW_SIZE = 100; //used for offset correction: the number of past scans to look at to correct the RT offset

        private static int window_size = DEFAULT_WINDOW_SIZE;
        private static List<Double> errors;
        private static List<Double> offsets;
        private static List<Double> scanTimes;

        /*
         * Computes an offset value for all retention time windows. This is done by
         * keeping track of the errors in predicted retention time (i.e.
         * observed_retention_time-predicted_retention_time) and calculating the offset
         * from the last WINDOW_SIZE error values
         */
        public static double computeRTOffset(double newError, double scanTime)
        {

            // initialize variables
            if (errors == null && offsets == null)
            {
                resetRTOffset();
            }
            errors.Add(newError);
            scanTimes.Add(scanTime);

            // get at max, the last WINDOW_SIZE # values or errors
            double[] values = new double[Math.Min(errors.Count, window_size)];
            for (int i = 0; i < values.Length; i++)
            {
                int index = errors.Count - (i + 1);
                values[i] = errors[index];
            }

            // compute new offset
            double offset = weightedMeanOffset(values);
            offsets.Add(offset);

            return offset;
        }

        public static void setRTAlignmentWindowSize(int window)
        {
            window_size = window;
        }

        public static void resetRTOffset()
        {
            errors = new List<Double>();
            offsets = new List<Double>();
            scanTimes = new List<Double>();
        }

        public static List<Double> getErrorsList()
        {
            return errors;
        }

        public static List<Double> getOffsetList()
        {
            return offsets;
        }

        public static List<Double> getScanTimes()
        {
            return scanTimes;
        }

        /*
         * Computes a new offset for the retention time, weighting the errors from the
         * more recent errors greater than the less recent ones. The order of values for
         * d have the most recent errors first and the least recents last. These values
         * are weighted for the values V with value v_i where i=(1,2,...,n-1,n),
         * weighted with weight w in W (1/1, 1/2, ..., 1/n-1, 1/n)
         */
        private static double weightedMeanOffset(double[] d)
        {
            double sum_weighted_value = 0.0;
            double sum_weights = 0.0;
            for (int i = 0; i < d.Length; i++)
            {
                double value = d[i];
                double weight = (1.0) / (i + 1);
                double weighted_value = value * weight;
                sum_weighted_value += weighted_value;
                sum_weights += weight;
            }
            return sum_weighted_value / sum_weights;
        }

        public static Dictionary<String, Double> calculateRetentionTime(List<Peptide> peptideList)
        {
            if (GlobalVar.useRTCalcComputedFile)
            {
                return Loader.parseRTCalcOutput(InputFileOrganizer.RTCalcResult);
            }
            // write the peptide list to a file, then call the calculate retention time
            // function on the new list
            log.Debug("Computing retention times for peptides...");
            Dictionary<String, Double> table = calculateRetentionTime(Writer.writePeptideList(peptideList));
            log.Debug("Done computing retention times for peptides");
            return table;
        }
      
        public static Dictionary<String, Double> calculateRetentionTime(String peptideList)
        {
            log.Debug("Calculating retention times using RTCalc...");
			Console.WriteLine("Calculating peptide retention times using RTCalc... This might take a while");
			// Dictionary<String, RetentionTime> rtmap = new Dictionary<String,
			// RetentionTime>();

			// This is the output file path
			String rtOutput = InputFileOrganizer.preExperimentFilesFolder +"\\" + IOUtils.getBaseName(peptideList) + "_rtOutput.txt";
            String rtCalc = InputFileOrganizer.RTCalc; // tool location

            // This is the RTCalc command to be run on the terminal
            String rtCalcCommand = rtCalc + " COEFF="+InputFileOrganizer.RTCalcCoeff+" PEPS=" + peptideList + " OUTFILE=" + rtOutput;

            // perform in-silico digestion
            log.Debug(rtCalcCommand);
            String rtTerminalOutput = ExecuteShellCommand.executeCommand(rtCalcCommand);
            log.Debug(rtTerminalOutput);
            // parse rtcalc output
            Dictionary<String, Double> parsedOutput =  Loader.parseRTCalcOutput(rtOutput);

            // foreach (String peptideSequence  in parsedOutput.Keys) {
            // // unsure whether or not d is a rt peak or rt start
            // double d = parsedOutput[&*peptideSequence);
            // rtmap.Add(peptideSequence, convertDoubleToRetentionTime(d));
            // }
            log.Debug("Parsed " + parsedOutput.Count + " retention times");

            // remove files
            //ExecuteShellCommand.executeCommand("rm " + rtOutput);
            log.Debug("Done calculating retention times using RTCalc.");

            return parsedOutput;
        }

        public static RetentionTime convertDoubleToRetentionTime(double d, double retention_time_start_offset,
                double retention_time_end_offset)
        {
            double start = Math.Max(RetentionTime.MINIMUM_RETENTION_TIME, d - retention_time_start_offset);
            double end = computeRetentionTimeEnd(d, retention_time_end_offset);
            return new RetentionTime(start, end);
        }

        // This function prevents negative double if value+flank > Double.MAX_VALUE
        // Also, if RetentionTime.MAXIMUM_RETENTION_TIME
        private static double computeRetentionTimeEnd(double value, double flank)
        {
            double max_value = RetentionTime.MAXIMUM_RETENTION_TIME;

            if ((max_value - flank) < value)
            {
                return max_value;
            }
            else
            {
                return value + flank;
            }

        }


    }

}
