using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.ExclusionProfiles.TestProfile
{
	//just an object used to store parsed experiment performance evaluation
	class ExperimentResult
	{
		
		const String experimentNameHeaderName = "ExperimentName";
		const String analyzedHeaderName = "NumMS2Analyzed";
		const String excludedHeaderName = "NumMS2Excluded";

		public String experimentName;
		public int numSpectraAnalyzed;
		public int numSpectraExcluded;
		public ExperimentResult(String resultStr, String headerStr)
		{
			List<String> header = new List<String>(headerStr.Split("\t".ToCharArray()));
			String[] result = resultStr.Split("\t".ToCharArray());

			experimentName = result[header.IndexOf(experimentNameHeaderName)];
			numSpectraAnalyzed = int.Parse(result[header.IndexOf(analyzedHeaderName)]);
			numSpectraExcluded = int.Parse(result[header.IndexOf(excludedHeaderName)]);

		}

		

	}
}
