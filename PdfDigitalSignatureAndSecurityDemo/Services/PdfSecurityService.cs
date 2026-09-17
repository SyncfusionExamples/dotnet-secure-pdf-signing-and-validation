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
        private string SamplePath(string fileName)
        {
            if (fileName.Contains(".pfx"))
            {
                return Path.Combine(_env.ContentRootPath, "Certificates", fileName);

            }
            else
                return Path.Combine(_env.WebRootPath, "SampleFiles", fileName);
        }

        /// <summary>
        /// Resolves an uploaded file (saves to a temp path) or returns the bundled default.
        /// </summary>
        private string ResolveInput(IFormFile? uploaded, string defaultFile)
        {
            if (uploaded != null && uploaded.Length > 0)
            {
                // Security: Validate file upload
                const long maxFileSize = 10 * 1024 * 1024; // 10 MB limit
                const string allowedExtension = ".pdf";
                
                if (uploaded.Length > maxFileSize)
                    throw new InvalidOperationException($"File size exceeds {maxFileSize / 1024 / 1024} MB limit.");
                
                var fileExtension = Path.GetExtension(uploaded.FileName).ToLowerInvariant();
                if (fileExtension != allowedExtension)
                    throw new InvalidOperationException($"Only {allowedExtension} files are allowed.");
                
                // Sanitize filename - use GUID to prevent path traversal
                var tempPath = Path.Combine(Path.GetTempPath(),
                    $"{Guid.NewGuid():N}.pdf");
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
            // Validate input
            if (string.IsNullOrWhiteSpace(model.OpenPassword) || model.OpenPassword.Length < 4)
                throw new InvalidOperationException("Password must be at least 4 characters.");
            
            // ---------- 1. Resolve inputs ----------
            var pdfPath         = ResolveInput(model.PdfFile,            "Input.pdf");
            var pfxPath         = ResolveInput(model.CertificateFile, "Certificate.pfx");
            var signatureImgPath = ResolveInput(model.SignatureImage, "signature.png");

            // Track temp files for cleanup
            var tempFilesToClean = new List<string>();
            try
            {
                // If these are uploaded files (not defaults), mark for cleanup
                if (model.PdfFile != null && model.PdfFile.Length > 0) tempFilesToClean.Add(pdfPath);
                if (model.CertificateFile != null && model.CertificateFile.Length > 0) tempFilesToClean.Add(pfxPath);
                if (model.SignatureImage != null && model.SignatureImage.Length > 0) tempFilesToClean.Add(signatureImgPath);

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
                    // Security: Generate a random owner password (not hardcoded)
                    security.OwnerPassword = Guid.NewGuid().ToString().Substring(0, 16);
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
            finally
            {
                // Clean up temporary files
                foreach (var tempFile in tempFilesToClean)
                {
                    try
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - file may be locked or already deleted
                        System.Diagnostics.Debug.WriteLine($"Failed to delete temp file {tempFile}: {ex.Message}");
                    }
                }
            }
        }
    }
}
