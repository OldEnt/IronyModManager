﻿// ***********************************************************************
// Assembly         : IronyModManager.Services
// Author           : Mario
// Created          : 02-24-2020
//
// Last Modified By : Mario
// Last Modified On : 02-16-2021
// ***********************************************************************
// <copyright file="ModService.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using IronyModManager.IO.Common.Mods;
using IronyModManager.IO.Common.Readers;
using IronyModManager.Models.Common;
using IronyModManager.Parser.Common.Mod;
using IronyModManager.Services.Common;
using IronyModManager.Shared;
using IronyModManager.Shared.Cache;
using IronyModManager.Storage.Common;

namespace IronyModManager.Services
{
    /// <summary>
    /// Class ModService.
    /// Implements the <see cref="IronyModManager.Services.ModBaseService" />
    /// Implements the <see cref="IronyModManager.Services.Common.IModService" />
    /// </summary>
    /// <seealso cref="IronyModManager.Services.ModBaseService" />
    /// <seealso cref="IronyModManager.Services.Common.IModService" />
    public class ModService : ModBaseService, IModService
    {
        #region Fields

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModService" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="definitionInfoProviders">The definition information providers.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="modParser">The mod parser.</param>
        /// <param name="modWriter">The mod writer.</param>
        /// <param name="gameService">The game service.</param>
        /// <param name="storageProvider">The storage provider.</param>
        /// <param name="mapper">The mapper.</param>
        public ModService(ILogger logger, ICache cache, IEnumerable<IDefinitionInfoProvider> definitionInfoProviders,
            IReader reader, IModParser modParser,
            IModWriter modWriter, IGameService gameService,
            IStorageProvider storageProvider, IMapper mapper) : base(cache, definitionInfoProviders, reader, modWriter, modParser, gameService, storageProvider, mapper)
        {
            this.logger = logger;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Builds the mod URL.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <returns>System.String.</returns>
        public virtual string BuildModUrl(IMod mod)
        {
            if (!mod.RemoteId.HasValue)
            {
                return string.Empty;
            }
            if (mod.Source == ModSource.Paradox)
            {
                return string.Format(Constants.Paradox_Url, mod.RemoteId);
            }
            else
            {
                return string.Format(Constants.Steam_Url, mod.RemoteId);
            }
        }

        /// <summary>
        /// Builds the steam URL.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <returns>System.String.</returns>
        public virtual string BuildSteamUrl(IMod mod)
        {
            if (mod.RemoteId.HasValue && mod.Source != ModSource.Paradox)
            {
                return string.Format(Constants.Steam_protocol_uri, BuildModUrl(mod));
            }
            return string.Empty;
        }

        /// <summary>
        /// delete descriptors as an asynchronous operation.
        /// </summary>
        /// <param name="mods">The mods.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public virtual Task<bool> DeleteDescriptorsAsync(IEnumerable<IMod> mods)
        {
            return DeleteDescriptorsInternalAsync(mods);
        }

        /// <summary>
        /// Evals the achievement compatibility.
        /// </summary>
        /// <param name="mods">The mods.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public virtual bool EvalAchievementCompatibility(IEnumerable<IMod> mods)
        {
            var game = GameService.GetSelected();
            if (game != null && mods?.Count() > 0)
            {
                foreach (var item in mods.Where(p => p.IsValid))
                {
                    if (item.Files.Any())
                    {
                        var isAchievementCompatible = !item.Files.Any(p => game.ChecksumFolders.Any(s => p.StartsWith(s, StringComparison.OrdinalIgnoreCase)));
                        item.AchievementStatus = isAchievementCompatible ? AchievementStatus.Compatible : AchievementStatus.NotCompatible;
                    }
                    else
                    {
                        item.AchievementStatus = AchievementStatus.NotEvaluated;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Exports the mods asynchronous.
        /// </summary>
        /// <param name="enabledMods">The mods.</param>
        /// <param name="regularMods">The regular mods.</param>
        /// <param name="modCollection">The mod collection.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public virtual async Task<bool> ExportModsAsync(IReadOnlyCollection<IMod> enabledMods, IReadOnlyCollection<IMod> regularMods, IModCollection modCollection)
        {
            var game = GameService.GetSelected();
            if (game == null || enabledMods == null || regularMods == null || modCollection == null)
            {
                return false;
            }
            var allMods = GetInstalledModsInternal(game, false);
            var mod = GeneratePatchModDescriptor(allMods, game, GenerateCollectionPatchName(modCollection.Name));
            var applyModParams = new ModWriterParameters()
            {
                OtherMods = regularMods.Where(p => !enabledMods.Any(m => m.DescriptorFile.Equals(p.DescriptorFile))).ToList(),
                EnabledMods = enabledMods,
                RootDirectory = game.UserDirectory
            };
            if (await ModWriter.ModDirectoryExistsAsync(new ModWriterParameters()
            {
                RootDirectory = game.UserDirectory,
                Path = mod.FileName
            }))
            {
                if (modCollection.PatchModEnabled)
                {
                    if (await ModWriter.WriteDescriptorAsync(new ModWriterParameters()
                    {
                        Mod = mod,
                        RootDirectory = game.UserDirectory,
                        Path = mod.DescriptorFile,
                        LockDescriptor = CheckIfModShouldBeLocked(game, mod)
                    }, IsPatchModInternal(mod)))
                    {
                        applyModParams.TopPriorityMods = new List<IMod>() { mod };
                        Cache.Invalidate(ModsCachePrefix, ConstructModsCacheKey(game, true), ConstructModsCacheKey(game, false));
                    }
                }
            }
            else
            {
                // Remove left over descriptor
                if (allMods.Any(p => p.Name.Equals(mod.Name)))
                {
                    await DeleteDescriptorsInternalAsync(new List<IMod>() { mod });
                }
            }
            return await ModWriter.ApplyModsAsync(applyModParams);
        }

        /// <summary>
        /// Gets the image stream asynchronous.
        /// </summary>
        /// <param name="modName">Name of the mod.</param>
        /// <param name="path">The path.</param>
        /// <returns>Task&lt;MemoryStream&gt;.</returns>
        public virtual Task<MemoryStream> GetImageStreamAsync(string modName, string path)
        {
            var game = GameService.GetSelected();
            if (game == null || string.IsNullOrWhiteSpace(modName))
            {
                return Task.FromResult((MemoryStream)null);
            }
            var mods = GetInstalledModsInternal(game, false);
            return GetImageStreamAsync(mods.FirstOrDefault(p => p.Name.Equals(modName)), path);
        }

        /// <summary>
        /// Gets the image stream asynchronous.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="path">The path.</param>
        /// <returns>Task&lt;MemoryStream&gt;.</returns>
        public virtual Task<MemoryStream> GetImageStreamAsync(IMod mod, string path)
        {
            if (mod != null && !string.IsNullOrWhiteSpace(path))
            {
                return Reader.GetImageStreamAsync(mod.FullPath, path);
            }
            return Task.FromResult((MemoryStream)null);
        }

        /// <summary>
        /// Gets the installed mods.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <returns>IEnumerable&lt;IMod&gt;.</returns>
        public virtual IEnumerable<IMod> GetInstalledMods(IGame game)
        {
            if (game != null)
            {
                Cache.Invalidate(ModsCachePrefix, ConstructModsCacheKey(game, true), ConstructModsCacheKey(game, false));
            }
            return GetInstalledModsInternal(game, true);
        }

        /// <summary>
        /// install mods as an asynchronous operation.
        /// </summary>
        /// <param name="statusToRetain">The status to retain.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public virtual async Task<IReadOnlyCollection<IModInstallationResult>> InstallModsAsync(IEnumerable<IMod> statusToRetain)
        {
            var game = GameService.GetSelected();
            if (game == null)
            {
                return null;
            }
            var mods = GetInstalledModsInternal(game, false);
            var descriptors = new List<IModInstallationResult>();
            var userDirectoryMods = GetAllModDescriptors(Path.Combine(game.UserDirectory, Shared.Constants.ModDirectory), ModSource.Local);
            if (userDirectoryMods?.Count() > 0)
            {
                descriptors.AddRange(userDirectoryMods);
            }
            var workshopDirectoryMods = GetAllModDescriptors(game.WorkshopDirectory, ModSource.Steam);
            if (workshopDirectoryMods?.Count() > 0)
            {
                descriptors.AddRange(workshopDirectoryMods);
            }
            var diffs = descriptors.Where(p => p.Mod != null && !mods.Any(m => m.DescriptorFile.Equals(p.Mod.DescriptorFile, StringComparison.OrdinalIgnoreCase))).ToList();
            if (diffs.Count > 0)
            {
                var result = new List<IModInstallationResult>();
                await ModWriter.CreateModDirectoryAsync(new ModWriterParameters()
                {
                    RootDirectory = game.UserDirectory,
                    Path = Shared.Constants.ModDirectory
                });
                var tasks = new List<Task>();
                foreach (var diff in diffs.GroupBy(p => p.Mod.DescriptorFile))
                {
                    var installResult = diff.FirstOrDefault();
                    var localDiff = diff.FirstOrDefault().Mod;
                    if (IsPatchModInternal(localDiff))
                    {
                        continue;
                    }
                    tasks.Add(Task.Run(async () =>
                    {
                        bool shouldLock = CheckIfModShouldBeLocked(game, localDiff);
                        if (statusToRetain != null && !shouldLock)
                        {
                            var mod = statusToRetain.FirstOrDefault(p => p.DescriptorFile.Equals(localDiff.DescriptorFile, StringComparison.OrdinalIgnoreCase));
                            if (mod != null)
                            {
                                shouldLock = mod.IsLocked;
                            }
                        }
                        await ModWriter.WriteDescriptorAsync(new ModWriterParameters()
                        {
                            Mod = localDiff,
                            RootDirectory = game.UserDirectory,
                            Path = localDiff.DescriptorFile,
                            LockDescriptor = shouldLock
                        }, IsPatchModInternal(localDiff)); ;
                    }));
                    installResult.Installed = true;
                    result.Add(installResult);
                }
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                    Cache.Invalidate(ModsCachePrefix, ConstructModsCacheKey(game, true), ConstructModsCacheKey(game, false));
                }
                if (descriptors.Any(p => p.Invalid))
                {
                    result.AddRange(descriptors.Where(p => p.Invalid));
                }
                return result;
            }
            if (descriptors.Any(p => p.Invalid))
            {
                return descriptors.Where(p => p.Invalid).ToList();
            }
            return null;
        }

        /// <summary>
        /// lock descriptors as an asynchronous operation.
        /// </summary>
        /// <param name="mods">The mods.</param>
        /// <param name="isLocked">if set to <c>true</c> [is locked].</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public virtual async Task<bool> LockDescriptorsAsync(IEnumerable<IMod> mods, bool isLocked)
        {
            var game = GameService.GetSelected();
            if (game != null && mods?.Count() > 0)
            {
                var tasks = new List<Task>();
                foreach (var item in mods)
                {
                    // Cannot lock\unlock mandatory local zipped mods
                    if (!CheckIfModShouldBeLocked(game, item))
                    {
                        var task = ModWriter.SetDescriptorLockAsync(new ModWriterParameters()
                        {
                            Mod = item,
                            RootDirectory = game.UserDirectory
                        }, isLocked);
                        item.IsLocked = isLocked;
                        tasks.Add(task);
                    }
                }
                await Task.WhenAll(tasks);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mods the directory exists asynchronous.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public virtual Task<bool> ModDirectoryExistsAsync(string folder)
        {
            var game = GameService.GetSelected();
            if (game == null)
            {
                return Task.FromResult(false);
            }
            return ModWriter.ModDirectoryExistsAsync(new ModWriterParameters()
            {
                RootDirectory = game.UserDirectory,
                Path = Path.Combine(Shared.Constants.ModDirectory, folder)
            });
        }

        /// <summary>
        /// populate mod files as an asynchronous operation.
        /// </summary>
        /// <param name="mods">The mods.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public virtual Task<bool> PopulateModFilesAsync(IEnumerable<IMod> mods)
        {
            return PopulateModFilesInternalAsync(mods);
        }

        /// <summary>
        /// Purges the mod directory asynchronous.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public virtual async Task<bool> PurgeModDirectoryAsync(string folder)
        {
            var game = GameService.GetSelected();
            if (game == null)
            {
                return false;
            }
            var fullPath = Path.Combine(game.UserDirectory, Shared.Constants.ModDirectory, folder);
            var result = await ModWriter.PurgeModDirectoryAsync(new ModWriterParameters()
            {
                RootDirectory = game.UserDirectory,
                Path = Path.Combine(Shared.Constants.ModDirectory, folder)
            }, true);
            var mods = GetInstalledModsInternal(game, false);
            if (mods.Any(p => !string.IsNullOrWhiteSpace(p.FullPath) && p.FullPath.Contains(fullPath)))
            {
                var mod = mods.Where(p => p.FullPath.Contains(fullPath));
                if (mod.Any())
                {
                    await DeleteDescriptorsInternalAsync(mod);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all mod descriptors.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="modSource">The mod source.</param>
        /// <returns>IEnumerable&lt;IMod&gt;.</returns>
        protected virtual IEnumerable<IModInstallationResult> GetAllModDescriptors(string path, ModSource modSource)
        {
            var files = Directory.Exists(path) ? Directory.EnumerateFiles(path, $"*{Shared.Constants.ZipExtension}").Union(Directory.EnumerateFiles(path, $"*{Shared.Constants.BinExtension}")) : Array.Empty<string>();
            var directories = Directory.Exists(path) ? Directory.EnumerateDirectories(path) : Array.Empty<string>();
            var mods = new List<IModInstallationResult>();

            static void setDescriptorPath(IMod mod, string desiredPath, string localPath)
            {
                if (desiredPath.Equals(localPath, StringComparison.OrdinalIgnoreCase))
                {
                    mod.DescriptorFile = desiredPath;
                }
                else
                {
                    if (mod.RemoteId.GetValueOrDefault() > 0)
                    {
                        mod.DescriptorFile = desiredPath;
                    }
                    else
                    {
                        mod.Source = ModSource.Local;
                        mod.DescriptorFile = localPath;
                    }
                }
            }

            void parseModFiles(string path, ModSource source, bool isDirectory)
            {
                var result = GetModelInstance<IModInstallationResult>();
                try
                {
                    var fileInfo = Reader.GetFileInfo(path, Shared.Constants.DescriptorFile);
                    if (fileInfo == null)
                    {
                        fileInfo = Reader.GetFileInfo(path, $"*{Shared.Constants.ModExtension}");
                        if (fileInfo == null)
                        {
                            return;
                        }
                    }
                    var mod = Mapper.Map<IMod>(ModParser.Parse(fileInfo.Content));
                    mod.FileName = path.Replace("\\", "/");
                    mod.FullPath = path.StandardizeDirectorySeparator();
                    mod.IsLocked = fileInfo.IsReadOnly;
                    mod.Source = source;
                    var cleanedPath = path;
                    if (!isDirectory)
                    {
                        cleanedPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                    }

                    var localPath = $"{Shared.Constants.ModDirectory}/{cleanedPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()}{Shared.Constants.ModExtension}";
                    switch (mod.Source)
                    {
                        case ModSource.Local:
                            setDescriptorPath(mod, localPath, localPath);
                            break;

                        case ModSource.Steam:
                            if (mod.RemoteId.GetValueOrDefault() == 0)
                            {
                                if (!isDirectory)
                                {
                                    var modParentDirectory = Path.GetDirectoryName(path);
                                    mod.RemoteId = GetSteamModId(modParentDirectory, isDirectory);
                                }
                                else
                                {
                                    mod.RemoteId = GetSteamModId(path, isDirectory);
                                }
                            }
                            setDescriptorPath(mod, $"{Shared.Constants.ModDirectory}/{Constants.Steam_mod_id}{mod.RemoteId}{Shared.Constants.ModExtension}", localPath);
                            break;

                        case ModSource.Paradox:
                            if (!isDirectory)
                            {
                                var modParentDirectory = Path.GetDirectoryName(path);
                                mod.RemoteId = GetPdxModId(modParentDirectory, isDirectory);
                            }
                            else
                            {
                                mod.RemoteId = GetPdxModId(path, isDirectory);
                            }
                            setDescriptorPath(mod, $"{Shared.Constants.ModDirectory}/{Constants.Paradox_mod_id}{mod.RemoteId}{Shared.Constants.ModExtension}", localPath);
                            break;

                        default:
                            break;
                    }
                    result.Mod = mod;
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    result.Invalid = true;
                }
                result.Path = path;
                mods.Add(result);
            }
            if (files.Any())
            {
                foreach (var file in files)
                {
                    parseModFiles(file, modSource, false);
                }
            }
            if (directories.Any())
            {
                foreach (var directory in directories)
                {
                    var modSourceOverride = directory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).
                            LastOrDefault().Contains(Constants.Paradox_mod_id, StringComparison.OrdinalIgnoreCase) ? ModSource.Paradox : modSource;
                    var zipFiles = Directory.EnumerateFiles(directory, $"*{Shared.Constants.ZipExtension}").Union(Directory.EnumerateFiles(directory, $"*{Shared.Constants.BinExtension}"));
                    if (zipFiles.Any())
                    {
                        foreach (var zip in zipFiles)
                        {
                            parseModFiles(zip, modSourceOverride, false);
                        }
                    }
                    else
                    {
                        parseModFiles(directory, modSourceOverride, true);
                    }
                }
            }
            return mods;
        }

        /// <summary>
        /// Gets the steam mod identifier.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">if set to <c>true</c> [is directory].</param>
        /// <returns>System.Int32.</returns>
        protected virtual long GetSteamModId(string path, bool isDirectory = false)
        {
            var name = !isDirectory ? Path.GetFileNameWithoutExtension(path) : path;
#pragma warning disable CA1806 // Do not ignore method results
            long.TryParse(name.Replace(Constants.Steam_mod_id, string.Empty), out var id);
#pragma warning restore CA1806 // Do not ignore method results
            return id;
        }

        #endregion Methods
    }
}
