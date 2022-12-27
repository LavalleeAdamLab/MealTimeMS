using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace MealTimeMS.Util
{
    public class HttpClientWrapper
    {
        private static readonly HttpClient client = new HttpClient();
    }
}
