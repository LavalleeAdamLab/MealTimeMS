using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Data;
namespace MealTimeMS.Data
{
	//Container object for an simulated experiment
	public class Experiment
	{
		public ExclusionProfile exclusionProfile;
		public ExclusionProfileEnum exclusionType;
		public String experimentName;
		public int experimentNumber;
		public double experimentStartTime;
		public double experimentTotalDuration;
		public ProteinProphetResult ppr;

		public Experiment(String _experimentName, int _experimentNum, ExclusionProfileEnum _exType)
		{
			experimentName = _experimentName;
			experimentNumber = _experimentNum;
			exclusionType = _exType;
		}

		public Experiment(ExclusionProfile _exclusionProfile, String _experimentName, int _experimentNum, ExclusionProfileEnum _exType):this(_experimentName,_experimentNum,_exType)
		{
			exclusionProfile = _exclusionProfile;
		}

	}
}
