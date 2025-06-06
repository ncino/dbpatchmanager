﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Logic
{
    public class BuildConfigurationWriter : IBuildConfigurationWriter
    {
        public BuildConfigurationWriter(string filePath, string localFilePath)
        {
            _filePath = filePath;
            _localFilePath = localFilePath;
        }

        /// <summary>
        /// Use System.IO.Abstraction to make testing easier.
        /// </summary>
        FileSystem _io = new FileSystem();

        /// <summary>
        /// Path and name of file to read and write.
        /// </summary>
        string _filePath;

        /// <summary>
        /// Path and name of secondary file to read and write.
        /// The local file is intended to contains settings which the user does not want to 
        /// place in the main file. For example, a user may not wish to put the connection string
        /// in the main file which is checked into source control. The connection string could
        /// be placed in he local file which is not checked into source control.
        /// </summary>
        string _localFilePath;

        /// <summary>
        /// Read DatabaseBuildConfiguration data from file path passed to constructor.
        /// </summary>
        /// <returns></returns>
        public DatabaseBuildConfiguration Read()
        {
            DatabaseBuildConfiguration rc = null;
            if (_io.File.Exists(_filePath))
            {
                //rc = JsonConvert.DeserializeObject<DatabaseBuildConfiguration>(_io.File.ReadAllText(_filePath), _jsonSettings);
                var o = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(
                    _io.File.ReadAllText(_filePath));

                if (_io.File.Exists(_localFilePath))
                {
                    var localO =
                        (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(
                            _io.File.ReadAllText(_localFilePath));
                    o.Merge(localO, new JsonMergeSettings()
                    {
                        MergeArrayHandling = MergeArrayHandling.Union
                    });
                }

                rc = new DatabaseBuildConfiguration();
                rc.CodeFolder = (string)o["CodeFolder"];
                rc.CodeFiles = o["CodeFiles"].ToObject<List<string>>();
                rc.ConnectionString = (string)o["ConnectionString"];
                rc.DatabaseType = (string)o["DatabaseType"];
                rc.PatchFolder = (string)o["PatchFolder"];

                // options specific to a database plugin
                rc.Options = o["Options"]?.ToObject<Dictionary<string, string>>();

                // populate patch list
                if (o["patches"] == null)
                {
                    rc.patches = new List<Patch>();
                }
                else
                {
                    var patches = (from p in o["patches"]
                        select new Patch()
                        {
                            Id = (string)p["id"]
                        }).ToList();
                    // populate DependsOn
                    foreach (var p in patches)
                    {
                        var cur = from x in o["patches"]
                            from d in x["dependsOn"]
                            from a in patches
                            where (string)x["id"] == p.Id &&
                                  a.Id == (string)d
                            select a;
                        p.DependsOn = cur.Distinct(new PatchComparer()).ToList();
                        //todo: double check this query
                        var children = from x in o["patches"]
                            from d in x["dependsOn"]
                            from a in patches
                            where (string)d == p.Id && (string)x["id"] == a.Id
                            select a;
                        p.Children = children.Distinct(new PatchComparer()).ToList();
                    }

                    rc.patches = patches.ToList();
                }
            }
            else
            {
                throw new ApplicationException("Configuration file does not exist. Call init first.");
            }

            return rc;
        }

        public void Write(DatabaseBuildConfiguration buildConfiguration)
        {
            JObject data;

            //todo: if local file exists, don't write values to patches.json if value exists in patches.local.json
            if (_io.File.Exists(_localFilePath))
            {
                var localO =
                    (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(
                        _io.File.ReadAllText(_localFilePath));
                data = new JObject();

                //todo: find a way to do this that isn't manual. can you loop over all values in buildConfiguration?
                if (localO["DatabaseType"] == null && buildConfiguration.DatabaseType != null)
                {
                    data["DatabaseType"] = buildConfiguration.DatabaseType;
                }

                if (localO["ConnectionString"] == null && buildConfiguration.ConnectionString != null)
                {
                    data["ConnectionString"] = buildConfiguration.ConnectionString;
                }

                if (localO["CodeFolder"] == null && buildConfiguration.CodeFolder != null)
                {
                    data["CodeFolder"] = buildConfiguration.CodeFolder;
                }

                if (localO["CodeFiles"] == null || localO["CodeFiles"].HasValues == false)
                {
                    buildConfiguration.CodeFiles = buildConfiguration.CodeFiles ?? new List<string>();
                    data["CodeFiles"] = JArray.FromObject(buildConfiguration.CodeFiles);
                }

                if (localO["PatchFolder"] == null && buildConfiguration.PatchFolder != null)
                {
                    data["PatchFolder"] = buildConfiguration.PatchFolder;
                }

                try
                {
                    if (localO["Options"] == null || localO["Options"].HasValues == false)
                    {
                        buildConfiguration.Options = buildConfiguration.Options ?? new Dictionary<string, string>();
                        data["Options"] = JObject.FromObject(buildConfiguration.Options);
                    }
                }
                catch (JsonException e)
                {
                    Console.WriteLine(e.Message);
                    data["Options"] = JObject.FromObject(new Dictionary<string, string>());
                }

                if (localO["patches"] == null && buildConfiguration.patches != null)
                {
                    data["patches"] = JArray.FromObject(from p in buildConfiguration.patches
                        select new
                        {
                            id = p.Id,
                            dependsOn = p.DependsOn != null
                                ? (from d in p.DependsOn
                                    select d.Id)
                                : null
                        });
                }
            }
            else
            {
                //string data = JsonConvert.SerializeObject(buildConfiguration, Formatting.Indented, _jsonSettings);
                data = JObject.FromObject(new
                {
                    DatabaseType = buildConfiguration.DatabaseType,
                    ConnectionString = buildConfiguration.ConnectionString,
                    CodeFolder = buildConfiguration.CodeFolder,
                    CodeFiles = buildConfiguration.CodeFiles,
                    PatchFolder = buildConfiguration.PatchFolder,
                    Options = buildConfiguration.Options,
                    patches = from p in buildConfiguration.patches
                        select new
                        {
                            id = p.Id,
                            dependsOn = p.DependsOn != null
                                ? (from d in p.DependsOn.Distinct(new PatchComparer())
                                    select d.Id)
                                : null
                        }
                });
            }

            _io.File.WriteAllText(_filePath, data.ToString());
        }
    }
}