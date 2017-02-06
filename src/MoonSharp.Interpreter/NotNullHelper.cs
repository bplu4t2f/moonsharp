using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonSharp.Interpreter
{
    internal static class NotNullHelper
    {
        public static T NotNull<T>(this T obj, string name)
        {
            if (Object.ReferenceEquals(obj, null))
            {
                throw new ArgumentNullException(name);
            }
            return obj;
        }
    }
}
