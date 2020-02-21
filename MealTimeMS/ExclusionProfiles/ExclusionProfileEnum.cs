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
                case ExclusionProfileEnum.NORA_EXCLUSION_PROFILE:
                    return "Nora";
                case ExclusionProfileEnum.NO_EXCLUSION_PROFILE:
                    return "NoEx";
            }
            return e.getDescription();
        }

        
        public static String ToString(this ExclusionProfileEnum e)
        {
            return e.getDescription();
        }

    }
    public enum ExclusionProfileEnum
    {
        [Description("MachineLearningGuidedExclusionProfile")]
        MACHINE_LEARNING_GUIDED_EXCLUSION_PROFILE,
        [Description("NoraExclusionProfile")]
        NORA_EXCLUSION_PROFILE,
        [Description("NoExclusionProfile")]
        NO_EXCLUSION_PROFILE,
        [Description("RandomExclusionProfile")]
        RANDOM_EXCLUSION_PROFILE,
		[Description("MLGEPepSequenceExclusionProfile")]
		MLGE_SEQUENCE_EXCLUSION_PROFILE,
		[Description("NoraSequenceExclusionProfile")]
		NORA_SEQUENCE_EXCLUSION_PROFILE,
		[Description("CombinedExclusionProfile")]
		COMBINED_EXCLUSION,
		[Description("SVMExclusionProfile")]
		SVMEXCLUSION

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
    //            case ProfileEnum.NORA_EXCLUSION_PROFILE:
    //                return "Nora";
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


