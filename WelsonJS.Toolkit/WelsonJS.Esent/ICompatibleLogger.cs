// ICompatibleLogger.cs (WelsonJS.Esent)
// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
// We use the ICompatibleLogger interface to maintain a BCL-first style.
// This allows for later replacement with logging libraries such as ILogger or Log4Net.
// 
namespace WelsonJS.Esent
{
    public interface ICompatibleLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
