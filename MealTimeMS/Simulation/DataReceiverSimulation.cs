#region legal notice
// Copyright(c) 2016 - 2018 Thermo Fisher Scientific - LSMS
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion legal notice
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using MealTimeMS.Util;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.RunTime;
using Thermo.Interfaces.ExactiveAccess_V1;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using IMsScan1 = Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer.IMsScan;
using IMsScan = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;
using ICentroid2 = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.ICentroid;

using MealTimeMS.Data;

namespace MealTimeMS
{
	//  This class contains eventListeners to listen to MS scans broadcasted by the InstrumentSimulation class, 
	//  the latter will be running in parallel with this class/thread, broadcasted scans are parsed from a .ms2 file
	//  The .ms2 file can be converted from the .mzml using Proteowizard.
	//  Use shared desktop in lab, open command line:
	//  msconvert --ms2 -o [output directory]  [mzml file]
	class DataReceiverSimulation
    {
        internal DataReceiverSimulation() { }

        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        internal void DoJob(ExclusionProfile exclusionProfile,List<Spectra> spectraList)
        {

			//Creates the simulated instrument object that will be broadcasting the scans
            InstrumentSimulation instrument = new InstrumentSimulation(spectraList);

            Thread DataProcessingThread = new Thread(() => DataProcessor.StartProcessing(exclusionProfile));
			//Currently not using input handling, user input will not be taken in runtime
			//Thread InputHandlingThread = new Thread(() => InputHandler.ReadConsoleInput());
            Thread SimulatedInstrumentThread = new Thread(() => instrument.StartInstrument());
			DataProcessor.reset();
			DataProcessingThread.Start();
			//Waiting for DataProcessor to parse and construct the Database and set up comet
            while (!DataProcessor.SetupFinished())
            {
                Thread.Sleep(500);
            }

			//InputHandlingThread.Start();

            instrument.AcquisitionStreamOpening += Orbitrap_AcquisitionStreamOpening;
            instrument.AcquisitionStreamClosing += Orbitrap_AcquisitionStreamClosing;
            instrument.MSScanArrived += Orbitrap_MsScanArrived;
			SimulatedInstrumentThread.Start();

            int durationCounter = 0; 
			//Listen for a duration specified in GlobalVar in seconds
            while (durationCounter < GlobalVar.listeningDuration && ExclusionExplorer.IsListening())
            {
                Thread.CurrentThread.Join(1000); //does the same thing as Thread.Sleep() but Join
                                                 //allows standard sendmessage pumping and COM to continue, ie allows event listening to continue
                durationCounter++; 
            }
		

            try
            {
				SimulatedInstrumentThread.Abort();
            }
            catch (Exception ex)
            {
            } 
            instrument.MSScanArrived -= Orbitrap_MsScanArrived;
            instrument.AcquisitionStreamOpening -= Orbitrap_AcquisitionStreamOpening;
            instrument.AcquisitionStreamClosing -= Orbitrap_AcquisitionStreamClosing;

            DataProcessor.EndProcessing();
            
            DataProcessingThread.Join(); //wait until dataProcessor finishes processing/outputing the scan
                                         //queue then returns to calling thread (in this case the exclusion explorer);

        }

		//The IMsScan will arrive at this method
		//It will be sent to DataProcessor to be parsed into a Scan object, and the IMsScan will be disposed to prevent blocking shared memory
		private void Orbitrap_MsScanArrived(object sender, MSScanEventArgs e)
        {

            using (IMsScan scan = (IMsScan)e.GetScan()) // caution! You must dispose this, or you block shared memory!
            {
                if (scan == null)
                {
                    logger.Warn("scan is null, possibly cast failed?");
                    return;
                }
                logger.Debug("\n{0:HH:mm:ss,fff} scan with {1} centroids arrived", DateTime.Now, scan.CentroidCount);
                DataProcessor.ParseIMsScan(scan);
            }
        }

        private void Orbitrap_AcquisitionStreamClosing(object sender, EventArgs e)
        {
            logger.Info("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream closed (end of method)");
		
			
			ExclusionExplorer.EndMealTimeMS();
		}

        private void Orbitrap_AcquisitionStreamOpening(object sender, MsAcquisitionOpeningEventArgs e)
        {

           // WriterClass.writePrintln(String.Format("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream opens (start of method)"));
            if (GlobalVar.acquisitionStartTime < 0)
            {
                GlobalVar.acquisitionStartTime = (Double)(DateTime.Now.TimeOfDay.TotalMinutes);
            }

			//WriterClass.writePrintln(String.Format("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream opens (start of method)"));
		}
    }
}
