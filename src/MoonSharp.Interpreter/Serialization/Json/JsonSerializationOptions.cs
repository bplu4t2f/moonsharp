using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonSharp.Interpreter.Serialization.Json
{
    public class JsonSerializationOptions
    {
        public bool EscapeForwardSlashes { get; set; }
        public bool InsertNewLines { get; set; }
    }
}
