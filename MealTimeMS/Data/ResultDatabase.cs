//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using MealTimeMS.Data;
//using MealTimeMS.IO;

//namespace MealTimeMS.Data
//{


//public class ResultDatabase
//    {

//        private Dictionary<int, IDs> identificationList;

//        public ResultDatabase(MZMLFile mzml, MZIDFile mzid)
//        {
//            identificationList = ResultDatabaseUtil.constructResultDatabase(mzml, mzid);
//        }

//        public ResultDatabase(Dictionary<int, IDs> _identificationList)
//        {
//            identificationList = _identificationList;
//        }

//        public IDs getID(int scan_num)
//        {
//            return identificationList[scan_num];
//        }

//        public List<IDs> getIDs()
//        {
//            return identificationList.Values.ToList();
//        }

//        public void writeResultDatabaseToFile(String file_path)
//        {
//            Writer.writeResultDatabaseToFile(file_path, this);
//        }

//        override
//        public String ToString()
//        {
//            return "ResultDatabase{num_scans=" + identificationList.Count + "}";
//        }

//    }

//}
