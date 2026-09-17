using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PdfDigitalSignatureAndSecurityDemo.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;

namespace PdfDigitalSignatureAndSecurityDemo.Services
{
    /// <summary>
    /// Reads signature information from a signed PDF using the Syncfusion .NET PDF library.
    /// </summary>
    public class PdfValidationService
    {
        private readonly IWebHostEnvironment _env;

        public PdfValidationService(IWebHostEnvironment env)
        {
            _env = env;
        }      

        private string SamplePath(string fileName) =>
            Path.Combine(_env.WebRootPath, "SampleFiles", fileName);

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
        /// Loads certificates from uploaded files and default root certificate.
        /// Returns a collection of X509Certificate2 objects for signature validation.
        /// </summary>
        private X509Certificate2Collection LoadCertificates(IFormFileCollection? uploadedCertificates)
        {
            var certificateCollection = new X509Certificate2Collection();

            // Always load the default root certificate
            var rootCertPath = Path.Combine(_env.ContentRootPath, "Certificates", "Rootcertificate.cer");
            if (File.Exists(rootCertPath))
            {
                try
                {
                    var rootCert = new X509Certificate2(rootCertPath);
                    certificateCollection.Add(rootCert);
                }
                catch (Exception ex)
                {
                    // Log or handle the error, but don't fail - continue with validation
                    System.Diagnostics.Debug.WriteLine($"Failed to load root certificate: {ex.Message}");
                }
            }

            // Load any uploaded certificates
            if (uploadedCertificates != null && uploadedCertificates.Count > 0)
            {
                foreach (var certFile in uploadedCertificates)
                {
                    if (certFile.Length > 0)
                    {
                        var tempPath = Path.Combine(Path.GetTempPath(),
                            $"{Guid.NewGuid():N}_{Path.GetFileName(certFile.FileName)}");
                        try
                        {
                            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                            {
                                certFile.CopyTo(fs);
                            }
                            var cert = new X509Certificate2(tempPath);
                            certificateCollection.Add(cert);

                            // Clean up temp file
                            if (File.Exists(tempPath))
                            {
                                try { File.Delete(tempPath); }
                                catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load certificate {certFile.FileName}: {ex.Message}");
                        }
                    }
                }
            }

            return certificateCollection;
        }

        /// <summary>
        /// Opens the supplied signed PDF and produces a human-readable summary
        /// of every signature field found in the document.
        /// </summary>
        public string ReadSignatureInformation(ValidateSignatureViewModel model)
        {
            var pdfPath = ResolveInput(model.SignedPdfFile, "SignedDocument.pdf");
            var certificates = LoadCertificates(model.CertificateFiles);

            using var inputStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Open the PDF. If the user-supplied file is encrypted without a password
            // we surface a friendly message instead of throwing.
            PdfLoadedDocument? loadedDocument = null;
            try
            {
                loadedDocument = new PdfLoadedDocument(inputStream);
            }
            catch (PdfInvalidPasswordException)
            {
                return "The supplied PDF is password-protected. Please upload an unencrypted copy " +
                       "or extend the controller to forward the open password.";
            }
            catch (Exception ex)
            {
                return $"Failed to open the PDF: {ex.Message}";
            }

            using (loadedDocument)
            {
                var sb = new StringBuilder();
                sb.AppendLine("========== Signature Information ==========");
                sb.AppendLine($"File: {Path.GetFileName(pdfPath)}");
                sb.AppendLine($"Pages: {loadedDocument.Pages.Count}");

                // Locate the first AcroForm signature field, if any.
                if (loadedDocument.Form == null || loadedDocument.Form.Fields.Count == 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("No AcroForm fields found in this document.");
                    return sb.ToString();
                }

                sb.AppendLine();

                int fieldCount = 0;
                bool foundSignatureField = false;

                // Process all signature fields
                foreach (var field in loadedDocument.Form.Fields)
                {
                    if (field is not PdfLoadedSignatureField sigField)
                        continue;

                    foundSignatureField = true;
                    fieldCount++;

                    sb.AppendLine($"--- Signature Field {fieldCount} ---");
                    sb.AppendLine($"Name             : {sigField.Name}");
                    sb.AppendLine($"Is Signed        : {sigField.IsSigned}");

                    if (!sigField.IsSigned)
                    {
                        sb.AppendLine("Status: UNSIGNED - This signature field has not been signed yet.");
                        sb.AppendLine();
                        continue;
                    }

                    if (sigField.Signature == null)
                    {
                        sb.AppendLine("Status: UNSIGNED - No signature data available.");
                        sb.AppendLine();
                        continue;
                    }

                    var signature  = sigField.Signature;
                    var cert       = signature.Certificate;
                    var statusInfo = TryGetSignatureStatus(sigField, certificates, out var statusMessage)
                                     ? statusMessage
                                     : "(trust chain not provided – integrity details unavailable)";

                    sb.AppendLine();
                    sb.AppendLine("--- Signature Details ---");
                    sb.AppendLine($"Signed Name      : {signature.SignedName}");
                    sb.AppendLine($"Signed Date      : {signature.SignedDate:yyyy-MM-dd HH:mm:ss zzz}");
                    sb.AppendLine($"Location         : {signature.LocationInfo}");
                    sb.AppendLine($"Reason           : {signature.Reason}");
                    sb.AppendLine($"Contact Info     : {signature.ContactInfo}");
                    sb.AppendLine($"Digest Algorithm : {signature.Settings.DigestAlgorithm}");
                    sb.AppendLine($"Crypto Standard  : {signature.Settings.CryptographicStandard}");

                    sb.AppendLine();
                    sb.AppendLine("--- Signer Certificate ---");
                    sb.AppendLine($"Subject          : {cert.SubjectName}");
                    sb.AppendLine($"Issuer           : {cert.IssuerName}");
                    sb.AppendLine($"Valid From       : {cert.ValidFrom:yyyy-MM-dd}");
                    sb.AppendLine($"Valid To         : {cert.ValidTo:yyyy-MM-dd}");

                    sb.AppendLine();
                    sb.AppendLine("--- Validation ---");
                    sb.AppendLine(statusInfo);
                    sb.AppendLine();
                }

                if (!foundSignatureField)
                {
                    sb.AppendLine("No digital signature fields were found in this document.");
                    return sb.ToString();
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Best-effort signature validation. Uses supplied root/intermediate certificates
        /// to validate the signature chain. If certificates are provided, returns Valid/Unknown status.
        /// </summary>
        private static bool TryGetSignatureStatus(PdfLoadedSignatureField field, X509Certificate2Collection? certificates, out string message)
        {
            try
            {
                PdfSignatureValidationResult result;

                // If certificates are provided, pass them to ValidateSignature for chain validation
                if (certificates != null && certificates.Count > 0)
                {
                    result = field.ValidateSignature(certificates);
                }
                else
                {
                    result = field.ValidateSignature();
                }

                message = $"Signature Status : {result.SignatureStatus}" +
                          $"\nDocument Modified: {result.IsDocumentModified}" +
                          $"\nSignature Alg.   : {result.SignatureAlgorithm}" +
                          $"\nDigest Alg.      : {result.DigestAlgorithm}";
                return true;
            }
            catch
            {
                message = string.Empty;
                return false;
            }
        }
    }
}
