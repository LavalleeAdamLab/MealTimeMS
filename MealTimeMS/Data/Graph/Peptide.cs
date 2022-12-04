using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Data.Graph
{

    public class Peptide
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private String sequence;
        private double mass;
        private RetentionTime retentionTime;
        private List<Double> scores; // stores the the cross correlational score each time this peptide is identified
		private List<Double> dCNs;  // same thing as the score list, but stores the dCNs
        private bool fromFasta;
        private bool isUniquelyIdentifyingPeptide;
        // make this accept multiple... and PTMs and theoretical masses
        // also make it accept retention time pairs (ExclusionPair.java)

        private Dictionary<String, Protein> parentProteins;
        private Dictionary<int, double> z_ook0;

        public Peptide(String _sequence, double _mass, bool _fromFasta)
        {
            // peptideEvidence = _peptideEvidence;
            fromFasta = _fromFasta;
            sequence = _sequence;
            mass = _mass;
            retentionTime = new RetentionTime();
            parentProteins = new Dictionary<String, Protein>();
            scores = new List<Double>();
			dCNs = new List<Double>();
            z_ook0 = new Dictionary<int, double>();
        }
        
        public void addScore(double score, double dCN)
        {
            scores.Add(score);
			dCNs.Add(dCN);
            foreach (String acc in parentProteins.Keys)
            {
                Protein parentProtein = parentProteins[acc];
                parentProtein.addScore(getSequence(), score, dCN);
            }
        }

        public void addScore(double score, double xCorrThreshold , double dCN)
        {
            scores.Add(score);
			dCNs.Add(dCN);
            foreach (String acc in parentProteins.Keys)
            {
                Protein parentProtein = parentProteins[acc];
                parentProtein.addScore(getSequence(), score, xCorrThreshold, dCN);
            }
        }

        public void addProtein(Protein p)
        {
            addProtein(p.getAccession(), p);
            p.addPeptide(sequence, this);
        }

        private void addProtein(String acc, Protein p)
        {
            if (parentProteins.Keys.Contains(acc))
            {
                //logger.Debug("Protein already exists, protein skipped");
            }
            else
            {
                parentProteins.Add(acc, p);
            }
            
        }

        public String getSequence()
        {
            return sequence;
        }

        public double getMass()
        {
            return mass;
        }

        public Protein getProtein(String acc)
        {
            return parentProteins[acc];
        }

        public List<Protein> getProteins()
        {
            List<Protein> ls = new List<Protein>();
            foreach(Protein pr in parentProteins.Values)
            {
                ls.Add(pr);
            }
            return ls;
        }
        public void setIonMobility (Dictionary<int, double> _z_ook0)
        {
            z_ook0 = _z_ook0;
        }
        public Dictionary<int, double> getIonMobility()
        {
            return z_ook0;
        }
        public void setRetentionTime(RetentionTime _retentionTime)
        {
            retentionTime = _retentionTime;
        }

        public RetentionTime getRetentionTime()
        {
            return retentionTime;
        }

        private int numGetParentProteins()
        {
            return parentProteins.Count;
        }

        public bool isFromFastaFile()
        {
            return fromFasta;
        }

        public void clearScores()
        {
            scores.Clear();
        }

        public bool isExcluded(double time)
        {
            double rtStart = retentionTime.getRetentionTimeStart();
            double rtEnd = retentionTime.getRetentionTimeEnd();
            if (rtStart <= time && rtEnd >= time)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private String GetParentProteins()
        {
            String returnValue = "[";
            foreach (String acc in parentProteins.Keys)
            {
                returnValue += acc + ",";
            }
            returnValue = returnValue.Length > 1 ? returnValue.Substring(0, returnValue.LastIndexOf(",")) + "]" : "[]";
            return returnValue;
        }

        override
        public bool Equals(Object o)
        {

            // If the object is compared with itself then return true
            if (o == this)
            {
                return true;
            }

            /*
             * Check if o is an instance of Peptide or not "null instanceof [type]" also
             * returns false
             */
            if (!(o is Peptide)) {
                return false;
            }

            // typecast o to Complex so that we can compare data members
            Peptide pep = (Peptide)o;

            // Compare the data members and return accordingly
            
            return pep.getMass().CompareTo( this.getMass()) == 0 && this.getSequence().Equals(pep.getSequence());
        }

        override
        public String ToString()
        {
            return "Peptide{sequence:" + sequence + ";mass:" + mass + ";" + retentionTime
                    + ";NumParentProteins:" + numGetParentProteins() + ";ParentProteins:" + GetParentProteins() + ";scores:"
                    + scores + ";fromFasta:" + isFromFastaFile() + "}";
        }
    }

}
