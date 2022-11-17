using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Util;

namespace MealTimeMS.ExclusionProfiles
{
    public class SimplifiedExclusionList : ExclusionList
    {
        private Dictionary<double, HashSet<double>> TimeBin;
        private const double massRounding = 1000;

        public SimplifiedExclusionList(double _ppmTolerance) : base(_ppmTolerance)
        {
            TimeBin = new Dictionary<double, HashSet<double>>();
        }

        override
        public void addPeptide(Peptide pep)
        {
            //exclusionList.Add(pep);
            double predictedRT = pep.getRetentionTime().getRetentionTimePeak();
            double mass = pep.getMass();
            // Fix the RT and mass, then add them to TimeBin
            double RT_fixed = FixRThalfmin(predictedRT);
            double mass_fixed = FixMass(mass);

            //Dictionary.TryGetValue is slightly faster than Dictionary.ContainsKey + retreive item
            if (TimeBin.TryGetValue(RT_fixed, out HashSet<double> excludedMasses))
            {
                //If exclusionList_TimeBin already contains a bin at this time
                excludedMasses.Add(mass_fixed);
            }
            else
            {
                HashSet<double> massesToAdd = new HashSet<double>();
                massesToAdd.Add(mass_fixed);
                TimeBin.Add(RT_fixed, massesToAdd);
            }
        }

        //To determine whether an observed precursor mass on the MS1 is on the exclusion list at the current time or not
        override public bool isExcluded(double queryMass)
        {
            /* The variable "currentTime" is the internal "clock" in minutes tracked by the exclusion list, it is the elapsed time of the mass spec experiment 
             * according to the mass spec data. Eg. During simulation of an experiment, when analyzing scan 2379 which has a recorded RT of 27s, 
             * currentTime will be updated to 27.
             * currentTime is updated by the function ExclusionList.setCurrentTime() whenever ExclusionProfile.updateExclusionList() is called, 
             * which in turn is called whenever a new spectra appears in ExclusionProfile.evaluate()
            */

            double correctedTime = currentTime - RetentionTime.getRetentionTimeOffset(); /* If you remember in the MTMS paper there is a time 
                                                                                            offset that is calculated in order to account for and correct 
                                                                                            retention time prediction error. 
                                                                                            //You can take out the offset by deleting the operation 
                                                                                            if you want to leave the offset correction alone for now
                                                                                            */

            //double queryMass_fixed = FixMass(queryMass);
            //return LookUP(correctedTime, queryMass_fixed, TimeBin);

            /* Below is my suggestion of a way to integrate the RT window tolerance feature , where you do a for loop and call LookUP
             * multiple times with different query time points, as expected, this resulted in a lot more spectra to be excluded when I tested it.
             * These code will not be reached at the moment until the return line above is commented out:
             */
            double timeBinWidth = 0.5;
            double rtWin = GlobalVar.retentionTimeWindowSize;
            for (double queryTime = correctedTime - rtWin; queryTime < correctedTime + rtWin; queryTime += timeBinWidth)
            {
                if (LookUpMass(queryTime, queryMass, TimeBin))
                {
                    return true;
                }
            }
            return false;

        }

        private static bool LookUpMass(double time, double query_mass, Dictionary<double, HashSet<double>> Time_Mass)
        {
            double corrRT = FixRThalfmin(time);
            if (Time_Mass.TryGetValue(corrRT, out HashSet<double> excludedMasses))
            {
                for (double query_m_withinTol = query_mass * (1.0 - GlobalVar.ppmTolerance);
                    query_m_withinTol <= query_mass * (1.0 + GlobalVar.ppmTolerance);
                    query_m_withinTol += 1 / massRounding)
                {
                    double fixedMass = FixMass(query_m_withinTol);
                    if (excludedMasses.Contains(fixedMass))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static double FixRThalfmin(double retentiontime)
        {
            //time in minutes
            /*double a = Math.Floor(retentiontime);
            if (retentiontime < (a + 0.3))
            {
                return a;
            }
            else
            {
                return (a + 0.5);
            }*/

            double a = Math.Floor(retentiontime);
            if (retentiontime < (a + 0.5))
            {
                return a;
            }
            else
            {
                return (a + 0.5);
            }
            //double corrRT = Math.Truncate(retentiontime * 1) / 1; //doesn;t round - just truncates (32.78 -->32.7) 10 is one dec
            //return corrRT;
            
        }

        /*private static double FixRThalfmin(double retentiontime)
        {
            double CorrRT = Math.Floor(retentiontime * 10) / 10;
            return CorrRT;
        }
        */
        //Returns the total number of mass values across all time bins on the exclusion list TimeBin object
        public int getExclusionListTotalSize()
        {
            int count = 0;
            foreach(HashSet<double> excludedMasses in TimeBin.Values)
            {
                count += excludedMasses.Count;
            }
            return count;
        }

        //method that find floor of the mz value
        private static double FixMass(double mass)
        {
            double corrMass = Math.Floor(mass * massRounding) / massRounding; //10 --> one decimal; 100--> 2 decimal; 1000 --> 3 dec;
            return corrMass;
        }
    }
}
