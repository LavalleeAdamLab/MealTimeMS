using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Util;
using System.Data;


namespace MealTimeMS.Data.Graph
{


public class Protein
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        // A name/descriptor for the protein
        private String accession;
        // The amino acid sequence of the protein
        private String sequence;
        // The mass of the protein
        private double mass; // TODO we don't use the mass of the protein, huh.
                             // TODO this should be in peptide, no?
        private String dbseqid;
        // Points to peptides that can result by the digestion of this protein
        private Dictionary<String, Peptide> peptides;
        // TODO how to identify the peptides?
        private bool isExcluded; // prevents redundant adding of peptides to the exclusion list

        private int numDB = 0;

        private List<PeptideScore> peptideScores;

        private double coverage = 0;
        public Protein(String _accession, String _sequence)
        {
            accession = _accession;
            sequence = _sequence;
            // mass = _mass;
            peptides = new Dictionary<String, Peptide>();
            peptideScores = new List<PeptideScore>();
            isExcluded = false;
        }

        public void addPeptide(String reference, Peptide pep)
        {
         
            
            if (peptides.Keys.Contains(reference))
            {
                log.Debug("Peptide already exists");
            }
            else
            {
                peptides.Add(reference, pep);

            }
            
        }

        public void setDBSeqID(String _dbseqid)
        {
            dbseqid = _dbseqid;
        }

        public String getAccession()
        {
            return accession;
        }

        public String getSequence()
        {
            return sequence;
        }

        public double getMass()
        {
            return mass;
        }

        public String getDBSeqID()
        {
            return dbseqid;
        }

        public Peptide getPeptide(String reference)
        {
            return peptides[reference];
        }

        public List<Peptide> getPeptides()
        {
            return new List<Peptide>(peptides.Values);
        }

        public Boolean containsPeptide(String reference)
        {
            return peptides.ContainsKey(reference);
        }

        private int numPeptides()
        {
            return peptides.Count;
        }

        public void removePeptide(Peptide pep)
        {
            if (peptides.Values.Contains(pep))
            {
                peptides.Remove(pep.getSequence());
            }
        }

        public void resetScores()
        {
            peptideScores.Clear();
            numDB = 0;
            setExcluded(false);
        }


        public void setExcluded(bool e)
        {
            isExcluded = e;
        }

        public bool IsExcluded()
        {
            return isExcluded;
        }

        public void addScore(String peptide, double xCorr, double dCN)
        {
            peptideScores.Add(new PeptideScore(peptide, xCorr, dCN));
        }

        public void addScore(String peptide, double xCorr, double xCorrThreshold, double dCN)
        {
            peptideScores.Add(new PeptideScore(peptide, xCorr, dCN));
            if (xCorr > xCorrThreshold)
            {
                numDB++;
            }
        }

        public List<PeptideScore> getPeptideScore() { return peptideScores; }

        public int getNumDB()
        {
            return numDB;
        }

        public double getCoverage()
        {
            computeCoverage();
            return coverage;
        }

        public void computeCoverage()
        {
            //TODO
            //		Dictionary<String, Peptide> peptides

            String protSq = getSequence();
            int[] covered = new int[protSq.Length];
            List<String> pepList = new List<String>();
            foreach (PeptideScore ps in peptideScores)
            {
                String ppSq = ps.getPeptideSequence();
                int startIndex = 0;
                while (true)
                {
                    int ind = protSq.IndexOf(ppSq, startIndex);
                    if (ind < 0)
                    {
                        break;
                    }
                    else
                    {
                        //if(protSq.charAt(ind-1)=='R'||protSq.charAt(ind-1)=='K') {
                        for (int i = ind; i < ind + ppSq.Length; i++)
                        {
                            covered[i] = 1;
                        }
                        //}
                        startIndex = ind + 1;
                    }
                }
            }
            int totalCovered = 0;
            foreach (int i in covered)
            {
                totalCovered += i;
            }
            coverage = (double)totalCovered / (double)sequence.Length;
        }


        public IdentificationFeatures extractFeatures()
        {
            return IdentificationFeatureExtractionUtil.extractFeatures(accession, peptideScores);
        }

        public DataRow vectorize()
        {
            return IdentificationFeatureExtractionUtil.extractFeatureVector(accession, peptideScores);
        }

        override
        public String ToString()
        {
		    return "Protein{accession:" + accession + ";NumPeptides:" + numPeptides() + ";Coverage:" + coverage + ";scores:" + peptideScores
				    + ";sequence:" + sequence + "}";
	    }

}


}
