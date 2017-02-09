using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using MoonSharp.Interpreter.Interop;
using MoonSharp.Interpreter.Interop.BasicDescriptors;
using MoonSharp.Interpreter.Interop.RegistrationPolicies;
using MoonSharp.Interpreter.Interop.StandardDescriptors;
using MoonSharp.Interpreter.Interop.UserDataRegistries;
using MoonSharp.Interpreter.Serialization.Json;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// Class exposing C# objects as Lua userdata.
	/// For efficiency, a global registry of types is maintained, instead of a per-script one.
	/// </summary>
	public class UserData : RefIdObject
	{
		private UserData(object @object, Type type)
		{
			// This type can only be instantiated using one of the Create methods
			this.Object = @object;
			this.Type = type.NotNull(nameof(type));
		}

		/// <summary>
		/// Gets or sets the "uservalue". See debug.getuservalue and debug.setuservalue.
		/// http://www.lua.org/manual/5.2/manual.html#pdf-debug.setuservalue
		/// </summary>
		public DynValue UserValue { get; set; }

		/// <summary>
		/// Gets the object associated to this userdata (null for statics)
		/// </summary>
		public object Object { get; }

        public Type Type { get; }

		/// <summary>
		/// Gets the type descriptor of this userdata
		/// </summary>
		//public IUserDataDescriptor Descriptor { get; private set; }
#warning TODO step 1: look up each time, step 2: use BoundUserData, should have almost the same performance

        public IUserDataDescriptor GetDescriptor(UserDataRegistry registry)
		{
			return GetDescriptorForType(registry, this.Type, this.Object != null);
		}


		static UserData()
		{
#warning TODO
        }

		/// <summary>
		/// Registers a type for userdata interop
		/// </summary>
		/// <typeparam name="T">The type to be registered</typeparam>
		/// <param name="accessMode">The access mode (optional).</param>
		/// <param name="friendlyName">Friendly name for the type (optional)</param>
		public static IUserDataDescriptor RegisterType<T>(UserDataRegistry registry, InteropAccessMode accessMode = InteropAccessMode.Default, string friendlyName = null)
		{
			return registry.NotNull(nameof(registry)).RegisterType(typeof(T), accessMode, friendlyName, null);
		}

#if RCOMPAT
		public static IUserDataDescriptor RegisterType<T>(InteropAccessMode accessMode = InteropAccessMode.Default, string friendlyName = null)
		{
			return RegisterType<T>(UserDataRegistry.DefaultRegistry, accessMode, friendlyName);
		}

		public static IUserDataDescriptor RegisterType(Type type, InteropAccessMode accessMode = InteropAccessMode.Default, string friendlyName = null)
		{
			return RegisterType(UserDataRegistry.DefaultRegistry, type, accessMode, friendlyName);
		}

		public static void RegisterExtensionType(Type type, InteropAccessMode accessMode = InteropAccessMode.Default)
		{
			RegisterExtensionType(UserDataRegistry.DefaultRegistry, type, accessMode);
		}

		public static void UnregisterType<T>()
		{
			UnregisterType<T>(UserDataRegistry.DefaultRegistry);
		}

		public static void UnregisterType(Type type)
		{
			UnregisterType(UserDataRegistry.DefaultRegistry, type);
		}

		public static IUserDataDescriptor RegisterType(IUserDataDescriptor descriptor)
		{
			return RegisterType(UserDataRegistry.DefaultRegistry, descriptor);
		}

		public static IUserDataDescriptor RegisterProxyType<TProxy, TTarget>(Func<TTarget, TProxy> wrapDelegate, InteropAccessMode accessMode = InteropAccessMode.Default, string friendlyName = null)
			where TProxy : class
			where TTarget : class
		{
			return RegisterProxyType<TProxy, TTarget>(UserDataRegistry.DefaultRegistry, wrapDelegate, accessMode, friendlyName);
		}

		public static IUserDataDescriptor RegisterType<T>(IUserDataDescriptor customDescriptor)
		{
			return RegisterType<T>(UserDataRegistry.DefaultRegistry, customDescriptor);
		}

		public static Table GetDescriptionOfRegisteredTypes(bool useHistoricalData = false)
		{
			return GetDescriptionOfRegisteredTypes(UserDataRegistry.DefaultRegistry, useHistoricalData);
		}
#endif

		/// <summary>
		/// Registers a type for userdata interop
		/// </summary>
		/// <param name="type">The type to be registered</param>
		/// <param name="accessMode">The access mode (optional).</param>
		/// <param name="friendlyName">Friendly name for the type (optional)</param>
		public static IUserDataDescriptor RegisterType(UserDataRegistry registry, Type type, InteropAccessMode accessMode = InteropAccessMode.Default, string friendlyName = null)
		{
			return registry.NotNull(nameof(registry)).RegisterType(type, accessMode, friendlyName, null);
		}


		/// <summary>
		/// Registers a proxy type.
		/// </summary>
		/// <param name="proxyFactory">The proxy factory.</param>
		/// <param name="accessMode">The access mode.</param>
		/// <param name="friendlyName">A friendly name for the descriptor.</param>
		/// <returns></returns>
		public static IUserDataDescriptor RegisterProxyType(UserDataRegistry registry, IProxyFactory proxyFactory, InteropAccessMode accessMode = InteropAccessMode.Default, string friendlyName = null)
		{
			return registry.NotNull(nameof(registry)).RegisterProxyType(proxyFactory, accessMode, friendlyName);
		}

		/// <summary>
		/// Registers a proxy type using a delegate.
		/// </summary>
		/// <typeparam name="TProxy">The type of the proxy.</typeparam>
		/// <typeparam name="TTarget">The type of the target.</typeparam>
		/// <param name="wrapDelegate">A delegate creating a proxy object from a target object.</param>
		/// <param name="accessMode">The access mode.</param>
		/// <param name="friendlyName">A friendly name for the descriptor.</param>
		/// <returns></returns>
		public static IUserDataDescriptor RegisterProxyType<TProxy, TTarget>(UserDataRegistry registry, Func<TTarget, TProxy> wrapDelegate, InteropAccessMode accessMode = InteropAccessMode.Default, string friendlyName = null)
			where TProxy : class
			where TTarget : class
		{
			return RegisterProxyType(registry, new DelegateProxyFactory<TProxy, TTarget>(wrapDelegate), accessMode, friendlyName);
		}



		/// <summary>
		/// Registers a type with a custom userdata descriptor
		/// </summary>
		/// <typeparam name="T">The type to be registered</typeparam>
		/// <param name="customDescriptor">The custom descriptor.</param>
		public static IUserDataDescriptor RegisterType<T>(UserDataRegistry registry, IUserDataDescriptor customDescriptor)
		{
			return registry.NotNull(nameof(registry)).RegisterType(typeof(T), InteropAccessMode.Default, null, customDescriptor);
		}
#warning TODO try to remove this - why can we register a IUserDataDescriptor with a possible incompatible type?

		/// <summary>
		/// Registers a type with a custom userdata descriptor
		/// </summary>
		/// <param name="type">The type to be registered</param>
		/// <param name="customDescriptor">The custom descriptor.</param>
		public static IUserDataDescriptor RegisterType(UserDataRegistry registry, Type type, IUserDataDescriptor customDescriptor)
		{
			return registry.NotNull(nameof(registry)).RegisterType(type, InteropAccessMode.Default, null, customDescriptor);
		}
#warning TODO try to remove this - why can we register a IUserDataDescriptor with a possible incompatible type?

		/// <summary>
		/// Registers a type with a custom userdata descriptor
		/// </summary>
		/// <param name="customDescriptor">The custom descriptor.</param>
		public static IUserDataDescriptor RegisterType(UserDataRegistry registry, IUserDataDescriptor customDescriptor)
		{
			return registry.NotNull(nameof(registry)).RegisterType(customDescriptor.Type, InteropAccessMode.Default, null, customDescriptor);
		}


		/// <summary>
		/// Registers all types marked with a MoonSharpUserDataAttribute that ar contained in an assembly.
		/// </summary>
		/// <param name="asm">The assembly.</param>
		/// <param name="includeExtensionTypes">if set to <c>true</c> extension types are registered to the appropriate registry.</param>
		public static void RegisterAssembly(UserDataRegistry registry, Assembly asm = null, bool includeExtensionTypes = false)
		{
			registry.NotNull(nameof(registry)).RegisterAssembly(asm, includeExtensionTypes);
		}

		/// <summary>
		/// Determines whether the specified type is registered. Note that this should be used only to check if a descriptor
		/// has been registered EXACTLY. For many types a descriptor can still be created, for example through the descriptor
		/// of a base type or implemented interfaces.
		/// </summary>
		/// <param name="t">The type.</param>
		/// <returns></returns>
		public static bool IsTypeRegistered(UserDataRegistry registry, Type t)
		{
			return registry.NotNull(nameof(registry)).IsTypeRegistered(t);
		}

		/// <summary>
		/// Determines whether the specified type is registered. Note that this should be used only to check if a descriptor
		/// has been registered EXACTLY. For many types a descriptor can still be created, for example through the descriptor
		/// of a base type or implemented interfaces.
		/// </summary>
		/// <typeparam name="T">The type.</typeparam>
		/// <returns></returns>
		public static bool IsTypeRegistered<T>(UserDataRegistry registry)
		{
			return registry.NotNull(nameof(registry)).IsTypeRegistered(typeof(T));
		}

		/// <summary>
		/// Unregisters a type. 
		/// WARNING: unregistering types at runtime is a dangerous practice and may cause unwanted errors.
		/// Use this only for testing purposes or to re-register the same type in a slightly different way.
		/// Additionally, it's a good practice to discard all previous loaded scripts after calling this method.
		/// </summary>
		/// <typeparam name="T">The type to be unregistered</typeparam>
		public static void UnregisterType<T>(UserDataRegistry registry)
		{
			registry.NotNull(nameof(registry)).UnregisterType(typeof(T));
		}

		/// <summary>
		/// Unregisters a type.
		/// WARNING: unregistering types at runtime is a dangerous practice and may cause unwanted errors.
		/// Use this only for testing purposes or to re-register the same type in a slightly different way.
		/// Additionally, it's a good practice to discard all previous loaded scripts after calling this method.
		/// </summary>
		/// <param name="t">The The type to be unregistered</param>
		public static void UnregisterType(UserDataRegistry registry, Type t)
		{
			registry.NotNull(nameof(registry)).UnregisterType(t);
		}

		/// <summary>
		/// Creates a userdata DynValue from the specified object, using a specific descriptor
		/// </summary>
		/// <param name="o">The object</param>
		/// <param name="descr">The descriptor.</param>
		/// <returns></returns>
		public static DynValue Create(object o, IUserDataDescriptor descr)
		{
			return DynValue.NewUserData(new UserData(o, o.GetType()));
			//{
			//	Descriptor = descr,
			//	Object = o
			//});
		}

		/// <summary>
		/// Creates a userdata DynValue from the specified object
		/// </summary>
		/// <param name="o">The object</param>
		/// <returns></returns>
		public static DynValue Create(object o)
		{
			//UserDataRegistry registry = null;
			#warning TODO this is the interesting part
			// we have to create an instance of UserData without an actual IUserDataDescriptor.
			// The IUserDataDescritor will be acquired when the user data is being assigned to a script.
			//var descr = GetDescriptorForObject(registry, o);
			//if (descr == null)
			//{
			//	if (o is Type)
			//		return CreateStatic((Type)o);

			//	return null;
			//}

			return Create(o, null);
		}

		/// <summary>
		/// Creates a static userdata DynValue from the specified IUserDataDescriptor
		/// </summary>
		/// <param name="descr">The IUserDataDescriptor</param>
		/// <returns></returns>
		public static DynValue CreateStatic(IUserDataDescriptor descr)
		{
			if (descr == null) return null;

			return CreateStatic(descr.Type);
		}

		/// <summary>
		/// Creates a static userdata DynValue from the specified Type
		/// </summary>
		/// <param name="t">The type</param>
		/// <returns></returns>
		public static DynValue CreateStatic(Type t)
		{
			//UserDataRegistry registry = null;
#warning TODO this is the interesting part
			// we have to create an instance of UserData without an actual IUserDataDescriptor.
			// The IUserDataDescritor will be acquired when the user data is being assigned to a script.
			//return CreateStatic(GetDescriptorForType(registry, t, false));
			return DynValue.NewUserData(new UserData(null, t));
		}

		/// <summary>
		/// Creates a static userdata DynValue from the specified Type
		/// </summary>
		/// <typeparam name="T">The Type</typeparam>
		/// <returns></returns>
		public static DynValue CreateStatic<T>()
		{
			return CreateStatic(typeof(T));
		}

		/// <summary>
		/// Gets or sets the registration policy to be used in the whole application
		/// </summary>
		public static IRegistrationPolicy RegistrationPolicy
		{
			get { return UserDataRegistry.DefaultRegistry.TypeDescriptorRegistry.RegistrationPolicy; }
			set { UserDataRegistry.DefaultRegistry.TypeDescriptorRegistry.RegistrationPolicy = value; }
		}

		public static IRegistrationPolicy GetRegistrationPolicy(UserDataRegistry registry)
        {
            return registry.NotNull(nameof(registry)).RegistrationPolicy;
        }

        public static void SetRegistrationPolicy(UserDataRegistry registry, IRegistrationPolicy value)
        {
            registry.NotNull(nameof(registry)).RegistrationPolicy = value;
        }

		/// <summary>
		/// Gets or sets the default access mode to be used in the whole application
		/// </summary>
		/// <value>
		/// The default access mode.
		/// </value>
		/// <exception cref="System.ArgumentException">InteropAccessMode is InteropAccessMode.Default</exception>
		public static InteropAccessMode DefaultAccessMode
		{
			get { return UserDataRegistry.DefaultRegistry.TypeDescriptorRegistry.DefaultAccessMode; }
			set { UserDataRegistry.DefaultRegistry.TypeDescriptorRegistry.DefaultAccessMode = value; }
		}

		/// <summary>
		/// Registers an extension Type (that is a type containing extension methods)
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="mode">The InteropAccessMode.</param>
		public static void RegisterExtensionType(UserDataRegistry registry, Type type, InteropAccessMode mode = InteropAccessMode.Default)
		{
			registry.NotNull(nameof(registry)).ExtensionMethodsRegistry.RegisterExtensionType(registry, type, mode);
		}

		/// <summary>
		/// Gets all the extension methods which can match a given name and extending a given Type
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="extendedType">The extended type.</param>
		/// <returns></returns>
		public static List<IOverloadableMemberDescriptor> GetExtensionMethodsByNameAndType(UserDataRegistry registry, string name, Type extendedType)
		{
			return registry.NotNull(nameof(registry)).ExtensionMethodsRegistry.GetExtensionMethodsByNameAndType(registry, name, extendedType);
		}

		/// <summary>
		/// Gets a number which gets incremented everytime the extension methods registry changes.
		/// Use this to invalidate caches based on extension methods
		/// </summary>
		/// <returns></returns>
		public static int GetExtensionMethodsChangeVersion(UserDataRegistry registry)
		{
			return registry.NotNull(nameof(registry)).ExtensionMethodsRegistry.GetExtensionMethodsChangeVersion();
		}

		/// <summary>
		/// Gets the best possible type descriptor for a specified CLR type.
		/// </summary>
		/// <typeparam name="T">The CLR type for which the descriptor is desired.</typeparam>
		/// <param name="searchInterfaces">if set to <c>true</c> interfaces are used in the search.</param>
		/// <returns></returns>
		public static IUserDataDescriptor GetDescriptorForType<T>(UserDataRegistry registry, bool searchInterfaces)
		{
			return registry.NotNull(nameof(registry)).GetDescriptorForType(typeof(T), searchInterfaces);
		}

		/// <summary>
		/// Gets the best possible type descriptor for a specified CLR type.
		/// </summary>
		/// <param name="type">The CLR type for which the descriptor is desired.</param>
		/// <param name="searchInterfaces">if set to <c>true</c> interfaces are used in the search.</param>
		/// <returns></returns>
		public static IUserDataDescriptor GetDescriptorForType(UserDataRegistry registry, Type type, bool searchInterfaces)
		{
			return registry.NotNull(nameof(registry)).GetDescriptorForType(type, searchInterfaces);
		}


		/// <summary>
		/// Gets the best possible type descriptor for a specified CLR object.
		/// </summary>
		/// <param name="o">The object.</param>
		/// <returns></returns>
		public static IUserDataDescriptor GetDescriptorForObject(UserDataRegistry registry, object o)
		{
			return registry.NotNull(nameof(registry)).GetDescriptorForType(o.GetType(), true);
		}


		/// <summary>
		/// Gets a table with the description of registered types.
		/// </summary>
		/// <param name="useHistoricalData">if set to true, it will also include the last found descriptor of all unregistered types.</param>
		/// <returns></returns>
		public static Table GetDescriptionOfRegisteredTypes(UserDataRegistry registry, bool useHistoricalData = false)
		{
			var registeredTypesPairs = registry.NotNull(nameof(registry)).GetRegisteredTypeDescriptors(useHistoricalData);
			DynValue output = DynValue.NewPrimeTable();

			foreach (var descpair in registeredTypesPairs)
			{
				IWireableDescriptor sd = descpair.Value as IWireableDescriptor;

				if (sd != null)
				{
					DynValue t = DynValue.NewPrimeTable();
					output.Table.Set(descpair.Key.FullName, t);
					sd.PrepareForWiring(t.Table);
				}
			}

			return output.Table;
		}

		/// <summary>
		/// Gets all the registered types.
		/// </summary>
		/// <param name="useHistoricalData">if set to true, it will also include the last found descriptor of all unregistered types.</param>
		/// <returns></returns>
		public static IEnumerable<Type> GetRegisteredTypes(UserDataRegistry registry, bool useHistoricalData = false)
		{
			var registeredTypesPairs = registry.NotNull(nameof(registry)).GetRegisteredTypeDescriptors(useHistoricalData);
#warning TODO shouldn't this be p => p.Key?
			//return registeredTypesPairs.Select(p => p.Value.Type);
			return registeredTypesPairs.Select(p => p.Key);
		}

		

	}
}
