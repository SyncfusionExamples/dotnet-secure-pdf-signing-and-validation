using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PdfDigitalSignatureAndSecurityDemo.Models
{
    /// <summary>
    /// View model backing the "Sign & Secure PDF" page.
    /// Holds the optional uploaded PDF plus all configurable security options.
    /// </summary>
    public class SignPdfViewModel
    {
        // ---------- Inputs (all optional – defaults are served from wwwroot/SampleFiles) ----------

        /// <summary>PDF document to sign. If null/empty, the bundled Input.pdf is used.</summary>
        public IFormFile? PdfFile { get; set; }

        /// <summary>Optional override for the PFX certificate. Defaults to wwwroot/SampleFiles/Certificate.pfx.</summary>
        public IFormFile? CertificateFile { get; set; }

        /// <summary>Optional override for the certificate password.</summary>
        [DataType(DataType.Password)]
        public string? CertificatePassword { get; set; }

        /// <summary>Optional override for the visible signature image. Defaults to wwwroot/SampleFiles/Signature.png.</summary>
        public IFormFile? SignatureImage { get; set; }

        // ---------- Options ----------

        /// <summary>Add a visible signature image overlay on the last page.</summary>
        public bool AddVisibleSignature { get; set; } = true;

        /// <summary>Encrypt the output PDF (AES-256).</summary>
        public bool EncryptPdf { get; set; }

        /// <summary>Open password – required to view the document.</summary>
        [DataType(DataType.Password)]
        public string? OpenPassword { get; set; }

        /// <summary>Disable the Print permission.</summary>
        public bool DisablePrinting { get; set; }

        /// <summary>Disable the CopyContent permission.</summary>
        public bool DisableCopying { get; set; }

        /// <summary>Disable EditContent / EditAnnotations / FillFields permissions.</summary>
        public bool DisableEditing { get; set; }

        /// <summary>Flatten AcroForm fields in the output document.</summary>
        public bool FlattenFormFields { get; set; }

        // ---------- Output ----------

        /// <summary>Generated signed &amp; secured PDF as a download stream.</summary>
        public byte[]? OutputPdf { get; set; }

        /// <summary>Suggested filename for the generated PDF.</summary>
        public string OutputFileName { get; set; } = "SignedDocument.pdf";

        /// <summary>Status / diagnostic message for the UI.</summary>
        public string? Message { get; set; }
    }
}
