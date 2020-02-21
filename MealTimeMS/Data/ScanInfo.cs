using System;
using IMsScan = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;

namespace MealTimeMS.Data
{
    public class ScanInfo
    {
        public IMsScan imsScan;
        public int ID;
        public double time;
        public ScanInfo(IMsScan _scan, int _ID, double _time)
        {
            imsScan = _scan;
            ID = _ID;
            time = _time;
        }

        public IMsScan getScan(){   return imsScan;    }
        public int getID() { return ID; }
        public double getTime() { return time; }

    }
}
