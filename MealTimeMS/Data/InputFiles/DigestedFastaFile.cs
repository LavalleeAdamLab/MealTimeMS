using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data;

namespace MealTimeMS.Data.InputFiles
{

    public class DigestedFastaFile
    {
        private String fileName;
        private List<DigestedPeptide> digestedPeptideArray;

        public DigestedFastaFile(String _fileName, List<DigestedPeptide> _digestedPeptideArray)
        {
            fileName = _fileName;
            digestedPeptideArray = _digestedPeptideArray;
        }

        public List<DigestedPeptide> getDigestedPeptideArray()
        {
            return digestedPeptideArray;
        }

        public String getFileName()
        {
            return fileName;
        }

        override
        public String ToString()
        {
            return "DigestedFastaFile{FileName:" + fileName + ", Number_of_sequences:" + digestedPeptideArray.Count() + "}";
        }
    }

}
