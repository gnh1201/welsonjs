// ImageColorPicker.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
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
    /// JSON handling: JsonCodec (ChakraCore) for both the request and the response.
    /// </summary>
    public class ImageColorPicker : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string Prefix = "image-color-picker";

        public ImageColorPicker(ResourceServer server, HttpClient httpClient, ILogger logger)
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
                Dictionary<string, object> request;
                try { request = JsonCodec.DeserializeObject(body); }
                catch (FormatException)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Request body must be a JSON object."
                    });
                    return;
                }

                // Required: "image" as JSON string
                request.TryGetValue("image", out object imageValue);

                // Required: "point": { "x": <num>, "y": <num> }
                object xValue = null, yValue = null;
                if (request.TryGetValue("point", out object pointValue) && pointValue is Dictionary<string, object> point)
                {
                    point.TryGetValue("x", out xValue);
                    point.TryGetValue("y", out yValue);
                }

                if (imageValue == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Missing required field: 'image' (base64 string)."
                    });
                    return;
                }
                if (xValue == null || yValue == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "Missing required field: 'point' with numeric 'x' and 'y'."
                    });
                    return;
                }

                // ---- Convert JSON values → CLR ------------------------------------------------
                string imageB64 = imageValue as string;
                if (string.IsNullOrWhiteSpace(imageB64))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context, new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["error"] = "'image' must be a non-empty JSON string."
                    });
                    return;
                }

                if (!TryGetInt(xValue, out var x) || !TryGetInt(yValue, out var y))
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

        private static bool TryGetInt(object jsonNumber, out int value)
        {
            // JSON numbers only (strings are rejected); fractions truncate toward zero.
            value = default;

            if (!(jsonNumber is int || jsonNumber is long || jsonNumber is double))
                return false;

            double d = Convert.ToDouble(jsonNumber, CultureInfo.InvariantCulture);
            if (d < int.MinValue || d > int.MaxValue)
                return false;

            value = (int)d;
            return true;
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
            byte[] payload = Encoding.UTF8.GetBytes(JsonCodec.Serialize(doc, space));
            await Server.ServeResource(ctx, payload, "application/json", 200);
        }
    }
}
