using System;
using System.Collections.Generic;
namespace MealTimeMS.Data
{
    //This class is really should be called ms2File - stores the ms2 file info
    public class MZMLFile
    {
        private String fileName;
        private List<Spectra> spectraArray;

        public MZMLFile(String _fileName, List<Spectra> _spectraArray)
        {
            fileName = _fileName;
            spectraArray = _spectraArray;
        }

        public List<Spectra> getSpectraArray()
        {
            return spectraArray;
        }

        public String getFileName()
        {
            return fileName;
        }

        override
        public String ToString()
        {
            return "MZMLFile{FileName:" + fileName + ", Number_of_spectra:" + spectraArray.Count + "}";
        }
    }
}
