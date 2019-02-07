using DinkToPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Casimodo.Lib.WebControllers
{
    public static class HttpRequestExtensions
    {
        public static async Task<byte[]> ReadBodyAsByteArrayAsync(this HttpRequest request, int capacity = 4096)
        {
            using (var ms = new MemoryStream(capacity))
            {
                await request.Body.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        [HttpPost]
        [Route("ConvertHtmlToPdf")]
        public async Task<IActionResult> ConvertHtmlToPdf(
            [FromServices] DinkToPdf.Contracts.IConverter converter)
        {
            try
            {
                // TODO: REMOVE? Decompress
#if (false)
                // Unzip            
                using (var outms = new MemoryStream(data.Length))
                using (var inms = new MemoryStream(data))
                using (var zip = new ZipInputStream(inms))
                {
                    zip.GetNextEntry();
                    StreamHelper.CopyStream(zip, outms);
                    data = outms.ToArray();
                }
#endif
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                        Outline = false,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4, // A4Plus
                    },
                    Objects = {
                        new ObjectSettings() {
                            RawContent = await Request.ReadBodyAsByteArrayAsync(),
                            // TODO: ISSUE: Setting PagesCount to false produces a DevidedByZeroException.
                            //   See https://github.com/rdvojmoc/DinkToPdf/issues/22
                            PagesCount = true,
                            ProduceForms = false,
                            IncludeInOutline = false,
                            WebSettings = { DefaultEncoding = "utf-8" },
                            HeaderSettings = {
                                FontSize = 6,
                                Right = "", //"Seite [page] von [toPage]",                              
                                Spacing = 1 // 2.812
                            }
                        }
                    }
                };

                return new FileContentResult(converter.Convert(doc), "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $@"Failed to generate a PDF document: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
