// ICompatibleLogger.cs (WelsonJS.Esent)
// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;

namespace WelsonJS.Esent
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(string message, Exception exception);
        void Fatal(string message);
        void Fatal(string message, Exception exception);
    }
}
