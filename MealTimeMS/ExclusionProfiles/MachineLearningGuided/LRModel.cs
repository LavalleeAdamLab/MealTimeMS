using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.ExclusionProfiles.MachineLearningGuided
{
	abstract class LRModel
	{
		internal double decisionThreshold;

		public double GetThreshold()
		{
			return decisionThreshold;
		}
		public void SetThreshold(double _threshold)
		{
			decisionThreshold = _threshold;
		}

	}
}
