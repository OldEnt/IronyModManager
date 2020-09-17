﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 09-16-2020
//
// Last Modified By : Mario
// Last Modified On : 09-17-2020
// ***********************************************************************
// <copyright file="IronyAppCast.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IronyModManager.DI;
using IronyModManager.Services.Common;
using NetSparkleUpdater;
using NetSparkleUpdater.AppCastHandlers;
using NetSparkleUpdater.Configurations;
using NetSparkleUpdater.Interfaces;

namespace IronyModManager.Implementation.Updater
{
    /// <summary>
    /// Class IronyAppCast.
    /// Implements the <see cref="NetSparkleUpdater.AppCastHandlers.XMLAppCast" />
    /// </summary>
    /// <seealso cref="NetSparkleUpdater.AppCastHandlers.XMLAppCast" />
    public class IronyAppCast : XMLAppCast
    {
        #region Fields

        /// <summary>
        /// The prerelease version tags
        /// </summary>
        private static readonly string[] prereleaseVersionTags = new string[] { "alpha", "beta", "preview", "rc" };

        /// <summary>
        /// The configuration
        /// </summary>
        private Configuration config;

        /// <summary>
        /// The signature verifier
        /// </summary>
        private ISignatureVerifier signatureVerifier;

        /// <summary>
        /// The updater service
        /// </summary>
        private IUpdaterService updaterService;

        #endregion Fields

        #region Methods

        /// <summary>
        /// Gets the available updates.
        /// </summary>
        /// <returns>List&lt;AppCastItem&gt;.</returns>
        public override List<AppCastItem> GetAvailableUpdates()
        {
            if (updaterService == null)
            {
                updaterService = DIResolver.Get<IUpdaterService>();
            }
            Version installed = new Version(config.InstalledVersion);
            var signatureNeeded = Utilities.IsSignatureNeeded(signatureVerifier.SecurityMode, signatureVerifier.HasValidKeyInformation(), false);
            var isInstallerVersion = IsInstallerVersion();
            var allowAlphaVersions = updaterService.Get().CheckForPrerelease;

            return Items.Where((item) =>
            {
                // Filter out prerelease tags if specified as such
                if (!allowAlphaVersions && prereleaseVersionTags.Any(p => item.Version.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                // Filter out by os
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Filter out if using portable or installer version
                    var fileName = item.DownloadLink.Split("/", StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    if (!item.IsWindowsUpdate)
                    {
                        return false;
                    }
                    else if (isInstallerVersion && !fileName.Contains("setup", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    else if (!isInstallerVersion && fileName.Contains("setup", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !item.IsMacOSUpdate)
                {
                    return false;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !item.IsLinuxUpdate)
                {
                    return false;
                }
                // Base validation stuff
                if (new Version(item.Version).CompareTo(installed) <= 0)
                {
                    return false;
                }
                if (signatureNeeded && string.IsNullOrEmpty(item.DownloadSignature) && !string.IsNullOrEmpty(item.DownloadLink))
                {
                    return false;
                }
                return true;
            }).ToList();
        }

        /// <summary>
        /// Setups the application cast handler.
        /// </summary>
        /// <param name="dataDownloader">The data downloader.</param>
        /// <param name="castUrl">The cast URL.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="signatureVerifier">The signature verifier.</param>
        /// <param name="logWriter">The log writer.</param>
        public new void SetupAppCastHandler(IAppCastDataDownloader dataDownloader, string castUrl, Configuration config, ISignatureVerifier signatureVerifier, ILogger logWriter = null)
        {
            // Why on earth would any of these files be marked as protected or maybe even declared as properties with a private setter?
            this.config = config;
            this.signatureVerifier = signatureVerifier;
            base.SetupAppCastHandler(dataDownloader, castUrl, config, signatureVerifier, logWriter);
        }

        /// <summary>
        /// Determines whether [is installer version].
        /// </summary>
        /// <returns><c>true</c> if [is installer version]; otherwise, <c>false</c>.</returns>
        private bool IsInstallerVersion()
        {
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.exe");
            return files.Any(p => p.StartsWith("unins", StringComparison.OrdinalIgnoreCase));
        }

        #endregion Methods
    }
}
