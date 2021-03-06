﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter.Tree;

namespace MoonSharp.Interpreter.Serialization.Json
{
	/// <summary>
	/// Class performing conversions between Tables and Json.
	/// NOTE : the conversions are done respecting json syntax but using Lua constructs. This means mostly that:
	/// 1) Lua string escapes can be accepted while they are not technically valid JSON, and viceversa
	/// 2) Null values are represented using a static userdata of type JsonNull
	/// 3) Do not use it when input cannot be entirely trusted
	/// </summary>
	public static class JsonTableConverter
	{
		/// <summary>
		/// Converts a table to a json string
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="options">(optional) Serialization options to customize the output string. May be null, in which case
		/// default options are used.</param>
		public static string TableToJson(this Table table, JsonSerializationOptions options = null)
		{
			if (options == null)
			{
				options = new JsonSerializationOptions();
			}

			StringBuilder sb = new StringBuilder();
			TableToJson(sb, table, options, 0);
			return sb.ToString();
		}

		/// <summary>
		/// The newline character that is inserted if <see cref="JsonSerializationOptions.HumanReadable"/> is true.
		/// </summary>
		public const string NewLine = "\n";
		/// <summary>
		/// The indentation character that is inserted if <see cref="JsonSerializationOptions.HumanReadable"/> is true.
		/// </summary>
		public const string Indentation = "\t";

		/// <summary>
		/// Inserts a <see cref="NewLine"/> character (only if applicable).
		/// </summary>
		private static void InsertNewLine(StringBuilder sb, JsonSerializationOptions options)
		{
			if (options.HumanReadable)
			{
				sb.Append(NewLine);
			}
		}
		
		/// <summary>
		/// Inserts <paramref name="indentLevel"/> amount of <see cref="Indentation"/> characters (only if applicable).
		/// </summary>
		private static void InsertIndentation(StringBuilder sb, JsonSerializationOptions options, int indentLevel)
		{	
			for (int i = 0; i < indentLevel; ++i)
			{
				sb.Append(Indentation);
			}
		}

		/// <summary>
		/// Inserts a colon token. If <see cref="JsonSerializationOptions.HumanReadable"/> is true, inserts a space as well.
		/// </summary>
		private static void InsertColon(StringBuilder sb, JsonSerializationOptions options)
		{
			if (options.HumanReadable)
			{
				sb.Append(": ");
			}
			else
			{
				sb.Append(':');
			}
		}

		/// <summary>
		/// Tables to json.
		/// </summary>
		/// <param name="sb">The sb.</param>
		/// <param name="table">The table.</param>
		/// <param name="options">Serialization options to customize the output string. May not be null.</param>
		/// <param name="indentLevel">The current indentation level (used for humanized output formatting).</param>
		/// <remarks>I LOVE GHOSTDOC</remarks>
		private static void TableToJson(StringBuilder sb, Table table, JsonSerializationOptions options, int indentLevel)
		{
			bool first = true;
            
			if (table.Length == 0)
			{
				sb.Append("{");
				InsertNewLine(sb, options);
				InsertIndentation(sb, options, ++indentLevel);
				foreach (TablePair pair in table.Pairs)
				{
					if (pair.Key.Type == DataType.String && IsValueJsonCompatible(pair.Value))
					{
						if (!first)
						{
							sb.Append(',');
							InsertNewLine(sb, options);
							InsertIndentation(sb, options, indentLevel);
						}

						ValueToJson(sb, pair.Key, options, indentLevel);
						InsertColon(sb, options);
						ValueToJson(sb, pair.Value, options, indentLevel);

						first = false;
					}
				}
				InsertNewLine(sb, options);
				InsertIndentation(sb, options, --indentLevel);
				sb.Append("}");
			}
			else
			{
				sb.Append("[");
				InsertNewLine(sb, options);
				InsertIndentation(sb, options, ++indentLevel);
				for (int i = 1; i <= table.Length; i++)
				{
					DynValue value = table.Get(i);
					if (IsValueJsonCompatible(value))
					{
						if (!first)
						{
							sb.Append(',');
							InsertNewLine(sb, options);
							InsertIndentation(sb, options, indentLevel);
						}

                        ValueToJson(sb, value, options, indentLevel);

						first = false;
					}
				}
				InsertNewLine(sb, options);
				InsertIndentation(sb, options, --indentLevel);
				sb.Append("]");
			}
		}

		/// <summary>
		/// Converts a generic object to JSON
		/// </summary>
		public static string ObjectToJson(object obj, JsonSerializationOptions options = null)
		{
			DynValue v = ObjectValueConverter.SerializeObjectToDynValue(null, obj, JsonNull.Create());
			return JsonTableConverter.TableToJson(v.Table, options);
		}



		private static void ValueToJson(StringBuilder sb, DynValue value, JsonSerializationOptions options, int indentLevel)
		{
			switch (value.Type)
			{
				case DataType.Boolean:
					sb.Append(value.Boolean ? "true" : "false");
					break;
				case DataType.Number:
					sb.Append(value.Number.ToString("r"));
					break;
				case DataType.String:
					sb.Append(EscapeString(value.String ?? "", options.EscapeForwardSlashes));
					break;
				case DataType.Table:
					TableToJson(sb, value.Table, options, indentLevel);
					break;
				case DataType.Nil:
				case DataType.Void:
				case DataType.UserData:
				default:
					sb.Append("null");
					break;
			}
		}

		private static string EscapeString(string s, bool escapeForwardSlashes)
		{
			s = s.Replace(@"\", @"\\");
            if (escapeForwardSlashes)
            {
                s = s.Replace(@"/", @"\/");
            }
			s = s.Replace("\"", "\\\"");
			s = s.Replace("\f", @"\f");
			s = s.Replace("\b", @"\b");
			s = s.Replace("\n", @"\n");
			s = s.Replace("\r", @"\r");
			s = s.Replace("\t", @"\t");
			return "\"" + s + "\"";
		}

		private static bool IsValueJsonCompatible(DynValue value)
		{
			return value.Type == DataType.Boolean || value.IsNil() ||
				value.Type == DataType.Number || value.Type == DataType.String ||
				value.Type == DataType.Table ||
				(JsonNull.IsJsonNull(value));
		}

		/// <summary>
		/// Converts a json string to a table
		/// </summary>
		/// <param name="json">The json.</param>
		/// <param name="script">The script to which the table is assigned (null for prime tables).</param>
		/// <returns>A table containing the representation of the given json.</returns>
		public static Table JsonToTable(string json, Script script = null)
		{
			Lexer L = new Lexer(0, json, false);

			if (L.Current.Type == TokenType.Brk_Open_Curly)
				return ParseJsonObject(L, script);
			else if (L.Current.Type == TokenType.Brk_Open_Square)
				return ParseJsonArray(L, script);
			else
				throw new SyntaxErrorException(L.Current, "Unexpected token : '{0}'", L.Current.Text);
		}

		private static void AssertToken(Lexer L, TokenType type)
		{
			if (L.Current.Type != type)
				throw new SyntaxErrorException(L.Current, "Unexpected token : '{0}'", L.Current.Text);
		}
		private static Table ParseJsonArray(Lexer L, Script script)
		{
			Table t = new Table(script);

			L.Next();

			while (L.Current.Type != TokenType.Brk_Close_Square)
			{
				DynValue v = ParseJsonValue(L, script);
				t.Append(v);
				L.Next();

				if (L.Current.Type == TokenType.Comma)
					L.Next();
			}

			return t;
		}

		private static Table ParseJsonObject(Lexer L, Script script)
		{
			Table t = new Table(script);

			L.Next();

			while (L.Current.Type != TokenType.Brk_Close_Curly)
			{
				AssertToken(L, TokenType.String);
				string key = L.Current.Text;
				L.Next();
				AssertToken(L, TokenType.Colon);
				L.Next();
				DynValue v = ParseJsonValue(L, script);
				t.Set(key, v);
				L.Next();

				if (L.Current.Type == TokenType.Comma)
					L.Next();
			}

			return t;
		}

		private static DynValue ParseJsonValue(Lexer L, Script script)
		{
			if (L.Current.Type == TokenType.Brk_Open_Curly)
			{
				Table t = ParseJsonObject(L, script);
				return DynValue.NewTable(t);
			}
			else if (L.Current.Type == TokenType.Brk_Open_Square)
			{
				Table t = ParseJsonArray(L, script);
				return DynValue.NewTable(t);
			}
			else if (L.Current.Type == TokenType.String)
			{
				return DynValue.NewString(L.Current.Text);
			}
			else if (L.Current.Type == TokenType.Number || L.Current.Type == TokenType.Op_MinusOrSub)
			{
				return ParseJsonNumberValue(L, script);
			}
			else if (L.Current.Type == TokenType.True)
			{
				return DynValue.True;
			}
			else if (L.Current.Type == TokenType.False)
			{
				return DynValue.False;
			}
			else if (L.Current.Type == TokenType.Name && L.Current.Text == "null")
			{
				return JsonNull.Create();
			}
			else
			{
				throw new SyntaxErrorException(L.Current, "Unexpected token : '{0}'", L.Current.Text);
			}
		}

		private static DynValue ParseJsonNumberValue(Lexer L, Script script)
		{
			bool negative;
			if (L.Current.Type == TokenType.Op_MinusOrSub)
			{
				// Negative number consists of 2 tokens.
				L.Next();
				negative = true;
			}
			else
			{
				negative = false;
			}
			if (L.Current.Type != TokenType.Number)
			{
				throw new SyntaxErrorException(L.Current, "Unexpected token : '{0}'", L.Current.Text);
			}
			var numberValue = L.Current.GetNumberValue();
			if (negative)
			{
				numberValue = -numberValue;
			}
			return DynValue.NewNumber(numberValue).AsReadOnly();
		}
	}
}
