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
		}

		internal TypeDescriptorRegistry TypeDescriptorRegistry { get; } = new TypeDescriptorRegistry();
		internal ExtensionMethodsRegistry ExtensionMethodsRegistry { get; } = new ExtensionMethodsRegistry();

		public IRegistrationPolicy RegistrationPolicy
		{
			get { return this.TypeDescriptorRegistry.RegistrationPolicy; }
			set { this.TypeDescriptorRegistry.RegistrationPolicy = value; }
		}

		public IUserDataDescriptor RegisterType(Type type, InteropAccessMode accessMode, string friendlyName, IUserDataDescriptor descriptor)
		{
			return this.TypeDescriptorRegistry.RegisterType_Impl(type, accessMode, friendlyName, descriptor);
		}

#warning TODO find out what a proxy type is
		public IUserDataDescriptor RegisterProxyType(IProxyFactory proxyFactory, InteropAccessMode accessMode, string friendlyName)
		{
			return this.TypeDescriptorRegistry.RegisterProxyType_Impl(proxyFactory, accessMode, friendlyName);
		}

		private void RegisterAssembly(Assembly asm, bool includeExtensionTypes)
		{
#warning TODO remove
			this.TypeDescriptorRegistry.RegisterAssembly(asm, includeExtensionTypes);
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
			return this.TypeDescriptorRegistry.GetDescriptorForType(type, searchInterfaces);
		}

		public IEnumerable<KeyValuePair<Type, IUserDataDescriptor>> GetRegisteredTypeDescriptors(bool useHistoricalData)
		{
			var registry = this.TypeDescriptorRegistry;
			return useHistoricalData ? registry.RegisteredTypesHistory : registry.RegisteredTypes;
		}
	}
}
