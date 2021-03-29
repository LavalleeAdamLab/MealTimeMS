using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Util
{
	public static class HelperFunction
	{

		public static String GetStringFromList(List<Object> ls)
		{
			String str = "";
			foreach(Object obj in ls)
			{
				str = str + obj.ToString() + "\n";
			}
			return str;
		}

		public static String GetStringFromList(List<String> ls)
		{
			String str = "";
			foreach (String ss in ls)
			{
                str = str + ss + "\n";
			}
			return str;
		}


	}
}
