using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MealTimeMS.Data.Graph;

namespace MealTimeMS.Data
{
    class ExclusionMSInterval
    {
        public int interval_id;
        public string charge ="";
        public string min_mass = "";
        public string max_mass = "";
        public string min_rt = ""; //ExclusionMS takes time in seconds
        public string max_rt = "";
        public string min_ook0 = "";
        public string max_ook0 = "";
        public string min_intensity = "";
        public string max_intensity = "";
        public ExclusionMSInterval(int _interval_id, double mass, double ppmTol, double rt, double rtWin)
        {
            interval_id = _interval_id;
            min_mass = (mass * (1.0 - ppmTol)).ToString();
            max_mass = (mass * (1.0 + ppmTol)).ToString();
            min_rt = ((rt - rtWin) * 60.0).ToString(); //ExclusionMS takes time in seconds
            max_rt = ((rt + rtWin) * 60.0).ToString();
        }
        public ExclusionMSInterval(int _interval_id, double mass, double ppmTol, double rt, double rtWin, 
            double IM, double IMWin): this(_interval_id, mass, ppmTol, rt, rtWin)
        {
            min_ook0 = (IM - IMWin).ToString();
            max_ook0 = (IM + IMWin).ToString();

        }
        public ExclusionMSInterval(int _interval_id)
        {
            interval_id = _interval_id;
        }
        
        public String toJSONString()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static List<String> getJSONStringsFromPeptide(Peptide pep, double ppmTol, double rtWin, double IMTol, bool useIonMobility = false)
        {
            int peptideID = pep.getPeptideID();
            double mass = pep.getMass();
            double rt = pep.getRetentionTime().getRetentionTimePeak();
            
            if (!useIonMobility|| pep.getIonMobility().Count==0)
            {
                var interval = new ExclusionMSInterval(peptideID, mass, ppmTol, rt, rtWin);
                return new List<String>() { interval.toJSONString() };
            }
            return null;
        }

        //Creates an empty interval object with only interval_id
        public static String getEmptyJSONStringFromID(int id)
        {
            return new ExclusionMSInterval(id).toJSONString();
        }
        
    }
}
