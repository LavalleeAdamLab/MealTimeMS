using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Data
{

	public class ProteinProphetResult
    {
        private double fdr_filter_threshold;
        private double probability_filter_threshold;
        private int num_proteins_identified;
        private List<String> proteinsIdentified;


        private readonly double DEFAULT_fdr_filter_threshold = 0.01;
        private readonly double DEFAULT_probability_filter_threshold = 0;


        public ProteinProphetResult(double fdr_filter_threshold, double probability_filter_threshold,
                                    List<String> _proteinsIdentified)
        {
            this.fdr_filter_threshold = fdr_filter_threshold;
            this.probability_filter_threshold = probability_filter_threshold;
            this.proteinsIdentified = _proteinsIdentified;
            this.num_proteins_identified = proteinsIdentified.Count;
        }

        public ProteinProphetResult(List<String> _proteinsIdentified)
        {

            this.fdr_filter_threshold = DEFAULT_fdr_filter_threshold;
            this.probability_filter_threshold = DEFAULT_probability_filter_threshold;
            this.proteinsIdentified = _proteinsIdentified;
            this.num_proteins_identified = proteinsIdentified.Count;
        }
        public ProteinProphetResult(List<String> _proteinsIdentified, double _fdrFilterThreshold)
        {
            
            this.fdr_filter_threshold = _fdrFilterThreshold;
            this.probability_filter_threshold = DEFAULT_probability_filter_threshold;
            this.proteinsIdentified = _proteinsIdentified;
            this.num_proteins_identified = proteinsIdentified.Count;
        }


        public double getFdr_filter_threshold()
        {
            return fdr_filter_threshold;
        }
        public double getProbability_filter_threshold()
        {
            return probability_filter_threshold;
        }
        public int getNum_proteins_identified()
        {
            return num_proteins_identified;
        }

        public List<String> getProteinsIdentified()
        {
            return proteinsIdentified;
        }

        override
        public String ToString()
        {
            return "ProteinProphetResult [fdr_filter_threshold=" + fdr_filter_threshold + ", probability_filter_threshold="
                    + probability_filter_threshold + ", num_proteins_identified=" + num_proteins_identified + "]";
        }

    }

}
