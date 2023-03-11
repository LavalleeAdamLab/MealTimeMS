using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealTimeMS.Data
{

    public class RetentionTime
    {
        // TODO MAKE SURE RETENTION TIME IS IN MINUTES
        public static readonly double MINIMUM_RETENTION_TIME = 0.0;
        public static readonly double MAXIMUM_RETENTION_TIME = Double.MaxValue;
        private static double retentionTimeOffset = 0.0;

        private readonly double retentionTimeStart;
        private readonly double retentionTimeEnd;
        private readonly double retentionTimePeak;
        private readonly bool isPredicted;

	/*
	 * Default retention time will be the entire run.
	 */
	public RetentionTime()
        {
            retentionTimeStart = MINIMUM_RETENTION_TIME;
            retentionTimeEnd = MINIMUM_RETENTION_TIME;
            retentionTimePeak = MINIMUM_RETENTION_TIME;
            isPredicted = true;
        }

        /*
         * Retention time starting at one time and ending at another time. If it is from
         * a retention time prediction software, isPredicted will be true. Otherwise, if
         * it was observed in the experiment, use isPredicted as false.
         */
        public RetentionTime(double start, double end, bool _isPredicted)
        {
            retentionTimeStart = start;
            retentionTimeEnd = end;
            retentionTimePeak = (retentionTimeStart + retentionTimeEnd) / 2.0;
            isPredicted = _isPredicted;
        }

        /*
         * Retention time range calculated from the middle, starting and ending based on
         * left and right flank offset. If it is from a retention time prediction
         * software, isPredicted will be true. Otherwise, if it was observed in the
         * experiment, use isPredicted as false.
         */
        public RetentionTime(double mid, double leftFlank, double rightFlank, bool _isPredicted)
        {
            retentionTimeStart = Math.Max(mid - leftFlank, MINIMUM_RETENTION_TIME);
            retentionTimeEnd = Math.Min(mid + rightFlank, MAXIMUM_RETENTION_TIME);
            retentionTimePeak = mid;
            isPredicted = _isPredicted;
        }

        /*
         * Retention time starting at one time and ending at another time. Assumed to be
         * a predicted retention time unless otherwise stated
         */
        public RetentionTime(double start, double end):this(start,end,true)
        {
            
        }

        /*
         * Retention time range calculated from the middle, starting and ending based on
         * left and right flank offset. Assumed to be a predicted retention time unless
         * otherwise stated
         */
        public RetentionTime(double mid, double leftFlank, double rightFlank): this(mid, leftFlank, rightFlank, true)
        {
            
        }


        public static void setRetentionTimeOffset(double d)
        {
            retentionTimeOffset = d;
        }

        public static double getRetentionTimeOffset()
        {
            return retentionTimeOffset;
        }

        public double getRetentionTimePeak()
        {
            return retentionTimePeak;
        }

        public bool IsPredicted()
        {
            return isPredicted;
        }

        public double getRetentionTimeStart()
        {
            if (isPredicted)
            {
                return Math.Min(Math.Max(retentionTimeStart + retentionTimeOffset, MINIMUM_RETENTION_TIME),
                        MAXIMUM_RETENTION_TIME);
            }
            else
            {
                return retentionTimeStart;
            }
        }

        public double getRetentionTimeEnd()
        {
            if (isPredicted)
            {
                return Math.Max(Math.Min(retentionTimeEnd + retentionTimeOffset, MAXIMUM_RETENTION_TIME),
                        MINIMUM_RETENTION_TIME);
            }
            else
            {
                return retentionTimeEnd;
            }
        }

        public String retentionTimeViewerToString()
        {
            return "[" + this.getRetentionTimeStart() + "," + this.getRetentionTimeEnd() + "]";
        }

        override
        public bool Equals(Object o)
        {
            if (o is RetentionTime) {
                RetentionTime rt = (RetentionTime)o;
                return ((rt.getRetentionTimeStart() == this.getRetentionTimeStart())
                        && (rt.getRetentionTimeEnd() == this.getRetentionTimeEnd()));
            } else
            {
                return false;
            }
        }

        override
        public String ToString()
        {
            return "RetentionTime[start=" + this.getRetentionTimeStart() + ",peak=" + this.getRetentionTimePeak() + ",end="
                    + this.getRetentionTimeEnd() + ",isPredicted=" + this.IsPredicted() + ",units=MINUTES]";
        }

    }

}
