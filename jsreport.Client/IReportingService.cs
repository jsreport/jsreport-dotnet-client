using jsreport.Shared;
using jsreport.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace jsreport.Client
{
    /// <summary>
    /// jsreport API .net Wrapper
    /// </summary>
    public interface IReportingService : IRenderService
    {
        /// <summary>
        /// Uri to jsreport server like http://localhost:2000/ or https://subdomain.jsreportonline.net 
        /// </summary>
        Uri ServiceUri { get; set; }

        /// <summary>
        /// Request list of recipes registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        Task<IEnumerable<string>> GetRecipesAsync();

        /// <summary>
        /// Request list of engines registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        Task<IEnumerable<string>> GetEnginesAsync();

        /// <summary>
        /// Request jsreport package version
        /// </summary>
        /// <returns></returns>
        Task<string> GetServerVersionAsync();

        /// <summary>
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// Boolean to indicate if compression should be enabled or not
        /// </summary>
        bool Compression { get; set; }

        /// <summary>
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Timeout for http client requests
        /// </summary>
        TimeSpan? HttpClientTimeout { get; set; }
    }
}