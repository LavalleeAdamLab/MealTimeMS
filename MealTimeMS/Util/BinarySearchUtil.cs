using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.Graph;
using MealTimeMS.Data;
namespace MealTimeMS.Util
{

public class BinarySearchUtil
    {
        // Used as a quick way to solve the problem of mass list being sorted reverse of
        // what we want
        public static readonly double ARBITRARILY_LARGE_MASS = 1000000000.0;

        public enum SortingScheme
        {
            RT_START, RT_END, MASS
        }

        /*
         * Input: sorted list of peptides, query mass, ppm tolerance of mass
         * Returns a list of peptides that fall within the ppm tolerance of a query mass.
         */
        public static List<Peptide> findPeptides(List<Peptide> list, double queryMass, double ppmTolerance)
        {
            List<Peptide> matchingPeptides = new List<Peptide>();

            // Find index of a matching peptide
            int index = binarySearch(list, queryMass, ppmTolerance, list.Count - 1, 0);
            if (index < 0)
            {
                // Not found
                return matchingPeptides;
            }

            // Find all matching peptides
            matchingPeptides.Add(list[index]);
            // Add peptides greater than index if within ppm tolerance
            int currentIndex = index + 1;
            while (currentIndex < list.Count)
            {
                double currentMass = list[currentIndex].getMass();
                if (withinPPMTolerance(queryMass, currentMass, ppmTolerance))
                {
                    matchingPeptides.Add(list[currentIndex]);
                }
                else
                {
                    break;
                }
                currentIndex++;
            }
            // Add peptides less than index if within ppm tolerance
            currentIndex = index - 1;
            while (currentIndex >= 0)
            {
                double currentMass = list[currentIndex].getMass();
                if (withinPPMTolerance(queryMass, currentMass, ppmTolerance))
                {
                    matchingPeptides.Add(list[currentIndex]);
                }
                else
                {
                    break;
                }
                currentIndex--;
            }
            // Sort and return list
            matchingPeptides.Sort((Peptide x, Peptide y) => (y.getMass()).CompareTo(x.getMass()));
            return matchingPeptides;
        }

		

		/*
         * Input: sorted list of peptides, query mass, ppm tolerance of mass
         * Returns true if there is a peptide within the mass tolerance, false otherwise
         */
		public static bool peptideFound(List<Peptide> list, double queryMass, double ppmTolerance)
        {
            int index = binarySearch(list, queryMass, ppmTolerance, list.Count - 1, 0);
            if (index < 0)
            {
                // Not found
                return false;
            }
            else
            {
                return true;
            }
        }

        /*
         * A recursive implementation of binary search
         * Returns the index of a peptide within the mass tolerance of the query mass
         */
        private static int binarySearch(List<Peptide> list, double queryMass, double ppmTolerance, int high, int low)
        {
            // Not found
            if (high < low)
            {
                return -1;
            }

            // Search peptide at the middle of the list
            int mid = (high + low) / 2;
            double midMass = list[mid].getMass();
            // System.out.println("{queryMass=" + queryMass + "; high=" + high + "; low=" +
            // low + "; mid=" + mid + "; midMass="
            // + midMass + "; ppmTolerance=" + ppmTolerance + "; (midMass-queryMass)=" +
            // (midMass - queryMass) + "}");

            if (withinPPMTolerance(queryMass, midMass, ppmTolerance))
            {
                // Peptide found. Falls within ppm tolerance
                return mid;
            }
            else
            {
                if (midMass > queryMass)
                {
                    // Search the top half of the list
                    return binarySearch(list, queryMass, ppmTolerance, high, mid + 1);
                }
                else
                {
                    // Search the bottom half of the list
                    return binarySearch(list, queryMass, ppmTolerance, mid - 1, low);

                }
            }
        }

        public static bool withinPPMTolerance(double observed, double theoretical, double ppmTolerance)
        {
            return (Math.Abs(observed - theoretical) / theoretical) < ppmTolerance;
        }

        private static double getValue(Peptide p, SortingScheme s)
        {
            switch (s)
            {
                case SortingScheme.MASS:
                    return ARBITRARILY_LARGE_MASS - p.getMass();
                case SortingScheme.RT_START:
                    return p.getRetentionTime().getRetentionTimePeak();
                case SortingScheme.RT_END:
                    return p.getRetentionTime().getRetentionTimePeak();
                default:
                    return Double.NaN;
            }
        }

        /*
         * input: List<Peptide> list which is a list of peptides you want to add
         * to, Peptide p the peptide you want to add, SortingScheme s how the original
         * list is sorted by to find where Peptide p belongs output: the index you want
         * to insert peptide p or -1 if there is an exact match
		 * Keep in mind, the peptide mass list is sorted from highest mass to lowest mass, 
		 * which is already accounted for in getValue function
         */
        public static int findPositionToAdd(List<Peptide> list, Peptide p, SortingScheme s)
        {
            return findPositionToAdd(list, p, s, list.Count - 1, 0);
        }

        /*
         * A recursive implementation of binary search Returns the index of a peptide
         * within the mass tolerance of the query mass
         */
        private static int findPositionToAdd(List<Peptide> list, Peptide p, SortingScheme s, int high, int low)
        {

            // System.out.println(p);
            // System.out.println(s);
            // System.out.println("hi: " + high + "\tlo: " + low);
            // System.out.println(list.Count);
            //
            // if (list.Count > 1 && s.Equals(SortingScheme.MASS)) {
            // System.out.println("asdf");
            // System.out.println(getValue(p, s));
            // }
            // // Not found
            double targetValue = getValue(p, s);
            if (high <= low)
            {
				if (list.Count == 0)
				{
					return 0;
				}
				if (targetValue > getValue(list[low],s))
				{
					return low + 1;
				}
				else
				{
					return low;
				}
            }

            // Search peptide at the middle of the list
            int mid = (high + low) / 2;
            Peptide midPep = list[mid];
            double midValue = getValue(midPep, s);
            // System.out.println("mid: " + midValue + "\ttarget: " + targetValue);

				////commented this out because this should already be checked in a previous step
            //if (midPep == p)
            //{
            //    // prevents adding the same peptide to the list 
            //    return -1;
            //}
            //else
            //{
            //}
                if (midValue > targetValue)
                {
                    // Search the bottom half of the list
                    return findPositionToAdd(list, p, s, mid - 1, low);
                }
                else if (midValue < targetValue)
                {
                    // Search the top half of the list
                    return findPositionToAdd(list, p, s, high, mid + 1);
                }
                else
                {
                    return mid;
                    // if RT, needs a tie breaker...
                    // TODO this is a problem if RToffset is too high or too low, so we need to
                    // check the peak value instead
                }
        }

        /*
         * input: List<Peptide> list which is a list of peptides you want to
         * search, Peptide p the peptide you are looking for, SortingScheme s how the
         * original list is sorted by to find where Peptide p belongs output: the index
         * of peptide p or -1 if not found
         */
        public static int findPosition(List<Peptide> list, Peptide p, SortingScheme s)
        {
            return findPosition(list, p, s, list.Count - 1, 0);
        }

        /*
         * A recursive implementation of binary search Returns the index of a peptide
         * within the mass tolerance of the query mass
         */
        private static int findPosition(List<Peptide> list, Peptide p, SortingScheme s, int high, int low)
        {

            // Not found
            if (high < low)
            {
                return -1;
            }

            // Search peptide at the middle of the list
            int mid = (high + low) / 2;
            Peptide midPep = list[mid];
            double midValue = getValue(midPep, s);
            double targetValue = getValue(p, s);

            if (midPep == p)
            {
                return mid;
            }
            else
            {
                if (midValue > targetValue)
                {
                    // Search the bottom half of the list
                    return findPosition(list, p, s, mid - 1, low);
                }
                else if (midValue < targetValue)
                {
                    // Search the top half of the list
                    return findPosition(list, p, s, high, mid + 1);
                }
                else
                {
                    return -1;
                }
            }
        }

    }

}
