//using System;
//using System.IO;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Diagnostics;
//​
//​
//​
//namespace TaxonDB
//{
//    class Program
//    {
//        public static List<Taxon> TaxonList = new List<Taxon>();
//​
//​
//        static void Main(string[] args)
//        {
//​
//            Taxon.SetUpTaxonStatisVariables(@"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\PepToLCA\TaxonClassDb2col.txt");
//            //Taxon.taxon_key_db = Taxon.TaxonKeyDict(@"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\TaxonDB_test3532_2col.txt"); // taxon as a key TAXON[PEPTIDES]
//            //Taxon.peptide_taxon_db = Taxon.PeptideKeyDict(@"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\TaxonDB_test3532_2col.txt"); // peptides as a key 
//​
//​
//​
//            //Input is given
//            //string SampleInput = @"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\SampleInput1K.txt";
//            //var peptides = File.ReadAllLines(SampleInput);
//​
//​
//            //string LCAoutput_to_file = "TAXON | COUNT | PRESENT IN SAMPLE | DB PEPTIDES | SAMPLE PEPTIDES ";
//            //string LCApresent_taxon_file = "TAXON | COUNT | PRESENT IN SAMPLE | DB PEPTIDES | SAMPLE PEPTIDES";
//​
//            List<Taxon> TaxonList = new List<Taxon>();
//            foreach (var entry in Taxon.taxon_key_db.Keys)
//            {
//                TaxonList.Add(new Taxon(entry));
//​
//            }
//​
            
//            /*
//            foreach (string entry in peptides)
//            {
//                foreach (Taxon taxon in TaxonList)
//                {
//                    taxon.AddSamplePeptides(entry, taxon);
//                }
//            }
//​
//            foreach (var taxon in TaxonList)
//            {
//                Console.WriteLine("{0}, COUNT {1} | PRESENT IN THE SAMPLE {2} | DB PEPTIDES: {3} | SAMPLE PEPTIDES: {4}", taxon.getTaxonName(), taxon.getCount(), taxon.taxonPresent(), string.Join(", ", taxon.getDatabasePeptides()), string.Join(", ", taxon.getSamplePeptides()));
//                LCAoutput_to_file = LCAoutput_to_file + "\n" + taxon.getTaxonName() + " | " + taxon.getCount() + " | " + taxon.taxonPresent() + " | " + string.Join(", ", taxon.getDatabasePeptides()) + " | " + string.Join(", ", taxon.getSamplePeptides());
//​
//                if (taxon.taxonPresent() == true)
//                {
//                    LCApresent_taxon_file = LCApresent_taxon_file + "\n" + taxon.getTaxonName() + " | " + taxon.getCount() + " | " + taxon.taxonPresent() + " | " + string.Join(", ", taxon.getDatabasePeptides()) + " | " + string.Join(", ", taxon.getSamplePeptides());
//​
//                }
//            }
//​
//            File.WriteAllText(@"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\LCATaxonClassesOutputFile.txt", LCAoutput_to_file);
//            File.WriteAllText(@"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\LCAPresentTaxonFile.txt", LCApresent_taxon_file);
//        */
//            }
//    } 
//​
//​
//​
//​
//​
//​
//        public class Taxon
//    {
//​
//            private string name; // taxon name
//                                 //private String sequence; //peptide sequence --> this can be dictionary of string and Taxon
//        private String[] DatabasePeptides; //peptides from the DB which correspond to this taxon
//        private List<String> SamplePeptides; //list o fpeptides which are present in the SAMPLE (input) and correspond to this object taxon
//        private int count; // number of peptides found which correspond to this taxon (currect classified: " if 5 are found than the taxon is CIied 
//        private bool isPresent;// is the taxon present in the sample (True or False)
//        static public Dictionary<String, String[]> taxon_key_db; //= TaxonKeyDict(@"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\TaxonDB_test3532_2col.txt"); //can delete it and just declare because the actual inst happens in the MAIN
//        static public Dictionary<String, String[]> peptide_taxon_db; // = PeptideKeyDict(@"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\TaxonDB_test3532_2col.txt");
//            //static public String sample_input = @"c:\Users\Alona Petrova\Desktop\LAB2021\Metaproteome project_MAIN\UniPept_Project\SampleInput1K.txt";
//            // constructor 
//​
//            public static void SetUpTaxonStatisVariables(String dbFilePath)
//        {
//            taxon_key_db = TaxonKeyDict(dbFilePath); // taxon as a key TAXON[PEPTIDES]
//            peptide_taxon_db = PeptideKeyDict(dbFilePath); // peptides as a key 
//        }
//        public Taxon(string _name) // do I include all the properties?? 
//        {
//​
//                this.name = _name;
//            DatabasePeptides = taxon_key_db[_name];
//            isPresent = new bool();
//            count = 0; // INITIAL IS ZERO - right?
//            SamplePeptides = new List<string>();
//        }
//​
//            public string getTaxonName()
//        {
//            return name;
//        }
//​
//            public int getCount()
//        {
//            return count;
//        }
//​
//            public string[] getDatabasePeptides()
//        {
//            return DatabasePeptides;
//        }
//​
//            public bool taxonPresent()
//        {
//            return isPresent;
//        }
//​
//            public List<String> getSamplePeptides()
//        {
//            return SamplePeptides;
//        }
//​
//​
//            // parse through the sample input and add the peptides to the object
//            public void AddSamplePeptides(string entry, Taxon taxon) //how do we tell that the input is given
//        {
//                //var peptides = File.ReadAllLines(sample_input);
//​
//                //foreach (string entry in peptides)
//                //{
//                if (taxon.DatabasePeptides.Contains(entry))
//            {
//                taxon.SamplePeptides.Add(entry);
//                taxon.count++;
//                if (taxon.count >= 5)
//                {
//                    taxon.isPresent = true;
//​
//                    }
//            }
//            //}
//        }
//​
//​
//​
//​
//​
//​
//​
//            // Dictionary Taxon as a Key
//            public static Dictionary<String, String[]> TaxonKeyDict(string path_to_taxon_key_DB)
//        {
//            Dictionary<String, String[]> taxon_key_db = new Dictionary<string, string[]>(); // DB: TAXON:[PEPTIDE 1, PEPTIDE 2]
//                                                                                                // WORKING WITH A DICTIONARY TAXONs:PEPTIDES []
//​
//                // reading database to taxon_key_DB dictionary
//                taxon_key_db = File
//                    .ReadLines(path_to_taxon_key_DB)
//                    .Where(line => !string.IsNullOrWhiteSpace(line)) // To be on the safe side
//                     .Skip(1)  // If we want to skip the header (the very 1st line)
//                     .Select(line => line.Split('\t'))
//                     .GroupBy(items => items[1].Trim(),
//                        items => items[0])
//                     //.ToDictionary(x => x[0], x => x[1]); // key = peptide, value = array of taxons
//                     .ToDictionary(chunk => chunk.Key,
//                             chunk => chunk.ToArray());
//​
//                return taxon_key_db;
//​
//            }
//​
//​
//            public static Dictionary<String, String[]> PeptideKeyDict(string path_to_pep_key_DB)
//        {
//            Dictionary<String, String[]> peptide_taxon_db = new Dictionary<string, string[]>();
//​
//                peptide_taxon_db = File
//                 .ReadLines(path_to_pep_key_DB)
//                 .Where(line => !string.IsNullOrWhiteSpace(line)) // To be on the safe side
//                 .Skip(1)  // If we want to skip the header (the very 1st line)
//                 .Select(line => line.Split('\t'))
//                 .GroupBy(items => items[0].Trim(),
//                    items => items[1])
//                 //.ToDictionary(x => x[0], x => x[1]); // key = peptide, value = array of taxons
//                 .ToDictionary(chunk => chunk.Key,
//                    chunk => chunk.ToArray()); // key = peptide, value = array of taxons
//​
//                return peptide_taxon_db;
//        }
//​
//        }
//​
//    }
////}