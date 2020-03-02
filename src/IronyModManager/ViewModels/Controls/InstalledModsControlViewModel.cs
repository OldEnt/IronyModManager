﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 02-29-2020
//
// Last Modified By : Mario
// Last Modified On : 03-02-2020
// ***********************************************************************
// <copyright file="InstalledModsControlViewModel.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using DynamicData;
using IronyModManager.Common;
using IronyModManager.Common.ViewModels;
using IronyModManager.Implementation.MenuActions;
using IronyModManager.Localization.Attributes;
using IronyModManager.Models.Common;
using IronyModManager.Services.Common;
using IronyModManager.Shared;
using ReactiveUI;

namespace IronyModManager.ViewModels.Controls
{
    /// <summary>
    /// Class InstalledModsControlViewModel.
    /// Implements the <see cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    /// </summary>
    /// <seealso cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    [ExcludeFromCoverage("This should be tested via functional testing.")]
    public class InstalledModsControlViewModel : BaseViewModel
    {
        #region Fields

        /// <summary>
        /// The game service
        /// </summary>
        private readonly IGameService gameService;

        /// <summary>
        /// The mod service
        /// </summary>
        private readonly IModService modService;

        /// <summary>
        /// The URL action
        /// </summary>
        private readonly IUrlAction urlAction;

        /// <summary>
        /// The mods changed
        /// </summary>
        private IDisposable modsChanged;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledModsControlViewModel" /> class.
        /// </summary>
        /// <param name="gameService">The game service.</param>
        /// <param name="modService">The mod service.</param>
        /// <param name="modNameSortOrder">The mod name sort order.</param>
        /// <param name="modVersionSortOrder">The mod version sort order.</param>
        /// <param name="urlAction">The URL action.</param>
        public InstalledModsControlViewModel(IGameService gameService,
            IModService modService, SortOrderControlViewModel modNameSortOrder,
            SortOrderControlViewModel modVersionSortOrder, IUrlAction urlAction)
        {
            this.modService = modService;
            this.gameService = gameService;
            this.urlAction = urlAction;

            ModNameSortOrder = modNameSortOrder;
            ModVersionSortOrder = modVersionSortOrder;
            // Set default order and sort order text
            ModNameSortOrder.SortOrder = Implementation.SortOrder.Asc;
            ModNameSortOrder.Text = ModName;
            ModVersionSortOrder.SortOrder = Implementation.SortOrder.None;
            ModVersionSortOrder.Text = ModVersion;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the copy URL.
        /// </summary>
        /// <value>The copy URL.</value>
        [StaticLocalization(LocalizationResources.Mod_Url.Copy)]
        public virtual string CopyUrl { get; protected set; }

        /// <summary>
        /// Gets or sets the copy URL command.
        /// </summary>
        /// <value>The copy URL command.</value>
        public virtual ReactiveCommand<Unit, Unit> CopyUrlCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the hovered item.
        /// </summary>
        /// <value>The hovered item.</value>
        public virtual IMod HoveredItem { get; set; }

        /// <summary>
        /// Gets or sets the name of the mod.
        /// </summary>
        /// <value>The name of the mod.</value>
        [StaticLocalization(LocalizationResources.Installed_Mods.Mod_Name)]
        public virtual string ModName { get; protected set; }

        /// <summary>
        /// Gets or sets the mod name sort order.
        /// </summary>
        /// <value>The mod name sort order.</value>
        public virtual SortOrderControlViewModel ModNameSortOrder { get; protected set; }

        /// <summary>
        /// Gets or sets the mods.
        /// </summary>
        /// <value>The mods.</value>
        public virtual IEnumerable<IMod> Mods { get; protected set; }

        /// <summary>
        /// Gets or sets the mod selected.
        /// </summary>
        /// <value>The mod selected.</value>
        [StaticLocalization(LocalizationResources.Installed_Mods.Selected)]
        public virtual string ModSelected { get; protected set; }

        /// <summary>
        /// Gets or sets the mod version.
        /// </summary>
        /// <value>The mod version.</value>
        [StaticLocalization(LocalizationResources.Installed_Mods.Supported_Version)]
        public virtual string ModVersion { get; protected set; }

        /// <summary>
        /// Gets or sets the mod version sort order.
        /// </summary>
        /// <value>The mod version sort order.</value>
        public virtual SortOrderControlViewModel ModVersionSortOrder { get; protected set; }

        /// <summary>
        /// Gets or sets the open URL.
        /// </summary>
        /// <value>The open URL.</value>
        [StaticLocalization(LocalizationResources.Mod_Url.Open)]
        public virtual string OpenUrl { get; protected set; }

        /// <summary>
        /// Gets or sets the open URL command.
        /// </summary>
        /// <value>The open URL command.</value>
        public virtual ReactiveCommand<Unit, Unit> OpenUrlCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the selected mods.
        /// </summary>
        /// <value>The selected mods.</value>
        public virtual IEnumerable<IMod> SelectedMods { get; protected set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Gets the selected mod URL.
        /// </summary>
        /// <returns>System.String.</returns>
        public virtual string GetHoveredModUrl()
        {
            if (HoveredItem != null)
            {
                var url = modService.BuildModUrl(HoveredItem);
                return url;
            }
            return string.Empty;
        }

        /// <summary>
        /// Called when [locale changed].
        /// </summary>
        /// <param name="newLocale">The new locale.</param>
        /// <param name="oldLocale">The old locale.</param>
        public override void OnLocaleChanged(string newLocale, string oldLocale)
        {
            ModVersionSortOrder.Text = ModVersion;
            ModNameSortOrder.Text = ModName;

            base.OnLocaleChanged(newLocale, oldLocale);
        }

        /// <summary>
        /// Binds the specified game.
        /// </summary>
        /// <param name="game">The game.</param>
        protected virtual void Bind(IGame game = null)
        {
            if (game == null)
            {
                game = gameService.GetSelected();
            }
            if (game != null)
            {
                Mods = modService.GetInstalledMods(game).ToObservableCollection();
                SelectedMods = Mods.Where(p => p.IsSelected);

                modsChanged?.Dispose();
                modsChanged = Mods.ToSourceList().Connect().WhenAnyPropertyChanged().Subscribe(s =>
                {
                    SelectedMods = Mods.Where(p => p.IsSelected);
                }).DisposeWith(Disposables);
            }
        }

        /// <summary>
        /// Called when [activated].
        /// </summary>
        /// <param name="disposables">The disposables.</param>
        protected override void OnActivated(CompositeDisposable disposables)
        {
            Bind();

            ModNameSortOrder.SortCommand.Subscribe(s =>
            {
                switch (ModNameSortOrder.SortOrder)
                {
                    case Implementation.SortOrder.Asc:
                        Mods = Mods.OrderBy(s => s.Name).ToObservableCollection();
                        ModVersionSortOrder.SetSortOrder(Implementation.SortOrder.None);
                        break;

                    case Implementation.SortOrder.Desc:
                        Mods = Mods.OrderByDescending(s => s.Name).ToObservableCollection();
                        ModVersionSortOrder.SetSortOrder(Implementation.SortOrder.None);
                        break;

                    default:
                        break;
                }
            }).DisposeWith(disposables);

            ModVersionSortOrder.SortCommand.Subscribe(s =>
            {
                switch (ModVersionSortOrder.SortOrder)
                {
                    case Implementation.SortOrder.Asc:
                        Mods = Mods.OrderBy(s => s.SupportedVersion).ToObservableCollection();
                        ModNameSortOrder.SetSortOrder(Implementation.SortOrder.None);
                        break;

                    case Implementation.SortOrder.Desc:
                        Mods = Mods.OrderByDescending(s => s.SupportedVersion).ToObservableCollection();
                        ModNameSortOrder.SetSortOrder(Implementation.SortOrder.None);
                        break;

                    default:
                        break;
                }
            }).DisposeWith(disposables);

            OpenUrlCommand = ReactiveCommand.Create(() =>
            {
                var url = GetHoveredModUrl();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    urlAction.OpenAsync(url);
                }
            }).DisposeWith(disposables);

            CopyUrlCommand = ReactiveCommand.Create(() =>
            {
                var url = GetHoveredModUrl();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    urlAction.CopyAsync(url);
                }
            }).DisposeWith(disposables);

            base.OnActivated(disposables);
        }

        /// <summary>
        /// Called when [selected game changed].
        /// </summary>
        /// <param name="game">The game.</param>
        protected override void OnSelectedGameChanged(IGame game)
        {
            Bind(game);

            base.OnSelectedGameChanged(game);
        }

        #endregion Methods
    }
}
