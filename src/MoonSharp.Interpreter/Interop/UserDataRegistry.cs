using MoonSharp.Interpreter.Interop.UserDataRegistries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MoonSharp.Interpreter.Interop.RegistrationPolicies;

namespace MoonSharp.Interpreter.Interop
{
	public class UserDataRegistry
	{
		public UserDataRegistry()
		{
			this.RegisterType<StandardDescriptors.EventFacade>(InteropAccessMode.NoReflectionAllowed);
			this.RegisterType<AnonWrapper>(InteropAccessMode.HideMembers);
			this.RegisterType<EnumerableWrapper>(InteropAccessMode.NoReflectionAllowed);
			this.RegisterType<Serialization.Json.JsonNull>(InteropAccessMode.Reflection);
		}

		internal TypeDescriptorRegistry TypeDescriptorRegistry { get; } = new TypeDescriptorRegistry();
		internal ExtensionMethodsRegistry ExtensionMethodsRegistry { get; } = new ExtensionMethodsRegistry();

		private void RegisterType<T>(InteropAccessMode accessMode)
		{
			this.RegisterType(typeof(T), accessMode, null, null);
		}

		public IRegistrationPolicy RegistrationPolicy
		{
			get { return this.TypeDescriptorRegistry.RegistrationPolicy; }
			set { this.TypeDescriptorRegistry.RegistrationPolicy = value; }
		}

		public IUserDataDescriptor RegisterType(Type type, InteropAccessMode accessMode, string friendlyName, IUserDataDescriptor descriptor)
		{
			return this.TypeDescriptorRegistry.RegisterType_Impl(this, type, accessMode, friendlyName, descriptor);
		}

#warning TODO find out what a proxy type is
		public IUserDataDescriptor RegisterProxyType(IProxyFactory proxyFactory, InteropAccessMode accessMode, string friendlyName)
		{
			return this.TypeDescriptorRegistry.RegisterProxyType_Impl(this, proxyFactory, accessMode, friendlyName);
		}

		/// <summary>
		/// Registers all types marked with a MoonSharpUserDataAttribute that ar contained in an assembly.
		/// </summary>
		/// <param name="asm">The assembly.</param>
		/// <param name="includeExtensionTypes">if set to <c>true</c> extension types are registered to the appropriate registry.</param>
		public void RegisterAssembly(Assembly asm, bool includeExtensionTypes)
		{
			if (asm == null)
			{
#if NETFX_CORE || DOTNET_CORE
					throw new NotSupportedException("Assembly.GetCallingAssembly is not supported on target framework.");
#else
				asm = Assembly.GetCallingAssembly();
#endif
			}

			if (includeExtensionTypes)
			{
				var extensionTypes = from t in asm.SafeGetTypes()
									 let attributes = Compatibility.Framework.Do.GetCustomAttributes(t, typeof(System.Runtime.CompilerServices.ExtensionAttribute), true)
									 where attributes != null && attributes.Length > 0
									 select new { Attributes = attributes, DataType = t };

				foreach (var extType in extensionTypes)
				{
					UserData.RegisterExtensionType(this, extType.DataType);
				}
			}


			var userDataTypes = from t in asm.SafeGetTypes()
								let attributes = Compatibility.Framework.Do.GetCustomAttributes(t, typeof(MoonSharpUserDataAttribute), true)
								where attributes != null && attributes.Length > 0
								select new { Attributes = attributes, DataType = t };

			foreach (var userDataType in userDataTypes)
			{
				UserData.RegisterType(this, userDataType.DataType, userDataType.Attributes
					.OfType<MoonSharpUserDataAttribute>()
					.First()
					.AccessMode);
			}
		}

		public bool IsTypeRegistered(Type type)
		{
			return this.TypeDescriptorRegistry.IsTypeRegistered(type);
		}

		public void UnregisterType(Type type)
		{
			this.TypeDescriptorRegistry.UnregisterType(type);
		}

		public IUserDataDescriptor GetDescriptorForType(Type type, bool searchInterfaces)
		{
			return this.TypeDescriptorRegistry.GetDescriptorForType(this, type, searchInterfaces);
		}

		public IEnumerable<KeyValuePair<Type, IUserDataDescriptor>> GetRegisteredTypeDescriptors(bool useHistoricalData)
		{
			var registry = this.TypeDescriptorRegistry;
			return useHistoricalData ? registry.RegisteredTypesHistory : registry.RegisteredTypes;
		}
	}
}
