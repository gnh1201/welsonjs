﻿// ScreenMatchResult.cs
// SPDX-License-Identifier: MS-RL
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Drawing;

namespace WelsonJS.Service
{
    public class ScreenMatchResult
    {
        public string FileName { get; set; }
        public int ScreenNumber { get; set; }
        public IntPtr WindowHandle { get; set; }
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public Point WindowPosition { get; set; }
        public Point Position { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return $"Template: {FileName}, Screen Number: {ScreenNumber}, Window Title: {WindowTitle}, " +
                   $"Process Name: {ProcessName}, Window Position: (x: {WindowPosition.X}, y: {WindowPosition.Y}), " +
                   $"Location: (x: {Position.X}, y: {Position.Y})";
        }
    }
}
