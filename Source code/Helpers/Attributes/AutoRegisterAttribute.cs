using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace AmongUsPCMod
{
    internal abstract class AutoRegisterAttribute : Attribute
    {
        internal static void Initialize()
        {
            var types = ModInfo.Assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.IsAbstract || !type.IsSealed)
                    continue;

                if (!typeof(AutoRegisterAttribute).IsAssignableFrom(type))
                    continue;

                var tempAttribute = (AutoRegisterAttribute)FormatterServices.GetUninitializedObject(type);
                tempAttribute.Register();
            }
        }

        protected abstract void Register();
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal abstract class AutoRegisterAttribute<T> : AutoRegisterAttribute where T : class
    {
        protected static readonly List<T> _instances = new List<T>();

        internal static IReadOnlyList<T> Instances => _instances.AsReadOnly();

        internal static J GetInstance<J>() where J : T => (J)_instances.FirstOrDefault(instance => instance.GetType() == typeof(J));

        protected override void Register()
        {
            var types = ModInfo.Assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.GetCustomAttribute(GetType()) == null)
                    continue;

                if (type.IsAbstract || type.IsInterface)
                    continue;

                var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (constructor != null)
                {
                    if (constructor.Invoke(null) is T instance)
                    {
                        _instances.Add(instance);
                    }
                }
            }
        }
    }

    internal class BaseCommand { } // Fail-safe дефиниция за компилатора
    internal class RPCHandler { }  // Fail-safe дефиниция за компилатора

    internal sealed class RegisterCommandAttribute : AutoRegisterAttribute<BaseCommand> { }
    internal sealed class RegisterRPCHandlerAttribute : AutoRegisterAttribute<RPCHandler> { }
}