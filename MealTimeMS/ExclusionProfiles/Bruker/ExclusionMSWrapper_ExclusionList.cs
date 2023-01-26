using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using System.Net.Http;

namespace MealTimeMS.ExclusionProfiles
{
    class ExclusionMSWrapper_ExclusionList : ExclusionList
    {
        Dictionary<string, int> pepSeqToIntervalID;
        private readonly HttpClient client;
        private string url_exclusionMS;
        private string url_POST_intervals;
        private string url_DELETE_interval;
        private string url_DELETE_exclusionMS; //url to call to clear the active exclusion list
        private string url_Offset;


        public ExclusionMSWrapper_ExclusionList(double _ppmTolerance, String _url_exclusionMS) : base(_ppmTolerance)
        {
            pepSeqToIntervalID = new Dictionary<string, int>();
            client = new HttpClient();
            url_exclusionMS = _url_exclusionMS;
            SetUpURL();
            ClearExclusionMS();
            updateRetentionTimeOffset(0);
        }
        public void SetUpURL()
        {
            url_POST_intervals = String.Concat(url_exclusionMS, "/exclusionms/intervals");
            url_DELETE_interval = String.Concat(url_exclusionMS, "/exclusionms/interval");
            url_DELETE_exclusionMS = String.Concat(url_exclusionMS, "/exclusionms");
            url_Offset = String.Concat(url_exclusionMS, "/exclusionms");
        }

        /*note that addProteins is different from addPeptide in this class: if a peptide is already in the exclusion
        list, addProteins  
    */
        public override void addProteins(List<Protein> proteins)
        {
            if (proteins.Count == 0)
            {
                return;
            }
            //extracts all peptides of all proteins and flattens them into a hashset to remove duplicates
            HashSet<Peptide> peptidesToAdd = new HashSet<Peptide>(proteins.SelectMany(prot => prot.getPeptides()));
            //HashSet<Peptide> peptidesToAdd = new HashSet<Peptide>();
            //foreach (Protein prot in proteins)
            //{
            //    foreach (Peptide pep in prot.getPeptides())
            //    {
            //        if (!pepSeqToIntervalID.ContainsKey(pep.getSequence()))
            //        {
            //            peptidesToAdd.Add(pep);
            //            pepSeqToIntervalID.Add(pep.getSequence(), pep.getPeptideID());
            //        }
            //    }
            //}
            addPeptides(peptidesToAdd.ToList());
        }
        public override void addProtein(Protein protein)
        {
            addProteins(new List<Protein>() { protein });
        }
        
        private bool CheckPeptideQualitification(Peptide pep)
        {
            if (!pep.isFromFastaFile())
            {
                //We do not want to exclude a peptide if it's not from a fasta file, eg. a semi-tryptic peptide
                return false;
            }
            if (pep.getIntervalJsons() == null)
            {
                return false;
            }
            return true;
        }
        public void addPeptides(List<Peptide> peptides)
        {
            List<String> intervalJsons = new List<String>();
            foreach(Peptide pep in peptides)
            {
                if (CheckPeptideQualitification(pep) == false)
                {
                    continue;
                }
                if (!pepSeqToIntervalID.ContainsKey(pep.getSequence())){
                    intervalJsons.AddRange(pep.getIntervalJsons());
                    pepSeqToIntervalID.Add(pep.getSequence(), pep.getPeptideID());
                }
            }
            PostIntervals(intervalJsons);
        }
        public override void addPeptide(Peptide pep)
        {
            addPeptides(new List<Peptide>() { pep });
        }
        public void RemovePeptide(Peptide pep)
        {
            String pepSeq = pep.getSequence();
            if (pepSeqToIntervalID.ContainsKey(pepSeq))
            {
                RemoveInterval(pepSeqToIntervalID[pepSeq]);
                pepSeqToIntervalID.Remove(pepSeq);
            }
        }
        static int DeleteRequestCounter = 0;
        private async void RemoveInterval(int id)
        {
            String intervalJson = ExclusionMSInterval.getEmptyJSONStringFromID(id);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri(url_DELETE_interval),
                Content = new StringContent(intervalJson, Encoding.UTF8, "application/json")
            };
            DeleteRequestCounter++;
            if (DeleteRequestCounter % GlobalVar.ScansPerOutput == 0)
            {
                Console.WriteLine("DELETE Request message: {0}", request.ToString());
                Console.WriteLine("msg content: {0}", intervalJson);
            }
            var response = await client.SendAsync(request);
        }
        static int PostRequestCounter = 0;
        public async void PostIntervals(List<String> intervalJsons)
        {
            String jsoncontent = String.Concat("[", String.Join(separator:",",intervalJsons),"]");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url_POST_intervals),
                Content = new StringContent(jsoncontent, Encoding.UTF8, "application/json")
            };
            if (intervalJsons.Count == 0 || intervalJsons[0] == "")
            {
                Console.WriteLine("!!POST request empty!!");
            }
            PostRequestCounter++;
            if (PostRequestCounter % GlobalVar.ScansPerOutput==0) {
                Console.WriteLine("POST Request message: {0}", request.ToString());
                Console.WriteLine("msg content: {0}",jsoncontent);
            }
            //var response = client.SendAsync(request);
            var response = Task.Run(async () => await client.SendAsync(request)).Result;
            //String responseString = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(responseString);
            //var status = ( response.StatusCode.ToString());
            //Console.WriteLine(status);
        }
        public override void addObservedPeptide(Peptide pep)
        {
            if (!pep.isFromFastaFile())
                return;
            RemovePeptide(pep);
            addPeptide(pep);
        }

        public void ClearExclusionMS()
        {
            client.DeleteAsync(url_DELETE_exclusionMS);
        }
     


        public override bool isExcluded(Spectra spec)
        {
            return false; // to be implemented, not necessary for exclusionMS
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
        public override void updateRetentionTimeOffset(double newOffset_min)
        {
            double newOffset_sec = newOffset_min * 60.0;
            //http://192.168.0.29:8000/exclusionms/offset?mass=0&rt=1.0&ook0=0&intensity=0
            string requestURI = String.Format("{0}/offset?mass=0&rt={1}&ook0=0&intensity=0",
                url_Offset, newOffset_sec);
            String jsoncontent ="";
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(requestURI),
                Content = new StringContent(jsoncontent, Encoding.UTF8, "application/json")
            };
            //var response = client.SendAsync(request);
            var response = Task.Run(async () => await client.SendAsync(request)).Result;
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
            return pepSeqToIntervalID.Count;
        }


    }
}
