using PdfDigitalSignatureAndSecurityDemo.Models;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;

namespace PdfDigitalSignatureAndSecurityDemo.Services
{
    /// <summary>
    /// Handles PDF signing + security (encryption, permissions, form flattening, visible signature image).
    /// Uses the Syncfusion .NET PDF library.
    /// </summary>
    public class PdfSecurityService
    {
        private readonly IWebHostEnvironment _env;

        public PdfSecurityService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Resolves a default sample file under wwwroot/SampleFiles.
        /// </summary>
        private string SamplePath(string fileName) =>
            Path.Combine(_env.WebRootPath, "SampleFiles", fileName);

        /// <summary>
        /// Resolves an uploaded file (saves to a temp path) or returns the bundled default.
        /// </summary>
        private string ResolveInput(IFormFile? uploaded, string defaultFile)
        {
            if (uploaded != null && uploaded.Length > 0)
            {
                var tempPath = Path.Combine(Path.GetTempPath(),
                    $"{Guid.NewGuid():N}_{Path.GetFileName(uploaded.FileName)}");
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    uploaded.CopyTo(fs);
                }
                return tempPath;
            }

            var defaultPath = SamplePath(defaultFile);
            if (!File.Exists(defaultPath))
                throw new FileNotFoundException(
                    $"Default sample file '{defaultFile}' was not found at {defaultPath}.");
            return defaultPath;
        }

        /// <summary>
        /// Signs the supplied PDF and applies the requested security options.
        /// </summary>
        public byte[] SignAndSecure(SignPdfViewModel model)
        {
            // ---------- 1. Resolve inputs ----------
            var pdfPath         = ResolveInput(model.PdfFile,            "Input.pdf");
            var pfxPath         = ResolveInput(model.CertificateFile, "PDFCertificate.pfx");
            var signatureImgPath = ResolveInput(model.SignatureImage, "signature.png");

            var certPassword = !string.IsNullOrWhiteSpace(model.CertificatePassword)
                ? model.CertificatePassword
                : "syncfusion";

            // ---------- 2. Open the existing PDF ----------
            using var inputStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var loadedDocument = new PdfLoadedDocument(inputStream);

            // ---------- 3. Apply security (encryption + permissions) ----------
            if (model.EncryptPdf && !string.IsNullOrWhiteSpace(model.OpenPassword))
            {
                // settings survive document.Save so the output is opened/edited per the flags.
                var security = loadedDocument.Security;
                security.KeySize = PdfEncryptionKeySize.Key256Bit;
                security.Algorithm = PdfEncryptionAlgorithm.AES;
                security.UserPassword  = model.OpenPassword;
                security.OwnerPassword = string.IsNullOrWhiteSpace(model.OpenPassword)
                    ? "owner"
                    : model.OpenPassword;
                // Compute the permission set. Start with FullQualityPrint + AccessibilityCopy
                var permissions = PdfPermissionsFlags.Print
                   | PdfPermissionsFlags.EditContent
                   | PdfPermissionsFlags.EditAnnotations
                   | PdfPermissionsFlags.FillFields
                   | PdfPermissionsFlags.AssembleDocument
                   | PdfPermissionsFlags.FullQualityPrint;

                if (model.DisablePrinting)
                {
                    permissions &= ~PdfPermissionsFlags.Print;
                    permissions &= ~PdfPermissionsFlags.FullQualityPrint;
                }

                if (model.DisableCopying)
                {
                    permissions &= ~PdfPermissionsFlags.CopyContent;
                    permissions &= ~PdfPermissionsFlags.AccessibilityCopyContent;
                }

                if (model.DisableEditing)
                {
                    permissions &= ~PdfPermissionsFlags.EditContent;
                    permissions &= ~PdfPermissionsFlags.EditAnnotations;
                    permissions &= ~PdfPermissionsFlags.FillFields;
                }

                // If the user disabled all common permissions, keep the document at least
                // accessible to accessibility tools.
                if (model.EncryptPdf) security.Permissions = permissions;
            }

            // ---------- 4. Flatten form fields (if requested) ----------
            if (model.FlattenFormFields && loadedDocument.Form != null)
            {
                loadedDocument.Form.FlattenFields();
            }

            // ---------- 5. Add a digital signature (invisible + optional visible image) ----------
            // Load the first page
            PdfLoadedPage page = loadedDocument.Pages[0] as PdfLoadedPage;

            // Open the PFX certificate.
            using var certStream = new FileStream(pfxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var certificate = new PdfCertificate(certStream, certPassword);

            // Place the visible signature at the bottom-right of the last page.
            var sigBounds = new RectangleF(227.6355f, 675.795044f, 150.57901f, 32.58f);

            PdfSignature signature = new PdfSignature(loadedDocument, page, certificate, "Signature")
            {
                Bounds       = sigBounds,
                ContactInfo  = "support@example.com",
                LocationInfo = "Office",
                Reason       = "Document approval"
            };
            signature.Settings.DigestAlgorithm        = DigestAlgorithm.SHA256;
            signature.Settings.CryptographicStandard  = CryptographicStandard.CADES;

            // ---------- 6. Optional visible signature image overlay ----------
            if (model.AddVisibleSignature && File.Exists(signatureImgPath))
            {
                using var imgStream = new FileStream(signatureImgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var signatureImage = new PdfBitmap(imgStream);
                signature.Appearance.Normal.Graphics.DrawImage(
                    signatureImage,
                    new RectangleF(0, 0, sigBounds.Width, sigBounds.Height));
            }
            else
            {
                // Fall back to a plain text appearance so the field is still recognisable.
                var font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                signature.Appearance.Normal.Graphics.DrawString(
                    "Signed digitally",
                    font, PdfBrushes.Black, new PointF(5, 5));
            }
            // ---------- 7. Save to memory and return ----------
            using var outputStream = new MemoryStream();
            loadedDocument.Save(outputStream);
            loadedDocument.Close(true);
            return outputStream.ToArray();
        }
    }
}
