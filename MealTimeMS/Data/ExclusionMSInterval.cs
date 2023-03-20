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
        public static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            FloatFormatHandling = FloatFormatHandling.DefaultValue,
            NullValueHandling = NullValueHandling.Ignore
        };
    public int interval_id;
        public Nullable<int> charge = null;
        public Nullable<double> min_mass;
        public Nullable<double> max_mass;
        public Nullable<double> min_rt; //ExclusionMS takes time in seconds
        public Nullable<double> max_rt ;
        public Nullable<double> min_ook0 = null;
        public Nullable<double> max_ook0 = null;
        public Nullable<double> min_intensity = null;
        public Nullable<double> max_intensity = null;
        public ExclusionMSInterval(int _interval_id, double mass, double ppmTol, double rt, double rtWin)
        {
            interval_id = _interval_id;
            min_mass = (mass * (1.0 - ppmTol));
            max_mass = (mass * (1.0 + ppmTol));
            min_rt = (rt - rtWin) * 60.0; //ExclusionMS takes time in seconds
            max_rt = (rt + rtWin) * 60.0;
        }
        public ExclusionMSInterval (int _interval_id, double _min_mass, double _max_mass, double _min_rt, double _max_rt, bool thisparamDoesntMatterItsJustHereForOverload)
        {
            interval_id = _interval_id;
            min_mass = _min_mass;
            max_mass = _max_mass;
            min_rt = _min_rt;
            max_rt = _max_rt;
        }
        public ExclusionMSInterval(int _interval_id, double mass, double ppmTol, double rt, double rtWin, 
            double IM, double IMWin): this(_interval_id, mass, ppmTol, rt, rtWin)
        {
            min_ook0 = (IM - IMWin);
            max_ook0 = (IM + IMWin);

        }
        public ExclusionMSInterval(int _interval_id)
        {
            interval_id = _interval_id;
        }
        
        public String toJSONString()
        {
            return JsonConvert.SerializeObject(this, serializerSettings);
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
