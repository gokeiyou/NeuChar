﻿using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.NeuChar
{
    /// <summary>
    /// NeuChar 注册
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// 是否API绑定已经执行完
        /// </summary>
        private static bool RegisterApiBindFinished = false;


        /// <summary>
        /// 节点类型注册集合
        /// </summary>
        public static Dictionary<string, Type> NeuralNodeRegisterCollection = new Dictionary<string, Type>();

        /// <summary>
        /// Api绑定信息集合
        /// </summary>
        public static Dictionary<string, ApiBindInfo> ApiBindInfoCollection = new Dictionary<string, ApiBindInfo>();

        static Register()
        {
            RegisterApiBind();
        }


        /// <summary>
        /// 注册节点
        /// </summary>
        /// <param name="name">唯一名称</param>
        /// <param name="type">节点类型</param>
        public static void RegisterNeuralNode(string name, Type type)
        {
            NeuralNodeRegisterCollection[name] = type;
        }

        /// <summary>
        /// 自动扫描并注册 ApiBind
        /// </summary>
        public static void RegisterApiBind()
        {
            var cacheStragegy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            using (cacheStragegy.BeginCacheLock("Senparc.NeuChar.Register", "RegisterApiBind"))
            {
                if (RegisterApiBindFinished == true)
                {
                    return;
                }

                //查找所有扩展缓存
                var scanTypesCount = 0;




                var assembiles = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assembiles)
                {

                    var methods1 = assembly.GetTypes()
      .SelectMany(t => t.GetMethods())
      .Where(m => m.GetCustomAttributes(typeof(ApiBindAttribute), false).Length > 0)
      .ToArray();

                    Console.WriteLine(methods1.Count());


                    try
                    {
                        scanTypesCount++;
                        var aTypes = assembly.GetTypes();

                        foreach (var type in aTypes)
                        {
                            if (type.IsAbstract || !type.IsPublic || !type.IsClass || type.IsEnum)
                            {
                                continue;
                            }

                            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
                            foreach (var method in methods)
                            {
                                var attrs = method.GetCustomAttributes(typeof(ApiBindAttribute), false);
                                foreach (var attr in attrs)
                                {
                                    var apiBindAttr = attr as ApiBindAttribute;
                                    var name = $"{apiBindAttr.Name}";//TODO：生成全局唯一名称
                                    ApiBindInfoCollection.Add(name, new ApiBindInfo(apiBindAttr, method));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("RegisterApiBind() 自动扫描程序集异常：" + assembly.FullName, ex.ToString());
                    }
                }

                RegisterApiBindFinished = true;
            }
        }
    }
}
