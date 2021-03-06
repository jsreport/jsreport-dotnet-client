﻿using jsreport.Types;
using System.Net.Http;

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
