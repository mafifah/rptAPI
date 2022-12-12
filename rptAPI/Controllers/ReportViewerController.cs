using BoldReports.Web;
using BoldReports.Web.ReportViewer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.Json.Nodes;

namespace rptAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [EnableCors("AllowAllOrigins")]
    public class ReportViewerController : Controller, IReportController
    {
        private IMemoryCache _cache;

        private IWebHostEnvironment _hostingEnvironment;
		Dictionary<string, object> jsonArray = null;
		public ReportViewerController(IMemoryCache memoryCache,
            IWebHostEnvironment hostingEnvironment)
        {
            _cache = memoryCache;
            _hostingEnvironment = hostingEnvironment;
        }

        [NonAction]
        public void OnInitReportOptions(ReportViewerOptions reportOption)
        {
            string basePath = _hostingEnvironment.WebRootPath;

            FileStream reportStream = new(basePath + @".\Resources\" + reportOption.ReportModel.ReportPath + ".rdl", FileMode.Open, FileAccess.Read);
            reportOption.ReportModel.Stream = reportStream;
        }

        [NonAction]
        public void OnReportLoaded(ReportViewerOptions reportOption)
        {
            var parameters = new List<ReportParameter>();
            if (jsonArray != null && jsonArray.ContainsKey("parameters"))
            {
                var parameter1 = jsonArray["parameters"].ToString();
                parameters = JsonConvert.DeserializeObject<List<ReportParameter>>(parameter1);
            }

            reportOption.ReportModel.Parameters = parameters;
		}

		[ActionName("GetResource")]
        [AcceptVerbs("GET")]
        public object GetResource(ReportResource resource)
        {
            return ReportHelper.GetResource(resource, this, _cache);
        }

        [HttpGet]
        public object PostFormReportAction()
        {
            return ReportHelper.ProcessReport(null, this, _cache);
        }

        [HttpPost]
        public object PostReportAction([FromBody]Dictionary<string, object> jsonResult)
        {
			jsonArray = jsonResult;
			return ReportHelper.ProcessReport(jsonResult, this, _cache);
        }

        [HttpPost]
        public object Export([FromBody] Dictionary<string, object> exportDetails)
        {
            string _token = exportDetails["reportViewerToken"].ToString();
            var stream = ReportHelper.GetReport(_token, exportDetails["exportType"].ToString(), this, _cache);
            stream.Position = 0;
            // Steps to generate PDF report using Report Writer.
            MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            byte[] data = memoryStream.ToArray();
            string file = Convert.ToBase64String(data, 0, data.Length);
            return file;
        }
    }
}
