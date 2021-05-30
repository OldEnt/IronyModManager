﻿// ***********************************************************************
// Assembly         : IronyModManager.IO
// Author           : Mario
// Created          : 03-09-2020
//
// Last Modified By : Mario
// Last Modified On : 05-30-2021
// ***********************************************************************
// <copyright file="ModCollectionExporter.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IronyModManager.IO.Common;
using IronyModManager.IO.Common.MessageBus;
using IronyModManager.IO.Common.Mods;
using IronyModManager.IO.Mods.Importers;
using IronyModManager.Shared;
using IronyModManager.Shared.MessageBus;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers.Zip;

namespace IronyModManager.IO.Mods
{
    /// <summary>
    /// Class ModCollectionExporter.
    /// Implements the <see cref="IronyModManager.IO.Common.Mods.IModCollectionExporter" />
    /// </summary>
    /// <seealso cref="IronyModManager.IO.Common.Mods.IModCollectionExporter" />
    [ExcludeFromCoverage("Skipping testing IO logic.")]
    public class ModCollectionExporter : IModCollectionExporter
    {
        #region Fields

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The message bus
        /// </summary>
        private readonly IMessageBus messageBus;

        /// <summary>
        /// The paradox importer
        /// </summary>
        private readonly ParadoxImporter paradoxImporter;

        /// <summary>
        /// The paradox launcher importer
        /// </summary>
        private readonly ParadoxLauncherImporter paradoxLauncherImporter;

        /// <summary>
        /// The paradoxos importer
        /// </summary>
        private readonly ParadoxosImporter paradoxosImporter;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModCollectionExporter" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="messageBus">The message bus.</param>
        public ModCollectionExporter(ILogger logger, IMessageBus messageBus)
        {
            paradoxosImporter = new ParadoxosImporter(logger);
            paradoxImporter = new ParadoxImporter(logger);
            paradoxLauncherImporter = new ParadoxLauncherImporter(logger);
            this.logger = logger;
            this.messageBus = messageBus;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Exports the asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> ExportAsync(ModCollectionExporterParams parameters)
        {
            using var zip = ArchiveFactory.Create(ArchiveType.Zip);
            using var stream = new System.IO.FileInfo(parameters.File).Open(FileMode.Create, FileAccess.Write);
            using var writer = new ZipWriter(stream, new ZipWriterOptions(CompressionType.Deflate));

            var content = JsonConvert.SerializeObject(parameters.Mod, Formatting.None);
            using var dataStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            zip.AddEntry(Common.Constants.ExportedModContentId, dataStream, false);

            var streams = new List<Stream>() { dataStream };
            if (Directory.Exists(parameters.ModDirectory) && !parameters.ExportModOrderOnly)
            {
                var files = Directory.GetFiles(parameters.ModDirectory, "*.*", SearchOption.AllDirectories);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    foreach (var item in files)
                    {
                        var fs = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var file = item.Replace(parameters.ModDirectory, string.Empty).Trim('\\').Trim('/');
                        zip.AddEntry(file, fs, false, modified: new System.IO.FileInfo(item).LastWriteTime);
                        streams.Add(fs);
                    }
                }
                else
                {
                    // Yeah, osx sucks (ulimit bypass)
                    foreach (var item in files)
                    {
                        var fs = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var ms = new MemoryStream();
                        await fs.CopyToAsync(ms);
                        var file = item.Replace(parameters.ModDirectory, string.Empty).Trim('\\').Trim('/');
                        zip.AddEntry(file, ms, false, modified: new System.IO.FileInfo(item).LastWriteTime);
                        fs.Close();
                        await fs.DisposeAsync();
                        streams.Add(ms);
                    }
                }
            }

            var entries = zip.Entries.Where(p => !p.IsDirectory);
            double total = entries.Count();
            double processed = 0;
            double previousProgress = 0;
            foreach (var item in entries)
            {
                using var entryStream = item.OpenEntryStream();
                writer.Write(item.Key, entryStream, item.LastModifiedTime);
                processed++;
                var perc = GetProgressPercentage(total, processed, 100);
                if (perc != previousProgress)
                {
                    messageBus.Publish(new ModExportProgressEvent(perc));
                    previousProgress = perc;
                }
            }
            zip.Dispose();
            if (streams.Any())
            {
                var task = streams.Select(async p =>
                {
                    p.Close();
                    await p.DisposeAsync();
                });
                await Task.WhenAll(task);
            }
            return true;
        }

        /// <summary>
        /// Imports the asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public Task<bool> ImportAsync(ModCollectionExporterParams parameters)
        {
            return Task.FromResult(ImportInternal(parameters, true));
        }

        /// <summary>
        /// Imports the mod directory asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public Task<bool> ImportModDirectoryAsync(ModCollectionExporterParams parameters)
        {
            return Task.FromResult(ImportInternal(parameters, false));
        }

        /// <summary>
        /// Imports the paradox asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public Task<bool> ImportParadoxAsync(ModCollectionExporterParams parameters)
        {
            return paradoxImporter.ImportAsync(parameters);
        }

        /// <summary>
        /// Imports the paradox launcher asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public Task<bool> ImportParadoxLauncherAsync(ModCollectionExporterParams parameters)
        {
            return paradoxLauncherImporter.ImportAsync(parameters);
        }

        /// <summary>
        /// import paradoxos as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public Task<bool> ImportParadoxosAsync(ModCollectionExporterParams parameters)
        {
            return paradoxosImporter.ImportAsync(parameters);
        }

        /// <summary>
        /// Gets the progress percentage.
        /// </summary>
        /// <param name="total">The total.</param>
        /// <param name="processed">The processed.</param>
        /// <param name="maxPerc">The maximum perc.</param>
        /// <returns>System.Double.</returns>
        protected virtual double GetProgressPercentage(double total, double processed, double maxPerc = 100)
        {
            var perc = Math.Round(processed / total * 100, 2);
            if (perc < 0)
            {
                perc = 0;
            }
            else if (perc > maxPerc)
            {
                perc = maxPerc;
            }
            return perc;
        }

        /// <summary>
        /// Imports the internal.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="importInstance">if set to <c>true</c> [import instance].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ImportInternal(ModCollectionExporterParams parameters, bool importInstance)
        {
            if (!importInstance)
            {
                if (Directory.Exists(parameters.ModDirectory))
                {
                    DiskOperations.DeleteDirectory(parameters.ModDirectory, true);
                }
            }

            var result = false;

            int getTotalFileCount()
            {
                var count = 0;
                using var fileStream = File.OpenRead(parameters.File);
                using var reader = ReaderFactory.Open(fileStream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        count++;
                        var relativePath = reader.Entry.Key.StandardizeDirectorySeparator().Trim(Path.DirectorySeparatorChar);
                        if (reader.Entry.Key.Equals(Common.Constants.ExportedModContentId, StringComparison.OrdinalIgnoreCase))
                        {
                            if (importInstance)
                            {
                                result = true;
                                break;
                            }
                        }
                        else
                        {
                            reader.WriteEntryToDirectory(parameters.ModDirectory, ZipExtractionOpts.GetExtractionOptions());
                        }
                    }
                }
                fileStream.Close();
                fileStream.Dispose();
                return count;
            }

            void parseUsingReaderFactory()
            {
                double total = getTotalFileCount();
                using var fileStream = File.OpenRead(parameters.File);
                using var reader = ReaderFactory.Open(fileStream);
                double processed = 0;
                double previousProgress = 0;
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        var relativePath = reader.Entry.Key.StandardizeDirectorySeparator().Trim(Path.DirectorySeparatorChar);
                        if (reader.Entry.Key.Equals(Common.Constants.ExportedModContentId, StringComparison.OrdinalIgnoreCase))
                        {
                            if (importInstance)
                            {
                                using var entryStream = reader.OpenEntryStream();
                                using var memoryStream = new MemoryStream();
                                entryStream.CopyTo(memoryStream);
                                memoryStream.Seek(0, SeekOrigin.Begin);
                                using var streamReader = new StreamReader(memoryStream, true);
                                var text = streamReader.ReadToEnd();
                                streamReader.Close();
                                streamReader.Dispose();
                                JsonConvert.PopulateObject(text, parameters.Mod);
                                result = true;
                                break;
                            }
                        }
                        else
                        {
                            reader.WriteEntryToDirectory(parameters.ModDirectory, ZipExtractionOpts.GetExtractionOptions());
                        }
                        processed++;
                        var perc = GetProgressPercentage(total, processed, 100);
                        if (perc != previousProgress)
                        {
                            messageBus.Publish(new ModExportProgressEvent(perc));
                            previousProgress = perc;
                        }
                    }
                }
            }

            void parseUsingArchiveFactory()
            {
                using var fileStream = File.OpenRead(parameters.File);
                using var reader = ArchiveFactory.Open(fileStream);
                var entries = reader.Entries.Where(entry => !entry.IsDirectory);
                double total = !importInstance ? entries.Count() : 1;
                double processed = 0;
                double previousProgress = 0;
                foreach (var entry in entries)
                {
                    var relativePath = entry.Key.StandardizeDirectorySeparator().Trim(Path.DirectorySeparatorChar);
                    if (entry.Key.Equals(Common.Constants.ExportedModContentId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (importInstance)
                        {
                            using var entryStream = entry.OpenEntryStream();
                            using var memoryStream = new MemoryStream();
                            entryStream.CopyTo(memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            using var streamReader = new StreamReader(memoryStream, true);
                            var text = streamReader.ReadToEnd();
                            streamReader.Close();
                            streamReader.Dispose();
                            JsonConvert.PopulateObject(text, parameters.Mod);
                            result = true;
                            break;
                        }
                    }
                    else
                    {
                        entry.WriteToDirectory(parameters.ModDirectory, ZipExtractionOpts.GetExtractionOptions());
                    }
                    processed++;
                    var perc = GetProgressPercentage(total, processed, 100);
                    if (perc != previousProgress)
                    {
                        messageBus.Publish(new ModExportProgressEvent(perc));
                        previousProgress = perc;
                    }
                }
            }
            try
            {
                parseUsingArchiveFactory();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                result = false;
                parseUsingReaderFactory();
            }
            return !importInstance || result;
        }

        #endregion Methods
    }
}
