// Copyright 2016-2017 Confluent Inc., 2015-2016 Andreas Heider
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
// Derived from: rdkafka-dotnet, licensed under the 2-clause BSD License.
//
// Refer to LICENSE for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Confluent.Kafka.Internal;


namespace Confluent.Kafka.Impl
{
    enum ConfRes
    {
        /// <summary>
        ///     Unknown configuration name.
        /// </summary>
        Unknown = -2,

        /// <summary>
        ///     Invalid configuration value.
        /// </summary>
        Invalid = -1,

        /// <summary>
        ///     Configuration okay
        /// </summary>
        Ok = 0
    }

    class SafeConfigHandle : SafeHandle
    {
        public SafeConfigHandle()
            : base(IntPtr.Zero, false) { }

        public override bool IsInvalid
            => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
            => true;

        internal static SafeConfigHandle Create()
        {
            var ch = LibRdKafka.conf_new();
            if (ch.IsInvalid)
            {
                throw new Exception("Failed to create config");
            }
            return ch;
        }

        internal IntPtr Dup()
            => LibRdKafka.conf_dup(handle);

        internal Dictionary<string, string> Dump()
        {
            UIntPtr cntp = (UIntPtr) 0;
            IntPtr data = LibRdKafka.conf_dump(handle, out cntp);

            if (data == IntPtr.Zero)
            {
                throw new Exception("Zero data");
            }

            try
            {
                if (((int) cntp & 1) != 0)
                {
                    // Expect Key -> Value, so even number of strings
                    throw new Exception("Invalid number of config entries");
                }

                var dict = new Dictionary<string, string>();
                for (int i = 0; i < (int) cntp / 2; i++)
                {
#if NET45
                    dict.Add(Util.Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(data, 2 * i * Util.Marshal.SizeOf<IntPtr>())),
                                                 Util.Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(data, (2 * i + 1) * Util.Marshal.SizeOf<IntPtr>())));
#else                    
                    dict.Add(Util.Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(data, 2 * i * Marshal.SizeOf<IntPtr>())),
                             Util.Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(data, (2 * i + 1) * Marshal.SizeOf<IntPtr>())));
#endif
                }
                // Filter out callback pointers
                return dict.Where(kv => !kv.Key.EndsWith("_cb")).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            finally
            {
                LibRdKafka.conf_dump_free(data, cntp);
            }
        }

        internal void Set(string name, string value)
        {
            // TODO: Constant instead of 512?
            var errorStringBuilder = new StringBuilder(512);
            ConfRes res = LibRdKafka.conf_set(handle, name, value,
                    errorStringBuilder, (UIntPtr) errorStringBuilder.Capacity);
            if (res == ConfRes.Ok)
            {
                return;
            }
            else if (res == ConfRes.Invalid)
            {
                throw new ArgumentException(errorStringBuilder.ToString());
            }
            else if (res == ConfRes.Unknown)
            {
                throw new InvalidOperationException(errorStringBuilder.ToString());
            }
            else
            {
                throw new Exception("Unknown error while setting configuration property");
            }
        }

        internal string Get(string name)
        {
            UIntPtr destSize = (UIntPtr) 0;
            StringBuilder sb = null;

            ConfRes res = LibRdKafka.conf_get(handle, name, null, ref destSize);
            if (res == ConfRes.Ok)
            {
                sb = new StringBuilder((int) destSize);
                res = LibRdKafka.conf_get(handle, name, sb, ref destSize);
            }
            if (res != ConfRes.Ok)
            {
                if (res == ConfRes.Unknown)
                {
                    throw new InvalidOperationException($"No such configuration property: {name}");
                }
                throw new Exception("Unknown error while getting configuration property");
            }
            return sb?.ToString();
        }
    }
}
