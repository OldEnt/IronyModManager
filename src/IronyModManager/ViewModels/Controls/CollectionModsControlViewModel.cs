﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 03-03-2020
//
// Last Modified By : Mario
// Last Modified On : 03-08-2020
// ***********************************************************************
// <copyright file="CollectionModsControlViewModel.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using IronyModManager.Common;
using IronyModManager.Common.ViewModels;
using IronyModManager.Implementation.Actions;
using IronyModManager.Localization;
using IronyModManager.Localization.Attributes;
using IronyModManager.Models.Common;
using IronyModManager.Services.Common;
using IronyModManager.Shared;
using ReactiveUI;
using SmartFormat;

namespace IronyModManager.ViewModels.Controls
{
    /// <summary>
    /// Class CollectionModsControlViewModel.
    /// Implements the <see cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    /// </summary>
    /// <seealso cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    [ExcludeFromCoverage("This should be tested via functional testing.")]
    public class CollectionModsControlViewModel : BaseViewModel
    {
        #region Fields

        /// <summary>
        /// The application state service
        /// </summary>
        private readonly IAppStateService appStateService;

        /// <summary>
        /// The localization manager
        /// </summary>
        private readonly ILocalizationManager localizationManager;

        /// <summary>
        /// The mod collection service
        /// </summary>
        private readonly IModCollectionService modCollectionService;

        /// <summary>
        /// The notification action
        /// </summary>
        private readonly INotificationAction notificationAction;

        /// <summary>
        /// The mods changed
        /// </summary>
        private IDisposable modsChanged;

        /// <summary>
        /// The skip mod collection save
        /// </summary>
        private bool skipModCollectionSave = false;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionModsControlViewModel" /> class.
        /// </summary>
        /// <param name="modCollectionService">The mod collection service.</param>
        /// <param name="appStateService">The application state service.</param>
        /// <param name="addNewCollection">The add new collection.</param>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="notificationAction">The notification action.</param>
        public CollectionModsControlViewModel(IModCollectionService modCollectionService,
            IAppStateService appStateService, AddNewCollectionControlViewModel addNewCollection,
            ILocalizationManager localizationManager, INotificationAction notificationAction)
        {
            this.modCollectionService = modCollectionService;
            this.appStateService = appStateService;
            AddNewCollection = addNewCollection;
            this.localizationManager = localizationManager;
            this.notificationAction = notificationAction;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the add new collection.
        /// </summary>
        /// <value>The add new collection.</value>
        public virtual AddNewCollectionControlViewModel AddNewCollection { get; protected set; }

        /// <summary>
        /// Gets or sets the create.
        /// </summary>
        /// <value>The create.</value>
        [StaticLocalization(LocalizationResources.Collection_Mods.Create)]
        public virtual string Create { get; protected set; }

        /// <summary>
        /// Gets or sets the create command.
        /// </summary>
        /// <value>The create command.</value>
        public virtual ReactiveCommand<Unit, Unit> CreateCommand { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether [entering new collection].
        /// </summary>
        /// <value><c>true</c> if [entering new collection]; otherwise, <c>false</c>.</value>
        public virtual bool EnteringNewCollection { get; protected set; }

        /// <summary>
        /// Gets or sets the mod collections.
        /// </summary>
        /// <value>The mod collections.</value>
        public virtual IEnumerable<IModCollection> ModCollections { get; protected set; }

        /// <summary>
        /// Gets or sets the mods.
        /// </summary>
        /// <value>The mods.</value>
        public virtual IEnumerable<IMod> Mods { get; protected set; }

        /// <summary>
        /// Gets or sets the remove.
        /// </summary>
        /// <value>The remove.</value>
        [StaticLocalization(LocalizationResources.Collection_Mods.Remove)]
        public virtual string Remove { get; protected set; }

        /// <summary>
        /// Gets or sets the remove command.
        /// </summary>
        /// <value>The remove command.</value>
        public virtual ReactiveCommand<Unit, Unit> RemoveCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the selected mod collection.
        /// </summary>
        /// <value>The selected mod collection.</value>
        public virtual IModCollection SelectedModCollection { get; protected set; }

        /// <summary>
        /// Gets or sets the selected mods.
        /// </summary>
        /// <value>The selected mods.</value>
        public virtual IEnumerable<IMod> SelectedMods { get; protected set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [StaticLocalization(LocalizationResources.Collection_Mods.Name)]
        public virtual string Title { get; protected set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Sets the mods.
        /// </summary>
        /// <param name="mods">The mods.</param>
        public void SetMods(IEnumerable<IMod> mods)
        {
            Mods = mods;
            SelectedMods = mods != null ? mods.Where(p => p.IsSelected) : null;

            SubscribeToMods();
        }

        /// <summary>
        /// Loads the mod collections.
        /// </summary>
        protected virtual void LoadModCollections()
        {
            ModCollections = modCollectionService.GetAll();
            var selected = ModCollections?.FirstOrDefault(p => p.IsSelected);
            if (selected != null)
            {
                SelectedModCollection = selected;
            }
        }

        /// <summary>
        /// Called when [activated].
        /// </summary>
        /// <param name="disposables">The disposables.</param>
        protected override void OnActivated(CompositeDisposable disposables)
        {
            SubscribeToMods();

            var state = appStateService.Get();

            LoadModCollections();

            CreateCommand = ReactiveCommand.Create(() =>
            {
                EnteringNewCollection = true;
            }).DisposeWith(disposables);

            RemoveCommand = ReactiveCommand.Create(() =>
            {
            }).DisposeWith(disposables);

            this.WhenAnyValue(c => c.SelectedModCollection).Subscribe(o =>
            {
                skipModCollectionSave = true;
                SaveState();
                foreach (var item in Mods)
                {
                    item.IsSelected = false;
                }
                var existingCollection = modCollectionService.Get(SelectedModCollection?.Name ?? string.Empty);
                if (existingCollection != null)
                {
                    foreach (var item in existingCollection.Mods)
                    {
                        var mod = Mods.FirstOrDefault(p => p.DescriptorFile.Equals(item, StringComparison.InvariantCultureIgnoreCase));
                        if (mod != null)
                        {
                            mod.IsSelected = true;
                        }
                    }
                }
                skipModCollectionSave = false;
            }).DisposeWith(disposables);

            this.WhenAnyValue(v => v.AddNewCollection.IsActivated).Where(p => p == true).Subscribe(activated =>
            {
                Observable.Merge(AddNewCollection.CreateCommand, AddNewCollection.CancelCommand.Select(_ => new Implementation.CommandResult<string>(string.Empty, Implementation.CommandState.NotExecuted))).Subscribe(result =>
                {
                    var notification = new
                    {
                        CollectionName = result.Result
                    };
                    switch (result.State)
                    {
                        case Implementation.CommandState.Success:
                            skipModCollectionSave = true;
                            foreach (var mod in Mods)
                            {
                                mod.IsSelected = false;
                            }
                            LoadModCollections();
                            SaveState();
                            skipModCollectionSave = EnteringNewCollection = false;
                            var successTitle = localizationManager.GetResource(LocalizationResources.Notifications.CollectionCreated.Title);
                            var successMessage = Smart.Format(localizationManager.GetResource(LocalizationResources.Notifications.CollectionCreated.Message), notification);
                            notificationAction.ShowNotification(successTitle, successMessage, NotificationType.Success);
                            break;

                        case Implementation.CommandState.Exists:
                            var existsTitle = localizationManager.GetResource(LocalizationResources.Notifications.CollectionExists.Title);
                            var existsMssage = Smart.Format(localizationManager.GetResource(LocalizationResources.Notifications.CollectionExists.Message), notification);
                            notificationAction.ShowNotification(existsTitle, existsMssage, NotificationType.Warning);
                            break;

                        case Implementation.CommandState.NotExecuted:
                            EnteringNewCollection = false;
                            break;

                        default:
                            break;
                    }
                }).DisposeWith(disposables);
            }).DisposeWith(disposables);

            base.OnActivated(disposables);
        }

        /// <summary>
        /// Called when [selected game changed].
        /// </summary>
        /// <param name="game">The game.</param>
        protected override void OnSelectedGameChanged(IGame game)
        {
            base.OnSelectedGameChanged(game);
            LoadModCollections();
        }

        /// <summary>
        /// Saves the selected collection.
        /// </summary>
        protected virtual void SaveSelectedCollection()
        {
            var collection = modCollectionService.Create();
            collection.Name = SelectedModCollection.Name;
            collection.Mods = SelectedMods.Where(p => p.IsSelected).Select(p => p.DescriptorFile);
            collection.IsSelected = true;
            modCollectionService.Save(collection);
        }

        /// <summary>
        /// Saves the state.
        /// </summary>
        protected virtual void SaveState()
        {
            var state = appStateService.Get();
            appStateService.Save(state);
        }

        /// <summary>
        /// Subscribes to mods.
        /// </summary>
        protected virtual void SubscribeToMods()
        {
            modsChanged?.Dispose();
            modsChanged = null;
            if (Mods != null && Disposables != null)
            {
                modsChanged = Mods.ToSourceList().Connect().WhenAnyPropertyChanged().Subscribe(s =>
                {
                    SelectedMods = Mods.Where(p => p.IsSelected);
                    if (!skipModCollectionSave && !string.IsNullOrWhiteSpace(SelectedModCollection?.Name))
                    {
                        SaveSelectedCollection();
                    }
                }).DisposeWith(Disposables);
            }
        }

        #endregion Methods
    }
}
