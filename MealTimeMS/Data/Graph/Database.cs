using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.InputFiles;
using MealTimeMS.Util;
using System.Diagnostics;

namespace MealTimeMS.Data.Graph
{


public class Database
    {

        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private static readonly bool DEFAULT_INCLUDE_CARBAMIDO_MODIFICATION = true;
        private static readonly bool DEFAULT_INCLUDE_RETENTION_TIME = true;
        private static readonly double DEFAULT_RETENTION_TIME_WINDOW = 0.75;


        private Dictionary<String, Protein> AccesstionToProtein;
        private Dictionary<String, Peptide> SequenceToPeptide;
        private List<Peptide> peptides; // sorted by mass

        // flag for if we're taking into account carbamido modification on cysteine or
        // not
        private bool includeCarbamidoModification;
        // flag for if we're incorporating retention time or not
        private bool includeRetentionTime;

        private double retentionTimeWindow;
        private Dictionary<String, Double> peptideRetentionTimes;

        public Database(FastaFile fastaFile, DigestedFastaFile digestedFastaFile) :
            this(fastaFile, digestedFastaFile, DEFAULT_INCLUDE_CARBAMIDO_MODIFICATION, DEFAULT_INCLUDE_RETENTION_TIME)
        { }

        public Database(FastaFile fastaFile, DigestedFastaFile digestedFastaFile, bool _includeCarbamidoModification,
                bool _includeRetentionTime) : this(fastaFile, digestedFastaFile, _includeCarbamidoModification, _includeRetentionTime,
                    DEFAULT_RETENTION_TIME_WINDOW)
        { }

        public Database(FastaFile fastaFile, DigestedFastaFile digestedFastaFile, bool _includeCarbamidoModification,
                bool _includeRetentionTime, double _retentionTimeWindow)
        {
            includeCarbamidoModification = _includeCarbamidoModification;
            includeRetentionTime = _includeRetentionTime;
            retentionTimeWindow = _retentionTimeWindow;
            log.Debug("Constructing graph...");
            SequenceToPeptide = new Dictionary<String, Peptide>();
            peptides = new List<Peptide>();
			AccesstionToProtein = fastaFile.getAccessionToFullSequence();
			addPeptides(digestedFastaFile.getDigestedPeptideArray());
			
            log.Debug("Done constructing graph.");
        }

        private void addPeptides(List<DigestedPeptide> digestedPeptideArray)
        {
            log.Debug("Adding peptides...");
            int counter = 0;
            foreach (DigestedPeptide dp in digestedPeptideArray)
            {
                //if(counter % 100000 == 0)
                //{
                //    Process currentProcess = Process.GetCurrentProcess();
                //    long usedMemory = currentProcess.PrivateMemorySize64;
                //    Console.WriteLine("At peptide {0}, using {1} MB memory", counter, (double)usedMemory/1000000);
                //}
                //counter++;


                String parentProteinAccession = dp.getAccession();
                String peptideSequence = dp.getSequence();
                double peptideMass = dp.getMass();

                // Add digested peptide to the graph
                Protein parentProtein = getProtein(parentProteinAccession);
                Peptide pep;
                if (containsPeptide(peptideSequence))
                {
                    pep = getPeptide(peptideSequence);
                }
                else
                {
                    // HERE is where you make the peptide objects!!
                    if (includeCarbamidoModification)
                    {
                        peptideMass = carbamidoModificationMass(peptideSequence, peptideMass);
                    }

                    pep = new Peptide(peptideSequence, peptideMass, true);
                    SequenceToPeptide.Add(peptideSequence, pep);
                    peptides.Add(pep);
                }

                if (parentProtein != null)
                {
                    pep.addProtein(parentProtein);
                }
                else
                {
                    log.Warn("WARNINGin Parent protein foreach this peptide was not found!!");
                }
            }

            // add retention time information
            setRetentionTimes();

            peptides.Sort((Peptide x, Peptide y) => (y.getMass()).CompareTo(x.getMass()));
            log.Debug("Done adding peptides.");
        }

        // line 152 in MapDigest.c# in Nora's original code
        private double carbamidoModificationMass(String peptideSequence, double peptideMass)
        {
            // Mass of the modification on cysteine (Da)
            const double MODIFICATION_MASS = 57.021464;
            int numCystine = 0;

            // count how many cystines
            foreach (char c in peptideSequence.ToCharArray())
            {
                if (c == 'C')
                {
                    numCystine++;
                }
            }
            // add the modification based on how many cystines there were in sequence
            return peptideMass + (numCystine * MODIFICATION_MASS);
        }

        public void changeRetentionTimeWindow(double _retentionTimeWindow)
        {
            retentionTimeWindow = _retentionTimeWindow;
            setRetentionTimes();
        }

        private void setRetentionTimes()
        {
            // if retention time isn't included, do nothing
            if (includeRetentionTime)
            {
                // compute the retention times if not already computed
                if (peptideRetentionTimes == null)
                {
                    peptideRetentionTimes = RetentionTimeUtil.calculateRetentionTime(peptides);

					if (GlobalVar.useMeasuredRT)
					{
						MealTimeMS.Tester.RandomTesterFunctions.LoadAndReplaceRT(ref peptideRetentionTimes);
					}

				}

                log.Debug("Setting retention times...");
                log.Debug("Retention time window size: " + retentionTimeWindow);
                foreach (String peptideSequence in peptideRetentionTimes.Keys)
                {
                    Peptide pep = getPeptide(peptideSequence);
                    double rt = peptideRetentionTimes[peptideSequence];
                    pep.setRetentionTime(
                            RetentionTimeUtil.convertDoubleToRetentionTime(rt, retentionTimeWindow, retentionTimeWindow));
                }
                log.Debug("Done setting retention times.");
            }
        }

		
        
        /*
         * These are from the experiment and will be removed if you call reset on the
         * graph
         */
        public Peptide addPeptideFromIdentification(IDs id, double currentTime)
        {
            String peptideSequence = id.getPeptideSequence();
            double peptideMass = id.getPeptideMass();
            HashSet<String> parentProteinAccessions = id.getParentProteinAccessions();
            Peptide pep;

            if (containsPeptide(peptideSequence))
            {
                return getPeptide(peptideSequence);
            }
            else
            {
                // Adds the peptide from the identification into the database
                pep = new Peptide(peptideSequence, peptideMass, false);
                // these peptides will be removed if reset() is called
                SequenceToPeptide.Add(peptideSequence, pep);
                peptides.Add(pep);

                if (includeRetentionTime)
                {
                    // TODO right now this takes the current time as the peak retention time...
                    // should we run it through RTCalc so we can better estimate our RT alignment?
                    // 2019-04-29 No, do not. Observed times are better than predicted
                    // pep.setRetentionTime(RetentionTimeUtil.convertDoubleToRetentionTime(rt,
                    // retentionTimeWindow,
                    // retentionTimeWindow));
                    RetentionTime rt = new RetentionTime(currentTime + retentionTimeWindow, retentionTimeWindow,
                            retentionTimeWindow, false);
                    pep.setRetentionTime(rt);
                }
            }
            // update its parent proteins
            foreach (String acc in parentProteinAccessions)
            {
                if (acc.Contains(GlobalVar.DecoyPrefix))
                {
                 
                    //If the this parent protein of the peptide is a decoy, don't do anything about it
                    log.Info("Decoy parent protein for this peptide was not found!!");
                    log.Info(acc);
                    continue;
                }
                if (AccesstionToProtein.ContainsKey(acc))
                {
                    //If the parent protein exists in the internal Database, update the peptide object such that it is linked to its parent protein
                    Protein parentProtein = AccesstionToProtein[acc];
                    pep.addProtein(parentProtein);
                }
                else
                {
                    //If the parent protein is not a decoy, but somehow is not present in the Database, this would mean the comet results was 
                    //searched using a different fasta file from the one given to MTMS, a little big issue here
                    log.Warn("WARNINGin Non-decoy parent protein for this peptide was not found!!");
                    log.Warn(acc);
                }
            }

            return pep;
        }

        public Protein addProteinFromIdentification(Peptide pep, HashSet<String> parentProteinAccessions)
        {
            // TODO
            log.Error("Implementation for adding protein from identification not completed...");
            return null;
            // foreach(String accessionin parentProteinAccessions) {
            // Protein prot;
            // if(!containsProtein(accession)) {
            // prot = new Protein(accession, null);
            // }
            // }
        }
		public List<String> getExcludedProteins()
		{
			List<String> excludedProts = new List<String>();
			foreach(String accession in AccesstionToProtein.Keys)
			{
				Protein protein = AccesstionToProtein[accession];
				if (protein.IsExcluded())
				{
					excludedProts.Add(accession);
				}
			}
			return excludedProts;
		}

        public Protein getProtein(String accession)
        {
            return AccesstionToProtein[accession];
        }

        public Peptide getPeptide(String sequence)
        {
			if (SequenceToPeptide.ContainsKey(sequence))
			{
				return SequenceToPeptide[sequence];
			}
			else
			{
				return null;
			}
		}

        public double getRetentionTimeWindow()
        {
            return retentionTimeWindow;
        }

        public Boolean containsProtein(String accession)
        {
            return AccesstionToProtein.ContainsKey(accession);
        }

        public Boolean containsPeptide(String sequence)
        {
            return SequenceToPeptide.ContainsKey(sequence);
        }

        public List<Peptide> findPeptides(double queryMass, double ppmTolerance)
        {
            return BinarySearchUtil.findPeptides(peptides, queryMass, ppmTolerance);
        }

        private int numProteins()
        {
            return AccesstionToProtein.Count;
        }

        private int numPeptides()
        {
            return SequenceToPeptide.Count;
        }

        public void printGraph()
        {
            log.Debug(ToString());
            foreach (String acc in AccesstionToProtein.Keys)
            {
                Protein p = getProtein(acc);
                log.Debug(p);
            }
            foreach (String seq in SequenceToPeptide.Keys)
            {
                Peptide pep = getPeptide(seq);
                log.Debug(pep);
            }
        }

        //public void scorePeptide(String sequence, double score)
        //{
        //    Peptide p = getPeptide(sequence);
        //    p.addScore(score);
        //}

        public void reset()
        {
            // reset scores on all peptides
            // remove all added peptides, from their parent protein too
            IEnumerable<String> itr = SequenceToPeptide.Keys.AsEnumerable();
            foreach(String pep in itr)
            {
                Peptide p = getPeptide(pep);
                bool isFromFasta = p.isFromFastaFile();
                if (!isFromFasta)
                {
                    foreach (Protein parentProtein in p.getProteins())
                    {
                        parentProtein.removePeptide(p);
                    }
                }
                else
                {
                    p.clearScores();
                }
            }
            // reset scores on proteins
            foreach (String acc in AccesstionToProtein.Keys)
            {
                Protein p = getProtein(acc);
                p.resetScores();
            }
            // reset the retention time on peptides
            setRetentionTimes();
        }

        public List<IdentificationFeatures> extractFeatures()
        {
            List<IdentificationFeatures> features = new List<IdentificationFeatures>();

            foreach (String acc in AccesstionToProtein.Keys)
            {
                Protein p = getProtein(acc);
                IdentificationFeatures f = p.extractFeatures();
                features.Add(f);
            }

            return features;
        }

		

        public List<String> getProteinSet()
        {
            return new List<String>(AccesstionToProtein.Keys);
        }

        override
        public String ToString()
        {
            return "Database{NumProteins:" + numProteins() + ";NumPeptides:" + numPeptides() + "}";
        }

    }


}
