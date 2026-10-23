// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Configuration.Errors
{
    [RuntimeTypeAlias("VivHelper.InvalidPropertyException")]
    public class InvalidPropertyException : Exception
    {
        public InvalidPropertyException(string message) : base(message)
        {
        }

        public InvalidPropertyException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
