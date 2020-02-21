using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Data.InputFiles
{
	public class ProteinProphetFile
	{
		private String protXMLFileName;
		private Dictionary<String, List<String>> proteinsToPeptides;
		private ProteinProphetResult proteinProphetResult;

		public ProteinProphetFile(String protXMLFileName, Dictionary<String, List<String>> proteinsToPeptides,
				double fdr_threshold, double protein_probablity_threshold)
		{
			this.protXMLFileName = protXMLFileName;
			this.proteinsToPeptides = proteinsToPeptides;
			int numProteinsIdentified = proteinsToPeptides.Keys.Count;
			proteinProphetResult = new ProteinProphetResult(fdr_threshold, protein_probablity_threshold,
					getProteinNames());
		}

		public ProteinProphetResult getProteinProphetResult()
		{
			return proteinProphetResult;
		}

		public bool containsProtein(String accession)
		{
			return proteinsToPeptides.ContainsKey(accession);
		}

		public bool containsPeptide(String proteinAccession, String peptideSequence)
		{
			List<String> peptides = proteinsToPeptides[proteinAccession];
			return peptides.Contains(peptideSequence);
		}

		public List<String> getProteinNames()
		{
			List<String> proteins = new List<String>();
			foreach (String s in proteinsToPeptides.Keys)
			{
				proteins.Add(s);
			}
			return proteins;
		}

		/*
		 * Returns the protein accessions which are uniquely found in this class (and
		 * not found in the passed in protein prophet file).
		 */
		public List<String> compareProteins(ProteinProphetFile ppf)
		{
			List<String> uniqueProteins = new List<String>();
			foreach (String proteinAccession in proteinsToPeptides.Keys)
			{
				bool notFoundInOtherPPF = ppf.containsProtein(proteinAccession);
				if (notFoundInOtherPPF)
				{
					uniqueProteins.Add(proteinAccession);
				}
			}
			return uniqueProteins;
		}

		/*
		 * Returns the set of peptides (mapping to a protein accession) found in this
		 * protein prophet file but not in the other.
		 */
		public Dictionary<String, List<String>> comparePeptides(ProteinProphetFile ppf)
		{
			Dictionary<String, List<String>> uniqueProteinToPeptides = new Dictionary<String, List<String>>();
			foreach (String proteinAccession in proteinsToPeptides.Keys)
			{
				List<String> peptides = proteinsToPeptides[proteinAccession];
				List<String> uniquePeptides = new List<String>();
				foreach (String peptideSequence in peptides)
				{
					bool notFoundInOtherPPF = ppf.containsPeptide(proteinAccession, peptideSequence);
					if (notFoundInOtherPPF)
					{
						uniquePeptides.Add(peptideSequence);
					}
				}
				if (uniquePeptides.Count!=0)
				{
					uniqueProteinToPeptides.Add(proteinAccession, uniquePeptides);
				}
			}
			return uniqueProteinToPeptides;
		}

		override
		public String ToString()
		{
			return "ProteinProphetFile [protXMLFileName=" + protXMLFileName + ", proteinsToPeptides=" + proteinsToPeptides
					+ ", proteinProphetResult=" + proteinProphetResult + "]";
		}

	}

}
