using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MealTimeMS.Util
{
    class ExecuteShellCommand
    {

		public static String executeCommand(String command)
		{


			StringBuilder output = new StringBuilder();

			System.Diagnostics.Process process = new System.Diagnostics.Process();
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
			startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			//startInfo.CreateNoWindow = true;
#if LINUX
			startInfo.FileName = "/bin/bash";
			startInfo.Arguments = "-c " + "\""+command+"\"";

#else
			startInfo.FileName = "cmd.exe";
			startInfo.Arguments = "/C " + command;

#endif
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			process.StartInfo = startInfo;

			try
			{
				process.Start();


				StreamReader reader = process.StandardOutput;

				String line = "";
				while ((line = reader.ReadLine()) != null)
				{
					output.Append(line + "\n");
				}
				process.WaitForExit();

			}
			catch (Exception e)
			{
				Console.WriteLine("Command Exception: " + command);
				//Console.WriteLine(e.Message);
			}
			Console.WriteLine(output.ToString());
			return output.ToString();

		}
		//public static String executeCommand(String command)
		//{


		//	StringBuilder output = new StringBuilder();
		//	StringBuilder error = new StringBuilder();

		//	System.Diagnostics.Process process = new System.Diagnostics.Process();
		//	System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
		//	startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
		//	startInfo.FileName = "cmd.exe";
		//	startInfo.Arguments = "/C " + command;
		//	startInfo.UseShellExecute = false;
		//	startInfo.RedirectStandardOutput = true;
		//	startInfo.RedirectStandardError = true;

		//	process.StartInfo = startInfo;
		//	using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
		//	using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
		//	{
		//		process.OutputDataReceived += (sender, e) => {
		//			if (e.Data == null)
		//			{
		//				outputWaitHandle.Set();
		//			}
		//			else
		//			{
		//				output.AppendLine(e.Data);
		//			}
		//		};
		//		process.ErrorDataReceived += (sender, e) =>
		//		{
		//			if (e.Data == null)
		//			{
		//				errorWaitHandle.Set();
		//			}
		//			else
		//			{
		//				error.AppendLine(e.Data);
		//			}
		//		};

		//		process.Start();

		//		process.BeginOutputReadLine();
		//		process.BeginErrorReadLine();

		//		if (process.WaitForExit(timeout) &&
		//			outputWaitHandle.WaitOne(timeout) &&
		//			errorWaitHandle.WaitOne(timeout))
		//		{
		//			// Process completed. Check process.ExitCode here.
		//		}
		//		else
		//		{
		//			// Timed out.
		//		}
		//	}



		//	return output.ToString();

		//}

		/// <param name="ogfilePath">original file path.</param>
		/// <param name="newDirectory">new directory.</param>
		public static void MoveFile(  String filePath, String newDirectory  )
		{
#if LINUX
			String command = "mv " + Path.GetFullPath(filePath) + " " + newDirectory;
#else
			String command = "move " + Path.GetFullPath(filePath) + " " + newDirectory;
#endif
			executeCommand(command);
			return;
		}

		public static void deleteFile(String fileName)
		{
			String command = "rm " + fileName;
			ExecuteShellCommand.executeCommand(command);
		}

		public static void CopyFile(String filePath, String newDirectory)
		{
#if LINUX
			String command = "cp " + Path.GetFullPath(filePath) + " " + newDirectory;

#else
			String command = "copy " + Path.GetFullPath(filePath) + " " + newDirectory;

#endif
			executeCommand(command);
			return;
		}

	}
}
