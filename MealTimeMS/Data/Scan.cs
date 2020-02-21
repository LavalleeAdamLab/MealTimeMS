using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using IMsScan = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using ICentroid2 = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.ICentroid;
using InfoContainer = Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer.IInfoContainer;
using MealTimeMS.Util;
namespace MealTimeMS.Data
{
    // Depricated, this class is not used anymore
    public class Scan
    {
        Spectra spec;
        int centroidCount = -1;
        public int ID;
        double time;
        public Dictionary<String, String> infoTable;
        StringBuilder detailedString;
        StringBuilder simpleString;

        public Scan(IMsScan imsScan, int _ID)
        {
            ID = _ID;
            
#if DEBUG
            detailedString = new StringBuilder();
            simpleString = new StringBuilder();
            infoTable = new Dictionary<string, string>();
            DumpScan(imsScan); //parses the scan info but does not store it anywhere, can comment this out later in real ms run
            DumpVars(imsScan); //do not comment this out, this parses the info table of the imsScan
                               //later on in development can parsee the two tables into one with only the information we wanted          
            BuildSimpleString();
#endif



            //TODO to boost performance, hard code which table to search in "TryGetValue" instead of determining on runtime
            String temp;
            int msLevel = TryGetValue(imsScan, GlobalVar.MSLevelHeader, out temp) ? parseMSLevel(temp) : -1;
            int scanNum = TryGetValue(imsScan, GlobalVar.ScanNumHeader, out temp) ? int.Parse(temp):ID;
            double startTime = TryGetValue(imsScan, GlobalVar.ScanTimeHeader, out temp) ? double.Parse(temp) : time;
            int precursorCharge = TryGetValue(imsScan, GlobalVar.PrecursorChargeHeader, out temp) ? int.Parse(temp) : -1;
            double precursorMZ = TryGetValue(imsScan, GlobalVar.PrecursorMZHeader, out temp) ? double.Parse(temp) : -1;
            centroidCount = (int)imsScan.CentroidCount;
            time = startTime;
            double[] peakIntensity = new double[centroidCount];
            double[] peakMZ = new double[centroidCount];

            int counter = 0;
            foreach(ICentroid2 cent in imsScan.Centroids )
            {
                peakIntensity[counter] = cent.Intensity;
                peakMZ[counter] = cent.Mz;
                counter++;
            }
            spec = new Spectra(ID, scanNum, msLevel, centroidCount, peakMZ, peakIntensity, startTime, precursorMZ, precursorCharge);
        }


        private bool TryGetValue(IMsScan scan, String name, out String value)
        {
            value = null;
            if(TryGetValue(scan.CommonInformation, name, out value))
            {
                return true;
            }else if (TryGetValue(scan.SpecificInformation, name, out value))
            {
                return true;
            }
            Console.WriteLine("Warning, value with name: {0} not found", name);
            return false;
        }
        private bool TryGetValue(IInfoContainer container, String name, out String value)
        {
            value = null;
            MsScanInformationSource i = MsScanInformationSource.Unknown;
            return (container.TryGetValue(name, out value, ref i));
        }

        private int parseMSLevel(String str)
        {
            if (str.Equals("MS"))
            {
                return 1;
            }else if(str.Equals("MS2")){
                return 2;
            }
            return -1;
        }

        public Spectra GetSpectra()
        {
            return spec;
        }

        private ICentroid2 GetTopPeak(IEnumerable<ICentroid2> centroids)
        {
            //gets the highest intensity centroid
            Double max = -1.0;
            ICentroid2 top = null;
            foreach (ICentroid2 cent in centroids)
            {
                
                if (max < cent.Intensity)
                {
                    max = cent.Intensity;
                    top = cent;
                }
            }
            return top;
        }


        public override string ToString()
        {
            return detailedString.ToString();

        }


        private void DumpScan(IMsScan scan)
        {
            
            StringBuilder sb = new StringBuilder();
            sb.Append("RT:");
            sb.Append(time);
            sb.Append(": ");
            sb.Append("scan ID: "+ID);
            sb.Append(", ");
            if (scan == null)
            {
                sb.Append("(empty_scan)");
                outputln(sb.ToString());
                return;
            }
            else
            {
              
                centroidCount = scan.CentroidCount ?? 0;
                sb.Append("detector=");
                sb.Append(scan.DetectorName);
                string id;
                if (scan.SpecificInformation.TryGetValue("Access Id:", out id))
                {
                    sb.Append(", id=");
                    sb.Append(id);
                }
                outputln(sb.ToString());
            }

            if(scan.NoiseBand != null)
            {
                output("  Noise: ");
                foreach (INoiseNode noise in scan.NoiseBand)
                {
                    output("[{0}, {1}], ", noise.Mz, noise.Intensity);
                }
                outputln();

                // Not so useful:
                
            }
            outputln("{0} centroids, {1} profile peaks", scan.CentroidCount ?? 0, scan.ProfileCount ?? 0);

            // Iterate over all centroids and access dump all profile elements for each.

            if (scan.CentroidCount > 0)
            {
                    outputln(CentroidToString(GetTopPeak(scan.Centroids)));
            }   
        }

        private String CentroidToString(ICentroid2 centroid)
        {
            if(centroid == null)
            {
                return "no centroid";
            }


            return String.Format(" Mz={0,10:F5}, I={1:E5}, C={2}, E={3,-5} F={4,-5} M={5,-5} R={6,-5} Res={7}",
                                    centroid.Mz, centroid.Intensity, centroid.Charge ?? -1, centroid.IsExceptional,
                                    centroid.IsFragmented, centroid.IsMerged, centroid.IsReferenced, centroid.Resolution);
        }

        /// <summary>
        /// Dump all variables belonging to a scan
        /// </summary>
        /// <param name="scan">the scan for which to dump all variables</param>
        public void DumpVars(IMsScan scan)
        {
            outputln("COMMON");
            DumpVars(scan.CommonInformation);
            outputln("SPECIFIC");
            DumpVars(scan.SpecificInformation);
        }

        /// <summary>
        /// Dump all scan variables belonging to a specific container in a scan.
        /// </summary>
        /// <param name="container">container to dump all contained variables for</param>
        private void DumpVars(IInfoContainer container)
        {
            if (container == null)
            {
                return;
            }

            foreach (string s in container.Names)
            {
                DumpVar(container, s);
            }
        }

        /// <summary>
        /// Dump the content of a single variable to the console after testing the consistency.
        /// </summary>
        /// <param name="container">container that variable belongs to</param>
        /// <param name="name">name of the variable</param>
        private void DumpVar(IInfoContainer container, string name)
        {
            object o = null;
            string s = null;
            MsScanInformationSource i = MsScanInformationSource.Unknown;

            if (container.TryGetValue(name, out s, ref i))
            {
                // i should have a reasonable value now
                if (container.TryGetRawValue(name, out o, ref i))
                {
                    outputln("  {0}: type={1}, text='{2}', raw='{3}'",
                        name, i, s, o);
                    infoTable.Add(name,s);
                }
            }
        }

        private void BuildSimpleString()
        {
            simpleString.Append(String.Format("RT:{0:0.##}",time));
            simpleString.Append(": scan ID"+ID);
            simpleString.Append("\t Centroid_count: "+centroidCount);
            simpleString.Append("\nOther Info available: ");
            foreach(String name in infoTable.Keys)
            {
                simpleString.Append(name + " ");
            }
        }

        private void output(String format, params object[] arg)
        {
            String str = String.Format(format, arg);
            detailedString.Append(str);
        }

        private void outputln(String format, params object[] arg)
        {
            String str = String.Format(format, arg);
            detailedString.Append(str + "\n");
        }

        private void outputln()
        {
            detailedString.Append("\n");
        }


        public String getDetailedString()
        {
            return detailedString.ToString();
        }

        public String getSimpleString()
        {
            return simpleString.ToString();
        }
    }


}
