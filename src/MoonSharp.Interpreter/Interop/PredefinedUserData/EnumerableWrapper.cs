﻿using System.Collections;
using MoonSharp.Interpreter.Interop.Converters;

namespace MoonSharp.Interpreter.Interop
{
	/// <summary>
	/// Wrappers for enumerables as return types
	/// </summary>
	internal class EnumerableWrapper : IUserDataType
	{
		IEnumerator m_Enumerator;
		DynValue m_Prev = DynValue.Nil;
		bool m_HasTurnOnce = false;

		private EnumerableWrapper(IEnumerator enumerator)
		{
			m_Enumerator = enumerator;
		}

		public void Reset()
		{
			if (m_HasTurnOnce)
				m_Enumerator.Reset();

			m_HasTurnOnce = true;
		}

		private DynValue GetNext(DynValue prev)
		{
			if (prev.IsNil())
				Reset();

			while (m_Enumerator.MoveNext())
			{
				DynValue v = ClrToScriptConversions.ObjectToDynValue(m_Enumerator.Current);

				if (!v.IsNil())
					return v;
			}

			return DynValue.Nil;
		}

		private DynValue LuaIteratorCallback(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			m_Prev = this.GetNext(m_Prev);
			return m_Prev;
		}

		internal static DynValue ConvertIterator(IEnumerator enumerator)
		{
			EnumerableWrapper ei = new EnumerableWrapper(enumerator);
			return DynValue.NewTuple(UserData.Create(ei), DynValue.Nil, DynValue.Nil);
		}

		internal static DynValue ConvertTable(Table table)
		{
			return ConvertIterator(table.Values.GetEnumerator());
		}


		public DynValue Index(Script script, DynValue index, bool isDirectIndexing)
		{
#warning TODO do we need script argument?
			if (index.Type == DataType.String)
			{
				string idx = index.String;

				if (idx == "Current" || idx == "current")
				{
					return DynValue.FromObject(m_Enumerator.Current);
				}
				else if (idx == "MoveNext" || idx == "moveNext" || idx == "move_next")
				{
					return DynValue.NewCallback((ctx, args) => DynValue.NewBoolean(m_Enumerator.MoveNext()));
				}
				else if (idx == "Reset" || idx == "reset")
				{
					return DynValue.NewCallback((ctx, args) => { Reset(); return DynValue.Nil; });
				}
			}
			return null;
		}

		public bool SetIndex(Script script, DynValue index, DynValue value, bool isDirectIndexing)
		{
			return false;
		}

		public DynValue MetaIndex(string metaname)
		{
			if (metaname == "__call")
				return DynValue.NewCallback(LuaIteratorCallback);
			else
				return null;
		}
	}
}
