using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;

using IMsScan1 = Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer.IMsScan;
using IMsScan2 = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;
using ICentroid1 = Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer.ICentroid;
using ICentroid2 = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.ICentroid;
using System.Collections;
using Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer;
using MealTimeMS.Data;
using MealTimeMS.IO;
using MealTimeMS.Util;
using MealTimeMS.RunTime;

namespace MealTimeMS
{
    
    //Simulates Thermo instrument broadcasting spectra info, to be received by DataReceiverSimulation listener functions.
    public  class InstrumentSimulation
    {

        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<MSScanEventArgs> MSScanArrived;
        public event EventHandler<MsAcquisitionOpeningEventArgs> AcquisitionStreamOpening;
        public event EventHandler<EventArgs> AcquisitionStreamClosing;

        public List<Spectra> specList;
        public int maxMS2ToSimulate = 100000000;
		public InstrumentSimulation(List<Spectra> _specList)
		{
			specList = _specList;
		}

		public InstrumentSimulation()
		{
		}
		public virtual void StartInstrument()
        {
			
			
			//int miliSecondsPerScan = (int)((GlobalVar.ExperimentTotalTimeInMinutes*60000)/ GlobalVar.ExperimentTotalScans);
			OnAcquisitionStreamOpening();
			double startTime = specList[0].getStartTime() * 60000; // first scan's time in miliseconds
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			

			int counter = 0;
			while (counter < specList.Count&&counter< maxMS2ToSimulate)
			{
				double currentTime = specList[counter].getStartTime();
				//set to less than < to simulate real time, or greater than > to dump all scans at once

#if IGNORE
				if (currentTime * 60000 >= watch.ElapsedMilliseconds + startTime)
				{
					Thread.Sleep(20);
					OnMSScanArrived(specList[counter]);
					counter++;
				}
#else
				if (true)
				{
					//Thread.Sleep(20);
					OnMSScanArrived(specList[counter]);
					counter++;
				}
#endif



			}
			Thread.Sleep(10);
			OnAcquisitionStreamClosing();
            
            //BroadcastFinished();
        }

        



        protected  virtual void OnMSScanArrived(Spectra spec)
		{
            MSScanEventArgs args = new MSScanEventArgs(new SimScan(spec));
            MSScanArrived(this, args);
        }

        protected virtual void OnAcquisitionStreamOpening()
        {
            AcquisitionStreamOpening(this, null);
        }
        protected virtual void OnAcquisitionStreamClosing()
        {
            AcquisitionStreamClosing (this, new EventArgs());
        }

    }
    

    public class MSScanEventArgs : EventArgs
    {
        public IMsScan1 scan { get; set; }
        public IMsScan1 GetScan()
        {
            return scan;
        }
        public MSScanEventArgs(SimScan simScan)
        {
            scan = simScan;
        }
        
    }

    //public class SimScan2: IMsScan2 , IDisposable
    //{
    //    public Nullable<int> CentroidCount { get; set; }
    //    public IEnumerable<ICentroid1> Centroids { get; set; }
    //    public string DetectorName { get; set; }
    //    public IEnumerable<INoiseNode> NoiseBand { get; set; }

    //    public IInfoContainer CommonInformation { get; set; }

    //    public IInfoContainer SpecificInformation { get; set; }

    //    public bool HasCentroidInformation { get; set; }

    //    public bool HasProfileInformation { get; set; }

    //    public int? ProfileCount { get; set; }

    //    public bool AdditiveNoise { get; }

    //    IEnumerable<ICentroid2> IMsScan2.Centroids { get {

    //            List<ICentroid1> ls = Centroids.ToList<ICentroid1>();
    //            List<ICentroid2> ls2 = new List<ICentroid2>();
    //            foreach(ICentroid1 cent in ls)
    //            {
    //                ls2.Add((ICentroid2)cent);
    //            }
    //            return ls2;

    //        }

    //    }

    //    public void Dispose()
    //    {

    //        GC.SuppressFinalize(this);
    //    }

    //    public void GetProfileData(ref double[] masses, ref double[] intensities)
    //    {
    //        throw new NotImplementedException();
    //    }
        
    //    public SimScan2(SimScan simScan)
    //    {
    //        CentroidCount = simScan.CentroidCount;
    //        Centroids = simScan.Centroids;
    //        DetectorName = simScan.DetectorName;
    //        NoiseBand = simScan.NoiseBand;
    //        CommonInformation = simScan.CommonInformation;
    //        SpecificInformation = simScan.SpecificInformation;
    //        HasCentroidInformation = simScan.HasCentroidInformation;
    //        HasProfileInformation = simScan.HasProfileInformation;
    //        ProfileCount = simScan.ProfileCount;
    //        AdditiveNoise = simScan.AdditiveNoise;

    //    }
    //}

    public class SimScan : IMsScan1, IDisposable, IMsScan2
    {
       
        IEnumerable<ICentroid2> IMsScan2.Centroids
        {
            get
            {
                List<ICentroid2> ls2 = new List<ICentroid2>();
                foreach (SimCentroid cent in simCentroid)
                {
                    ls2.Add((ICentroid2)cent);
                    
                }
                return ls2;

            }

        }
        public List<SimCentroid> simCentroid;

        public Nullable<int> CentroidCount { get; set; }
        public IEnumerable<ICentroid1> Centroids { get; set; }
        public string DetectorName { get; set; }
        public IEnumerable<INoiseNode> NoiseBand { get; set; }

        public IInfoContainer CommonInformation { get; set; }

        public IInfoContainer SpecificInformation { get; set; }

        public bool HasCentroidInformation { get; set; }

        public bool HasProfileInformation { get; set; }

        public int? ProfileCount { get; set; }

        public bool AdditiveNoise { get; }
       
        public void Dispose()
        {

            GC.SuppressFinalize(this);
        }

        public void GetProfileData(ref double[] masses, ref double[] intensities)
        {
            throw new NotImplementedException();
        }

        public SimScan(Spectra spec)
        {
            Random rnd = new Random();
            List<SimCentroid> ls = new List<SimCentroid>();

            CentroidCount = spec.getPeakCount();
            for(int i =0; i<CentroidCount;i++)
            {
                SimCentroid cent = new SimCentroid(spec,i,rnd);
                ls.Add(cent);
            }
            Centroids = ls;
            simCentroid = ls;
            DetectorName = "simulatedDetector";
            NoiseBand = null;
            
            HasCentroidInformation = true;
            HasProfileInformation = false;
            ProfileCount = 0;
            CommonInformation = new InfoContainer(true, spec);
            SpecificInformation = new InfoContainer(false, spec);

        }
    }

    public class InfoContainer : IInfoContainer
    {
        public IEnumerable<string> Names { get; set; }
        public Dictionary<String, String> table;
        public InfoContainer(bool common, Spectra spec)
        {
       
            table = new Dictionary<string, string>();

            String msLevel = "MS";
            if (spec.getMSLevel() == 2)
            {
                msLevel = "MS2";
            }

            if (common)
            {
                table.Add(GlobalVar.MSLevelHeader, msLevel);
                table.Add(GlobalVar.PrecursorChargeHeader, spec.getPrecursorCharge().ToString());
                table.Add(GlobalVar.PrecursorMZHeader, spec.getPrecursorMz().ToString());
                table.Add(GlobalVar.ScanNumHeader, spec.getScanNum().ToString());
                table.Add(GlobalVar.ScanTimeHeader, spec.getStartTime().ToString());
               
            }
            else
            {
                table.Add("specificInfo1", "aa");
                table.Add("specificInfo2","pp");
            }
            
            Names = table.Keys;

        }

        public bool TryGetRawValue(string name, out object value)
        {
            String str;
            bool flag;
            flag= table.TryGetValue(name,out str);
            value = str;
            return flag;
        }

        public bool TryGetRawValue(string name, out object value, ref MsScanInformationSource source)
        {
            return TryGetRawValue(name, out value);
        }

        public bool TryGetValue(string name, out string value)
        {
            return table.TryGetValue(name,out value);
        }

        public bool TryGetValue(string name, out string value, ref MsScanInformationSource source)
        {
            return TryGetValue(name, out value);
        }
    }

    public class SimCentroid : ICentroid1, ICentroid2
    {
       
        public bool? IsExceptional { get; set; }

        public bool? IsReferenced { get; set; }

        public bool? IsMerged { get; set; }

        public bool? IsFragmented { get; set; }

        public bool? IsMonoisotopic { get; set; }

        public int? Charge { get; set; }

        public IMassIntensity[] Profile { get; set; }

        public double Mz { get; set; }

        public double Intensity { get; set; }

        IMassIntensity[] ICentroid1.Profile => throw new NotImplementedException();

        public double? Resolution { get; set; }

        public SimCentroid(Spectra spec, int i, Random rnd)
        {

            
            IsExceptional = true;
            IsReferenced = false;
            IsMerged = true;
            IsFragmented = true;
            IsMonoisotopic = true;
            Charge = rnd.Next(1, 6); //note this charge is the charge of each peak on a ms2, so this is not the precursor charge
            Profile = null;
            Mz = spec.getPeakMz()[i];
            Intensity = spec.getPeakIntensity()[i];
            Resolution = 100000;

        }
    }

    //public class SimCentroidEnumerable : IEnumerable<SimCentroid>
    //{
    //    public IEnumerator<SimCentroid> GetEnumerator()
    //    {
    //        return new SimCentroidEnumerator();
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return this.GetEnumerator();
    //    }


    //}

    //public class SimCentroidEnumerator : IEnumerator<SimCentroid>
    //{
    //    public SimCentroid Current { get ; set;}

    //    object IEnumerator.Current { get; }
    //    int currentIndex;
    //    public int centroidCount;
    //    ArrayList centroidList;

    //    public SimCentroidEnumerator()
    //    {
    //        centroidList = new ArrayList();
    //        Random rnd = new Random();
    //        centroidCount = rnd.Next(1,9);
    //        for(int i = 0; i < centroidCount; i++)
    //        {
    //            centroidList.Add(new SimCentroid());
    //        }
    //        currentIndex = 0;
    //        Current = (SimCentroid)centroidList[currentIndex];

    //    }

    //    public bool MoveNext()
    //    {
    //        currentIndex++;
    //        if (currentIndex >= centroidList.Count)
    //        {
    //            return false;
    //        }
    //        else
    //        {
    //            Current = (SimCentroid)centroidList[currentIndex];
    //            return true;
    //        }

    //    }

    //    public void Reset()
    //    {
    //        currentIndex = 0;
    //        Current = (SimCentroid)centroidList[currentIndex];
    //    }

    //    #region IDisposable Support
    //    private bool disposedValue = false; // To detect redundant calls

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!disposedValue)
    //        {
    //            if (disposing)
    //            {
    //                // TODO: dispose managed state (managed objects).
    //            }

    //            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
    //            // TODO: set large fields to null.

    //            disposedValue = true;
    //        }
    //    }

    //    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    //    // ~SimCentroidEnumerator()
    //    // {
    //    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //    //   Dispose(false);
    //    // }

    //    // This code added to correctly implement the disposable pattern.
    //    public void Dispose()
    //    {
    //        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //        Dispose(true);
    //        // TODO: uncomment the following line if the finalizer is overridden above.
    //        // GC.SuppressFinalize(this);
    //    }
    //    #endregion

    //}




}
