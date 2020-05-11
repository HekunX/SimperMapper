using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Util
{
    public interface IPropertyProxy 
    {
        void SetValue(object reference,object value);
        object GetValue(object reference);
    };
    public interface IPropertyProxy<T>: IPropertyProxy
    {
        void SetValue(object reference, T value);
        new T GetValue(object reference);
    }


    public class PropertyProxy : IPropertyProxy
    {
  
        private Delegate _unkownSet;
        private Delegate _unkownGet;

        public PropertyProxy(Delegate set, Delegate get)
        {
            _unkownSet = set;
            _unkownGet = get;
        }

        public object GetValue(object reference)
        {
            return _unkownGet.DynamicInvoke(reference);
        }

        public void SetValue(object reference, object value)
        {
            _unkownSet.DynamicInvoke(reference,value);
        }
    }

    public class PropertyProxy<T> :PropertyProxy, IPropertyProxy<T>
    {
        private readonly Action<object, T> _set;
        private readonly Func<object, T> _get;

        public PropertyProxy(Action<object,T> set,Func<object,T> get):base(set,get)
        {
            _set = set;
            _get = get;
        }
        public void SetValue(object reference, T value)
        {
            _set(reference, value);
        }

        T IPropertyProxy<T>.GetValue(object reference)
        {
            return _get(reference);
        }
    }


}
