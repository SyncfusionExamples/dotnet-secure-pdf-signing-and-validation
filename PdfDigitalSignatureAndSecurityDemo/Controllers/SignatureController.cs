using System;
using Microsoft.AspNetCore.Mvc;
using PdfDigitalSignatureAndSecurityDemo.Models;
using PdfDigitalSignatureAndSecurityDemo.Services;

namespace PdfDigitalSignatureAndSecurityDemo.Controllers
{
    /// <summary>
    /// Controller for both pages:
    ///  - GET  /Signature/Sign     – show the "Sign & Secure PDF" form
    ///  - POST /Signature/Sign     – sign & secure the PDF, return it as a download
    ///  - GET  /Signature/Validate – show the "Validate Signature" form
    ///  - POST /Signature/Validate – parse the signed PDF and show signature info
    /// </summary>
    public class SignatureController : Controller
    {
        private readonly PdfSecurityService _securityService;
        private readonly PdfValidationService _validationService;

        public SignatureController(PdfSecurityService securityService,
                                   PdfValidationService validationService)
        {
            _securityService  = securityService;
            _validationService = validationService;
        }

        // ---------------- Page 1 : Sign & Secure ----------------

        [HttpGet]
        public IActionResult Sign() => View(new SignPdfViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sign(SignPdfViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // Generate the PDF and stream it back as a download so the user does
                // not have to click a second "Download" button after the post.
                var bytes = _securityService.SignAndSecure(model);
                return File(bytes, "application/pdf", model.OutputFileName);
            }
            catch (Exception ex)
            {
                model.Message = $"Error: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadSigned(SignPdfViewModel model)
        {
            // Re-runs the pipeline so the in-memory byte[] is not required to round-trip
            // through hidden form fields (keeps the page under the 4 KB request-form limit).
            try
            {
                var bytes = _securityService.SignAndSecure(model);
                return File(bytes, "application/pdf", model.OutputFileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Sign));
            }
        }

        // ---------------- Page 2 : Validate Signature ----------------

        [HttpGet]
        public IActionResult Validate() => View(new ValidateSignatureViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Validate(ValidateSignatureViewModel model)
        {
            try
            {
                model.SignatureInformation = _validationService.ReadSignatureInformation(model);
                if (string.IsNullOrEmpty(model.Message) &&
                    model.SignatureInformation.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    model.Message = model.SignatureInformation;
                }
            }
            catch (Exception ex)
            {
                model.SignatureInformation = string.Empty;
                model.Message              = $"Error: {ex.Message}";
            }
            return View(model);
        }
    }
}
