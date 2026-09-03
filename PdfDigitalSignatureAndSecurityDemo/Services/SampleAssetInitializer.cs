using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Security;

namespace PdfDigitalSignatureAndSecurityDemo.Services
{
    /// <summary>
    /// On first start-up, materialises the default <c>Input.pdf</c> and
    /// <c>SignedDocument.pdf</c> files under <c>wwwroot/SampleFiles/</c>
    /// if they don't already exist. This makes the demo runnable out of the
    /// box without forcing the user to ship binary fixtures in source control.
    /// </summary>
    public class SampleAssetInitializer
    {
        private readonly IWebHostEnvironment _env;
        public SampleAssetInitializer(IWebHostEnvironment env) => _env = env;

        public void EnsureDefaults()
        {
            var sampleDir = Path.Combine(_env.WebRootPath, "SampleFiles");
            Directory.CreateDirectory(sampleDir);

            // 1. A simple, always-readable Input.pdf
            var inputPath = Path.Combine(sampleDir, "Input.pdf");
            if (!File.Exists(inputPath))
            {
                CreateSamplePdf(inputPath);
            }

            // 2. A pre-signed SignedDocument.pdf (uses the bundled certificate.pfx)
            var signedPath = Path.Combine(sampleDir, "SignedDocument.pdf");
            if (!File.Exists(signedPath))
            {
                CreateSignedSample(signedPath, sampleDir);
            }
        }

        // ------------------------------------------------------------------
        //  Build a tiny one-page PDF that explains how to use the demo.
        // ------------------------------------------------------------------
        private static void CreateSamplePdf(string outputPath)
        {
            using var doc = new PdfDocument();
            var page = doc.Pages.Add();
            var g     = page.Graphics;

            var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold);
            var bodyFont  = new PdfStandardFont(PdfFontFamily.Helvetica, 12);

            g.DrawString("Syncfusion PDF Sign & Secure – Sample Input",
                titleFont, PdfBrushes.DarkSlateBlue, new PointF(20, 20));

            g.DrawString(
                "This PDF was auto-generated the first time the demo was started. " +
                "Upload it on the Sign & Secure page, choose your options, and the " +
                "resulting signed & secured PDF will be returned to you.",
                bodyFont, PdfBrushes.Black,
                new RectangleF(20, 60, page.GetClientSize().Width - 40, 120));

            g.DrawString(
                "Features demonstrated:\n" +
                "  • Visible signature image on the last page\n" +
                "  • Invisible digital signature (PKCS#7 / CAdES, SHA-256)\n" +
                "  • AES-256 encryption with an open password\n" +
                "  • Permissions: disable print / copy / edit\n" +
                "  • Optional AcroForm flattening",
                bodyFont, PdfBrushes.DimGray, new PointF(20, 200));

            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            doc.Save(fs);
            doc.Close(true);
        }

        // ------------------------------------------------------------------
        //  Build a pre-signed PDF using the bundled certificate.pfx, so the
        //  Validate page has something to read even on the first run.
        // ------------------------------------------------------------------
        private static void CreateSignedSample(string outputPath, string sampleDir)
        {
            var pfxPath = Path.Combine(sampleDir, "certificate.pfx");
            if (!File.Exists(pfxPath))
            {
                // Without a real PFX, fall back to a plain PDF (Validate page
                // will then report "no signature field" – still valid output).
                CreateSamplePdf(outputPath);
                return;
            }

            // Use a self-signed certificate with a known password if the bundled
            // one cannot be opened (the project ships a placeholder).
            string password = "syncfusion";
            X509Certificate2? cert = null;
            try
            {
                cert = new X509Certificate2(pfxPath, password,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
            }
            catch
            {
                cert = null;
            }

            // Build a one-page PDF.
            using var doc = new PdfDocument();
            var page = doc.Pages.Add();
            var g     = page.Graphics;

            var font = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
            g.DrawString("Sample pre-signed document.",
                font, PdfBrushes.Black, new PointF(20, 20));
            g.DrawString("Generated automatically by the Sign & Secure demo.",
                font, PdfBrushes.DimGray, new PointF(20, 40));

            if (cert != null)
            {
                var pdfCert = new PdfCertificate(cert);
                var signature = new PdfSignature(doc, page, pdfCert, "Signature")
                {
                    Bounds       = new RectangleF(20, 80, 200, 60),
                    ContactInfo  = "support@example.com",
                    LocationInfo = "Demo",
                    Reason       = "Sample signed document"
                };
                signature.Settings.DigestAlgorithm       = DigestAlgorithm.SHA256;
                signature.Settings.CryptographicStandard = CryptographicStandard.CADES;
            }

            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            doc.Save(fs);
            doc.Close(true);
        }
    }
}
