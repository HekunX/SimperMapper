using AutoMapper;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using Util;

namespace Test
{
    public enum Category 
    {
        APP,
        PC,
    }
    public class Model
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime CeratedTime { get; set; }
        public bool IsActive { get; set; }
        public double Money { get; set; }

        public Category Category { get; set; }
        public DateTime? T { get; set; }
        public void SetX()
        {
            Money = 10;
        }
    }
    public class DesModel
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime CeratedTime { get; set; }
        public bool IsActive { get; set; }
        public double Money { get; set; }

        public Category Category { get; set; }
        public DateTime? T { get; set; }
        public DesModel()
        {
        }


    }
    class Program
    {
        public static void  Test()
        {
            var propertyInfo  = typeof(Model).GetProperty("Category");
            var setMethod = propertyInfo.SetMethod;
            DynamicMethod dynamicSetMethod = new DynamicMethod($"{propertyInfo.Name}_SetProxy", null, new Type[] { typeof(object),propertyInfo.PropertyType}, false);
            var setterIL = dynamicSetMethod.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Callvirt, setMethod);
            setterIL.Emit(OpCodes.Ret);

            var genericAction = typeof(Action<,>);
             var type = genericAction.MakeGenericType(new Type[] { typeof(object), propertyInfo.PropertyType });
       
            var setter = dynamicSetMethod.CreateDelegate(type) as Action<object,object>;
        }
        public static void Show(int? o)
        {
            Console.WriteLine(o);
        }

        static void Main(string[] args)
        {

            var model = new Model { };

            List<Model> models = new List<Model>();

            Stopwatch sw = new Stopwatch();
            sw.Restart();
            for (int i = 0; i < 100000; i++) 
            {
                models.Add(new Model());
                models[i].SetX();
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            //Util.Mapper.Map<Model, DesModel>(models[0]);
            var cfg = new MapperConfiguration(config => config.CreateMap<Model, DesModel>());
            var mapper = cfg.CreateMapper();



            sw.Restart();
            var z = mapper.Map<List<DesModel>>(models);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            sw.Restart();
            var zz = mapper.Map<List<DesModel>>(models);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);




            sw.Restart();
            var desmodels = Util.Mapper.Map<Model, DesModel>(models);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);


            sw.Restart();
            var desmodels2 = Util.Mapper.Map<Model, DesModel>(models);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);






        }
    }
}
