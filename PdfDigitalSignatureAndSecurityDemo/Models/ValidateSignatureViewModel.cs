using Microsoft.AspNetCore.Http;

namespace PdfDigitalSignatureAndSecurityDemo.Models
{
    /// <summary>
    /// View model backing the "Validate Signature" page.
    /// </summary>
    public class ValidateSignatureViewModel
    {
        /// <summary>Optional uploaded signed PDF. If null, the bundled SignedDocument.pdf is used.</summary>
        public IFormFile? SignedPdfFile { get; set; }

        /// <summary>Human-readable text containing the signature information.</summary>
        public string SignatureInformation { get; set; } = string.Empty;

        /// <summary>Optional diagnostic / error message for the UI.</summary>
        public string? Message { get; set; }
    }
}
