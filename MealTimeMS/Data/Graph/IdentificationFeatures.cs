using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace MealTimeMS.Data.Graph
{
  

public class IdentificationFeatures
    {
        private String accession;
        private int cardinality;
        private double highestConfidenceScore;
        private double meanConfidenceScore;
        private double medianConfidenceScore;
        private double stdevConfidenceScore;
		private double highestDCN;
		private double meanDCN;
		private double medianDCN;

		public IdentificationFeatures(String accession, int cardinality, double highestConfidenceScore,
                double meanConfidenceScore, double medianConfidenceScore, double _highestDCN, double _meanDCN, double _medianDCN, double stdevConfidenceScore)
        {
            
            this.accession = accession;
            this.cardinality = cardinality;
            this.highestConfidenceScore = highestConfidenceScore;
            this.meanConfidenceScore = meanConfidenceScore;
            this.medianConfidenceScore = medianConfidenceScore;
			this.highestDCN = _highestDCN;
			this.meanDCN = _meanDCN;
			this.medianDCN = _medianDCN;
            this.stdevConfidenceScore = stdevConfidenceScore;
        }

        public IdentificationFeatures(DataRow row)
        {

        }

        public String getAccession()
        {
            return accession;
        }

        public int getCardinality()
        {
            return cardinality;
        }

        public double getHighestConfidenceScore()
        {
            return highestConfidenceScore;
        }

        public double getMeanConfidenceScore()
        {
            return meanConfidenceScore;
        }

        public double getMedianConfidenceScore()
        {
            return medianConfidenceScore;
        }

        public double getStdevConfidenceScore()
        {
            return stdevConfidenceScore;
        }
		public double getHighestDCN()
		{
			return highestDCN;
		}

		public double getMeanDCN()
		{
			return meanDCN;
		}

		public double getMedianDCN()
		{
			return medianDCN;
		}

		public static String getHeader()
        {
#if STDEVINCLUDED
            return "Accession,Cardinality,HighestConfidenceScore,MeanConfidenceScore,MedianConfidenceScore,HighestDCN,MeanDCN,MedianDCN,StDevConfidenceScore";  
#else
			return "Accession,Cardinality,HighestConfidenceScore,MeanConfidenceScore,MedianConfidenceScore,HighestDCN,MeanDCN,MedianDCN";
#endif
		}

		public String writeToFile()
		{
#if STDEVINCLUDED
            return accession + "," + cardinality + "," + highestConfidenceScore + "," + meanConfidenceScore + ","
				   + medianConfidenceScore + "," + highestDCN + "," +meanDCN + "," +medianDCN + "," + stdevConfidenceScore;
#else
			return accession + "," + cardinality + "," + highestConfidenceScore + "," + meanConfidenceScore + ","
				   + medianConfidenceScore + "," + highestDCN + "," +meanDCN + "," +medianDCN;
#endif
		}


        public void setStdevConfidenceScore(double d)
        {
            stdevConfidenceScore = d;
        }

        override
        public String ToString()
        {
            return "IdentificationFeatures [accession=" + accession + ", cardinality=" + cardinality
                    + ", highestConfidenceScore=" + highestConfidenceScore + ", meanConfidenceScore=" + meanConfidenceScore
                    + ", medianConfidenceScore=" + medianConfidenceScore + ", stdevConfidenceScore=" + stdevConfidenceScore
                    + "]";
        }

    }

}
