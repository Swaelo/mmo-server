﻿// ================================================================================================================================
// File:        SpanConverter.cs
// Description: 
// ================================================================================================================================

using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerUtilities
{
    public static class SpanConverter
    {
        public static unsafe Span<T> AsSpan<T>(in Buffer<T> buffer)
        {
            return new Span<T>(buffer.Memory, buffer.Length);
        }
    }
}
