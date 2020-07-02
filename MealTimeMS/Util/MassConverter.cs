using System;
namespace MealTimeMS.Util
{
    public static class MassConverter
    {
        public const double MASS_OF_PROTON = 1.00727646688; // AMU

        public static double convertMZ(double precursor_mz, int charge)
        {
            // Calculates the actual mass
            // must subtract the mass of a proton for each charged
            return (precursor_mz * charge) - (charge * MASS_OF_PROTON);
			// comet uses this: dMZ * iPrecursorCharge - (iPrecursorCharge - 1)*PROTON_MASS
		}

		//converts MH+ mass to m/z
		public static double MHPlusToMZ(double MHp, double charge)
		{
			return (MHp + (charge - 1) * MASS_OF_PROTON) / charge;
		}
	}
}
