﻿// Disable warnings about XML documentation
#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing basic Lua functions (print, type, tostring, etc) as a MoonSharp module.
	/// </summary>
	[MoonSharpModule]
	public class BasicModule
	{
		//type (v)
		//----------------------------------------------------------------------------------------------------------------
		//Returns the type of its only argument, coded as a string. The possible results of this function are "nil" 
		//(a string, not the value nil), "number", "string", "boolean", "table", "function", "thread", and "userdata". 
		[MoonSharpModuleMethod]
		public static DynValue type(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			if (args.Count < 1) throw ScriptRuntimeException.BadArgumentValueExpected(0, "type");

			DynValue v = args[0];
			return DynValue.NewString(v.Type.ToLuaTypeString());
		}



		//assert (v [, message])
		//----------------------------------------------------------------------------------------------------------------
		//Issues an error when the value of its argument v is false (i.e., nil or false); 
		//otherwise, returns all its arguments. message is an error message; when absent, it defaults to "assertion failed!" 
		[MoonSharpModuleMethod]
		public static DynValue assert(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue v = args[0];
			DynValue message = args[1];

			if (!v.CastToBool())
			{
				if (message.IsNil())
					throw new ScriptRuntimeException("assertion failed!"); // { DoNotDecorateMessage = true };
				else
					throw new ScriptRuntimeException(message.ToPrintString()); // { DoNotDecorateMessage = true };
			}

			return DynValue.NewTupleNested(args.GetArray());
		}

		// collectgarbage  ([opt [, arg]])
		// ----------------------------------------------------------------------------------------------------------------
		// This function is mostly a stub towards the CLR GC. If mode is nil, "collect" or "restart", a GC is forced.
		[MoonSharpModuleMethod]
		public static DynValue collectgarbage(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue opt = args[0];

			string mode = opt.CastToString();

			if (mode == null || mode == "collect" || mode == "restart")
			{
#if PCL || ENABLE_DOTNET
				GC.Collect();
#else
				GC.Collect(2, GCCollectionMode.Forced);
#endif
			}

			return DynValue.Nil;
		}

		// error (message [, level])
		// ----------------------------------------------------------------------------------------------------------------
		// Terminates the last protected function called and returns message as the error message. Function error never returns.
		// Usually, error adds some information about the error position at the beginning of the message. 
		// The level argument specifies how to get the error position. 
		// With level 1 (the default), the error position is where the error function was called. 
		// Level 2 points the error to where the function that called error was called; and so on. 
		// Passing a level 0 avoids the addition of error position information to the message. 
		[MoonSharpModuleMethod]
		public static DynValue error(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue message = args.AsType(0, "error", DataType.String, false);
			throw new ScriptRuntimeException(message.String); // { DoNotDecorateMessage = true };
		}


		// tostring (v)
		// ----------------------------------------------------------------------------------------------------------------
		// Receives a value of any type and converts it to a string in a reasonable format. (For complete control of how 
		// numbers are converted, use string.format.)
		// 
		// If the metatable of v has a "__tostring" field, then tostring calls the corresponding value with v as argument, 
		// and uses the result of the call as its result. 
		[MoonSharpModuleMethod]
		public static DynValue tostring(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			if (args.Count < 1) throw ScriptRuntimeException.BadArgumentValueExpected(0, "tostring");

			DynValue v = args[0];
			DynValue tail = executionContext.GetMetamethodTailCall(v, "__tostring", v);
			
			if (tail == null || tail.IsNil())
				return DynValue.NewString(v.ToPrintString());

			tail.TailCallData.Continuation = new CallbackFunction(__tostring_continuation, "__tostring");

			return tail;
		}

		private static DynValue __tostring_continuation(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue b = args[0].ToScalar();

			if (b.IsNil())
				return b;

			if (b.Type != DataType.String)
				throw new ScriptRuntimeException("'tostring' must return a string");


			return b;
		}

		// select (index, ...)
		// -----------------------------------------------------------------------------
		// If index is a number, returns all arguments after argument number index; a negative number indexes from 
		// the end (-1 is the last argument). Otherwise, index must be the string "#", and select returns the total
		// number of extra arguments it received. 
		[MoonSharpModuleMethod]
		public static DynValue select(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			if (args[0].Type == DataType.String && args[0].String == "#")
			{
				if (args[args.Count - 1].Type == DataType.Tuple)
				{
					return DynValue.NewNumber(args.Count - 1 + args[args.Count - 1].Tuple.Length);
				}
				else
				{
					return DynValue.NewNumber(args.Count - 1);
				}
			}

			DynValue v_num = args.AsType(0, "select", DataType.Number, false);
			int num = (int)v_num.Number;

			List<DynValue> values = new List<DynValue>();

			if (num > 0)
			{
				for (int i = num; i < args.Count; i++)
					values.Add(args[i]);
			}
			else if (num < 0)
			{
				num = args.Count + num;

				if (num < 1)
					throw ScriptRuntimeException.BadArgumentIndexOutOfRange("select", 0);

				for (int i = num; i < args.Count; i++)
					values.Add(args[i]);
			}
			else
			{
				throw ScriptRuntimeException.BadArgumentIndexOutOfRange("select", 0);
			}

			return DynValue.NewTupleNested(values.ToArray());
		}




		// tonumber (e [, base])
		// ----------------------------------------------------------------------------------------------------------------
		// When called with no base, tonumber tries to convert its argument to a number. If the argument is already 
		// a number or a string convertible to a number (see §3.4.2), then tonumber returns this number; otherwise, 
		// it returns nil.
		//
		// When called with base, then e should be a string to be interpreted as an integer numeral in that base. 
		// The base may be any integer between 2 and 36, inclusive. In bases above 10, the letter 'A' (in either 
		// upper or lower case) represents 10, 'B' represents 11, and so forth, with 'Z' representing 35. If the 
		// string e is not a valid numeral in the given base, the function returns nil. 
		[MoonSharpModuleMethod]
		public static DynValue tonumber(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			if (args.Count < 1) throw ScriptRuntimeException.BadArgumentValueExpected(0, "tonumber");

			DynValue e = args[0];
			DynValue b = args.AsType(1, "tonumber", DataType.Number, true);

			if (b.IsNil())
			{
				if (e.Type == DataType.Number)
					return e;

				if (e.Type != DataType.String)
					return DynValue.Nil;

				double d;
				if (double.TryParse(e.String, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
				{
                    return DynValue.NewNumber(d);
				}
				return DynValue.Nil;
			}
			else
			{
                //!COMPAT: tonumber supports only 2,8,10 or 16 as base
                //UPDATE: added support for 3-9 base numbers
                //UPDATE: added radix 11-36
                DynValue ee;

                // In standard Lua, first tonumber arg MUST be a string if base is specified.
                // Even number is invalid.
                // 
                if (args[0].Type != DataType.String)
                {
                    throw new ScriptRuntimeException("bad argument #1 to 'tonumber' (string expected, got number)");
                }
                ee = args[0];

                int bb;
                {
                    double base_tmp = b.Number;
                    bb = (int)base_tmp;
                    if (bb != base_tmp)
                    {
                        throw new ScriptRuntimeException("bad argument #2 to 'tonumber' (number has no integer representation)");
                    }
                }

                // Lua supports negative integers here, and well as integers larger than 32 bit.
                // FFFFffffFFFFffff parses as -1 in the 5.3 reference implementation, through, so long will probably do.
                long uiv = 0;
                // Even though Convert.ToInt64 may (or may not) be a a highly optimized compiler intrinsic, using it is problematic
                // because it throws on error, and passing an invalid string to this function cannot be considered exceptional.
                // Also it can only handle bases 2, 8, 10, 16.
                // So we're always using our own implementation below.
                
                // Support for bases 2 .. 36.
                if (!(bb <= 36 && bb >= 2))
                {
                    throw new ScriptRuntimeException("bad argument #2 to 'tonumber' (base out of range)");
                }

                var trimmedString = ee.String.Trim();
                if (trimmedString.Length == 0)
                {
                    return DynValue.Nil;
                }
                bool isNegative = false;
                int currentCharIndex = 0;
                if (trimmedString[currentCharIndex] == '-')
                {
                    // This is a negative number.
                    isNegative = true;
                    currentCharIndex += 1;
                    if (trimmedString.Length <= 1)
                    {
                        // This is nothing but an unary minus. Lua returns null in this case.
                        return DynValue.Nil;
                    }
                }
			    for (; currentCharIndex < trimmedString.Length; ++currentCharIndex)
			    {
                    char digit = trimmedString[currentCharIndex];
                    // Initialize to MaxValue so that the if handles an invalid character as well.
                    int value = Int32.MaxValue;
                    if (digit >= '0' && digit <= '9')
                    {
                        value = digit - '0';
                    }
                    else if (digit >= 'A' && digit <= 'Z')
                    {
                        value = digit - 'A' + 10;
                    }
                    else if (digit >= 'a' && digit <= 'z')
                    {
                        value = digit - 'a' + 10;
                    }
			            
                    // Handles invalid character (i.e. none of the ifs above hit) as well as digit greater than base.
                    if (value >= bb)
			        {
                        // In case of an invalid char, standard Lua tonumber returns Nil.
                        // This is reasonable because this is pretty much the only way for the user to check if a string is a number.
                        return DynValue.Nil;
                    }

                    uiv = (uiv * bb) + value;
			    }

                if (isNegative)
                {
                    uiv = -uiv;
                }

				return DynValue.NewNumber(uiv);
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue print(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < args.Count; i++)
			{
				if (args[i].IsVoid())
					break;

				if (i != 0)
					sb.Append('\t');

				sb.Append(args.AsStringUsingMeta(executionContext, i, "print"));
			}

			executionContext.GetScript().Options.DebugPrint(sb.ToString());

			return DynValue.Nil;
		}
	}
}
