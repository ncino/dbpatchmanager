using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Ormico.DbPatchManager.Common;

namespace Ormico.DbPatchManager.Logic
{
    class PluginManager
    {
        public IDatabase LoadDatabasePlugin(string PluginType)
        {
            IDatabase rc = null;
            if (string.Equals(PluginType, "TestDatabase", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(PluginType, "test", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(PluginType, typeof(TestDatabase).ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rc = new TestDatabase();
            }
            else if (string.Equals(PluginType, "OdbcDatabase", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(PluginType, "odbc", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(PluginType, typeof(OdbcDatabase).ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rc = new OdbcDatabase();
            }
            else if (string.Equals(PluginType, "SqlDatabase", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(PluginType, "sqlserver", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(PluginType, typeof(SqlServer.SqlDatabase).ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                rc = new SqlServer.SqlDatabase();
            }
            else
            {
                string[] parts = PluginType.Split(',');
                string fileName, typeName;
                if (parts != null && parts.Length > 0)
                {
                    fileName = parts[0];
                    if (parts.Length > 1)
                    {
                        typeName = parts[1];
                        //todo: what to do if null?

                        // LoadFile() or LoadFrom()
                        Assembly pa = Assembly.LoadFrom(fileName);
                        var t = pa.GetType(typeName);
                        string interfaceName = typeof(IDatabase).ToString();
                        if (t.IsPublic && t.IsClass && t.GetInterface(interfaceName) != null)
                        {
                            rc = pa.CreateInstance(typeName) as IDatabase;
                        }
                    }
                }
            }

            return rc;
        }

        public IBuildConfigurationWriter LoadBuildConfigurationWriterPlugin(string pluginType, string filePath,
            string localFilePath)
        {
            IBuildConfigurationWriter rc = null;

            if (string.Equals(pluginType, "NewBuildConfig", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pluginType, "new", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pluginType, typeof(NewBuildConfigurationWriter).ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                rc = new NewBuildConfigurationWriter(filePath, localFilePath);
            }
            else if (string.Equals(pluginType, "OriginalBuildConfig", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(pluginType, "og", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(pluginType, typeof(BuildConfigurationWriter).ToString(),
                         StringComparison.OrdinalIgnoreCase))

            {
                rc = new BuildConfigurationWriter(filePath, localFilePath);
            }
            else
            {
                var parts = pluginType.Split(',');
                if (parts.Length <= 1) return null;
                var fileName = parts[0];
                var typeName = parts[1];
                //todo: what to do if null?

                // LoadFile() or LoadFrom()
                var pa = Assembly.LoadFrom(fileName);
                var t = pa.GetType(typeName);
                var interfaceName = typeof(IBuildConfigurationWriter).ToString();
                if (t.IsPublic && t.IsClass && t.GetInterface(interfaceName) != null)
                {
                    rc = pa.CreateInstance(typeName) as IBuildConfigurationWriter;
                }
            }

            return rc;
        }
    }
}