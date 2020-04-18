using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Util
{
	public static class ListUtil
	{

		public static List<String> FindIntersection(List<String> l1, List<String> l2)
		{
			List<String> intersection = new List<String>();
			foreach (String item in l1)
			{
				if (l2.Contains(item))
				{
					intersection.Add(item);
				}
			}
			return intersection;
		}
		public static List<String> FindUnion(List<String> l1, List<String> l2)
		{
			HashSet<String> union = new HashSet<String>();
			foreach (String item in l1)
			{
				union.Add(item);
			}
			foreach (String item in l2)
			{
				union.Add(item);
			}
			List<String> unionList = new List<String>(union);
			return unionList;
		}
	}
}
