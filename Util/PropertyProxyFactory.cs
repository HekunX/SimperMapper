using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Util
{
    public static class PropertyProxyFactory
    {
        public static IPropertyProxy GetPropertyProxy(PropertyInfo propertyInfo)
        {
            switch (propertyInfo.PropertyType.FullName)
            {
                case "System.String":
                    {
                        return CreateProppertyProxy<String>(propertyInfo);
                    }
                case "System.Int32":
                    {
                        return CreateProppertyProxy<Int32>(propertyInfo);
                    }
                case "System.Int64":
                    {
                        return CreateProppertyProxy<Int64>(propertyInfo);
                    }
                case "System.Boolean":
                    {
                        return CreateProppertyProxy<Boolean>(propertyInfo);
                    }
                case "System.Double":
                    {
                        return CreateProppertyProxy<Double>(propertyInfo);
                    }
                case "System.DateTime":
                    {
                        return CreateProppertyProxy<DateTime>(propertyInfo);
                    }
                default:
                    {
                        if (propertyInfo.PropertyType.IsEnum) return CreateProppertyProxy(propertyInfo);
                        if (propertyInfo.PropertyType.IsValueType && propertyInfo.PropertyType.Name.Equals("Nullable`1"))
                        {
                            switch (propertyInfo.PropertyType.GenericTypeArguments[0].FullName)
                            {
                                case "System.Int32":
                                    {
                                        return CreateProppertyProxy<Nullable<Int32>>(propertyInfo);
                                    }
                                case "System.Int64":
                                    {
                                        return CreateProppertyProxy<Nullable<Int64>>(propertyInfo);
                                    }
                                case "System.Boolean":
                                    {
                                        return CreateProppertyProxy<Nullable<Boolean>>(propertyInfo);
                                    }
                                case "System.Double":
                                    {
                                        return CreateProppertyProxy<Nullable<Double>>(propertyInfo);
                                    }
                                case "System.DateTime":
                                    {
                                        return CreateProppertyProxy<Nullable<DateTime>>(propertyInfo);
                                    }
                                default: break;
                            }
                        }
                        return CreateProppertyProxy(propertyInfo);
                    }
                
            }
        }
        private static PropertyProxy<T> CreateProppertyProxy<T>(PropertyInfo propertyInfo)
        {
            var setMethod = propertyInfo.SetMethod;
            DynamicMethod dynamicSetMethod = new DynamicMethod($"{propertyInfo.Name}_SetProxy", null, new Type[] { typeof(object), typeof(T) }, true);
            var setterIL = dynamicSetMethod.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Callvirt, setMethod);
            setterIL.Emit(OpCodes.Ret);
            var setter = dynamicSetMethod.CreateDelegate(typeof(Action<object, T>)) as Action<object, T>;

            var getMethod = propertyInfo.GetMethod;
            DynamicMethod dynamicGetMethod = new DynamicMethod($"{propertyInfo.Name}_GetProxy", typeof(T), new Type[] { typeof(object) }, true);
            var getterIL = dynamicGetMethod.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Callvirt, getMethod);
            getterIL.Emit(OpCodes.Ret);
            var getter = dynamicGetMethod.CreateDelegate(typeof(Func<object, T>)) as Func<object, T>;

            var proxy = new PropertyProxy<T>(setter, getter);
            return proxy;
        }

        private static PropertyProxy CreateProppertyProxy (PropertyInfo propertyInfo)
        {
            var setMethod = propertyInfo.SetMethod;
            DynamicMethod dynamicSetMethod = new DynamicMethod($"{propertyInfo.Name}_SetProxy", null, new Type[] { typeof(object), propertyInfo.PropertyType }, true);
            var setterIL = dynamicSetMethod.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Callvirt, setMethod);
            setterIL.Emit(OpCodes.Ret);

            var genericAction = typeof(Action<,>);
            var getDelegate = genericAction.MakeGenericType(new Type[] { typeof(object), propertyInfo.PropertyType });

            var setter = dynamicSetMethod.CreateDelegate(getDelegate);

            var getMethod = propertyInfo.GetMethod;
            DynamicMethod dynamicGetMethod = new DynamicMethod($"{propertyInfo.Name}_GetProxy", propertyInfo.PropertyType, new Type[] { typeof(object) }, true);
            var getterIL = dynamicGetMethod.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Callvirt, getMethod);
            getterIL.Emit(OpCodes.Ret);
            var genericFunc = typeof(Func<,>);
            var setDelegate = genericFunc.MakeGenericType(new Type[] { typeof(object),propertyInfo.PropertyType });
            var getter = dynamicGetMethod.CreateDelegate(setDelegate);
            var proxy = new PropertyProxy(setter, getter);
            return proxy;
        }

    }
}
