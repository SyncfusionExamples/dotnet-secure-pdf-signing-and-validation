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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
