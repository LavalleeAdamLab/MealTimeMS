using System;
namespace MealTimeMS.Util
{
    public class Logger
    {
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		public Logger()
        {
        }

        public static void debug(String str)
        {
            log.Debug(str);
        }
        public void info(String str)
        {

        }
    }
}
