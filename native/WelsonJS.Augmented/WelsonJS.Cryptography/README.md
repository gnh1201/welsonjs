# WelsonJS.Cryptography

Korean block ciphers for .NET (netstandard2.0), with the standard modes of operation:

| Cipher | Block | Key | Standard |
| --- | --- | --- | --- |
| `AriaAlgorithm` | 128 bits | 128, 192, 256 bits | KS X 1213, RFC 5794 |
| `SeedAlgorithm` | 128 bits | 128 bits | TTAS.KO-12.0004/R1, RFC 4269 |
| `HightAlgorithm` | 64 bits | 128 bits | TTAS.KO-12.0040/R1, ISO/IEC 18033-3 |

Each is a `SymmetricAlgorithm` (through `BlockCipherAlgorithm`), so it is used like `Aes`.

### HIGHT byte order

Keys, plaintexts and ciphertexts are byte arrays with byte 0 first, as in the KISA reference code (`KISA_HIGHT_ECB.c`), its Java version and Crypto++. The HIGHT specification document writes them the other way round, most significant byte first: the key as `MK15 ‖ … ‖ MK0`, the plaintext as `P7 … P0`, the ciphertext as `C7 … C0`. To use a vector from the specification, reverse the key, the plaintext and the ciphertext:

| | As written in the specification | As a byte array here |
| --- | --- | --- |
| Key | `00 11 22 33 44 55 66 77 88 99 aa bb cc dd ee ff` | `ff ee dd cc bb aa 99 88 77 66 55 44 33 22 11 00` |
| Plaintext | `00 00 00 00 00 00 00 00` | `00 00 00 00 00 00 00 00` |
| Ciphertext | `00 f4 18 ae d9 4f 03 f2` | `f2 03 4f d9 ae 18 f4 00` |

The key written in the specification's order, used as a byte array, gives `ca 4c b6 02 91 ff 81 31` instead. The two outputs are not two versions of HIGHT: the inputs differ. Before PR #560, this library's HIGHT was not HIGHT either (issue #293).

## Modes

| Mode | How | Notes |
| --- | --- | --- |
| ECB, CBC | `Mode = CipherMode.ECB` / `CBC`, then `CreateEncryptor()` / `CreateDecryptor()` | Padding: `PKCS7` (default), `ANSIX923`, `ISO10126`, `Zeros`, `None` |
| CFB | `Mode = CipherMode.CFB`, `FeedbackSize` 8 to the block size | CFB-8 to CFB-128; padding to the segment size, or `None` for any length |
| OFB | `Mode = CipherMode.OFB` | Full-block feedback (NIST SP 800-38A); `None` allows any length |
| CTS | `Mode = CipherMode.CTS` | CBC with ciphertext stealing, CS3 (RFC 3962); any length of at least one block, no padding |
| CTR | `CreateCtrTransform()` | The IV is the first counter block, incremented as one big-endian number; no padding |
| GCM | `CreateGcm()` → `BlockCipherGcm` | 128-bit block only (ARIA, SEED); tags of 4, 8, 12 to 16 bytes; any nonce length |
| CCM | `CreateCcm()` → `BlockCipherCcm` | 128-bit block only (ARIA, SEED); nonces of 7 to 13 bytes, tags of 4 to 16 bytes (even) |
| CMAC | `CreateCmac()` → `BlockCipherCmac` | A `HashAlgorithm`; 64-bit and 128-bit blocks (NIST SP 800-38B, RFC 4493) |

On .NET Core and .NET 5+, `SymmetricAlgorithm.Mode` accepts only ECB, CBC and CFB; these classes override it to accept OFB and CTS too. The transforms work with `CryptoStream`.

```vb
Dim aria As New AriaAlgorithm() With {.Mode = CipherMode.CBC}
aria.GenerateKey()
aria.GenerateIV()
Dim ciphertext = aria.CreateEncryptor().TransformFinalBlock(data, 0, data.Length)

Using gcm = aria.CreateGcm()
    Dim sealed = gcm.Seal(nonce, plaintext, associatedData)      ' ciphertext || 16-byte tag
    Dim opened = gcm.Open(nonce, sealed, associatedData)         ' throws if the tag does not match
End Using

Using cmac = aria.CreateCmac()
    Dim mac = cmac.ComputeHash(message)
End Using
```

`BlockCipherTransform`, `BlockCipherGcm`, `BlockCipherCcm` and `BlockCipherCmac` take any `IBlockCipher`, so another block cipher can use the same modes. `AriaEcbTransform`, `SeedEcbTransform` and `HightEcbTransform` remain for compatibility.

## Tests

`WelsonJS.Cryptography.Test` checks the library and exits with 1 on a failure:

```
dotnet run --project native\WelsonJS.Augmented\WelsonJS.Cryptography.Test
```

- **Ciphers:** the published test vectors: ARIA (RFC 5794), SEED (RFC 4269) and HIGHT (the KISA reference code).
- **Modes with the library's ciphers:** SEED-CBC (RFC 4196), ARIA-CTR and ARIA-GCM (RFC 8269), and HIGHT-CBC and HIGHT-CTR (KISA).
- **Modes with AES** (the generic code, run with .NET's AES as the block cipher):
  - NIST SP 800-38A for CBC, CFB-8, CFB-128, OFB and CTR.
  - RFC 3962 for CTS.
  - The GCM specification (a 64-bit IV).
  - RFC 4493 for CMAC.
- **Against .NET** (random messages): `Aes` for ECB, CBC and CFB with every padding, `AesGcm`, and `AesCcm`.
- **Streaming:** `CryptoStream` with random chunk sizes, for every cipher, mode and padding.
