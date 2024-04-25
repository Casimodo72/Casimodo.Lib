using Casimodo.Web.HtmlToPdf;
using Microsoft.Playwright;

var builder = WebApplication.CreateBuilder(args);

var playwright = await Playwright.CreateAsync();
var chromiumBrowser = await playwright.Chromium.LaunchAsync(new()
{
    Headless = true,
    Devtools = false,
    Args = new List<string>() { "--no-sandbox" }
});
builder.Services.AddSingleton(chromiumBrowser);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(builder =>
    {
        builder
            .AllowAnyOrigin()
            //.WithOrigins("https://localhost:5001")
            //.AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    }));

var app = builder.Build();

app.UseCors();

// TODO: REMOVE:
app.UseHttpsRedirection();

app.MapGet("", () =>
{
    return Results.Ok("OK");
});

app.MapPost("/convert", async (HtmlToPdfInput input, [Microsoft.AspNetCore.Mvc.FromServices] IBrowser chromiumBrowser) =>
{
    var context = await chromiumBrowser.NewContextAsync(new BrowserNewContextOptions()
    {
        JavaScriptEnabled = false
    });
    try
    {
        var page = await context.NewPageAsync();
        await page.SetContentAsync(input.Content ?? "");

        // Docs: https://playwright.dev/docs/api/class-page#page-pdf
        var pdfBytes = await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            HeaderTemplate = input.Header,
            FooterTemplate = input.Footer,
            PrintBackground = true
        });

        return Results.File(pdfBytes, "application/pdf");
    }
    finally
    {
        // TODO: Chromium might have a memory leak.
        //   See https://github.com/microsoft/playwright/issues/6319
        //   and https://github.com/microsoft/playwright/issues/8775
        await context.CloseAsync();
    }
});

app.Run();
