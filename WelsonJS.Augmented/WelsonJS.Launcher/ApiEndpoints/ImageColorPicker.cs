// ImageColorPicker.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// POST image-color-picker/ with a unified JSON body:
    ///   { "image": "<B64>", "point": { "x": 10, "y": 20 } }
    ///
    /// Response (success):
    ///   { "ok": true, "hex": "#RRGGBB", "name": "white", "rgb": { "r": 255, "g": 255, "b": 255 } }
    ///
    /// Response (error):
    ///   { "ok": false, "error": "..." }
    ///
    /// JSON handling:
    ///  - Request parse: JsSerializer.Load/ExtractFrom (engine) + tiny local decoders for literals.
    ///  - Response encode: Dictionary<string, object> → JsSerializer.Serialize(...).
    /// </summary>
    public class ImageColorPicker : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        private const string Prefix = "image-color-picker";

        public ImageColorPicker(ResourceServer server, HttpClient httpClient, ILog logger)
        {
            Server = server;

            _httpClient = httpClient;
            _logger = logger;
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            return path != null && path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            try
            {
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Only POST is supported."
                    });
                    return;
                }

                // ---- Read request body ---------------------------------------------------------
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
                    body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Empty request body."
                    });
                    return;
                }

                // ---- Extract strictly in unified format ---------------------------------------
                string imageJson, xJson, yJson;
                using (var ser = new JsSerializer())
                {
                    int id = ser.Load(body);

                    // Required: "image" as JSON string
                    imageJson = ser.ExtractFrom(id, "image");

                    // Required: "point": { "x": <num>, "y": <num> }
                    xJson = ser.ExtractFrom(id, "point", "x");
                    yJson = ser.ExtractFrom(id, "point", "y");

                    ser.Unload(id);
                }

                if (string.IsNullOrEmpty(imageJson))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Missing required field: 'image' (base64 string)."
                    });
                    return;
                }
                if (string.IsNullOrEmpty(xJson) || string.IsNullOrEmpty(yJson))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Missing required field: 'point' with numeric 'x' and 'y'."
                    });
                    return;
                }

                // ---- Convert JSON literals → CLR ----------------------------------------------
                string imageB64;
                if (!TryParseJsonString(imageJson, out imageB64) || string.IsNullOrWhiteSpace(imageB64))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "'image' must be a non-empty JSON string."
                    });
                    return;
                }

                if (!TryParseJsonInt(xJson, out var x) || !TryParseJsonInt(yJson, out var y))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "'point.x' and 'point.y' must be JSON numbers."
                    });
                    return;
                }

                // Allow data URL prefix (data:*;base64,....)
                imageB64 = StripDataUrlPrefix(imageB64);

                byte[] bytes;
                try { bytes = Convert.FromBase64String(imageB64); }
                catch
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Invalid base64 image data."
                    });
                    return;
                }

                // ---- Decode image and sample pixel --------------------------------------------
                using (var ms = new MemoryStream(bytes, writable: false))
                using (var bmp = new Bitmap(ms))
                {
                    if (x < 0 || y < 0 || x >= bmp.Width || y >= bmp.Height)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await WriteJsonAsync(context, new Dictionary<string, object>
                        {
                            ["ok"] = false,
                            ["error"] = $"Point out of bounds. Image size: {bmp.Width}x{bmp.Height}, requested: ({x},{y})."
                        });
                        return;
                    }

                    var color = bmp.GetPixel(x, y);
                    var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                    var name = ToCommonColorName(color); // null if not close to a known color

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["hex"] = hex,
                        ["name"] = name, // may be null → serializer will emit "name": null
                        ["rgb"] = new Dictionary<string, object>
                        {
                            ["r"] = color.R,
                            ["g"] = color.G,
                            ["b"] = color.B
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"ImageColorPicker error: {ex}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteJsonAsync(context, new Dictionary<string, object>
                {
                    ["ok"] = false,
                    ["error"] = "Internal error while processing image."
                });
            }
            finally
            {
                try { context.Response.OutputStream.Flush(); } catch { }
                try { context.Response.OutputStream.Close(); } catch { }
            }
        }

        // ---------------------------- Helpers -----------------------------------------------

        private static bool TryParseJsonInt(string jsonNumberLiteral, out int value)
        {
            // Accept JSON numbers like: 12, -3, 10.0, 1e2, -4E+1
            // Strategy: parse as double using invariant culture, then cast if within int range.
            value = default;

            if (string.IsNullOrWhiteSpace(jsonNumberLiteral))
                return false;

            var s = jsonNumberLiteral.Trim();

            // Reject if wrapped in quotes (should be a number literal, not a string)
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                return false;

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                if (d >= int.MinValue && d <= int.MaxValue)
                {
                    value = (int)d; // truncate toward zero (JSON has no ints; this is reasonable)
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseJsonString(string jsonStringLiteral, out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(jsonStringLiteral))
                return false;

            var s = jsonStringLiteral.Trim();
            if (s.Length < 2 || s[0] != '"' || s[s.Length - 1] != '"')
                return false;

            // Remove surrounding quotes and unescape JSON sequences
            value = JsonUnescape(s.Substring(1, s.Length - 2));
            return true;
        }

        private static string JsonUnescape(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '\\') { sb.Append(c); continue; }

                if (i + 1 >= s.Length) { sb.Append('\\'); break; }
                char n = s[++i];
                switch (n)
                {
                    case '\"': sb.Append('\"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 < s.Length)
                        {
                            string hex = s.Substring(i + 1, 4);
                            if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
                            {
                                sb.Append((char)cp);
                                i += 4;
                            }
                            else sb.Append('u');
                        }
                        else sb.Append('u');
                        break;
                    default:
                        sb.Append(n);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string StripDataUrlPrefix(string b64)
        {
            var idx = b64.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? b64.Substring(idx + "base64,".Length) : b64;
        }

        private static string ToCommonColorName(Color c)
        {
            (string name, Color color)[] palette =
            {
                ("white",   Color.FromArgb(255,255,255)),
                ("black",   Color.FromArgb(0,0,0)),
                ("red",     Color.FromArgb(255,0,0)),
                ("green",   Color.FromArgb(0,128,0)),
                ("blue",    Color.FromArgb(0,0,255)),
                ("yellow",  Color.FromArgb(255,255,0)),
                ("cyan",    Color.FromArgb(0,255,255)),
                ("magenta", Color.FromArgb(255,0,255)),
                ("gray",    Color.FromArgb(128,128,128))
            };

            const int tolerance = 20; // distance threshold
            string best = null;
            int bestDist = int.MaxValue;

            foreach (var (name, col) in palette)
            {
                int dr = c.R - col.R, dg = c.G - col.G, db = c.B - col.B;
                int dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist) { bestDist = dist; best = name; }
            }

            return bestDist <= tolerance * tolerance ? best : null;
        }

        private async Task WriteJsonAsync(HttpListenerContext ctx, Dictionary<string, object> doc, int space = 0)
        {
            using (var ser = new JsSerializer())
            {
                string json = ser.Serialize(doc, space);
                byte[] payload = Encoding.UTF8.GetBytes(json);

                await Server.ServeResource(ctx, payload, "application/json", 200);
            }
        }
    }
}
