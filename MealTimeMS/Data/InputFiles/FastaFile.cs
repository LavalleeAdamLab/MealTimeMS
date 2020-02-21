using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.Graph;

namespace MealTimeMS.Data.InputFiles
{

  

    public class FastaFile
    {
        private String fileName;
        private Dictionary<String, Protein> proteins;

        public FastaFile(String _fileName, Dictionary<String, Protein> _proteins)
        {
            fileName = _fileName;
            proteins = _proteins;
        }

        public Dictionary<String, Protein> getAccessionToFullSequence()
        {
            return proteins;
        }

        public String getFileName()
        {
            return fileName;
        }

        override
        public String ToString()
        {
            return "FastaFile{FileName:" + fileName + ", Number_of_sequences:" + proteins.Count + "}";
        }
    }

}
