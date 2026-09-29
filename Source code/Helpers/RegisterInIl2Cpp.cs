using System;
using System.Reflection;
using Il2CppInterop.Runtime.Injection;

namespace AmongUsPCMod
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class RegisterInIl2Cpp : Attribute
    {
        public Type[] Interfaces { get; }

        public RegisterInIl2Cpp(params Type[] interfaces)
        {
            Interfaces = interfaces ?? new Type[0];
        }

        internal static void Initialize()
        {
            var types = ModInfo.Assembly.GetTypes();
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<RegisterInIl2Cpp>();
                if (attr == null)
                    continue;

                try
                {
                    if (attr.Interfaces.Length > 0)
                        ClassInjector.RegisterTypeInIl2Cpp(type, new RegisterTypeOptions { Interfaces = attr.Interfaces });
                    else
                        ClassInjector.RegisterTypeInIl2Cpp(type);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[THE FAC ERROR]: Failed to register {type.Name}: {ex.Message}");
                }
            }
        }
    }
}
