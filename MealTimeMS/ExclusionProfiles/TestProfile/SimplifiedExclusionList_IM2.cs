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
    public class SimplifiedExclusionList_IM2 : ExclusionList
    {

        private Dictionary<int, Dictionary<String, HashSet<String>>> exclusionBin;
        private Dictionary<String,Dictionary<int,String>> PepSeqToExclusionKey;
        private const double massBinSize = 0.001;
        private const double timeBinSize = 0.05;
        private static double ionMobilityBinSize = 0.005;
        //private const double ionMobilityBinSize = 5;
        private const int massRoundingDecimal = 3;
        private const int rtRoundingDecimal = 2;
        private const int IMRoundingDecimal = 3;
        private static readonly HashSet<int> chargesToConsider = new HashSet<int> { 2, 3, 4 };

        private static HashSet<String> peptidesWithinExclusionWindow_cache; // For the purpose of evaluating whether an exclusion is correct, this variable caches the peptide sequences that lands in the same exclusion window as the queried precursor in the most recent isExcluded call 
        private static String matchedExclusionKey_cache;
        public SimplifiedExclusionList_IM2(double _ppmTolerance) : base(_ppmTolerance)
        {
            exclusionBin = new Dictionary<int, Dictionary<String, HashSet<String>>>();
            foreach(int charge in chargesToConsider)
            {
                exclusionBin.Add(charge, new Dictionary<String, HashSet<String>>());
            }
            PepSeqToExclusionKey = new Dictionary<String, Dictionary<int, String>>();
            peptidesWithinExclusionWindow_cache = new HashSet<String>();
            exclusionTracker = new List<HashSet<string>>();
            if(GlobalVar.includeIonMobility == false)
            {
                ionMobilityBinSize = 100; // sets the bin so large so that we doesn't consider IM
            }
        }

        override
        public void addPeptide(Peptide pep)
        {
            
            if (!pep.isFromFastaFile() | pep.getIonMobility() == null)
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

            HashSet<String> excludedPepSeq = null;
            foreach(int z in z_IM.Keys)
            {
                double IM_fixed = FixIM(z_IM[z]);
                String key = String.Join("_", RT_fixed, mass_fixed, IM_fixed);

                if (exclusionBin[z].TryGetValue(key, out excludedPepSeq))
                {
                    excludedPepSeq.Add(pep.getSequence());
                }
                else
                {
                    exclusionBin[z].Add(key, new HashSet<String> { pep.getSequence() });
                }
                if (PepSeqToExclusionKey.ContainsKey(pep.getSequence()))
                {
                    PepSeqToExclusionKey[pep.getSequence()].Add(z,key);
                }
                else
                {
                    var exclusionKey = new Dictionary<int, String>();
                    exclusionKey.Add(z, key);
                    PepSeqToExclusionKey.Add(pep.getSequence(), exclusionKey);
                }
            }
        }
        public bool RemovePeptide(String peptideSequence)
        {
            Dictionary<int,String> exclusionKeys = null;
            if (PepSeqToExclusionKey.TryGetValue(peptideSequence, out exclusionKeys))
            {
                PepSeqToExclusionKey.Remove(peptideSequence);
                HashSet<String> excludedPeptdes;
                foreach(int z in exclusionKeys.Keys)
                {
                    String exclusionKey = exclusionKeys[z];
                    if (exclusionBin[z].TryGetValue(exclusionKey, out excludedPeptdes))
                    {
                        if (excludedPeptdes.Count == 1)
                        {
                            //if the exlusion list only contains 1 peptide at this exclusion window, which is THIS peptide, then remove the entire entry
                            exclusionBin[z].Remove(exclusionKey);
                        }
                        else
                        {
                            //if the exclusion list contains more than 1 peptide at this exclusion window, just remove the sequence from the hashset
                            excludedPeptdes.Remove(peptideSequence);
                        }
                    }
                }
                
                return true;
            }
            return false;

        }

        private string FormatExclusionKey(double rt, double mass, double IM)
        {
            double fixedrt = FixTime(rt);
            double fixedMass = FixMass(mass);
            double fixedIM = FixIM(IM);
            String key = String.Join("_", fixedrt, fixedMass, fixedIM);
            return key;
        }
        private string GetQueryExclusionKeyFromSpec(Spectra spec)
        {
            double correctedTime = currentTime - getRetentionTimeOffset();

            double queryMass = spec.getCalculatedPrecursorMass() * (1.0 - getPPMOffset()); //corrected by ppmOffset in real time
            int z = spec.getPrecursorCharge();
            double queryIM = spec.getIonMobility();
            return FormatExclusionKey(correctedTime, queryMass, queryIM);
        }

        public override bool isExcluded(Spectra spec)
        {
            HashSet<String> peptidesMatched;
            return isExcluded(spec, out peptidesMatched);
        }

        static List<int> DebugScanNums = new List<int>() {168,300, 280 };
        //To determine whether an observed precursor mass on the MS1 is on the exclusion list at the current time or not
        private bool isExcluded(Spectra spec, out HashSet<String> peptidesMatched)
        {
            /* The variable "currentTime" is the internal "clock" in minutes tracked by the exclusion list, it is the elapsed time of the mass spec experiment 
             * according to the mass spec data. Eg. During simulation of an experiment, when analyzing scan 2379 which has a recorded RT of 27s, 
             * currentTime will be updated to 27.
             * currentTime is updated by the function ExclusionList.setCurrentTime() whenever ExclusionProfile.updateExclusionList() is called, 
             * which in turn is called whenever a new spectra appears in ExclusionProfile.evaluate()
            */
            //if (DebugScanNums.Contains(spec.getScanNum() ))
            //{
            //    Console.WriteLine("Pause");
            //}
            bool isExcluded = false;
            if (!chargesToConsider.Contains(spec.getPrecursorCharge())){
                peptidesMatched = null;
                return false;
            }
            double correctedTime = currentTime - getRetentionTimeOffset();

            double queryMass = spec.getCalculatedPrecursorMass() * (1.0 - getPPMOffset()); //corrected by ppmOffset in real time
            int z = spec.getPrecursorCharge();
            double queryIM = spec.getIonMobility();
            double timeBinWidth = timeBinSize;
            double rtWin = GlobalVar.retentionTimeWindowSize;
            double IMWin = GlobalVar.IMWindowSize;
            for (double queryTime = correctedTime - rtWin; 
                queryTime <= correctedTime + rtWin; 
                queryTime += timeBinWidth)
            {
                for (double query_m_withinTol = queryMass * (1.0 - GlobalVar.ppmTolerance);
                    query_m_withinTol <= queryMass * (1.0 + GlobalVar.ppmTolerance);
                    query_m_withinTol += massBinSize)
                {
                    for (double query_IM_withinTol = queryIM - IMWin;
                            query_IM_withinTol <= queryIM + IMWin;
                            query_IM_withinTol += ionMobilityBinSize)
                    {
                        String searchKey = FormatExclusionKey(queryTime, query_m_withinTol, query_IM_withinTol);
                        if (exclusionBin[z].TryGetValue(searchKey, out peptidesMatched))
                        {
                            peptidesWithinExclusionWindow_cache.UnionWith(peptidesMatched);
                            isExcluded = true; ;
                            matchedExclusionKey_cache = searchKey;
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

            if (pep == null || !chargesToConsider.Contains(spec.getPrecursorCharge()))
            {
                return true;
            }
            if (PepSeqToExclusionKey.ContainsKey(pep.getSequence()))
            {
                //if we meant to exclude this pepide, but we ended up analyzing it
                //IntendedExclusionFailed
                pe.incrementValue(Header.SE_ExclusionFailed);
                String exclusionKey = PepSeqToExclusionKey[pep.getSequence()][spec.getPrecursorCharge()];
                //GetQueryExclusionKeyFromSpec(spec);
                recordExclusion(spec.getScanNum(), "ExclusionFailed", GetQueryExclusionKeyFromSpec(spec), exclusionKey, pep.getRetentionTime().IsPredicted());

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
                return true;
            }

            if (PepSeqToExclusionKey.ContainsKey(pep.getSequence()))
            {
                String exclusionKey = PepSeqToExclusionKey[pep.getSequence()][spec.getPrecursorCharge()];

                //if we intend to exclude this peptide and we did exclude it
                if (peptidesWithinExclusionWindow_cache.Contains(pep.getSequence()))
                {
                    //ExclusionSuccessful and at correct window
                    pe.incrementValue(Header.SE_ExclusionSuccessfulAtCorrectWindow);
                    recordExclusion(spec.getScanNum(), "ExclusionSuccessfulAtCorrectWindow", GetQueryExclusionKeyFromSpec(spec), exclusionKey, pep.getRetentionTime().IsPredicted());
                    return true;
                }
                else
                {
                    //ExclusionSuccesful but at incorrect window
                    pe.incrementValue(Header.SE_ExclusionSuccessfulAtIncorrectWIndow);
                    recordExclusion(spec.getScanNum(), "ExclusionSuccessfulAtIncorrectWIndow", GetQueryExclusionKeyFromSpec(spec), exclusionKey, pep.getRetentionTime().IsPredicted());

                    return true;
                }
            }
            else
            {
                //AnalysisFailed (we do not want to exclude this peptide, but it was excluded)
                pe.incrementValue(Header.SE_AnalysisFailed);
                recordExclusion(spec.getScanNum(), "AnalysisFailed", GetQueryExclusionKeyFromSpec(spec), matchedExclusionKey_cache, pep.getRetentionTime().IsPredicted());
                return false;
            }

            return true;
        }

        static List<HashSet<String>> exclusionTracker;
        private static void recordExclusion(int scanNum, String condition, String scanKey, String exclusionKey, bool isPredicted)
        {
            String isPredictedTag = "0";
            if (isPredicted)
            {
                isPredictedTag = "1";
            }
            exclusionTracker.Add(new HashSet<String> { scanNum.ToString(), condition, scanKey, exclusionKey, isPredictedTag });
        }
       
        public static void WriteRecordedExclusion(String experimentName)
        {
            String outputFolder = System.IO.Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,"DetailedExclusionRecords");
            if (!System.IO.Directory.Exists(outputFolder))
            {
                System.IO.Directory.CreateDirectory(outputFolder);
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(
                System.IO.Path.Combine(outputFolder, experimentName+"_ExclusionRecord.tsv")))
            {
                sw.WriteLine(String.Join(separator: "\t", "scanNum", "condition", "scanKey", "exclusionKey","isPredicted"));
                foreach(var record in exclusionTracker)
                {
                    sw.WriteLine(String.Join(separator: "\t", record));
                }
                sw.Close();
            };
        }

        private static double FixTime(double retentiontime)
        {
            //time in minutes
            return Math.Round(retentiontime - retentiontime % timeBinSize, rtRoundingDecimal);
        }
        private static double FixIM(double ook0)
        {
            //time in minutes
            return Math.Round(ook0 - ook0 % ionMobilityBinSize,IMRoundingDecimal);
        }


        //method that find floor of the mz value
        private static double FixMass(double mass)
        { 
            return Math.Round(mass - mass % massBinSize , massRoundingDecimal); 
        }
        public int getExclusionListTotalSize()
        {
            return PepSeqToExclusionKey.Count;
        }
    }
}
