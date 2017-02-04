using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonSharp.Interpreter.Serialization.Json
{
    /// <summary>
    /// Can be used to customize the JSON serialization output.
    /// </summary>
    public class JsonSerializationOptions
    {
        /// <summary>
        /// Whether forward slashes ("/") that occur in strings should be escaped ("\/").
        /// <para>Defaults to <c>true</c>.</para>
        /// </summary>
        public bool EscapeForwardSlashes { get; set; } = true;
    }
}
