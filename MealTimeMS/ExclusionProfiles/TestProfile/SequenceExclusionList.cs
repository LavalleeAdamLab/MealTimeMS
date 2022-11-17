using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Util;

namespace MealTimeMS.ExclusionProfiles.TestProfile
{
    public class SequenceExclusionList:ExclusionList
    {
        private HashSet<string> ExcludedPeptides;

        public SequenceExclusionList(double _ppmTolerance) : base(_ppmTolerance)
        {
            ExcludedPeptides = new HashSet<string>();
        }

        override
        public void addPeptide(Peptide pep)
        {
            ExcludedPeptides.Add(pep.getSequence());
        }
        public override bool containsPeptide(Peptide pep)
        {
            return ExcludedPeptides.Contains(pep.getSequence());
        }




        
    }
}
