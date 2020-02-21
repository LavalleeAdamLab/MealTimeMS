using System;
namespace MealTimeMS.Util
{
    public static class MassConverter
    {
        private const double MASS_OF_PROTON = 1.00727647; // AMU

        public static double convertMZ(double precursor_mass, int charge)
        {
            // Calculates the actual mass
            // must subtract the mass of a proton for each charged
            return (precursor_mass * charge) - (charge * MASS_OF_PROTON);
        }
    }
}
