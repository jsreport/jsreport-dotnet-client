using jsreport.Types;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace jsreport.Client
{
    public class ReportHttp : Report
    {
        /// <summary>
        /// The full response
        /// </summary>
        public HttpResponseMessage Response { get; set; }
    }
}
