using System.Collections.Generic;
using MoonSharp.Interpreter.Interop.BasicDescriptors;

namespace MoonSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors
{
	public abstract class HardwiredMethodMemberDescriptor : FunctionMemberDescriptorBase
	{
		public override DynValue Execute(object obj, ScriptExecutionContext context, CallbackArguments args)
		{
			this.CheckAccess(MemberDescriptorAccess.CanExecute, obj);

			List<int> outParams = null;
			object[] pars = base.BuildArgumentList(obj, context, args, out outParams);
			object retv = Invoke(context.OwnerScript, obj, pars, CalcArgsCount(pars));

			return DynValue.FromObject(retv);
		}

		private int CalcArgsCount(object[] pars)
		{
			int count = pars.Length;

			for(int i = 0; i < pars.Length; i++)
				if (Parameters[i].HasDefaultValue && (pars[i] is DefaultValue))
				{
					count -= 1;
				}

			return count;
		}

		// TODO do we need Script argument? Considering that it's already in pars[0] anyway if needed.
		protected abstract object Invoke(Script script, object obj, object[] pars, int argscount);
	}
}
