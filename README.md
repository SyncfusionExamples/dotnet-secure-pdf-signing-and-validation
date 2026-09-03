# Secure PDF Documents with Digital Signatures in ASP.NET Core Using Syncfusion® .NET PDF Library

A small ASP.NET Core 8 MVC sample that uses the **Syncfusion .NET PDF Library**
to sign and secure PDF documents, and to read back the signature information
from a signed file.

## Pages

| Page | Route | Description |
| --- | --- | --- |
| **Home** | `/` | Landing page with links to both demo pages. |
| **Sign & Secure** | `/Signature/Sign` | Upload (or use the default) PDF, PFX, signature image, then sign + apply encryption/permissions and download. |
| **Validate** | `/Signature/Validate` | Upload (or use the default) signed PDF, view signer certificate and signature info. |

## Features

* Visible signature image overlay on the last page
* Invisible digital signature (PKCS#7 / CAdES, SHA-256)
* AES-256 encryption with an open password
* Disable printing / copying / editing
* Flatten AcroForm fields
* Read-back of signature + signer certificate details

## Project structure

```
PdfDigitalSignatureAndSecurityDemo
├── Controllers
│   ├── HomeController.cs
│   └── SignatureController.cs
├── Models
│   ├── SignPdfViewModel.cs
│   └── ValidateSignatureViewModel.cs
├── Services
│   ├── PdfSecurityService.cs
│   └── PdfValidationService.cs
├── Views
│   ├── Home
│   │   └── Index.cshtml
│   ├── Signature
│   │   ├── Sign.cshtml
│   │   └── Validate.cshtml
│   └── Shared
│       └── _Layout.cshtml
├── wwwroot
│   └── SampleFiles
│       ├── Input.pdf
│       ├── SignedDocument.pdf
│       ├── Signature.png
│       └── Certificate.pfx
├── Properties
│   └── launchSettings.json
├── Program.cs
├── appsettings.json
└── PdfDigitalSignatureAndSecurityDemo.csproj
```

## Run

```powershell
cd PdfDigitalSignatureAndSecurityDemo
dotnet restore
dotnet run
```

Then open <https://localhost:5001> (or <http://localhost:5000>).

## Syncfusion license

Set your Syncfusion license key via:

* Environment variable `SYNCFUSION_LICENSE_KEY`, **or**
* `appsettings.json` → `Syncfusion:LicenseKey`, **or**
* The `SyncfusionLicenseProvider.RegisterLicense(...)` call in `Program.cs`.

A free community license is available at
<https://www.syncfusion.com/products/communitylicense>.

## Default sample files

| File | Default password (when none supplied in UI) |
| --- | --- |
| `Certificate.pfx` | `syncfusion` |

Replace the bundled `Input.pdf`, `Signature.png`, `Certificate.pfx`, and
`SignedDocument.pdf` with your own files to try the demo end-to-end.
