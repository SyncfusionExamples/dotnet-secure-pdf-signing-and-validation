# PDF Digital Signature & Security Demo (.NET 8)

An ASP.NET Core MVC application demonstrating digital signature creation, AES-256 encryption, permission restrictions, form field flattening, and signature validation using the **Syncfusion .NET PDF Library**.

---

## ✨ Features

- **Digital Signature**: Sign PDFs using PFX certificates (CAdES standard, SHA-256 digest) with optional visible signature overlays.
- **AES-256 Encryption**: Password-protect documents with open/user passwords.
- **Granular Permissions**: Restrict printing, content copying, and editing.
- **Form Flattening**: Permanently flatten AcroForm fields into page content.
- **One-Click Download**: Automatically generate and download secured PDFs.
- **Signature Validation**: Inspect signer information, certificate validity, and document integrity.

---

## 🛠️ Tech Stack

- **Framework**: .NET 8.0 (ASP.NET Core MVC)
- **PDF Engine**: `Syncfusion.Pdf.Net.Core` (v30.2.4)
- **UI**: Bootstrap 5.3

---

## 📂 Project Structure

```text
PdfDigitalSignatureAndSecurityDemo/
├── Controllers/
│   ├── HomeController.cs        # Home navigation
│   └── SignatureController.cs   # Sign, Secure & Validate actions
├── Models/
│   ├── SignPdfViewModel.cs      # Sign & security model
│   └── ValidateSignatureViewModel.cs # Validation model
├── Services/
│   ├── PdfSecurityService.cs    # Signing, encryption & permissions logic
│   └── PdfValidationService.cs  # Validation & certificate parsing logic
├── Views/
│   ├── Home/Index.cshtml        # Home dashboard
│   └── Signature/
│       ├── Sign.cshtml          # Sign & Secure UI
│       └── Validate.cshtml      # Signature Validation UI
└── wwwroot/SampleFiles/         # Bundled sample PDFs, PFX & images
```

---

## 🚀 Quick Start

### 1. Clone & Run
```bash
git clone https://github.com/<your-username>/dotnet-secure-pdf-signing-and-validation.git
cd dotnet-secure-pdf-signing-and-validation
dotnet restore
dotnet run
```

Open in browser:
- **HTTPS**: `https://localhost:5001`
- **HTTP**: `http://localhost:5000`

---

## 🔑 Default Sample Credentials

If no custom files are uploaded in the UI, the application automatically uses default sample assets from `wwwroot/SampleFiles/`:

| Asset | Default Password |
|---|---|
| `PDFCertificate.pfx` | `syncfusion` |

---

## 📄 License

This project uses Syncfusion® PDF libraries for PDF processing, digital signatures, encryption, and validation.

Use of Syncfusion components is governed by Syncfusion's licensing terms. A valid Syncfusion Community or Commercial license may be required to build, run, or distribute applications that use these components.

For complete licensing information, refer to the official Syncfusion documentation: [Link](https://help.syncfusion.com/document-processing/licensing/overview)