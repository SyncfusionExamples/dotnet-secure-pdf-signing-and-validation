# Sample assets

The default sample files referenced by the demo are:

| File | Purpose |
| --- | --- |
| `Input.pdf`         | The PDF that will be signed (a one-page document). |
| `Signature.png`     | A small PNG / JPG rendered as the **visible** signature. |
| `Certificate.pfx`   | A PFX (PKCS#12) certificate used to produce the digital signature. |
| `SignedDocument.pdf`| A pre-signed PDF consumed by the **Validate** page. |

The application works out-of-the-box with the bundled defaults — but for a
real demo please replace these files with your own.

The bundled PFX default password (used only when the user leaves the
"Certificate password" field blank) is **`syncfusion`**.
