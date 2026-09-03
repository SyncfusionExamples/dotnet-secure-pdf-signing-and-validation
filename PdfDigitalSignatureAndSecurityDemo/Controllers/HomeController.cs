using Microsoft.AspNetCore.Mvc;

namespace PdfDigitalSignatureAndSecurityDemo.Controllers
{
    /// <summary>
    /// Landing page – just links to the two demo pages.
    /// </summary>
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
