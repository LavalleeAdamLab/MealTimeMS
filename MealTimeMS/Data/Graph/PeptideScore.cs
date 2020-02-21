using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Data.Graph
{
    

    public class PeptideScore
    {
        private String peptideSequence;
        private double xCorr;
		private double dCN = 0;

        public PeptideScore(String _peptide, double _xCorr)
        {
            peptideSequence = _peptide;
            xCorr = _xCorr;
        }

		public PeptideScore(String _peptide, double _xCorr, double _dCN )
		{
			peptideSequence = _peptide;
			xCorr = _xCorr;
			dCN = _dCN;
		}

		public String getPeptideSequence()
        {
            return peptideSequence;
        }

        public double getXCorr()
        {
            return xCorr;
        }
		public double getdCN()
		{
			return dCN;
		}

        override
        public String ToString()
        {
		return "PeptideScore{peptide=" + peptideSequence + ";xCorr=" + xCorr + "}";
        }
    }

}
