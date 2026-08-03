// KeyValueCodec.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class KeyValueCodec : IEnumerable<KeyValuePair<string, string>>
{
    private readonly Dictionary<string, string> _dict;

    public KeyValueCodec()
    {
        _dict = new Dictionary<string, string>();
    }

    public int Count
    {
        get { return _dict.Count; }
    }

    public string this[string key]
    {
        get { return _dict[key]; }
        set
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _dict[key] = value ?? String.Empty;
        }
    }

    public KeyValueCodec Add(string key, string value)
    {
        if (key == null)
            throw new ArgumentNullException("key");

        _dict[key] = value ?? String.Empty;
        return this;
    }

    public bool Remove(string key)
    {
        return _dict.Remove(key);
    }

    public bool ContainsKey(string key)
    {
        return _dict.ContainsKey(key);
    }

    public bool TryGetValue(string key, out string value)
    {
        return _dict.TryGetValue(key, out value);
    }

    public void Clear()
    {
        _dict.Clear();
    }

    /// <summary>
    /// Encodes the key-value pairs into a string.
    /// </summary>
    public string Encode()
    {
        StringBuilder sb = new StringBuilder();

        foreach (KeyValuePair<string, string> pair in _dict)
        {
            if (sb.Length > 0)
                sb.Append("; ");

            sb.Append(Escape(pair.Key))
              .Append('=')
              .Append(Escape(pair.Value));
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        return Encode();
    }

    /// <summary>
    /// Decodes a key-value string into a KeyValueCodec instance.
    /// </summary>
    public static KeyValueCodec Decode(string text)
    {
        KeyValueCodec codec = new KeyValueCodec();

        if (String.IsNullOrEmpty(text))
            return codec;

        foreach (string pair in Split(text, ';'))
        {
            if (pair.Trim().Length == 0)
                continue;

            List<string> kv = Split(pair, '=');

            string key = kv.Count > 0 ? Unescape(kv[0].Trim()) : String.Empty;
            string value = kv.Count > 1 ? Unescape(kv[1].Trim()) : String.Empty;

            codec.Add(key, value);
        }

        return codec;
    }

    private static string Escape(string value)
    {
        if (value == null)
            return String.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("=", "\\=")
            .Replace(";", "\\;");
    }

    private static string Unescape(string value)
    {
        if (String.IsNullOrEmpty(value))
            return String.Empty;

        StringBuilder sb = new StringBuilder();
        bool escaped = false;

        foreach (char c in value)
        {
            if (escaped)
            {
                sb.Append(c);
                escaped = false;
            }
            else if (c == '\\')
            {
                escaped = true;
            }
            else
            {
                sb.Append(c);
            }
        }

        if (escaped)
            sb.Append('\\');

        return sb.ToString();
    }

    private static List<string> Split(string text, char separator)
    {
        List<string> list = new List<string>();
        StringBuilder sb = new StringBuilder();

        bool escaped = false;

        foreach (char c in text)
        {
            if (escaped)
            {
                sb.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == separator)
            {
                list.Add(sb.ToString());
                sb.Length = 0;
                continue;
            }

            sb.Append(c);
        }

        list.Add(sb.ToString());

        return list;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
