using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using System.Net.Http;

namespace MealTimeMS.ExclusionProfiles.TestProfile
{
    class ExclusionMSWrapper_ExclusionList: ExclusionList
    {
        Dictionary<string, int> pepSeqToIntervalID;
        private readonly HttpClient client;
        public ExclusionMSWrapper_ExclusionList(double _ppmTolerance) : base(_ppmTolerance)
        {
            pepSeqToIntervalID = new Dictionary<string, int>();
            client = new HttpClient();
        }
        

        public override void addProteins(List<Protein> proteins)
        {
            
        }
        public override void addPeptide(Peptide pep)
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
            foreach (int z in z_IM.Keys)
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
                    PepSeqToExclusionKey[pep.getSequence()].Add(z, key);
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

        }

        private string FormatExclusionKey(double rt, double mass, double IM)
        {
            double fixedrt = FixTime(rt);
            double fixedMass = FixMass(mass);
            double fixedIM = FixIM(IM);
            String key = String.Join("_", fixedrt, fixedMass, fixedIM);
            return key;
        }
        private string GetQueryExclusionPointFromSpec(Spectra spec)
        {
            double correctedTime = currentTime - getRetentionTimeOffset();

            
        }

        public override bool isExcluded(Spectra spec)
        {
            
        }

        public override void setRetentionTimeOffset(double rtOffset)
        {

        }

        //To determine whether an observed precursor mass on the MS1 is on the exclusion list at the current time or not
        private bool isExcluded(Spectra spec, out HashSet<String> peptidesMatched)
        {
           

        }
        private double getRetentionTimeOffset()
        {
            //return 0;
            return RetentionTime.getRetentionTimeOffset();
        }
        private double getPPMOffset()
        {
            return 0;
            //average of ppm calculated from ms2Precursor mass - theoretical pep mass
            // return 5.660 / 1000000;
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
            String outputFolder = System.IO.Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "DetailedExclusionRecords");
            if (!System.IO.Directory.Exists(outputFolder))
            {
                System.IO.Directory.CreateDirectory(outputFolder);
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(
                System.IO.Path.Combine(outputFolder, experimentName + "_ExclusionRecord.tsv")))
            {
                sw.WriteLine(String.Join(separator: "\t", "scanNum", "condition", "scanKey", "exclusionKey", "isPredicted"));
                foreach (var record in exclusionTracker)
                {
                    sw.WriteLine(String.Join(separator: "\t", record));
                }
                sw.Close();
            };
        }

        
        public int getExclusionListTotalSize()
        {
            return PepSeqToExclusionKey.Count;
        }


    }
}
