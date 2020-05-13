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
	// Parses IMsScan into Spectra object
	public class IMsScanParser
	{


		public static Spectra Parse(IMsScan imsScan, int ID)
		{

			//TODO to boost performance, hard code which table/container to search in "TryGetValue" instead of determining on runtime
			String temp;
			int msLevel = TryGetValue(imsScan, GlobalVar.MSLevelHeader, out temp) ? parseMSLevel(temp) : -1;
			int scanNum = TryGetValue(imsScan, GlobalVar.ScanNumHeader, out temp) ? int.Parse(temp) : ID;
			double startTime = TryGetValue(imsScan, GlobalVar.ScanTimeHeader, out temp) ? double.Parse(temp) : -1;
			int precursorCharge = TryGetValue(imsScan, GlobalVar.PrecursorChargeHeader, out temp) ? int.Parse(temp) : -1;
			double precursorMZ = TryGetValue(imsScan, GlobalVar.PrecursorMZHeader, out temp) ? double.Parse(temp) : -1;
			int centroidCount = (int)imsScan.CentroidCount;
			double[] peakIntensity = new double[centroidCount];
			double[] peakMZ = new double[centroidCount];

			int counter = 0;
			foreach (ICentroid2 cent in imsScan.Centroids)
			{
				peakIntensity[counter] = cent.Intensity;
				peakMZ[counter] = cent.Mz;
				counter++;
			}
			Spectra spec = new Spectra(ID, scanNum, msLevel, centroidCount, peakMZ, peakIntensity, startTime, precursorMZ, precursorCharge);
			return spec;
		}

		public static Spectra Parse(IMsScan imsScan, int ID, double arrivalTime)
		{
			//As opposed to "StartTime" contained in the IMsScan information, arrival time uses the computer's own clock in milisecond rather than using the Mass spec's clock
			String temp;
			int msLevel = TryGetValue(imsScan, GlobalVar.MSLevelHeader, out temp) ? parseMSLevel(temp) : -1;
			int scanNum = TryGetValue(imsScan, GlobalVar.ScanNumHeader, out temp) ? int.Parse(temp) : ID;
			double startTime = TryGetValue(imsScan, GlobalVar.ScanTimeHeader, out temp) ? double.Parse(temp) : -1;
			int precursorCharge = TryGetValue(imsScan, GlobalVar.PrecursorChargeHeader, out temp) ? int.Parse(temp) : -1;
			double precursorMZ = TryGetValue(imsScan, GlobalVar.PrecursorMZHeader, out temp) ? double.Parse(temp) : -1;
			int centroidCount = (int)imsScan.CentroidCount;

			double[] peakIntensity = new double[centroidCount];
			double[] peakMZ = new double[centroidCount];
			int counter = 0;
			foreach (ICentroid2 cent in imsScan.Centroids)
			{
				peakIntensity[counter] = cent.Intensity;
				peakMZ[counter] = cent.Mz;
				counter++;
			}
			Spectra spec = new Spectra(ID, scanNum, msLevel, centroidCount, peakMZ, peakIntensity, startTime, precursorMZ, precursorCharge, arrivalTime);
			return spec;
		}

		private static bool TryGetValue(IMsScan scan, String name, out String value)
		{
			value = null;
			if (TryGetValue(scan.CommonInformation, name, out value))
			{
				return true;
			}
			else if (TryGetValue(scan.SpecificInformation, name, out value))
			{
				return true;
			}
			Console.WriteLine("Warning, value with name: {0} not found", name);
			return false;
		}
		private static bool TryGetValue(IInfoContainer container, String name, out String value)
		{
			value = null;
			MsScanInformationSource i = MsScanInformationSource.Unknown;
			return (container.TryGetValue(name, out value, ref i));
		}

		private static int parseMSLevel(String str)
		{
			if (str.Equals("MS"))
			{
				return 1;
			}
			else if (str.Equals("MS2"))
			{
				return 2;
			}
			return -1;
		}


	}
}