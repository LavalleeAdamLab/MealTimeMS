using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;

namespace MealTimeMS.ExclusionProfiles
{
    class BrukerNoExclusion : ExclusionProfile
    {
        private int analyzedSpectraCount = 0;
        public BrukerNoExclusion():base(null, 0)
        {
            analyzedSpectraCount = 0;
        }

        public override void reset()
        {
            analyzedSpectraCount = 0;
            return;
        }
        public override ExclusionProfileEnum getAnalysisType()
        {
            return ExclusionProfileEnum.NO_EXCLUSION_PROFILE;
        }
        public int getAnalyzedSpectraCount()
        {
            return analyzedSpectraCount;
        }
        public override bool evaluate(Spectra spec)
        {
            if (spec.getMSLevel() != 2)
            {
                return false;
            }
            includedSpectra.Add(spec.getScanNum());
            analyzedSpectraCount++;

            return true;
        }

        protected override void evaluateIdentification(IDs id)
        {
            return;
        }
    }
}
