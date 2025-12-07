# Catswords.Phantomizer

**Catswords.Phantomizer** is an HTTP-based dynamic-link library (DLL) loader designed for .NET applications.
It allows your application to fetch and load assemblies directly from your CDN (Azure Blob, S3, Cloudflare R2, etc.) at runtime, with optional GZip compression support.

---

## 🚀 Features

* Load managed (`*.dll`) and native (`*.dll`) assemblies over HTTP
* Optional `.dll.gz` decompression for faster network delivery
* CDN-friendly URL structure
* Easy bootstrap through a small embedded loader
* Loader is implemented using **pure .NET BCL only**, ensuring stable operation without external dependencies
* Built-in **code-signing verification** support to ensure assemblies are trusted and tamper-free

---

## 📦 How to Use

### 1. Embed Phantomizer into your project

Add `Catswords.Phantomizer.dll.gz` to your `Resources.resx` file.

---

### 2. Initialize Phantomizer at application startup

Place the following code inside your `Main` method, static constructor, or any early entry point:

```csharp
static Program() {
    // ...
    InitializeAssemblyLoader();
    // ...
}

private static void InitializeAssemblyLoader()
{
    byte[] gzBytes = Properties.Resources.Phantomizer;

    byte[] dllBytes;
    using (var input = new MemoryStream(gzBytes))
    using (var gz = new GZipStream(input, CompressionMode.Decompress))
    using (var output = new MemoryStream())
    {
        gz.CopyTo(output);
        dllBytes = output.ToArray();
    }

    Assembly phantomAsm = Assembly.Load(dllBytes);
    Type loaderType = phantomAsm.GetType("Catswords.Phantomizer.AssemblyLoader", true);

    loaderType.GetProperty("BaseUrl")?.SetValue(null, GetAppConfig("AssemblyBaseUrl"));  // Set your CDN base URL
    loaderType.GetProperty("LoaderNamespace")?.SetValue(null, typeof(Program).Namespace);
    loaderType.GetProperty("AppName")?.SetValue(null, "WelsonJS");                        // Set your application name
    loaderType.GetMethod("Register")?.Invoke(null, null);

    var loadNativeModulesMethod = loaderType.GetMethod(
        "LoadNativeModules",
        BindingFlags.Public | BindingFlags.Static,
        binder: null,
        types: new[] { typeof(string), typeof(Version), typeof(string[]) },
        modifiers: null
    );

    if (loadNativeModulesMethod == null)
        throw new InvalidOperationException("LoadNativeModules(string, Version, string[]) method not found.");

    loadNativeModulesMethod.Invoke(null, new object[]
    {
        "ChakraCore",
        new Version(1, 13, 0, 0),
        new[] { "ChakraCore.dll" }
    });
}
```

---

### 3. Upload your DLL files to a CDN

Upload your managed and native assemblies to your CDN following the URL pattern below.

#### 📁 URL Rules

| Type               | Example URL                                                                         | Description                     |
| ------------------ | ----------------------------------------------------------------------------------- | ------------------------------- |
| Managed DLL        | `https://example.cdn.tld/packages/managed/MyManagedLib/1.0.0.0/MyManagedLib.dll`    | Normal .NET assembly            |
| Managed DLL (GZip) | `https://example.cdn.tld/packages/managed/MyManagedLib/1.0.0.0/MyManagedLib.dll.gz` | GZip-compressed .NET assembly   |
| Native DLL         | `https://example.cdn.tld/packages/native/MyNativeLib/1.0.0.0/MyNativeLib.dll`       | Native assembly                 |
| Native DLL (GZip)  | `https://example.cdn.tld/packages/native/MyNativeLib/1.0.0.0/MyNativeLib.dll.gz`    | GZip-compressed native assembly |

---

### 4. 🎉 Start loading assemblies over HTTP

Once Phantomizer is initialized, your application will automatically fetch missing assemblies from your CDN.

---

## Download the pre-compiled `Catswords.Phantomizer.dll.gz` file
* https://catswords.blob.core.windows.net/welsonjs/packages/managed/Catswords.Phantomizer/1.0.0.0/Catswords.Phantomizer.dll.gz

---

## Report abuse
* [GitHub Security Advisories (gnh1201/welsonjs)](https://github.com/gnh1201/welsonjs/security)
* [abuse@catswords.re.kr](mailto:abuse@catswords.re.kr)

## Join the community
I am always open. Collaboration, opportunities, and community activities are all welcome.

* ActivityPub [@catswords_oss@catswords.social](https://catswords.social/@catswords_oss)
* XMPP [catswords@conference.omemo.id](xmpp:catswords@conference.omemo.id?join)
* [Join Catswords OSS on Microsoft Teams (teams.live.com)](https://teams.live.com/l/community/FEACHncAhq8ldnojAI)
* [Join Catswords OSS #welsonjs on Discord (discord.gg)](https://discord.gg/XKG5CjtXEj)
