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
    public class SimplifiedExclusionList_Key : ExclusionList
    {
        private Dictionary<String, HashSet<String>> TimeBin;
        private Dictionary<String, String> PepSeqToExclusionKey;
        private const double massRounding = 1000;
        private const double timeBinSize = 0.2;

        public SimplifiedExclusionList_Key(double _ppmTolerance) : base(_ppmTolerance)
        {
            TimeBin = new Dictionary<String, HashSet<String>>();
            PepSeqToExclusionKey = new Dictionary<string, string>();
        }

        override
        public void addPeptide(Peptide pep)
        {
            if (!pep.isFromFastaFile())
            {
                //We do not want to exclude a peptide if it's not from a fasta file, eg. a semi-tryptic peptide
                return;
            }
            //RemovePeptide(pep.getSequence());//Prevents duplicate peptides
            double predictedRT = pep.getRetentionTime().getRetentionTimePeak();
            double mass = pep.getMass();
            // Fix the RT and mass, then add them to TimeBin
            double RT_fixed = FixRThalfmin(predictedRT);
            double mass_fixed = FixMass(mass);
            String key = String.Join("_", RT_fixed, mass_fixed);

            //Dictionary.TryGetValue is slightly faster than Dictionary.ContainsKey + retreive item
            if (TimeBin.TryGetValue(key, out HashSet<String> peptides))
            {
                //If exclusionList_TimeBin already contains a bin at this time
                peptides.Add(pep.getSequence());
            }
            else
            {
                HashSet<String> peptidesToAdd = new HashSet<String>();
                peptidesToAdd.Add(pep.getSequence());
                TimeBin.Add(key, peptidesToAdd);
            }
            //PepSeqToExclusionKey.Add(pep.getSequence(), key);
        }
        public bool RemovePeptide(String peptideSequence)
        {
            String exclusionKey = "";
            if (PepSeqToExclusionKey.TryGetValue(peptideSequence, out exclusionKey)){
                PepSeqToExclusionKey.Remove(peptideSequence);
                HashSet<String> excludedPeptdes = null;
                if(TimeBin.TryGetValue(exclusionKey, out excludedPeptdes))
                {
                    if (excludedPeptdes.Count == 1)
                    {
                        //if the exlusion list only contains 1 peptide at this exclusion window, which is THIS peptide, then remove the entire entry
                        TimeBin.Remove(exclusionKey);
                    }
                    else
                    {
                        //if the exclusion list contains more than 1 peptide at this exclusion window, just remove the sequence from the hashset
                        excludedPeptdes.Remove(peptideSequence);
                    }
                }
                return true;
            }
            return false;
        
        }

        public override bool isExcluded(double queryMass)
        {
            HashSet<String> peptidesMatched;
            return isExcluded(queryMass, out peptidesMatched);
        }
        //To determine whether an observed precursor mass on the MS1 is on the exclusion list at the current time or not
        private bool isExcluded(double queryMass, out HashSet<String> peptidesMatched)
        {
            /* The variable "currentTime" is the internal "clock" in minutes tracked by the exclusion list, it is the elapsed time of the mass spec experiment 
             * according to the mass spec data. Eg. During simulation of an experiment, when analyzing scan 2379 which has a recorded RT of 27s, 
             * currentTime will be updated to 27.
             * currentTime is updated by the function ExclusionList.setCurrentTime() whenever ExclusionProfile.updateExclusionList() is called, 
             * which in turn is called whenever a new spectra appears in ExclusionProfile.evaluate()
            */

            double correctedTime = currentTime - getRetentionTimeOffset();

            queryMass = queryMass * (1.0 - getPPMOffset()); //corrected by ppmOffset in real time

            double timeBinWidth = timeBinSize;
            double rtWin = GlobalVar.retentionTimeWindowSize;
            for (double queryTime = correctedTime - rtWin; queryTime <= correctedTime + rtWin; queryTime += timeBinWidth)
            {
                for (double query_m_withinTol = queryMass * (1.0 - GlobalVar.ppmTolerance);
                    query_m_withinTol <= queryMass * (1.0 + GlobalVar.ppmTolerance);
                    query_m_withinTol += 1.0 / massRounding)
                {
                    double rt = FixRThalfmin(queryTime);
                    double fixedMass = FixMass(query_m_withinTol);
                    String searchKey = String.Join("_", rt, fixedMass);
                    
                    if (TimeBin.TryGetValue(searchKey, out peptidesMatched))
                    {
                        return true;
                    }
                }
            }
            peptidesMatched = null;
            return false;

        }

        //Deprecated: this is wrong, since the peptidesMatchedHashSet only contains the peptide sequences 
        //in the bin in the first successful time_mz match, but the sequence could be in the second 
        //successful time_mz match
        //Returns true if peptide sequence is on the exclusion list, at this exact time (within time window tolerance) and in its mass tolerance window
        public bool EvaluateExclusion_deprecated(Peptide p)
        {
            
            HashSet<String> peptidesMatched;
            if (isExcluded(p.getMass(), out peptidesMatched))
            {
                if (peptidesMatched.Contains(p.getSequence()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        private double getRetentionTimeOffset()
        {
            //return 0;
            return RetentionTime.getRetentionTimeOffset();
        }
        private double getPPMOffset()
        {
            //return 0;
            //average of ppm calculated from ms2Precursor mass - theoretical pep mass
            return 5.660/1000000;
        }

        public bool EvaluateExclusion(Spectra spec, Peptide pep)
        {
            if(pep== null)
            {
                return false;
            }
            double queryMass = spec.getCalculatedPrecursorMass();
            queryMass = queryMass * (1.0 - getPPMOffset()); //corrected by ppmOffset in real time
            double correctedTime = currentTime - getRetentionTimeOffset();
           
            double timeBinWidth = timeBinSize;
            double rtWin = GlobalVar.retentionTimeWindowSize;
            for (double queryTime = correctedTime - rtWin; 
                queryTime <= correctedTime + rtWin; 
                queryTime += timeBinWidth)
            {
                for (double query_m_withinTol = queryMass * (1.0 - GlobalVar.ppmTolerance);
                    query_m_withinTol <= queryMass * (1.0 + GlobalVar.ppmTolerance);
                    query_m_withinTol += 1.0 / massRounding)
                {
                    double rt = FixRThalfmin(queryTime);
                    double fixedMass = FixMass(query_m_withinTol);
                    String searchKey = String.Join("_", rt, fixedMass);
                    HashSet<string> peptides = new HashSet<string>();
                    if (TimeBin.TryGetValue(searchKey, out peptides))
                    {
                        if (peptides.Contains(pep.getSequence()))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

       
        private static double FixRThalfmin(double retentiontime)
        {
            //time in minutes
            return retentiontime - retentiontime % timeBinSize;
        }

        /*private static double FixRThalfmin(double retentiontime)
        {
            double CorrRT = Math.Floor(retentiontime * 10) / 10;
            return CorrRT;
        }
        */
   

        //method that find floor of the mz value
        private static double FixMass(double mass)
        {
            double corrMass = Math.Floor(mass * massRounding) / massRounding; //10 --> one decimal; 100--> 2 decimal; 1000 --> 3 dec;
            return corrMass;
        }
        public int getExclusionListTotalSize()
        {
            return TimeBin.Count;
        }
    }
}
