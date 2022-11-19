using System;
using MealTimeMS.Util;
namespace MealTimeMS.Data
{


    /*
     * Used when reading the .ms2 file 
     * This class contains all the attributes of each individual spectrum
     * An array of objects, spectrum, of class Spectra will be created to store each spectrum and its attributes
     */
    public class Spectra
    {
        //Each attribute is private so that other programs cannot change their values

        //params used only by this program locally
        private double index; // The index of the spectra locally in MTMS, only used to track progress. This is not the scanNum, see the variable scanNum.
        private double arrivalTime; //As opposed to "startTime", arrivalTime is the time recorded using the clock on the computer
        private double ionMobility;
        /*--> spectrum*/
        private int scanNum; //The scanNum provided by the instrument. Retrieve by spectrum.getScan()
        private int msLevel; // cvParams.get(1).value()
                             // private ? positiveScan; //cvParams.get(2).value()
                             // private ? profileSpec; //cvParams.get(3).value()
        private double baseMZ; // cvParams.get(4).value()
        private double baseIntensity; // cvParams.get(5).value()
        private double[] peakIntensity;
        private double[] peakMz;
        private int peakCount; //peak çount
        private double precursorMz;
        private int precursorCharge;
        private double totCurr; // cvParams.get(6).value()
        private double lowMZ; // cvParams.get(7).value()
        private double highMZ; // cvParams.get(8).value()

        /*--> scan --> ScanList --> spectrum*/
        private double startTime; // cvParams.get(0)
        private String filter; // cvParams.get(1)
        private double scanConfig; // cvParams.get(2)
        private double injectionTime; // cvParams.get(3)

        /*--> scanWindow --> scanWindowList --> scan --> ScanList --> spectrum*/
        private double scanLower; // cvParams.get(0).value()
        private double scanUpper; // cvParams.get(1).value()

        private double calculatedPrecursorMass;



        //Constructor used when creating a new object of type Spectra (associates the object's variables with those of the class)
        public Spectra(double index, int scanNum, int msLevel, double baseMZ, double baseIntensity, double totCurr,
                double lowMZ, double highMZ, double startTime, String filter, double scanConfig, double injectionTime,
                double scanLower, double scanUpper, double calculatedPrecursorMass)
        {

            this.index = index;
            this.scanNum = scanNum;
            this.msLevel = msLevel;
            this.baseMZ = baseMZ;
            this.baseIntensity = baseIntensity;
            this.totCurr = totCurr;
            this.lowMZ = lowMZ;
            this.highMZ = highMZ;
            this.startTime = startTime;
            this.filter = filter;
            this.scanConfig = scanConfig;
            this.injectionTime = injectionTime;
            this.scanLower = scanLower;
            this.scanUpper = scanUpper;
            this.calculatedPrecursorMass = calculatedPrecursorMass;
        }

        public Spectra(double index, int scanNum, int msLevel, int peakCount, double[] peakMz, double[] peakIntensity,
            double startTime, double precursorMz, double precursorCharge)
        {

            this.index = index;
            this.scanNum = scanNum;
            this.msLevel = msLevel;
            this.peakCount = peakCount;
            this.peakMz = peakMz;
            this.peakIntensity = peakIntensity;
            this.startTime = startTime;
            this.precursorMz = precursorMz;
            this.precursorCharge = (int)precursorCharge;
            this.calculatedPrecursorMass = MassConverter.convertMZ(precursorMz, (int)precursorCharge);
        }
        public Spectra(double index, int scanNum, int msLevel, int peakCount, double[] peakMz, double[] peakIntensity,
            double startTime, double precursorMz, double precursorCharge, double _arrivalTime) : this(index, scanNum, msLevel, peakCount, peakMz, peakIntensity,
            startTime, precursorMz, precursorCharge)
        {
            arrivalTime = _arrivalTime;

        }


        public static Spectra GetSpectraFromPasefMs2Spectrum(com.bruker.paser.avro.PasefMs2Spectrum pSpec)
        {
            pSpec.ms2_id = GlobalVar.TIMSTOF_Precursor_ID_to_ms2_id[pSpec.ms2_id];
            double rt_min = pSpec.rt / 60.0;
            var spec = new Spectra(-1, pSpec.ms2_id, 2, pSpec.intensity_data.Length, null, null,
               rt_min, pSpec.mono_mz, pSpec.charge);
            spec.SetIonMobility(pSpec.ooK0);
            if (GlobalVar.CheatingMonoPrecursorMassTable != null)
            {
                double correctedPrecursorMass = 0;
                if (GlobalVar.CheatingMonoPrecursorMassTable.TryGetValue(spec.getScanNum(), out correctedPrecursorMass))
                {
                    spec.calculatedPrecursorMass = correctedPrecursorMass;
                }
            }
            return spec;
        }


        public static Spectra CreateEmptyMS1(int _scanNum)
        {
            return new Spectra(-1, _scanNum, 1, -1, new double[] { 0, 0 }, new double[] { 0, 0 }, -1, -1, -1);
        }
        public void SetIonMobility(double _ionMobility)
        {
            ionMobility = _ionMobility;
        }
        // Functions to access the private values from class Spectra
        public double getCalculatedPrecursorMass()
        {
            return calculatedPrecursorMass;
        }
        public double getIonMobility()
        {
            return ionMobility;
        }

        public double getArrivalTime()
        {
            return arrivalTime;
        }

        public double getIndex()
        {
            return index;
        }

        public int getScanNum()
        {
            return scanNum;
        }

        public int getMSLevel()
        {
            return msLevel;
        }

        public double getBaseMZ()
        {
            return baseMZ;
        }

        public double getBaseIntensity()
        {
            return baseIntensity;
        }

        public double getTotCurr()
        {
            return totCurr;
        }

        public double getLowMZ()
        {
            return lowMZ;
        }

        public double getHighMZ()
        {
            return highMZ;
        }

        public double getStartTime()
        {
            return startTime;
        }

        public String getFilter()
        {
            return filter;
        }

        public double getScanConfig()
        {
            return scanConfig;
        }

        public double getInjectionTime()
        {
            return injectionTime;
        }

        public double getScanLower()
        {
            return scanLower;
        }

        public double getScanUpper()
        {
            return scanUpper;
        }

        public double[] getPeakMz()
        {
            return peakMz;
        }
        public double[] getPeakIntensity()
        {
            return peakIntensity;
        }
        public int getPeakCount()
        {
            return peakCount;
        }
        public double getPrecursorMz()
        {
            return precursorMz;
        }
        public int getPrecursorCharge()
        {
            return precursorCharge;
        }

        override
        public String ToString()
        {
            return "Spectra{index=" + index + "; scanNum=" + scanNum + "; msLevel=" + msLevel + "; start_t=" + startTime
                + "; precursor_MZ= " + precursorMz + "; precursor_Charge= " + precursorCharge + "; calculated_p_mass=" + calculatedPrecursorMass
                + "; base_mz=" + baseMZ
                    + "; base_intens=" + baseIntensity + "; tot_curr=" + totCurr + "; low_mz=" + lowMZ + "; high_mz="
                    + highMZ + "; start_t=" + startTime + "; filter=" + filter + "; scan_config=" + scanConfig
                    + "; inject_t=" + injectionTime + "; scan_lower=" + scanLower + ";scan_upper=" + scanUpper + "}";
        }

    } //End of class Spectra



}
