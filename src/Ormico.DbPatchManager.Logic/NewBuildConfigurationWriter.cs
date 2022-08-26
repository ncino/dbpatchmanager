using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Ormico.DbPatchManager.Common;


namespace Ormico.DbPatchManager.Logic
{
    /// <summary>
    /// Read and Write DatabaseBuildConfiguration to storage.
    /// </summary>
    public class NewBuildConfigurationWriter : IBuildConfigurationWriter
    {
        private readonly Random _rand;
        public NewBuildConfigurationWriter(string filePath, string localFilePath)
        {
            _filePath = filePath;
            _localFilePath = localFilePath;
            _rand = new Random();
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
            var rc = new DatabaseBuildConfiguration();
            if (_io.File.Exists(_filePath))
            {
                var localPatchFileContents = new PatchFile();
                if (File.Exists(_localFilePath))
                {
                    localPatchFileContents = PatchFile.FromJson(_io.File.ReadAllText(_localFilePath));
                }

                // var primaryPatchFileStream = File.OpenText(_filePath);
                var patchFileContents = PatchFile.FromJson(_io.File.ReadAllText(_filePath));
                rc.CodeFiles = localPatchFileContents.CodeFiles.Count > 0
                    ? localPatchFileContents.CodeFiles
                    : (patchFileContents.CodeFiles ?? new List<string>());
                rc.Options = localPatchFileContents.Options.Count > 0
                    ? localPatchFileContents.Options
                    : (patchFileContents.Options ?? new Dictionary<string, string>());
                rc.CodeFolder = localPatchFileContents.CodeFolder ?? patchFileContents.CodeFolder;
                rc.DatabaseType = localPatchFileContents.DatabaseType ?? patchFileContents.DatabaseType;
                rc.PatchFolder = localPatchFileContents.PatchFolder ?? patchFileContents.PatchFolder;
                rc.ConnectionString = localPatchFileContents.ConnectionString ?? patchFileContents.ConnectionString;

                //Merge local patches

                foreach (var localPatch in localPatchFileContents.Patches)
                {
                    patchFileContents.Patches.RemoveAll(p => p.Id == localPatch.Id);
                    patchFileContents.Patches.Add(localPatch);
                }

                var distinctPatchesFromFile = patchFileContents.Patches.Distinct().ToList();
                var patches = distinctPatchesFromFile.Select(pff => new Patch(pff.Id, new List<Patch>())).ToList();

                var dependsOn = new Dictionary<string, List<Patch>>();
                
                foreach (var patchFromFile in distinctPatchesFromFile)
                {
                    var dependents = patchFromFile.DependsOn.Select(dependent => patches.Find(p => p.Id == dependent))
                        .ToList();
                    if (!dependsOn.ContainsKey(patchFromFile.Id))
                    {
                        dependsOn.Add(patchFromFile.Id, dependents);
                    }
                }

                foreach (var patchToUpdate in patches)
                {
                    patchToUpdate.DependsOn = dependsOn[patchToUpdate.Id];
                }

                var children = new Dictionary<string, List<Patch>>();
                // 1. Loop thru all of the patches.  Named childPatch here in order to maintain 
                //  logical consistency with it's parents (ie. contents of it's [DependsOn] prop
                foreach (var childPatch in patches)
                {
                    // 2. Loop thur "child's" parents
                    foreach (var parentPatch in childPatch.DependsOn)
                    {
                        // 3a. If dictionary already contains an entry for the parent
                        //  then add this child if necessary
                        if (children.ContainsKey(parentPatch.Id))
                        {
                            if (!children[parentPatch.Id].Contains(childPatch))
                            {
                                children[parentPatch.Id].Add(childPatch);
                            }
                        }
                        // 3b.  otherwise add an entry for this parent and current child
                        else
                        {
                            children.Add(parentPatch.Id, new List<Patch>()
                            {
                                childPatch
                            });
                        }
                    }
                }
                
                // Now look thru all the patches again and update the Children property as needed
                foreach (var patchToUpdate in patches)
                {
                    patchToUpdate.Children = new List<Patch>();
                    if (children.ContainsKey(patchToUpdate.Id) && children[patchToUpdate.Id].Any())
                    {
                        patchToUpdate.Children.AddRange(children[patchToUpdate.Id]);
                    };
                }
                
                rc.patches = patches;
            }
            else
            {
                throw new ApplicationException("Configuration file does not exist. Call init first.");
            }

            return rc;
        }

        public void Write(DatabaseBuildConfiguration buildConfiguration)
        {
            var patchFileContent = new PatchFile();
            var localPatchFileContents = new PatchFile();
            if (_io.File.Exists(_localFilePath))
            {
                localPatchFileContents = PatchFile.FromJson(_io.File.ReadAllText(_localFilePath));
            }

            patchFileContent.CodeFiles =
                localPatchFileContents.CodeFiles.Count == 0 && buildConfiguration.CodeFiles?.Count > 0
                    ? (buildConfiguration.CodeFiles ?? new List<string>())
                    : new List<string>();
            patchFileContent.Options = localPatchFileContents.Options.Count == 0 && buildConfiguration.Options?.Count > 0
                ? (buildConfiguration.Options ?? new Dictionary<string, string>())
                : new Dictionary<string, string>();
            patchFileContent.CodeFolder =
                string.IsNullOrWhiteSpace(localPatchFileContents.CodeFolder) &&
                !string.IsNullOrWhiteSpace(buildConfiguration.CodeFolder)
                    ? buildConfiguration.CodeFolder
                    : null;
            patchFileContent.DatabaseType =
                string.IsNullOrWhiteSpace(localPatchFileContents.DatabaseType) &&
                !string.IsNullOrWhiteSpace(buildConfiguration.DatabaseType)
                    ? buildConfiguration.DatabaseType
                    : null;
            patchFileContent.PatchFolder =
                string.IsNullOrWhiteSpace(localPatchFileContents.PatchFolder) &&
                !string.IsNullOrWhiteSpace(buildConfiguration.PatchFolder)
                    ? buildConfiguration.PatchFolder
                    : null;
            patchFileContent.ConnectionString =
                string.IsNullOrWhiteSpace(localPatchFileContents.ConnectionString) &&
                !string.IsNullOrWhiteSpace(buildConfiguration.ConnectionString)
                    ? buildConfiguration.ConnectionString
                    : null;

            // Attempt to move the last patch to Random spot 2 to 10 patches from bottom
            //  This is to try and help git merge conflicts.
            var patchCount = buildConfiguration.patches.Count;
            if (patchCount > 10)
            {
                // We only want to move the patch if there are more than 10 patches.  This number is
                // arbitrary but 10 should be enough
                var mergeTargetIndex = _rand.Next(patchCount - 10, patchCount - 2);
                var lastPatch = buildConfiguration.patches.Last();
                var lastPatchId = lastPatch.Id;
                var lastPatchDependsOn = lastPatch.DependsOn;
                var lastPatchChildren = lastPatch.Children;
            
                buildConfiguration.patches.Remove(lastPatch);
                buildConfiguration.patches.Insert(mergeTargetIndex, new Patch()
                {
                    Id = lastPatchId,
                    Children = lastPatchChildren,
                    DependsOn = lastPatchDependsOn
                });
                
            }
            foreach (var buildConfigurationPatch in buildConfiguration.patches)
            {
                var pff = new PatchFromFile()
                {
                    Id = buildConfigurationPatch.Id,
                };
                foreach (var dependant in buildConfigurationPatch.DependsOn)
                {
                    pff.DependsOn.Add(dependant.Id);
                }

                patchFileContent.Patches.Add(pff);
            }

            _io.File.WriteAllText(_filePath, patchFileContent.ToJson());
        }
    }
}