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

using Thermo.Interfaces.ExactiveAccess_V1;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Methods;
using IMsScan = Thermo.Interfaces.InstrumentAccess_V2.MsScanContainer.IMsScan;
using MealTimeMS.Util;
using MealTimeMS.RunTime;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Data;

namespace MealTimeMS
{
	/// <summary>
	/// Show incoming data packets and signals of acquisition start, acquisition stop and each scan.
	/// </summary>
	/// 
	//  This class contains eventListeners to listen to MS scans broadcasted by Tune, 
	//  used when running on the laptop hooked onto the mass spectrometer during a real experiment
    //  For simulation see class DataReceiverSimulation.cs
	class DataReceiver
	{
		internal DataReceiver() { }
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        ITable m_replacementTable;
        IMethods m_methods;

		internal void DoJob(ExclusionProfile exclusionProfile)
		{
			using (IExactiveInstrumentAccess instrument = Connection.GetFirstInstrument())
			{
                if(instrument == null)
                {
                    Console.WriteLine("Failed to create Instrument, program will now exit");
                    return;
                }

				IMsScanContainer orbitrap = instrument.GetMsScanContainer(0);
                //if (GlobalVar.SeeExclusionFormat)
                //{
                //    m_methods = instrument.Control.Methods;
                //    m_replacementTable = CreateReplacementTable();
                //}

				Console.WriteLine("Waiting 60 seconds for scans on detector " + orbitrap.DetectorClass + "...");
				DataProcessor.reset();
				Thread DataProcessingThread = new Thread(() => DataProcessor.StartProcessing(exclusionProfile));
                //Thread InputHandling = new Thread(() => InputHandler.ReadConsoleInput());
                DataProcessingThread.Start();
                //InputHandling.Start();
                while (!DataProcessor.SetupFinished())
                {
                    Thread.Sleep(500);
                }
                orbitrap.AcquisitionStreamOpening += Orbitrap_AcquisitionStreamOpening;
				orbitrap.AcquisitionStreamClosing += Orbitrap_AcquisitionStreamClosing;
				Console.WriteLine("Waiting on acquisition stream to open");
				while (!acquisitionStreamOpened&& !Console.ReadKey().KeyChar.ToString().Equals("y"))
				{
					Console.WriteLine("Waiting on acquisition stream to open");
					Thread.CurrentThread.Join(1000);
				}
				Console.WriteLine("MSScan Arrive event listener added");
				orbitrap.MsScanArrived += Orbitrap_MsScanArrived;


                int durationCounter = 0;
                while (durationCounter < GlobalVar.listeningDuration && (ExclusionExplorer.IsListening()))
                {
                    Thread.CurrentThread.Join(1000); //does the same thing as Thread.Sleep() but Join
                                                     //allows standard sendmessage pumping and COM to continue
                    durationCounter++;
                }
                orbitrap.MsScanArrived -= Orbitrap_MsScanArrived;
				orbitrap.AcquisitionStreamClosing -= Orbitrap_AcquisitionStreamClosing;
				orbitrap.AcquisitionStreamOpening -= Orbitrap_AcquisitionStreamOpening;

                DataProcessor.EndProcessing();
                DataProcessingThread.Join(); //wait until dataProcessor finishes processing/outputing the scan
                                             //queue then returns to Main thread;
			}
		}
		bool acquisitionStreamOpened = false;
        
		//The IMsScan will arrive at this method
		//It will be sent to DataProcessor to be parsed into a Scan object, and the IMsScan will be disposed to prevent blocking shared memory
		private void Orbitrap_MsScanArrived(object sender, MsScanEventArgs e)
		{
			using (IMsScan scan = (IMsScan) e.GetScan())	// caution! You must dispose this, or you block shared memory!
			{
				log.Debug("\n{0:HH:mm:ss,fff} scan with {1} centroids arrived", DateTime.Now, scan.CentroidCount);
                DataProcessor.ParseIMsScan(scan);
			}
		}

		private void Orbitrap_AcquisitionStreamClosing(object sender, EventArgs e)
		{
            log.Info(String.Format("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream closed (end of method)"));
			log.Info("press y to continue finish processing");
			while (!Console.ReadKey().KeyChar.ToString().Equals("y"))
			{
			}
			ExclusionExplorer.EndMealTimeMS();
		}

		private void Orbitrap_AcquisitionStreamOpening(object sender, MsAcquisitionOpeningEventArgs e)
		{
			
            if (GlobalVar.SetExclusionTable)
            {
                m_methods.ReplaceTable(1, 13 /* must be != 0 */, m_replacementTable);
                log.Info("Replaced the table");
            }
            WriterClass.writePrintln(String.Format("\n{0:HH:mm:ss,fff} {1}", DateTime.Now, "Acquisition stream opens (start of method)"));
			acquisitionStreamOpened = true;





		}

        private ITable CreateReplacementTable()
        {
            WriterClass.writeln("Exclusion Table Info:");
            // Show possible columns, their names, help and value options
            ITable replacementTable = m_methods.CreateTable(typeof(IExclusionTable));
            foreach (ITableColumnDescription col in replacementTable.ColumnInfo)
            {
                log.Info("{0,-12} {1,-25} {2}", col.Name, col.Selection, (col.Help ?? "").Trim().Replace("\r", "").Replace("\n", "; "));
                WriterClass.writeln(String.Format("{0,-12} {1,-25} {2}", col.Name, col.Selection, (col.Help ?? "").Trim().Replace("\r", "").Replace("\n", "; ")));
                if (col.AcceptedHeaderNames.Length > 0)
                {
                    log.Info("{0,38} alternative names: {1}", "", string.Join(", ", col.AcceptedHeaderNames));
                    WriterClass.writeln(String.Format("{0,38} alternative names: {1}", "", string.Join(", ", col.AcceptedHeaderNames)));
                }
            }
            WriterClass.Flush();
            WriterClass.writeln("======================================\n");
            WriterClass.writeln("Now attempting to write default value\n");
            


            foreach (ITableColumnDescription col in replacementTable.ColumnInfo)
            {

                log.Info("{0,-12} {1,-25} {2} Default Value: {3}; Optional: {4}", col.Name, col.Selection, (col.Help ?? "").Trim().Replace("\r", "").Replace("\n", "; "), col.DefaultValue, col.Optional.ToString());
                WriterClass.writeln(String.Format("{0,-12} {1,-25} {2} Default Value: {3}; Optional: {4}", col.Name, col.Selection, (col.Help ?? "").Trim().Replace("\r", "").Replace("\n", "; "), col.DefaultValue, col.Optional.ToString()));
                if (col.AcceptedHeaderNames.Length > 0)
                {
                    log.Info("{0,38} alternative names: {1}", "", string.Join(", ", col.AcceptedHeaderNames));
                    WriterClass.writeln(String.Format("{0,38} alternative names: {1}", "", string.Join(", ", col.AcceptedHeaderNames)));
                }
            }

            WriterClass.Flush();

            if (GlobalVar.SetExclusionTable)
            {
                log.Info("Attempting to set exclusio table values");
                //// Create a new table with two rows.
                ITableRow row = replacementTable.CreateRow();
                row.ColumnValues["Polarity"] = "Positive";  // must match, PRM has a polarity setting
                row.ColumnValues["Mass [m/z]"] = "700";
                replacementTable.Rows.Add(row);
                row = replacementTable.CreateRow();
                row.ColumnValues["Polarity"] = "Negative";
                row.ColumnValues["Mass [m/z]"] = "1600";
                replacementTable.Rows.Add(row);
            }
            else
            {
                replacementTable = null;
            }
           

            return replacementTable;
        }
    }
}
