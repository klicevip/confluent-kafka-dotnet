// Copyright 2016-2017 Confluent Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Refer to LICENSE for more information.

using System;
using System.Text;
#if NET45
using RunTimeMarshal = System.Runtime.InteropServices.Marshal;
#endif


namespace Confluent.Kafka.Internal
{
    internal static class Util
    {
        internal static class Marshal
        {
            /// <summary>
            ///     Interpret a zero terminated c string as UTF-8.
            /// </summary>
            public static string PtrToStringUTF8(IntPtr strPtr)
            {
                // TODO: Is there a built in / vectorized / better way to implement this?
                var length = 0;
                unsafe
                {
                    byte* pTraverse = (byte *)strPtr;
                    while (*pTraverse != 0) { pTraverse += 1; }
                    length = (int)(pTraverse - (byte*)strPtr);
                }
                var strBuffer = new byte[length];
                System.Runtime.InteropServices.Marshal.Copy(strPtr, strBuffer, 0, length);
                return Encoding.UTF8.GetString(strBuffer);
            }

#if NET45
            public static int SizeOf<T>()
            {
                return RunTimeMarshal.SizeOf(typeof(T));
            }

            public static T PtrToStructure<T>(IntPtr ptr)
            {
                return (T)RunTimeMarshal.PtrToStructure(ptr, typeof(T));
            }

            public static void Copy(IntPtr source, int[] destination, int startIndex, int length)
            {
                RunTimeMarshal.Copy(source, destination, startIndex, length);
            }

            public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
            {
                RunTimeMarshal.Copy(source, destination, startIndex, length);
            }

            public static void WriteInt64(IntPtr ptr, int ofs, long val)
            {
                RunTimeMarshal.WriteInt64(ptr, ofs, val);
            }

            public static IntPtr OffsetOf<T>(string fieldName)
            {
                return RunTimeMarshal.OffsetOf(typeof(T), fieldName);
            }
#endif
        }
    }
}
