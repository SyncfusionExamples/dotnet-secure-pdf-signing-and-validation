using PdfDigitalSignatureAndSecurityDemo.Services;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Syncfusion license registration
// ---------------------------------------------------------------------------
// In production, register your Syncfusion license key via the
// SYNCFUSION_LICENSE_KEY environment variable, user secrets, or
// a SyncfusionLicense.txt file on disk. The key below is intentionally
// blank so the demo builds "out of the box" – apply your own key here
// (or set the env var) to remove the evaluation banner.
var licenseKey =
    Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY")
    ?? builder.Configuration["Syncfusion:LicenseKey"]
    ?? string.Empty;
if (!string.IsNullOrWhiteSpace(licenseKey))
{
    SyncfusionLicenseProvider.RegisterLicense(licenseKey);
}

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<PdfSecurityService>();
builder.Services.AddSingleton<PdfValidationService>();
builder.Services.AddSingleton<SampleAssetInitializer>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Materialise default sample files (Input.pdf, SignedDocument.pdf) on first run
// ---------------------------------------------------------------------------
app.Services.GetRequiredService<SampleAssetInitializer>().EnsureDefaults();

// ---------------------------------------------------------------------------
// Middleware
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Host the app under a virtual path so Url.Content("~/") and link generation
// resolve to /dotnet-secure-pdf-signing-and-validation
var basePath = "/dotnet-secure-pdf-signing-and-validation";
app.UsePathBase(basePath);

// If a request arrives without the expected PathBase (for example a user
// navigates to /Signature/Sign), redirect to the same path prefixed with
// the base path. This ensures bookmarks or absolute links to the root get
// normalized into the application's virtual path.
app.Use(async (context, next) =>
{
    if (!context.Request.PathBase.HasValue || context.Request.PathBase == PathString.Empty)
    {
        var redirectUrl = basePath + context.Request.Path + context.Request.QueryString;
        context.Response.Redirect(redirectUrl, permanent: false);
        return;
    }
    await next();
});

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    // CSP allows self + trusted CDNs for Bootstrap CSS
    var cspPolicy = "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline'; " +
                    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                    "font-src 'self' data: https://cdn.jsdelivr.net; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self' https:;";
    context.Response.Headers["Content-Security-Policy"] = cspPolicy;
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
