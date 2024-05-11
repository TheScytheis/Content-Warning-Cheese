using System;
using System.Collections.Generic;
using System.Reflection;

namespace TestUnityPlugin
{
    public class ReflectionUtil<R>
    {
        private const BindingFlags privateInst = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags privateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        private const BindingFlags privateField = privateInst | BindingFlags.GetField;
        private const BindingFlags privateProp = privateInst | BindingFlags.GetProperty;
        private const BindingFlags privateMethod = privateInst | BindingFlags.InvokeMethod;
        private const BindingFlags staticField = privateStatic | BindingFlags.GetField;
        private const BindingFlags staticProp = privateStatic | BindingFlags.GetProperty;
        private const BindingFlags staticMethod = privateStatic | BindingFlags.InvokeMethod;

        private R @object { get; }
        private Type type { get; }

        internal ReflectionUtil(R obj)
        {
            this.@object = obj;
            this.type = typeof(R);
        }

        private object GetValue(string variableName, BindingFlags flags)
        {
            var field = this.type.GetField(variableName, flags);
            return field != null ? field.GetValue(this.@object) : null;
        }

        private object GetProperty(string propertyName, BindingFlags flags)
        {
            var property = this.type.GetProperty(propertyName, flags);
            return property != null ? property.GetValue(this.@object) : null;
        }

        private void SetValue(string variableName, object value, BindingFlags flags)
        {
            var field = this.type.GetField(variableName, flags);
            if (field != null)
            {
                field.SetValue(this.@object, value);
            }
        }

        private void SetProperty(string propertyName, object value, BindingFlags flags)
        {
            var property = this.type.GetProperty(propertyName, flags);
            if (property != null)
            {
                property.SetValue(this.@object, value);
            }
        }

        private object InvokeMethod(string methodName, BindingFlags flags, params object[] args)
        {
            var methods = this.type.GetMethods(flags);
            foreach (var method in methods)
            {
                if (method.Name == methodName && MatchMethodParameters(method, args))
                {
                    return method.Invoke(this.@object, args);
                }
            }
            return null;
        }

        private bool MatchMethodParameters(MethodInfo method, object[] args)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != args.Length) return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].ParameterType.IsInstanceOfType(args[i]) && args[i] != null)
                    return false;
            }
            return true;
        }

        public object GetValue(string fieldName, bool isStatic = false, bool isProperty = false)
        {
            BindingFlags flags = isProperty ? isStatic ? staticProp : privateProp : isStatic ? staticField : privateField;
            return isProperty ? GetProperty(fieldName, flags) : GetValue(fieldName, flags);
        }

        public void SetValue(string fieldName, object value, bool isStatic = false, bool isProperty = false)
        {
            BindingFlags flags = isProperty ? isStatic ? staticProp : privateProp : isStatic ? staticField : privateField;
            if (isProperty) SetProperty(fieldName, value, flags);
            else SetValue(fieldName, value, flags);
        }

        public object Invoke(string methodName, bool isStatic = false, params object[] args)
        {
            return InvokeMethod(methodName, isStatic ? staticMethod : privateMethod, args);
        }
    }

    public static class ReflectorExtensions
    {
        public static ReflectionUtil<R> Reflect<R>(this R obj) => new ReflectionUtil<R>(obj);
    }
}
