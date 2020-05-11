using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

namespace Util
{
    public static class Mapper
    {
        private static Dictionary<Type, PropertyInfo[]> _propCaches;
        private static Dictionary<PropertyInfo, IPropertyProxy> _proxyCaches;

        /// <summary>
        /// 获得类型构造器，直接申请内存空间
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static Func<T> GetConstructor<T>() where T:class,new()
        {
            Type type = typeof(T);
            var ctor = type.GetConstructors()[0];
            DynamicMethod method = new DynamicMethod(String.Empty, type, null,true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            return method.CreateDelegate(typeof(Func<T>)) as Func<T>;
        }
        static Mapper()
        {
            _propCaches = new Dictionary<Type, PropertyInfo[]>();
            _proxyCaches = new Dictionary<PropertyInfo, IPropertyProxy>();
        }
             
        public static TDes Map<TSource,TDes>(TSource source) where TDes:class,new()  where TSource : class,new()
        {
            var des = new TDes();
            PropertyInfo[] desProps = GetPropinfos<TDes>(),sourceProps = GetPropinfos<TSource>();
            foreach(var desProp in desProps)
            {
                foreach(var sourceProp in sourceProps)
                {
                    //如果属性匹配上了，则赋值
                    if(desProp.Name.Equals(sourceProp.Name,StringComparison.Ordinal) && desProp.PropertyType == sourceProp.PropertyType)
                    {
                        //首先获取原属性值,然后设置目标属性值
                        GetPropertyProxy(desProp).SetValue(des, GetPropertyProxy(sourceProp).GetValue(source));
                        break;
                    }
                }
            }

            return des;
        }

        public static List<TDes> Map<TSource, TDes>(List<TSource> source ) where TDes : class, new() where TSource : class, new()
        {

            List<AB> abs = new List<AB>();
            var desList = new List<TDes>(source.Count);
            PropertyInfo[] desProps = GetPropinfos<TDes>(), sourceProps = GetPropinfos<TSource>();
            foreach (var desProp in desProps)
            {
                foreach (var sourceProp in sourceProps)
                {
                    //如果属性匹配上了，则加入到集合
                    if (desProp.Name.Equals(sourceProp.Name, StringComparison.OrdinalIgnoreCase) && desProp.PropertyType == sourceProp.PropertyType)
                    {
                        abs.Add(new AB {PropertyInfo = desProp,A = GetPropertyProxy(sourceProp),B = GetPropertyProxy(desProp) });
                        break;
                    }
                }
            }
            var constructor = GetConstructor<TDes>();
            //开始赋值
            for (int i = 0; i < source.Count; i++) desList.Add(constructor());
            for(int i =0;i < source.Count; i++)
            {
                for(int j=0;j < abs.Count; j++)
                {
                    switch (abs[j].PropertyInfo.PropertyType.FullName)
                    {
                        case "System.String":
                            {
                                CopyValue<String>(abs[j], source[i], desList[i]);
                                break;
                            }
                        case "System.Int32":
                            {
                                CopyValue<Int32>(abs[j], source[i], desList[i]);
                                break;
                            }
                        case "System.Int64":
                            {
                                CopyValue<Int64>(abs[j], source[i], desList[i]);
                                break;
                            }
                        case "System.Boolean":
                            {
                                CopyValue<Boolean>(abs[j], source[i], desList[i]);
                                break;
                            }
                        case "System.DateTime":
                            {
                                CopyValue<DateTime>(abs[j], source[i], desList[i]);
                                break;
                            }
                        case "System.Double":
                            {
                                CopyValue<Double>(abs[j], source[i], desList[i]);
                                break;
                            }
                        default: 
                            {
                                if (abs[j].PropertyInfo.PropertyType.IsValueType && abs[j].PropertyInfo.PropertyType.Name.Equals("Nullable`1"))
                                {
                                    switch (abs[j].PropertyInfo.PropertyType.GenericTypeArguments[0].FullName)
                                    {
                                        case "System.Int32":
                                            {
                                                CopyValue<Nullable<Int32>>(abs[j], source[i], desList[i]);
                                                break;
                                            }
                                        case "System.Int64":
                                            {
                                                CopyValue<Nullable<Int64>>(abs[j], source[i], desList[i]);
                                                break;
                                            }
                                        case "System.Boolean":
                                            {
                                                CopyValue<Nullable<Boolean>>(abs[j], source[i], desList[i]);
                                                break;
                                            }
                                        case "System.DateTime":
                                            {
                                                CopyValue<Nullable<DateTime>>(abs[j], source[i], desList[i]);
                                                break;
                                            }
                                        case "System.Double":
                                            {
                                                CopyValue<Nullable<Double>>(abs[j], source[i], desList[i]);
                                                break;
                                            }
                                        default:
                                            {
                                                CopyValue(abs[j], source[i], desList[i]);
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    CopyValue(abs[j], source[i], desList[i]);
                                }
       
                                break; 
                            }
                    }
                }
            }
            return desList;
        }

        private static void CopyValue<T>(AB ab,object sourceReference,object desReference)
        {
            var sourceRealProxy = ab.A as IPropertyProxy<T>;
            var desRealProxy = ab.B as IPropertyProxy<T>;

            desRealProxy.SetValue(desReference, sourceRealProxy.GetValue(sourceReference));
        }
        private static void CopyValue(AB ab, object sourceReference, object desReference)
        {
            var sourceRealProxy = ab.A as IPropertyProxy;
            var desRealProxy = ab.B as IPropertyProxy;

            desRealProxy.SetValue(desReference, sourceRealProxy.GetValue(sourceReference));
        }

        /// <summary>
        /// 根据属性信息获取相应属性代理
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static IPropertyProxy GetPropertyProxy(PropertyInfo prop)
        {
            //如果缓存中已有对应属性代理则直接获取
            if (_proxyCaches.TryGetValue(prop, out var proxy)) return proxy;
            //否则创建属性代理并加入代理缓存中去
            CreateAndInsertPropProxy(prop);
            return _proxyCaches[prop];
        }

        private static void CreateAndInsertPropProxy(PropertyInfo propertyInfo)
        {
            _proxyCaches.Add(propertyInfo, PropertyProxyFactory.GetPropertyProxy(propertyInfo));
        } 

        private static PropertyInfo[] GetPropinfos<T>()where T : class
        {
            Type type = typeof(T);
            if (_propCaches.TryGetValue(type, out var props)) return props;
            _propCaches.Add( type,type.GetProperties());
            return _propCaches[type];
        }

    }

    public struct AB
    {
        public PropertyInfo PropertyInfo;
        public IPropertyProxy A;
        public IPropertyProxy B;
    }

}
