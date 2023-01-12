using System;
using System.Collections.Generic;
using System.Linq;
using MealTimeMS.Util;
namespace MealTimeMS.Data
{

    //This class stores the peptide-spectrum match information from comet database searches.
	public class IDs
	{
		private double scan_t; // stores the scan time from the mzML file to help find the scan number
		private int scan; // associates the mzML file to the mzID file and map to the peptideEvidence_ref
		private String peptide_evidence; // map to the PeptideEvidence_id
		private String peptide_reference; // map to the peptide_ref and dBsequence_ref
		private HashSet<String> database_sequence_id; // map to the accession
		private String peptide_sequence; // found through peptide evidence
		private String peptide_sequence_withModification;
		private HashSet<String> parent_proteins; // found through dBSequence
		private double peptide_mass; // found through the peptide reference
		private double xCorr;
		private double deltaCN;
		private double deltaCNStar;
		private double spScore;
		private double spRank;
		private double evalue;

		public IDs(double startTime, int scanNum, String pepEv, String pepRef, HashSet<String> dBSeq, String pepSeq,
				HashSet<String> accs, double pep_mass, double x_Corr, double _deltaCN, double _deltaCNStar, double _spScore,
				double _spRank, double _evalue)
		{
			// Initializes the variables
			scan_t = startTime;
			scan = scanNum;
			peptide_evidence = pepEv;
			peptide_reference = pepRef;
			database_sequence_id = dBSeq;
			peptide_sequence = pepSeq;
			parent_proteins = accs;
			peptide_mass = pep_mass;
			xCorr = x_Corr;
			deltaCN = _deltaCN;
			deltaCNStar = _deltaCNStar;
			spScore = _spScore;
			spRank = _spRank;
			evalue = _evalue;
		}

		public IDs(double startTime, int scanNum, String pepSeq, double pep_mass, double x_Corr,
			double _deltaCN)
		{
			// Initializes the variables
			scan_t = startTime;
			scan = scanNum;
			peptide_sequence = pepSeq;
			//parent_proteins = accs;
			peptide_mass = pep_mass;
			xCorr = x_Corr;
			deltaCN = _deltaCN;


		}
		public IDs(double startTime, int scanNum, String pepSeq, double pep_mass, double x_Corr,
			double _deltaCN, HashSet<String> accessions) : this(startTime, scanNum, pepSeq, pep_mass, x_Corr, _deltaCN)
		{
			parent_proteins = accessions;


		}
        
        public static IDs getIDsFromPSMProlucid(com.bruker.paser.avro.PsmProlucid psm)
        {
            double dCN = 1;
            switch (psm.candidates.Count)
            {
                case 0:
                    return null;
                case 1:
                    dCN = 1;
                    break;
                default:
                    //note that prolucid stores the dCN of the top hit at the second candidate's dCN field
                    dCN = psm.candidates[1].deltaCN;
                    break;
            }
            var candidate = psm.candidates[0];
            var accessions = psm.candidates[0].locus.Select(c => c.locus_name);
            HashSet<String> parentProteins = new HashSet<String>(accessions);
           
            IDs id = new IDs(psm.rt/60.0, psm.ms2_id, candidate.stripped_peptide, 
                candidate.calc_mass,candidate.Xcorr, dCN, parentProteins);
            List<String> peptideSeq = candidate.stripped_peptide.ToCharArray().Select(x=>x.ToString()).ToList(); 
            List<String> ptms = candidate.ptms.ToList();
            List<int> ptmLocations = candidate.ptm_locations.ToList();
            for(int i = ptms.Count-1; i >=0; i--)
            {
                peptideSeq.Insert(ptmLocations[i]+1, ptms[i]);
            }
            String modifiedPepSeq =  String.Join(separator: "", peptideSeq);
            id.setPeptideSequence_withModification(modifiedPepSeq);
                    //(double startTime, int scanNum, String pepSeq, double pep_mass, double x_Corr, double _deltaCN)
            return id;
        }


		// Functions to access the private values from class 
		public double getScanTime()
		{
			return scan_t;
		}
        public void setScanTime(double _scanTime)
        {
            scan_t= _scanTime;
        }
        public int getScanNum()
		{
			return scan;
		}

		public String getPepEvid()
		{
			return peptide_evidence;
		}

		public String getPepRef()
		{
			return peptide_reference;
		}

		public HashSet<String> getDBSeqID()
		{
			return database_sequence_id;
		}

		public String getPeptideSequence()
		{
			return peptide_sequence;
		}
		public String getPeptideSequence_withModification()
		{
			return peptide_sequence_withModification;
		}
		public void setPeptideSequence_withModification(String _seq)
		{
			peptide_sequence_withModification = _seq;
		}

		public HashSet<String> getParentProteinAccessions()
		{
			return parent_proteins;
		}
		public String getParentProteinAccessionsAsString()
		{
			return String.Join(separator: ",", parent_proteins);
		}

		public double getPeptideMass()
		{
			return peptide_mass;
		}

		public double getXCorr()
		{
			return xCorr;
		}

		public double getDeltaCN()
		{
			return deltaCN;
		}

		public double getDeltaCNStar()
		{
			return deltaCNStar;
		}

		public double getSPScore()
		{
			return spScore;
		}

		public double getSPRank()
		{
			return spRank;
		}

		public double getEValue()
		{
			return evalue;
		}

		public Boolean isDecoy()
		{
			foreach(String access in parent_proteins)
			{
				if (!access.Contains(GlobalVar.DecoyPrefix))
				{
					//if there is a single parent protein that's not a decoy, it is not a decoy peptide
					return false;
				}
			}
			return true;
		}

		override
		public String ToString()
		{
			return "IDs{scan=" + scan + "; scan_t=" + scan_t + "; peptide_mass=" + peptide_mass
					+ "; peptide_sequence=" + peptide_sequence
					+ "; xCorr=" + xCorr + "; deltaCN=" + deltaCN
					+ "}";
		}

		public String ToDetailedString()
		{
			return "IDs{scan=" + scan + "; scan_t=" + scan_t + "; peptide_mass=" + peptide_mass
				   + "; peptide_sequence=" + peptide_sequence + "; parent_proteins=" + parent_proteins
				   + "; peptide_evidence=" + peptide_evidence + "; peptide_reference=" + peptide_reference
				   + "; database_sequence_id=" + database_sequence_id + "; xCorr=" + xCorr + "; deltaCN=" + deltaCN
				   + "; deltaCNStar=" + deltaCNStar + "; spScore=" + spScore + "; spRank=" + spRank + "; evalue=" + evalue
				   + "}";
		}

	}

}
