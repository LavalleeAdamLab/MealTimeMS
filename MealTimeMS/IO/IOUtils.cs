using System.IO;

namespace MealTimeMS.IO
{
  using System;

  public class IOUtils {
	/*
	 * Gets the absolute file path of an input file
	 */
	public static String getAbsolutePath(String file_name) {
		
		return Path.GetFullPath(file_name);
	}

	/*
	 * Essentially gets the base name of the file without file extension Use with
	 * caution... Removes everything after the last '.'
	 */
	public static String removeFileExtention(String file_name) {
		String output = file_name;
		if (file_name.Contains(".")) {
			output = file_name.Substring(0, file_name.LastIndexOf('.'));
		}
		return output;
	}

	/*
	 * Gets the base name without the filepaths
	 * TODO change this for Windows or Mac
	 */
	public static String getBaseName(String file_name) {
		//String withoutFileExtention = removeFileExtention(file_name);
		//String[] split = withoutFileExtention.Split("/".ToCharArray());
		//return split[split.Length - 1];
		return Path.GetFileNameWithoutExtension(file_name);
	}

	/*
	 * Gets the directory this file is from
	 */
	public static String getDirectory(String file_name) {
//		File f = new File(file_name);
//		return f.getParent();
		return Path.GetDirectoryName(file_name);
	}

	/*
	 * Replaces the field after the tag with the new value
	 */
	public static String replaceField(String startTag, String endTag, String line, String value) {
		int beginIndex = line.IndexOf(startTag) + startTag.Length;
		int endIndex = line.IndexOf(endTag, beginIndex);
		String pre = line.Substring(0, beginIndex);
		String post = line.Substring(endIndex);
		return pre + value + post;
	}

	/*
	 * Checks if the file path starts with "/" (macOS) or "x:\" (windows) Needs
	 * absolute file path on some files... but this handles issues with checking if
	 * the file is valid if it's not even on the same computer...
	 */
	public static bool isAbsPath(String file_path) {
		// valid absolute file path for macOS
		if (file_path.StartsWith("/")) {
			return true;
		}

		// valid absolute file path for windows
		if (file_path.Substring(1).StartsWith(":\\")) {
			return true;
		}

		return false;
	}

	/*
	 * Checks if the file path is a folder
	 */
	public static bool isFolder(String file_path) {
//		File file = new File(file_path);
//		return file.isDirectory();
		return Directory.Exists(file_path);
	}

	/*
	 * TODO WARNING, assumes we only have 2 significant figures
	 */
	public static String formatDouble(double d) {
		return String.Format("%.2f", d);
	}

	/*
	 * TODO WARNING, assumes we don't have > 999 experiments
	 */
	public static String padExperimentNum(int num) {
		return padExperimentNum(num, 101);
	}

	public static String padExperimentNum(int num, int maxNumExperiments) {
		String numExperimentsString = "" + maxNumExperiments;
		String numString = "" + num;
		return numString.PadLeft(numExperimentsString.Length, '0');
	}

}

}