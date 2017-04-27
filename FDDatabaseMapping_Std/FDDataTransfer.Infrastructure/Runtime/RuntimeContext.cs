using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FDDataTransfer.Infrastructure.Runtime
{
    public class RuntimeContext
    {
        private static RuntimeContext _current;
        private readonly string _tableConfigDir = "configs";
        private string _currentDir;
        private List<Type> _types;
        private RuntimeContext() { Load(); }

        static RuntimeContext()
        {
            _current = new RuntimeContext();
        }

        public static RuntimeContext Current
        {
            get { return _current; }
        }

        /// <summary>
        /// 表配置文件目录
        /// </summary>
        public string TableConfigPath { get { return Path.Combine(_currentDir, _tableConfigDir); } }

        private void Load()
        {
            _currentDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            _types = new List<Type>();
            //var loadedAssembly = Assembly.GetEntryAssembly().Where(p => !p.Name.StartsWith("System."));//.Select(p=>p.FullName);
            //Func<string, bool> contains = file =>
            //  {
            //      foreach (var asm in loadedAssembly)
            //      {
            //          var aName = asm.Name;
            //          var fileName = Path.GetFileNameWithoutExtension(file);
            //          if (aName.Equals(fileName))
            //              return true;
            //      }
            //      return false;
            //  };
            var asms = Directory.GetFiles(_currentDir).Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            //&& !contains(p)
            );
            foreach (var asm in asms)
            {
                var assemblyName = System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName(asm);
                var assembly = Assembly.Load(assemblyName);//AssemblyLoadContext.Default.LoadFromAssemblyPath(asm);
                assembly.GetExportedTypes().ToList().ForEach(t =>
                {
                    if (!_types.Contains(t))
                    {
                        _types.Add(t);
                    }
                });
            }
        }
        /// <summary>
        /// 创建类型
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <returns></returns>
        public T GetInstance<T, V>(string typeName)
        {
            return GetInstance<T, V>(typeName, null);
        }

        /// <summary>
        /// 创建类型
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <param name="parameters">构造方法参数</param>
        /// <returns></returns>
        public T GetInstance<T, V>(string typeName, params object[] parameters)
        {
            var type = _types.FirstOrDefault(t => t.GetTypeInfo().IsGenericType && (t.FullName.StartsWith(typeName) || t.Name.StartsWith(typeName)));
            if (type == null)
            {
                return default(T);
            }

            var obj = parameters != null ? Activator.CreateInstance(type.MakeGenericType(typeof(V)), parameters) : Activator.CreateInstance(type.MakeGenericType(typeof(V)));
            return (T)obj;
        }
    }
}
