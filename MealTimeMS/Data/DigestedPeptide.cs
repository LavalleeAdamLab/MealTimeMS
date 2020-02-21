using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Data
{

    /*
    - Function stores the attributes of the digested FASTA file
    - Proteins digested in-silico with Trypsin & a certain amount of missed cleavages and minimum peptide length
    */

    public class DigestedPeptide
    {
        //Creates the variables
        private String sequence;
        private String accession;
        private double mass;

        //Public constructor for creating objects with these attributes
        public DigestedPeptide(String _sequence, String _accession, Double _mass)
        {
            sequence = _sequence;
            accession = _accession;
            mass = _mass;
        }

        public String getSequence()
        {
            return sequence;
        }

        public String getAccession()
        {
            return accession;
        }

        public double getMass()
        {
            return mass;
        }

    }

}
