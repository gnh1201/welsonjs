// ICompatibleLogger.cs (WelsonJS.Launcher)
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
// We use the ICompatibleLogger interface to maintain a BCL-first style.
// This allows for later replacement with logging libraries such as ILogger or Log4Net.
// 
namespace WelsonJS.Launcher
{
    public interface ICompatibleLogger
    {
        void Info(params object[] args);
        void Warn(params object[] args);
        void Error(params object[] args);
    }
}
