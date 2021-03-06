﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.IO;

using Afx.Configuration;
using Afx.Threading;
using Afx.Aop;
using Afx.Collections;
using Afx.Utils;

namespace Afx.Ioc
{
    /// <summary>
    /// ioc工厂
    /// </summary>
    public class IocContainer
    {
        private Assembly m_defaultAssembly;

        private ReadWriteLock m_rwLock;
        private Dictionary<Type, List<Type>> m_dicType;

        private AopProxy m_aopProxy;
        /// <summary>
        /// IocContainer
        /// </summary>
        public IocContainer()
            : this(null, false)
        {

        }
        /// <summary>
        /// IocContainer
        /// </summary>
        /// <param name="isEnabledAop">true:Enabled Aop, false: disabled Aop</param>
        public IocContainer(bool isEnabledAop)
            : this(null, isEnabledAop)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configFile">配置文件</param>
        public IocContainer(string configFile)
            : this(configFile, false)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configFile">配置文件</param>
        /// <param name="isEnabledAop">是否使用Aop</param>
        public IocContainer(string configFile, bool isEnabledAop)
        {
            if (!string.IsNullOrEmpty(configFile))
            {
                string filePath = PathUtils.GetFileFullPath(configFile);
                if (!File.Exists(filePath)) throw new FileNotFoundException(configFile + " not found！", configFile);
            }

            this.m_defaultAssembly = null;
            this.m_rwLock = new ReadWriteLock();
            this.m_dicType = new Dictionary<Type, List<Type>>();

            if (isEnabledAop) this.m_aopProxy = new AopProxy();

            if (!string.IsNullOrEmpty(configFile)) this.Load(configFile);
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="configFile"></param>
        public void Load(string configFile)
        {
            if (string.IsNullOrEmpty(configFile)) throw new ArgumentNullException("configFile");
            string fullpath = PathUtils.GetFileFullPath(configFile);
            if (!File.Exists(fullpath)) throw new FileNotFoundException(configFile + " not found！", configFile);

            XmlDocument xmlDoc = new XmlDocument();
            XmlReaderSettings xmlStting = new XmlReaderSettings();
            xmlStting.IgnoreComments = true;
            xmlStting.CloseInput = true;
            using (var fs = File.OpenRead(fullpath))
            {
                using (var xmlRead = XmlReader.Create(fs, xmlStting))
                {
                    xmlDoc.Load(xmlRead);
                    if (xmlDoc.ChildNodes.Count > 0)
                    {
                        var rootElement = xmlDoc.DocumentElement;
                        if (rootElement != null)
                        {
                            XmlElement nodeElement = rootElement["Global"];
                            if (nodeElement != null)
                            {
                                this.LoadGlobal(nodeElement);
                            }
                            nodeElement = rootElement["Object"];
                            if (nodeElement != null)
                            {
                                this.LoadObject(nodeElement);
                            }
                        }
                    }
                    xmlRead.Close();
                }
            }
        }

        private Dictionary<string, Assembly> assemblyDic = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private Assembly GetAssembly(string name)
        {
            Assembly assembly = null;
            if (!string.IsNullOrEmpty(name))
            {
                if (assemblyDic.TryGetValue(name, out assembly))
                {
                    return assembly;
                }

                var arr = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var item in arr)
                {
                    var s = item.FullName.Split(',')[0].Trim();
                    if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(item.ManifestModule.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        assembly = item;
                        assemblyDic[name] = assembly;
                        return assembly;
                    }
                }

                if (assembly == null)
                {
                    try
                    {
                        if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            assembly = Assembly.Load(name.Substring(0, name.Length - ".dll".Length));
                        }
                        else
                        {
                            assembly = Assembly.Load(name);
                        }
                        if (assembly != null)
                        {
                            assemblyDic[name] = assembly;
                            return assembly;
                        }
                    }
                    catch { }
                }

                string filename = name;
                bool isExists = File.Exists(filename);
                if (!isExists && File.Exists(filename + ".dll"))
                {
                    isExists = true;
                    filename = filename + ".dll";
                }
                else if (!isExists && File.Exists(filename + ".exe"))
                {
                    isExists = true;
                    filename = filename + ".exe";
                }

                if (!isExists)
                {
                    var s = PathUtils.GetFileFullPath(name);
                    if (File.Exists(s))
                    {
                        isExists = true;
                        filename = s;
                    }
                    if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        s = PathUtils.GetFileFullPath(name + ".dll");
                        if (File.Exists(s))
                        {
                            isExists = true;
                            filename = s;
                        }
                    }
                    else if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        s = PathUtils.GetFileFullPath(name + ".exe");
                        if (File.Exists(s))
                        {
                            isExists = true;
                            filename = s;
                        }
                    }
                }

                if (isExists)
                {
                    try
                    {
                        assembly = Assembly.LoadFrom(filename);
                        assemblyDic[name] = assembly;
                    }
                    catch { }
                }
            }

            return assembly;
        }

        private Type GetAopType(XmlElement nodeElement)
        {
            Type type = null;
            var s = nodeElement.GetAttribute("type");
            if (string.IsNullOrEmpty(s)) return type;
            var arr = s.Split(',');
            var t = arr[0].Trim();
            Assembly assembly = null;
            s = null;
            if (arr.Length > 1 && !string.IsNullOrEmpty(arr[1]))
            {
                assembly = this.GetAssembly(arr[1].Trim());
            }
            else
            {
                assembly = this.m_defaultAssembly;
            }
            if (assembly != null)
            {
                type = assembly.GetType(t, false);
            }

            return type;
        }

        private void LoadGlobal(XmlElement nodeElement)
        {
            var s = nodeElement.GetAttribute("defaultAssembly");
            if (!string.IsNullOrEmpty(s))
            {
                this.m_defaultAssembly = this.GetAssembly(s);
            }

            if (this.m_aopProxy != null)
            {
                foreach (XmlElement aopElement in nodeElement)
                {
                    if (aopElement.Name == "Aop")
                    {
                        var type = this.GetAopType(aopElement);
                        if (type != null && type.IsClass && !type.IsAbstract)
                        {
                            this.m_aopProxy.AddOfGlobal(type);
                        }
                    }
                }
            }
        }

        private void LoadObject(XmlElement nodeElement)
        {
            foreach (XmlElement classElement in nodeElement)
            {
                if (classElement.Name == "Class")
                {
                    var name = classElement.GetAttribute("name");
                    if (string.IsNullOrEmpty(name)) continue;
                    var s = classElement.GetAttribute("type");
                    if (string.IsNullOrEmpty(s)) continue;
                    var arr = s.Split(',');
                    var t = arr[0].Trim();
                    Assembly assembly = null;
                    if (arr.Length > 1 && !string.IsNullOrEmpty(arr[1]))
                    {
                        assembly = this.GetAssembly(arr[1].Trim());
                    }
                    else
                    {
                        assembly = this.m_defaultAssembly;
                    }

                    if (assembly == null) continue;

                    var type = assembly.GetType(t, false);
                    if (type != null && type.IsClass && !type.IsAbstract)
                    {
                        var intefaces = type.GetInterface(name);
                        if (intefaces == null) continue;
                        using (this.m_rwLock.GetWriteLock())
                        {
                            List<Type> list = null;
                            if (!this.m_dicType.TryGetValue(intefaces, out list))
                            {
                                this.m_dicType[intefaces] = list = new List<Type>();
                            }
                            if (!list.Contains(type)) list.Add(type);
                        }

                        if (this.m_aopProxy != null)
                        {
                            var aopTypeList = new List<Type>();
                            foreach (XmlElement childElement in classElement)
                            {
                                if (childElement.Name == "Aop")
                                {
                                    var aopType = GetAopType(childElement);
                                    if (aopType != null && type.IsClass && !type.IsAbstract)
                                        aopTypeList.Add(aopType);
                                }
                            }
                            if (aopTypeList.Count > 0)
                                this.m_aopProxy.AddOfType(type, aopTypeList);
                        }
                    }
                }
            }
        }

        private Type GetProxyType(Type t)
        {
            Type tagetType = t;
            if (t != null && this.m_aopProxy != null)
            {
                tagetType = this.m_aopProxy.GetProxyType(t);
            }

            return tagetType;
        }
        
        private Type GetTagetTypeUnLock(Type interfaceType, string name, out List<Type> list)
        {
            Type tagetType = null;
            list = null;
            if (this.m_dicType.TryGetValue(interfaceType, out list) && list.Count > 0)
            {
                if (string.IsNullOrEmpty(name))
                {
                    tagetType = list[list.Count - 1];
                }
                else if (name.Contains(","))
                {
                    string[] arr = name.Split(',');
                    if (arr.Length == 2)
                    {
                        arr[0] = arr[0].Trim();
                        arr[1] = arr[1].Trim();
                        tagetType = list.Find(q => string.Equals(q.Name, arr[0], StringComparison.OrdinalIgnoreCase)
                        && q.Assembly != null && q.Assembly.ManifestModule != null
                        && string.Equals(q.Assembly.ManifestModule.Name, arr[1], StringComparison.OrdinalIgnoreCase));
                    }
                }
                else
                {
                    tagetType = list.Find(q => string.Equals(q.Name, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(q.FullName, name, StringComparison.OrdinalIgnoreCase));
                }
            }

            return tagetType;
        }

        private Type GetObjectType(Type interfaceType, string name)
        {
            Type tagetType = null;
            if (interfaceType != null)
            {
                using (this.m_rwLock.GetReadLock())
                {
                    List<Type> list;
                    tagetType = this.GetTagetTypeUnLock(interfaceType, name, out list);
                }
                if (tagetType == null && this.m_defaultAssembly != null)
                {
                    using (this.m_rwLock.GetWriteLock())
                    {
                        List<Type> list;
                        tagetType = this.GetTagetTypeUnLock(interfaceType, name, out list);
                        if (tagetType == null)
                        {
                            var arr = this.m_defaultAssembly.GetExportedTypes();
                            foreach (var t in arr)
                            {
                                if (t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t))
                                {
                                    var ctors = t.GetConstructors();
                                    if (ctors != null && ctors.Length > 0)
                                    {
                                        if (list == null)
                                        {
                                            this.m_dicType[interfaceType] = list = new List<Type>();
                                        }
                                        if (!list.Contains(t)) list.Add(t);
                                        tagetType = t;
                                        break; ;
                                    }
                                }
                            }
                        }
                    }
                }

                tagetType = this.GetProxyType(tagetType);
            }

            return tagetType;
        }
        /// <summary>
        /// 注册 ioc
        /// </summary>
        /// <param name="interfaceType">接口type</param>
        /// <param name="classType">实现类type</param>
        public void Register(Type interfaceType, Type classType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (classType == null) throw new ArgumentNullException("classType");
            if (!interfaceType.IsPublic) throw new Exception("Argument interfaceType is not public!");
            if (!classType.IsPublic) throw new Exception("Argument classType is not public!");
            if (!classType.IsClass) throw new Exception("Argument classType is not class!");
            if (classType.IsAbstract) throw new Exception("Argument classType is abstract class!");
            if (!(interfaceType == classType || interfaceType.IsAssignableFrom(classType)))
                throw new Exception("Determines whether an instance of a specified interfaceType can be assigned to the current classType instance.!");

            var ctors = classType.GetConstructors();
            if (ctors == null || ctors.Length == 0)
            {
                throw new Exception("Not found public constructor!");
            }

            using (this.m_rwLock.GetWriteLock())
            {
                List<Type> list;
                if (!this.m_dicType.TryGetValue(interfaceType, out list))
                {
                    this.m_dicType[interfaceType] = list = new List<Type>();
                }
                if (!list.Contains(classType)) list.Add(classType);
            }

        }
        /// <summary>
        /// 注册ioc
        /// </summary>
        /// <typeparam name="TInterface">接口</typeparam>
        /// <typeparam name="TClass">实现类</typeparam>
        public void Register<TInterface, TClass>()
        {
            this.Register(typeof(TInterface), typeof(TClass));
        }

        /// <summary>
        /// 注册程序集所有接口实现
        /// </summary>
        /// <typeparam name="TBaseInterface">接口</typeparam>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns></returns>
        public int Register<TBaseInterface>(string assemblyName)
        {
            return this.Register(typeof(TBaseInterface), assemblyName);
        }

        /// <summary>
        /// 注册程序集所有接口实现
        /// </summary>
        /// <typeparam name="TBaseInterface">接口</typeparam>
        /// <param name="assembly">程序集</param>
        /// <returns></returns>
        public int Register<TBaseInterface>(Assembly assembly)
        {
            return this.Register(typeof(TBaseInterface), assembly);
        }

        /// <summary>
        /// 注册程序集所有接口实现
        /// </summary>
        /// <param name="baseInterfaceType">接口 type</param>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns></returns>
        public int Register(Type baseInterfaceType, string assemblyName)
        {
            int count = 0;
            if (string.IsNullOrEmpty(assemblyName)) throw new ArgumentNullException("assemblyName");
            var assembly = this.GetAssembly(assemblyName);
            if (assembly == null) throw new FileNotFoundException(assemblyName + " not found!", assemblyName);
            this.Register(baseInterfaceType, assembly);

            return count;
        }
        /// <summary>
        /// 注册程序集所有接口实现
        /// </summary>
        /// <param name="baseInterfaceType">接口 type</param>
        /// <param name="assembly">程序集</param>
        /// <returns></returns>
        public int Register(Type baseInterfaceType, Assembly assembly)
        {
            int count = 0;
            if (baseInterfaceType == null) throw new ArgumentNullException("baseInterfaceType");
            if (assembly == null) throw new ArgumentNullException("assembly");
            if (!baseInterfaceType.IsClass && !baseInterfaceType.IsInterface) throw new ArgumentException("baseInterfaceType is not interface or class!");

            var arr = assembly.GetExportedTypes();
            if (arr != null && arr.Length > 0)
            {
                foreach (var t in arr)
                {
                    if (t.IsClass && !t.IsAbstract && baseInterfaceType.IsAssignableFrom(t))
                    {
                        var ctors = t.GetConstructors();
                        if (ctors != null && ctors.Length > 0)
                        {
                            var interfas = t.GetInterfaces();
                            foreach (var f in interfas)
                            {
                                if (baseInterfaceType != f && baseInterfaceType.IsAssignableFrom(f))
                                {
                                    using (this.m_rwLock.GetWriteLock())
                                    {
                                        List<Type> list;
                                        if (!this.m_dicType.TryGetValue(f, out list))
                                        {
                                            this.m_dicType[f] = list = new List<Type>();
                                        }
                                        if (!list.Contains(t)) list.Add(t);
                                    }
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 根据指定类型获取
        /// </summary>
        /// <typeparam name="TInterface">返回接口类型</typeparam>
        /// <param name="name">类名，不传返回最后注册实现类</param>
        /// <param name="args">构造函数参数</param>
        /// <returns></returns>
        public TInterface Get<TInterface>(string name, object[] args)
        {
            TInterface result = default(TInterface);
            var t = typeof(TInterface);
            if (!t.IsClass && !t.IsInterface) throw new ArgumentException("TInterface type is not interface or class!");
            Type tagetType = this.GetObjectType(t, name);
            if (tagetType != null)
            {
                try
                {
                    object obj = null;
                    if (args != null && args.Length > 0)
                    {
                        obj = Activator.CreateInstance(tagetType, args);
                    }
                    else
                    {
                        obj = Activator.CreateInstance(tagetType);
                    }

                    result = (TInterface)obj;
                }
                catch { }
            }

            return result;
        }

        /// <summary>
        /// 根据指定类型获取
        /// </summary>
        /// <typeparam name="TInterface">返回接口类型</typeparam>
        /// <param name="name">类名，不传返回最后注册实现类</param>
        /// <returns></returns>
        public TInterface Get<TInterface>(string name)
        {
            return this.Get<TInterface>(name, null);
        }

        /// <summary>
        /// 根据指定类型获取
        /// </summary>
        /// <typeparam name="TInterface">返回类型</typeparam>
        /// <returns>返回最后注册实现类</returns>
        public TInterface Get<TInterface>()
        {
            return this.Get<TInterface>(null, null);
        }


        /// <summary>
        /// 当前AOP代理工厂，未启用aop 返回null
        /// </summary>
        public AopProxy CurrentAopProxy
        {
            get { return this.m_aopProxy; }
        }

        /// <summary>
        /// 添加全局IAop实现类型
        /// </summary>
        /// <param name="aopTypeList">IAop实现类型 list</param>
        public void AddGlobalAop(List<Type> aopTypeList)
        {
            if(this.m_aopProxy != null)
                this.m_aopProxy.AddOfGlobal(aopTypeList);
        }

        /// <summary>
        /// 添加全局IAop实现类型
        /// </summary>
        /// <typeparam name="TAop"></typeparam>
        public void AddGlobalAop<TAop>() where TAop : class, IAop
        {
            this.AddGlobalAop(typeof(TAop));
        }

        /// <summary>
        /// 添加全局IAop实现类型
        /// </summary>
        /// <param name="aopType">IAop实现类型</param>
        public void AddGlobalAop(Type aopType)
        {
            if (this.m_aopProxy != null)
                this.m_aopProxy.AddOfGlobal(aopType);
        }

        /// <summary>
        /// 添加指定实现类的IAop
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="aopTypeList"></param>
        public void AddClassAop<TClass>(List<Type> aopTypeList)
        {
            this.AddClassAop(typeof(TClass), aopTypeList);
        }

        /// <summary>
        /// 添加指定类型的IAop实现类型
        /// </summary>
        /// <param name="classType">需要aop的类型</param>
        /// <param name="aopTypeList">IAop实现类型 list</param>
        public void AddClassAop(Type classType, List<Type> aopTypeList)
        {
            if (this.m_aopProxy != null)
                this.m_aopProxy.AddOfType(classType, aopTypeList);
        }

        /// <summary>
        /// 添加指定实现类的IAop
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <typeparam name="TAop"></typeparam>
        public void AddClassAop<TClass, TAop>() where TAop : class, IAop
        {
            this.AddClassAop(typeof(TClass), typeof(TAop));
        }

        /// <summary>
        /// 添加指定实现类的IAop
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <param name="aopType"></param>
        public void AddClassAop<TClass>(Type aopType)
        {
            this.AddClassAop(typeof(TClass), aopType);
        }

        /// <summary>
        /// 添加指定类型的IAop实现类型
        /// </summary>
        /// <param name="classType">需要aop的类型</param>
        /// <param name="aopType">IAop实现类型</param>
        public void AddClassAop(Type classType, Type aopType)
        {
            if (this.m_aopProxy != null)
                this.m_aopProxy.AddOfType(classType, aopType);
        }

    }
}
