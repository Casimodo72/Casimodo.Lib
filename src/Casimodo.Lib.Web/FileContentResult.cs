using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Casimodo.Lib.Web
{
    /// <summary>
    /// A FileContentResult that supports the "inline" content-disposition.
    /// </summary>
    public class FileContentResult2 : FileContentResult
    {
        public FileContentResult2(byte[] fileContent, string contentType, string fileName, bool inline)
            : base(fileContent, contentType)
        {
            IsInline = inline;
            FileDownloadName = fileName;
        }

        public bool IsInline { get; set; }

        public string FileName { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = ContentType;

            if (!string.IsNullOrEmpty(FileDownloadName))
            {
                context.HttpContext.Response.AddHeader("Content-Disposition", new ContentDisposition
                {
                    FileName = FileDownloadName,
                    Inline = IsInline
                }.ToString());
            }

            WriteFile(response);
        }
    }
}
