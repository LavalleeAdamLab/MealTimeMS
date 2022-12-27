using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MealTimeMS.ExclusionProfiles
{


    public static class ExclusionProfileEnumExtension
    {
        public static String getDescription(this ExclusionProfileEnum e)
        {
            return Enum.GetName(typeof(ExclusionProfileEnum),e);
        }

		

        public static String getShortDescription(this ExclusionProfileEnum e)
        {
            
            switch (e)
            {
                case ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE:
                    return "MLGE";
                case ExclusionProfileEnum.HEURISTIC_EXCLUSION_PROFILE:
                    return "Heuristic";
                case ExclusionProfileEnum.NO_EXCLUSION_PROFILE:
                    return "NoEx";
            }
            return e.getDescription();
        }
        
        public static String ToString(this ExclusionProfileEnum e)
        {
            return e.getDescription();
        }
		public static List<ExclusionTypeParamEnum> getParamsRequired(ExclusionProfileEnum e)
		{
			List<ExclusionTypeParamEnum> paramsRequired = new List<ExclusionTypeParamEnum>();
			if (e.Equals(ExclusionProfileEnum.NO_EXCLUSION_PROFILE))
			{
				return paramsRequired;
			}

			paramsRequired.Add(ExclusionTypeParamEnum.ppmTol);
			paramsRequired.Add(ExclusionTypeParamEnum.rtWin);
			paramsRequired.Add(ExclusionTypeParamEnum.imWin);
			switch (e)
			{
				case ExclusionProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE:
					paramsRequired.Add(ExclusionTypeParamEnum.prThr);
					break;
				case ExclusionProfileEnum.HEURISTIC_EXCLUSION_PROFILE:
					paramsRequired.Add(ExclusionTypeParamEnum.xCorr);
					paramsRequired.Add(ExclusionTypeParamEnum.numDB);
					break;
				case ExclusionProfileEnum.COMBINED_EXCLUSION:
					paramsRequired.Add(ExclusionTypeParamEnum.xCorr);
					paramsRequired.Add(ExclusionTypeParamEnum.numDB);
					paramsRequired.Add(ExclusionTypeParamEnum.prThr);
					break;
			}
			return paramsRequired;
		}
		public static String getShortDescription(this ExclusionTypeParamEnum e)
		{

			switch (e)
			{
				case ExclusionTypeParamEnum.ppmTol:
					return "ppmTol";
				case ExclusionTypeParamEnum.rtWin:
					return "rtWin";
                case ExclusionTypeParamEnum.imWin:
					return "imWin";
				case ExclusionTypeParamEnum.xCorr:
					return "xCorr";
				case ExclusionTypeParamEnum.numDB:
					return "numDB";
				case ExclusionTypeParamEnum.prThr:
					return "prThr";
			}
			return "";
		}
	}
    public enum ExclusionProfileEnum
    {
        [Description("MachineLearningGuidedExclusionProfile")]
        MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE,
        [Description("HeuristicExclusionProfile")]
        HEURISTIC_EXCLUSION_PROFILE,
        [Description("NoExclusionProfile")]
        NO_EXCLUSION_PROFILE,
        [Description("RandomExclusionProfile")]
        RANDOM_EXCLUSION_PROFILE,
		[Description("MLGEPepSequenceExclusionProfile")]
		MLGE_SEQUENCE_EXCLUSION_PROFILE,
		[Description("HeuristicSequenceExclusionProfile")]
		HEURISTIC_SEQUENCE_EXCLUSION_PROFILE,
		[Description("CombinedExclusionProfile")]
		COMBINED_EXCLUSION,
		[Description("SVMExclusionProfile")]
		SVMEXCLUSION

	};
	public enum ExclusionTypeParamEnum
	{
		[Description("PPM tolerance")]
		ppmTol,
		[Description("Retention time window")]
		rtWin,
        [Description("Ion mobility window")]
        imWin,
        [Description("XCorr Threshold")]
		xCorr,
		[Description("Number of ms2 above xCorr threshold")]
		numDB,
		[Description("Classifier probability threshold")]
		prThr
	};

	//public class ExclusionProfileEnum
	//{


	//    public ProfileEnum profileEnum;
	//    private readonly String description;

	//    private ExclusionProfileEnum(String description, ProfileEnum _profileEnum)
	//    {
	//        this.description = description;
	//        this.profileEnum = _profileEnum;

	//    }

	//    public String getDescription()
	//    {
	//        return description;
	//    }

	//    public String getShortDescription()
	//    {
	//        switch (this.profileEnum)
	//        {
	//            case ProfileEnum.MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE:
	//                return "MLGE";
	//            case ProfileEnum.HEURISTIC_EXCLUSION_PROFILE:
	//                return "Heuristic";
	//            case ProfileEnum.NO_EXCLUSION_PROFILE:
	//                return "NoEx";
	//        }
	//        return description;
	//    }

	//    override
	//    public String ToString()
	//    {
	//        return getDescription();
	//    }
	//}


}


