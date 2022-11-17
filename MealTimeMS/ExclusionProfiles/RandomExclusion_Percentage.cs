using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;
using MealTimeMS.Data.Graph;
using MealTimeMS.Simulation;

namespace MealTimeMS.ExclusionProfiles
{
    class RandomExclusion_Percentage: ExclusionProfile
    {
        Random r = new Random();
        double chanceToExclude;
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public RandomExclusion_Percentage(Database _database, List<Spectra> spectraArray,
            int numExcluded, int numAnalyzed) : base(_database)
        {
            chanceToExclude = (double)numExcluded / ((double)(numExcluded + numAnalyzed));
        }
        override
        protected void evaluateIdentification(IDs id)
        {

        }

        override
        protected void evaluateExclusion(IDs id)
        {

        }

        override
        protected bool processMS2(Spectra spec)
        {
            log.Debug(spec);
            performanceEvaluator.countMS2();
            //IDs id = performDatabaseSearch(spec);
            bool isExcluded = r.NextDouble()<chanceToExclude;
            if (isExcluded)
            {
                log.Debug("MS2 spectra was randomly excluded");
                performanceEvaluator.countExcludedSpectra();
                //evaluateExclusion(id);
                excludedSpectra.Add(spec.getScanNum());
                return false;
            }
            else
            {
                log.Debug("MS2 spectra was analyzed");
                performanceEvaluator.countAnalyzedSpectra();
                //evaluateIdentification(id);
                includedSpectra.Add(spec.getScanNum());
                return true;
            }
        }

        override
        public String ToString()
        {
            double retentionTimeWindow = database.getRetentionTimeWindow();
            double ppmTolerance = exclusionList.getPPMTolerance();
            return "RandomExclusion[" + "RT_window: " + retentionTimeWindow + ";ppmTolerance: " + ppmTolerance + "]";
        }

        override
        public ExclusionProfileEnum getAnalysisType()
        {
            return ExclusionProfileEnum.RANDOM_EXCLUSION_PROFILE;
        }

    }
}
