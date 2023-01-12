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
    public class SimplifiedExclusionList_IM : ExclusionList
    {
    
        private Dictionary<String, Dictionary<int, Dictionary<double, HashSet<String>>>> exclusionBin;
        private Dictionary<String, String> PepSeqToExclusionKey;
        private const double massRounding = 1000;
        private const double timeBinSize = 0.2;
        private const double ionMobilityBinSize = 0.005;
        private static readonly HashSet<int> chargesToConsider = new HashSet<int>{2,3,4};

        private static HashSet<String> peptidesWithinExclusionWindow_cache; // For the purpose of evaluating whether an exclusion is correct, this variable caches the peptide sequences that lands in the same exclusion window as the queried precursor in the most recent isExcluded call 
        public SimplifiedExclusionList_IM(double _ppmTolerance) : base(_ppmTolerance)
        {
            exclusionBin = new Dictionary<String, Dictionary<int, Dictionary<double, HashSet<String>>>>();
            PepSeqToExclusionKey = new Dictionary<string, string>();
            peptidesWithinExclusionWindow_cache = new HashSet<String>();
        }

        override
        public void addPeptide(Peptide pep)
        {
            if (!pep.isFromFastaFile()|pep.getIonMobility()==null)
            {
                //We do not want to exclude a peptide if it's not from a fasta file, eg. a semi-tryptic peptide
                return;
            }
            if (pep.getRetentionTime().IsPredicted() && PepSeqToExclusionKey.ContainsKey(pep.getSequence()))
            {
                //If this peptide is predicted (hasn't been observed),and it's already on the exclusion list, there's no need to add it again
                return;
            }
            RemovePeptide(pep.getSequence());//Prevents duplicate peptides
            String sequence = pep.getSequence();
            double predictedRT = pep.getRetentionTime().getRetentionTimePeak();
            double mass = pep.getMass();
            Dictionary<int, double> z_IM = pep.getIonMobility();
            // Fix the RT and mass, then add them to TimeBin
            double RT_fixed = FixTime(predictedRT);
            double mass_fixed = FixMass(mass);
            
            String rt_mass = String.Join("_", RT_fixed, mass_fixed);

            Dictionary<int, Dictionary<double, HashSet<String>> > excluded_z_IM_pep;
            if (exclusionBin.TryGetValue(rt_mass, out excluded_z_IM_pep))
            {
                //If exclusionBin already has a value at this mz-rt combination,
                //loop through z and add the IMs
                foreach(int z in z_IM.Keys)
                {
                    double IM_fixed = FixIM(z_IM[z]);
                    if (excluded_z_IM_pep.ContainsKey(z))
                    {
                        //if already contains this z
                        if (excluded_z_IM_pep[z].ContainsKey(IM_fixed))
                        {
                            excluded_z_IM_pep[z][IM_fixed].Add(sequence);
                        }
                        else
                        {
                            excluded_z_IM_pep[z].Add(IM_fixed, new HashSet<String>{ sequence });
                        }
                        
                    }
                    else
                    {
                        var IM_pep_toAdd = new Dictionary<double, HashSet<String>>();
                        IM_pep_toAdd.Add(IM_fixed, new HashSet<String> { sequence });
                        excluded_z_IM_pep.Add(z, IM_pep_toAdd);
                    }
                }
            }
            else
            {
                excluded_z_IM_pep = new Dictionary<int, Dictionary<double, HashSet<String>>>();
                foreach (int z in z_IM.Keys)
                {
                    double IM_fixed = FixIM(z_IM[z]);
                    var IM_pep_toAdd = new Dictionary<double, HashSet<String>>();
                    IM_pep_toAdd.Add(IM_fixed, new HashSet<String> { sequence });
                    excluded_z_IM_pep.Add(z, IM_pep_toAdd);
                }
               
                exclusionBin.Add(rt_mass, excluded_z_IM_pep);
            }
          
            PepSeqToExclusionKey.Add(pep.getSequence(), rt_mass);
        }
        public bool RemovePeptide(String peptideSequence)
        {
            String exclusionKey = "";
            if (PepSeqToExclusionKey.TryGetValue(peptideSequence, out exclusionKey))
            {
                PepSeqToExclusionKey.Remove(peptideSequence);
                Dictionary<int, Dictionary<double, HashSet<String>>> excludedPeptdes = null;
                if (exclusionBin.TryGetValue(exclusionKey, out excludedPeptdes))
                {
                    if (excludedPeptdes.Count == 1)
                    {
                        //if the exlusion list only contains 1 peptide at this exclusion window, which is THIS peptide, then remove the entire entry
                        exclusionBin.Remove(exclusionKey);
                    }
                    else
                    {
                        //if the exclusion list contains more than 1 peptide at this exclusion window, just remove the sequence from the hashset
                        //excludedPeptdes.Remove(/);
                    }
                }
                return true;
            }
            return false;

        }

        public override bool isExcluded(Spectra spec)
        {
            HashSet<String> peptidesMatched;
            return isExcluded(spec, out peptidesMatched);
        }

        //To determine whether an observed precursor mass on the MS1 is on the exclusion list at the current time or not
        private bool isExcluded(Spectra spec, out HashSet<String> peptidesMatched)
        {
            /* The variable "currentTime" is the internal "clock" in minutes tracked by the exclusion list, it is the elapsed time of the mass spec experiment 
             * according to the mass spec data. Eg. During simulation of an experiment, when analyzing scan 2379 which has a recorded RT of 27s, 
             * currentTime will be updated to 27.
             * currentTime is updated by the function ExclusionList.setCurrentTime() whenever ExclusionProfile.updateExclusionList() is called, 
             * which in turn is called whenever a new spectra appears in ExclusionProfile.evaluate()
            */
            bool isExcluded = false;
            bool pepSeqMatched = false;


            double correctedTime = currentTime - getRetentionTimeOffset();

            double queryMass = spec.getCalculatedPrecursorMass() * (1.0 - getPPMOffset()); //corrected by ppmOffset in real time
            int z = spec.getPrecursorCharge();
            double queryIM = spec.getIonMobility();
            double timeBinWidth = timeBinSize;
            double rtWin = GlobalVar.retentionTimeWindowSize;
            double IMWin = GlobalVar.IMWindowSize;
            for (double queryTime = correctedTime - rtWin; queryTime <= correctedTime + rtWin; queryTime += timeBinWidth)
            {
                for (double query_m_withinTol = queryMass * (1.0 - GlobalVar.ppmTolerance);
                    query_m_withinTol <= queryMass * (1.0 + GlobalVar.ppmTolerance);
                    query_m_withinTol += 1.0 / massRounding)
                {

                        double rt = FixTime(queryTime);
                        double fixedMass = FixMass(query_m_withinTol);
                        String rt_mass_searchKey = String.Join("_", rt, fixedMass);

                        Dictionary<int, Dictionary<double,HashSet<String>>> z_IM_pep;
                        if (exclusionBin.TryGetValue(rt_mass_searchKey, out z_IM_pep))
                        {
                            if (z_IM_pep.ContainsKey(z))
                            {
                                for (double query_IM_withinTol = queryIM  - IMWin;
                                query_IM_withinTol <= queryIM + IMWin ;
                                query_IM_withinTol += ionMobilityBinSize)
                                {
                                    double fixedIM = FixIM(query_IM_withinTol);
                                    if (z_IM_pep[z].ContainsKey(fixedIM))
                                    {
                                        peptidesMatched = z_IM_pep[z][fixedIM];
                                        peptidesWithinExclusionWindow_cache.UnionWith(peptidesMatched);
                                        isExcluded =  true;;
                                    }
                                
                                }
                            }
                        }
                }
            }
            peptidesMatched = null;
            return isExcluded;

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
            return 5.660 / 1000000;
        }
        public bool EvaluateAnalysis(PerformanceEvaluator pe, Spectra spec, Peptide pep)
        {

            if (pep == null)
            {
                return false;
            }
            if(PepSeqToExclusionKey.ContainsKey(pep.getSequence()))
            {
                //if we meant to exclude this pepide, but we ended up analyzing it
                //IntendedExclusionFailed
                pe.incrementValue(Header.SE_ExclusionFailed);
                return false;
            }
            else
            {
                //AnalysisSuccessful
                pe.incrementValue(Header.SE_AnalysisSuccessful);
                return true;
            }
        }

        public bool EvaluateExclusion(PerformanceEvaluator pe, Spectra spec, Peptide pep)
        {
            if (pep == null)
            {
                return false;
            }
            if (PepSeqToExclusionKey.ContainsKey(pep.getSequence()))
            {
                //if we intend to exclude this peptide and we did exclude it
                if (peptidesWithinExclusionWindow_cache.Contains(pep.getSequence()))
                {
                    //ExclusionSuccessful and at correct window
                    pe.incrementValue(Header.SE_ExclusionSuccessfulAtCorrectWindow);
                }
                else
                {
                    //ExclusionSuccesful but at incorrect window
                    pe.incrementValue(Header.SE_ExclusionSuccessfulAtIncorrectWIndow);
                }
            }
            else
            {
                //AnalysisFailed (we do not want to exclude this peptide, but it was excluded)
                pe.incrementValue(Header.SE_AnalysisFailed);
            }
            
            return true;
        }


        private static double FixTime(double retentiontime)
        {
            //time in minutes
            return retentiontime - retentiontime % timeBinSize;
        }
        private static double FixIM(double ook0)
        {
            //time in minutes
            return ook0 - ook0 % ionMobilityBinSize;
        }


        //method that find floor of the mz value
        private static double FixMass(double mass)
        {
            double corrMass = Math.Floor(mass * massRounding) / massRounding; //10 --> one decimal; 100--> 2 decimal; 1000 --> 3 dec;
            return corrMass;
        }
        public int getExclusionListTotalSize()
        {
            return exclusionBin.Count;
        }
    }
}
