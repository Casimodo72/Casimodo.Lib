"use strict";

// KABU TODO: Eliminate references geoassistant.MoTypeKeys.
var kendomodo;
(function (kendomodo) {
    (function (ui) {

        function MoFileSystemException(message) {
            this.message = message;
            this.name = "MoFileSystemException";
        }

        ui.MoFileExplorerViewModel = (function () {

            var MoFileExplorerViewModel = function (options) {
                // TODO: Use a deep copy of options.
                this._options = options;

                this._options.isRecycleBinEnabled = typeof options.isRecycleBinEnabled !== "undefined" ? options.isRecycleBinEnabled : true;
                this._options.isFileSystemTemplateEnabled = typeof options.isFileSystemTemplateEnabled !== "undefined" ? options.isFileSystemTemplateEnabled : true;
                this._options.areFileSelectorsVisible = typeof options.areFileSelectorsVisible !== "undefined" ? options.areFileSelectorsVisible : false;

                // Owner fields: Id, TypeId, Name and Kind.
                if (typeof options.owners === "function")
                    this._options.owners = options.owners();
                if (!this._options.owners)
                    this._options.owners = [];

                this._isComponentInitialized = false;
                this._componentModels = {};
                this._components = {};
                // NOTE: _initComponent() on demand using initComponent().
            };

            var fn = MoFileExplorerViewModel.prototype;

            fn.initComponent = function () {
                this._initComponent(this._options);
            };

            fn.refresh = function () {
                return this._components.treeViewModel.refresh();
            };

            fn.clear = function () {
                if (!this._isComponentInitialized)
                    return;
                this._components.filesViewModel.clearSelection();
                this._components.treeViewModel.clear();
                this._components.filesViewModel.clear();
            };

            fn._getCurrentOwner = function () {
                return this._componentModels.owners.current;
            };

            fn.activateOwner = function (ownerKind) {
                if (!this._getCurrentOwner() && !ownerKind) {
                    // Activate first owner.
                    this._activateOwnerAt(0);
                    return;
                }

                if (!this._getCurrentOwner() && ownerKind) {
                    // Set current owner by provided owner kind.
                    this._activateOwnerAt(this._getDisplayedOwners().indexOf(x => x.Kind === ownerKind));
                    return;
                }

                if (this._getCurrentOwner() && !ownerKind) {
                    // Reactivate current owner.
                    this._onCurrentOwnerChanged();
                    return;
                }
            };

            fn._activateOwnerAt = function (index) {
                if (index < 0)
                    return;

                this._componentModels.owners.set("current", this._componentModels.owners.items[index]);
                this._onCurrentOwnerChanged();
            };

            fn.getCurrentOwner = function () {
                return this._getCurrentOwner();
            };

            fn.getIsCurrentOwnerValid = function () {
                var owner = this._getCurrentOwner();
                return owner && owner.Id;
            };

            fn.clearAllOwnerValues = function () {
                this._getDisplayedOwners().forEach(x => {
                    x.set("Id", null);
                    x.set("Name", "?");
                    x.set("CompanyId", null);
                });
            };

            fn.setOwnerValues = function (ownerKind, values) {
                var owner = this._getDisplayedOwners().find(x => x.Kind === ownerKind);
                if (!owner)
                    return;

                owner.set("Id", values.Id);
                owner.set("Name", values.Name);
                owner.set("CompanyId", values.CompanyId || null);
            };

            fn._getDisplayedOwners = function () {
                return this._componentModels.owners.items;
            };

            fn._onCurrentOwnerChanged = function () {
                var owner = this._getCurrentOwner();

                // Clear the folders tree view model. This will also enable
                // expansion of all folders after the next refresh.
                this._components.treeViewModel.clear();
                this._components.filesViewModel.clear();
                // Set the selected owner.
                this._components.treeViewModel.setOwner(owner);
                // Don't refresh initially, but alwas refresh subsequently, so that the
                // consumer can have control over the first refresh.
                if (this._isComponentInitialized) {
                    this._components.treeViewModel.refresh();
                }

                // Update available tags.
                if (this._components.tagFilterSelector &&
                    this._lastCompanyId !== owner.CompanyId) {

                    this._lastCompanyId = owner.CompanyId;

                    this._components.tagFilterSelector.dataSource.filter(
                        kendomodo.buildTagsDataSourceFilters(geoassistant.MoTypeKeys.File, owner.CompanyId)
                    );
                }
            };

            fn._onTagFilterChanged = function () {
                var tagFilter = this._componentModels.tags.current;
            };

            fn.getCurrentFolder = function () {
                return this._components.treeViewModel.select();
            };

            fn._getSelectionManager = function () {
                return this._components.filesGridViewModel.selectionManager;
            };

            fn.getFoldersViewModel = function () {
                return this._components.treeViewModel;
            };

            fn.getFilesViewModel = function () {
                return this._components.filesViewModel;
            };

            fn.saveExistingFilesToFolder = function (folderId, fileInfos) {
                var self = this;

                // Confirm and save.
                var ownerName = this.getCurrentOwner().Name;
                var path = this.getFoldersViewModel().getFolderPath(folderId);

                kendomodo.ui.openConfirmationDialog(
                    "Ziel: " + ownerName + " \"" + path + "\"\n\n" +
                    "Wollen Sie die Dateien wirklich in diesem Verzeichnis speichern?",
                    //"Verzeichnis: \"" + path + "\"\n" +
                    //"Besitzer: \"" + ownerName + "\"",
                    {
                        title: "Dateien speichern",
                        kind: "warning"
                    })
                    .then(function (result) {

                        if (result !== true)
                            return;

                        _executeServerMoOperation(self, {
                            operation: "SaveExistingFilesToFolder",
                            owner: self.owner,
                            folderId: folderId,
                            files: fileInfos,
                            errorMessage: "Fehler beim Speichern. Der Vorgang wurde abgebrochen. Es wurde nichts gespeichert.",
                            success: function (result) {
                                self._components.treeViewModel.refresh();
                                kendomodo.ui.openInfoDialog("Die Dateien wurden im Verzeichnis \"" + path + "\" gespeichert.");
                            }
                        });
                    });
            };

            fn._initComponent = function (options) {
                if (this._isComponentInitialized)
                    return;
                this._isComponentInitialized = true;

                var self = this;

                // Folder tree view for Mo folders to select from.
                this._components.treeViewModel = new kendomodo.ui.MoFolderTreeViewModel({
                    $area: options.$area,
                    isRecycleBinEnabled: !!options.isRecycleBinEnabled, // NOTE: Recycle bin won't be fetched
                    isExpandAllOnLoadedEnabled: true,
                    isFileSystemTemplateEnabled: options.isFileSystemTemplateEnabled
                });

                // Grid view for Mo files to select from.
                this._components.filesViewModel = this._createMoFileCollectionViewModel(options);

                this._components.treeViewModel.setFileCollectionViewModel(this._components.filesViewModel);

                // Combobox for selection of Mo file system owner (i.e. either ProjectSeries, ProjectSegment or Contract).
                // NOTE: This has to be initialized after the source files view models have been created
                // because is will immediately call "setOwner" on those view models.
                this._componentModels.owners = kendo.observable({
                    current: null,
                    items: this._options.owners,
                    changed: function (e) {
                        //var item = e.sender.dataItem();
                        self._onCurrentOwnerChanged();
                    }
                });
                kendo.bind(options.$area.find("input.mo-file-system-owner-selector"), this._componentModels.owners);

                var $selector = options.$area.find("input.mo-file-system-tag-filter-selector");
                if ($selector.length) {
                    this._components.tagFilterSelector = kendomodo.ui.createMoTagFilterSelector(
                        $selector,
                        {
                            changed: function (tagId) {
                                self._components.treeViewModel.setTagFilter(tagId);
                                self._components.treeViewModel.refresh();
                            },
                            filters: kendomodo.buildTagsDataSourceFilters(geoassistant.MoTypeKeys.File)
                        }
                    );
                }
            };

            fn._createMoFileCollectionViewModel = function (options) {
                // Grid view model for source Mo files to select from.
                var self = this;

                var filesGridViewModel = self._components.filesGridViewModel = this._createFilesGridViewModel(options);

                var files = new kendomodo.ui.MoFileCollectionViewModel({
                    $area: options.$area,
                    moFolderTreeViewModel: this._components.treeViewModel,
                    // Assign generated grid view model for the kendo grid
                    // (its code is in the generated JS file "mo.forFiles.list.vm.generated.js").
                    filesGridViewModel: filesGridViewModel
                });

                return files;
            };

            fn._createFilesGridViewModel = function () {

                var filesOptions = {};
                filesOptions.$component = this._options.$area.find("div.mo-file-list");
                if (this._options.areFileSelectorsVisible)
                    filesOptions.selectionMode = "multiple";

                // This is the generated grid view model for the kendo grid.
                var vm = casimodo.ui.componentRegistry.getById("4f846a0e-fe28-451a-a5bf-7a2bcb9f3712").vmOnly(filesOptions);
                vm.initComponentOptions();

                if (!this._options.areFileSelectorsVisible) {
                    vm.componentOptions.selectable = {
                        mode: "row"
                    };
                }

                vm.createComponent();

                if (this._options.areFileSelectorsVisible)
                    vm.selectionManager.showSelectors();

                return vm;
            };

            return MoFileExplorerViewModel;
        })();

        ui.MoFileCollectionViewModel = (function () {
            var MoFileCollectionViewModel = function (options) {
                var self = this;

                this.$area = options.$area;
                this.moFolderTreeViewModel = options.moFolderTreeViewModel;
                this.filesGridViewModel = options.filesGridViewModel;

                this.isAddEnabled = true;
                this.isDeleteEnabled = true;
                this.isRenameEnabled = true;
                this.owner = null;
                this._bindCoreEvents();
                this._events = new casimodo.EventManager(this);

                this._isComponentInitialized = false;
                this._components = {};
                this._initComponent();
            };

            var fn = MoFileCollectionViewModel.prototype;

            fn._bindCoreEvents = function () {
                var self = this;
                this.filesGridViewModel.on("dataBound", function (e) { self.trigger("dataBound", e); });
                this.filesGridViewModel.selectionManager.on("selectionChanged", function (e) { self.trigger("selectionChanged", e); });
                this.filesGridViewModel.selectionManager.on("selectionItemAdded", function (e) { self.trigger("selectionItemAdded", e); });
                this.filesGridViewModel.selectionManager.on("selectionItemRemoved", function (e) { self.trigger("selectionItemRemoved", e); });
            };

            fn.setTagFilter = function (tagId) {
                this._filterTagId = tagId;
            };

            fn.getDataSource = function () {
                return this.getKendoGrid().dataSource;
            };

            fn.getKendoGrid = function () {
                return this.filesGridViewModel.component;
            };

            fn.setOwner = function (owner) {
                this.owner = owner;
            };

            fn.selectFolder = function () {
                return this.moFolderTreeViewModel.select();
            };

            fn.select = function (e) {
                return this.getKendoGrid().dataItem($(e.target).closest("tr"));
            };

            fn.clearSelection = function () {
                this.filesGridViewModel.selectionManager.clearSelection();
            };

            // Show the checkbox column.
            fn.showSelectors = function () {
                this.filesGridViewModel.selectionManager.showSelectors();
            };

            fn.hideSelectors = function () {
                this.filesGridViewModel.hideSelectors();
            };

            fn.applyFilter = function (filter) {
                this.filesGridViewModel.applyFilter(filter);
            };

            fn.clear = function () {
                this.filesGridViewModel.clear();
            };

            // Reloads data.
            fn.refresh = function (selectId) {
                var self = this;

                return new Promise(function (resolve, reject) {

                    self.getDataSource().read()
                        .then(function () {

                            if (!selectId) return;

                            // Selected the file with the provided ID.

                            var item = self.getDataSource().get(selectId);
                            if (!item) return;

                            var component = self.getKendoGrid();
                            if (typeof component.selectable !== "undefined")
                                component.select(component.tbody.find("tr[data-uid='" + item.uid + "']"));

                            resolve();
                        })
                        .fail((ex) => reject(ex));
                });
            };

            fn._resetFileUpload = function () {
                if (!this._fileUploadViewModel)
                    return;

                this._fileUploadViewModel.reset();
            };

            fn._setFileUploadVisible = function (value) {
                if (!this._fileUploadViewModel)
                    return;

                this._fileUploadViewModel.visible(value);
            };

            fn._initComponent = function () {
                if (this._isComponentInitialized) return;
                this._isComponentInitialized = true;

                var self = this;

                // File context menu widget
                var $fileList = this.$area.find("div.mo-file-list");
                var $fileContextMenu = this.$area.find("ul.mo-file-context-menu");
                if ($fileList.length && $fileContextMenu.length) {
                    this._components.kendoContextMenu = $fileContextMenu.kendoContextMenu({
                        target: $fileList,
                        filter: "div.file-tile",
                        animation: kendomodo.getDefaultContextMenuAnimation(),
                        open: function (e) {

                            var file = self.select(e);
                            if (!file || file.IsDeleted) {
                                e.preventDefault();
                                return;
                            }

                            self.filesGridViewModel.gridSelectByItem(file);

                            var $menu = e.sender.wrapper;
                            var renameAction = $menu.find("li[data-name='RenameFile']");
                            var deleteAction = $menu.find("li[data-name='DeleteFile']");
                            var downloadAction = $menu.find("li[data-name='DownloadFile']");
                            var tagAction = $menu.find("li[data-name='EditFileTags']");

                            _enableContextMenuItem(renameAction, self.isRenameEnabled && !file.IsReadOnly);
                            // KABU TODO: Can't use IsDeletable, due to a bug. I.e. IsDeletable is always false.
                            _enableContextMenuItem(deleteAction, self.isDeleteEnabled && /* file.IsDeletable && */ !file.IsReadOnly);
                        },
                        select: function (e) {

                            var file = self.select(e);
                            if (!file) {
                                e.preventDefault();
                                return;
                            }

                            var name = $(e.item).data("name");

                            if (name === "DownloadFile") {
                                casimodo.downloadFile(file.File.fileDownloadUrl, file.File.FileName);
                            }
                            else if (name === "RenameFile") {

                                _openMoCoreEditor(self, {
                                    mode: "modify",
                                    itemId: file.Id,

                                    operation: "Rename",
                                    item: file,
                                    fileName: casimodo.removeFileNameExtension(file.File.FileName, file.File.FileExtension),
                                    // KABU TODO: LOCALIZE
                                    title: "Umbennen",
                                    errorMessage: "Die Datei konnte nicht umbenannt werden.",
                                    success: function (result) {
                                        self.refresh(result.Id);
                                    }
                                });
                            }
                            else if (name === "DeleteFile") {

                                // Confirm deletion and delete.
                                kendomodo.ui.openDeletionConfirmationDialog(
                                    "Wollen Sie die Datei '" + file.Name + "' wirklich löschen?")
                                    .then(function (result) {

                                        if (result !== true)
                                            return;

                                        _executeServerMoOperation(self, {
                                            operation: "MoveToRecycleBin",
                                            owner: self.owner,
                                            item: file,
                                            // KABU TODO: LOCALIZE
                                            title: "Löschen",
                                            errorMessage: "Die Datei konnte nicht in den Papierkorb verschoben werden.",
                                            success: function (result) {

                                                self.moFolderTreeViewModel.refresh();
                                            }
                                        });

                                    });
                            }
                            else if (name === "EditFileTags") {

                                // Open Mo file tags editor.
                                kendomodo.ui.openById("844ed81d-dbbb-4278-abf4-2947f11fa4d3",
                                    {
                                        filters: kendomodo.buildTagsDataSourceFilters(geoassistant.MoTypeKeys.File, self.owner.CompanyId),
                                        itemId: file.Id,
                                        title: "Datei-Markierungen bearbeiten",
                                        message: file.File.FileName,
                                        minWidth: 400,
                                        minHeight: 500,

                                        finished: function (result) {
                                            if (result.isOk) {
                                                self.moFolderTreeViewModel.refresh();
                                            }
                                        }
                                    });
                            }
                        }
                    }).data("kendoContextMenu");
                }

                // File upload
                if (this.$area.find("input.mo-file-upload").length) {
                    this._fileUploadViewModel = new kendomodo.ui.MoFileUploadViewModel({
                        $area: this.$area,
                        moFileCollectionViewModel: this
                    });
                }
            };

            // Event handling

            fn.on = function (eventName, func) {
                this._events.on(eventName, func);
            };

            fn.one = function (eventName, func) {
                this._events.one(eventName, func);
            };

            fn.trigger = function (eventName, e) {
                this._events.trigger(eventName, e);
            };

            function _enableContextMenuItem($item, enabled) {
                if (enabled)
                    $item.removeClass("k-hidden");
                else
                    $item.addClass("k-hidden");
            }

            return MoFileCollectionViewModel;
        })();

        ui.MoFileUploadViewModel = (function () {

            var MODE_DEFAULT = "default",
                MODE_EMAIL = "email-extraction";

            var MoFileUploadViewModel = function (options) {
                //var self = this;

                this.$area = options.$area;
                this.moFileCollectionViewModel = options.moFileCollectionViewModel;
                this._baseUrl = "api/UploadFileToFolder/";
                this._uploadMode = MODE_DEFAULT;
                this._scope = kendo.observable({
                    isEmailCreateFolderEnabled: false,
                    isEmailStorageEnabled: false,
                    isEmailBodyExtractionEnabled: false,
                    isEmailAttachmentExtractionEnabled: true
                });

                this._isRefreshingAfterUpload = false;
                this._isComponentInitialized = false;
                this._components = {};
                this._initComponent();
            };

            var fn = MoFileUploadViewModel.prototype;

            fn.reset = function () {
                if (this._isRefreshingAfterUpload)
                    // The folder tree or file list components will request a reset
                    //   when they are being refreshed immediately after files were uploaded.
                    //   We don't want to reset in this case. Thus skip.
                    return;

                var $elem = this._components.kendoUpload.wrapper;
                $elem.find(".k-upload-files").remove();
                $elem.find(".k-upload-status").remove();
                $elem.find(".k-upload.k-header").addClass("k-upload-empty");
                $elem.find(".k-upload-button").removeClass("k-state-focused");
                this.showResetButton(false);
            };

            fn.onSelecting = function (e) {
                this.reset();
                this.trySelectFolder(e);
            };

            fn.onUploading = function (e) {

                var folder = this.trySelectFolder(e);
                if (!folder) {
                    casimodo.ui.showError("Wählen Sie zuerst einen Ordner bevor Sie Dateien hochladen.");

                    // Cancel.
                    e.preventDefault();

                    return;
                }

                e.sender.options.async.saveUrl = this._baseUrl + folder.Id + "?mode=" + this._uploadMode;

                // Validate
                var files = e.files;
                var file;
                for (var i = 0; i < files.length; i++) {
                    file = files[i];

                    if (this._uploadMode === MODE_EMAIL) {
                        if (!file.extension || file.extension.toLowerCase() !== ".msg") {

                            casimodo.ui.showError("Die Datei wurde nicht hinzugefügt. " +
                                "In diesem Modus können nur 'MSG' (Outlook E-Mail) Dateien hinzugefügt werden.");

                            // Cancel.
                            e.preventDefault();

                            return;
                        }
                    }
                }

                if (this._uploadMode === MODE_EMAIL) {
                    e.sender.options.async.saveUrl +=
                        "&isEmailCreateFolderEnabled=" + this._scope.isEmailCreateFolderEnabled +
                        "&isEmailStorageEnabled=" + this._scope.isEmailStorageEnabled +
                        "&isEmailBodyExtractionEnabled=" + this._scope.isEmailBodyExtractionEnabled +
                        "&isEmailAttachmentExtractionEnabled=" + this._scope.isEmailAttachmentExtractionEnabled;
                }
            };

            fn.trySelectFolder = function (e) {
                var folder = this.moFileCollectionViewModel.selectFolder();
                if (!folder || !folder.Id) {
                    e.preventDefault();

                    casimodo.ui.showError("Bitte wählen Sie zuerst einen Order aus bevor Sie eine Datei hochladen.");

                    return null;
                }

                return folder;
            };

            fn.onCompleted = function (e) {
                var self = this;
                this.showResetButton(true);

                this._isRefreshingAfterUpload = true;
                try {
                    var refreshFunc = null;
                    // Refresh collection of files.
                    if (this.moFileCollectionViewModel.moFolderTreeViewModel)
                        refreshFunc = function () { return self.moFileCollectionViewModel.moFolderTreeViewModel.refresh(); };
                    else
                        refreshFunc = function () { return self.moFileCollectionViewModel.refresh(); };

                    refreshFunc()
                        .finally(function () {
                            self._isRefreshingAfterUpload = false;
                        });
                }
                catch (ex) {
                    this._isRefreshingAfterUpload = false;
                }
            };

            fn._onUploadModeChanged = function () {
                // TODO: Show options panel for mode "email-extraction".
                var $emailModeProps = this.$area.find("div.mo-file-upload-mode-email-prop");
                if (this._uploadMode === MODE_EMAIL)
                    $emailModeProps.show(100);
                else
                    $emailModeProps.hide(100);
            };

            fn.showResetButton = function (visible) {
                var self = this;
                var $btn = this.$area.find(".kmodo-upload-reset-command");

                if (visible && !$btn.length) {
                    // Add reset button
                    $btn = this._components.kendoUpload.wrapper
                        .find(".k-dropzone")
                        .append('<button type="button" class="k-button k-upload-action kmodo-upload-reset-command" aria-label="Bereinigen"><span class="k-icon k-i-close k-i-x" title="Bereinigen"></span></button>');

                    $btn.on("click", function () {
                        self.reset();
                    });
                }

                if (visible)
                    $btn.show(100);
                else
                    $btn.remove();
            }

            fn.visible = function (value) {
                this._components.kendoUpload.enable(value);

                var $elem = this._$uploadArea;
                if (value)
                    this._$uploadArea.show(100);
                else
                    this._$uploadArea.hide(100);
            };

            fn._initComponent = function () {
                if (this._isComponentInitialized) return;
                this._isComponentInitialized = true;

                var self = this;

                this._$uploadArea = this.$area.find(".mo-file-upload-area");
                var $upload = this._$uploadArea.find("input.mo-file-upload");

                if (!this.moFileCollectionViewModel.isAddEnabled) {
                    $upload.remove();
                    return;
                }

                this._components.kendoUpload = $upload.kendoUpload({
                    localization: {
                        select: "Wählen..."
                    },
                    multiple: true, // KABU TODO: IMPORTANT: Really multiple?
                    showFileList: true,
                    select: $.proxy(this.onSelecting, this),
                    upload: $.proxy(this.onUploading, this),
                    async: {
                        saveUrl: this._baseUrl,
                        autoUpload: true
                    },
                    success: function (e) {
                        // Hide the irritating warning (or info) icon on success.
                        //$upload.find(".k-icon.k-warning").hide();
                    },
                    error: function (e) {
                        var $status = e.sender.wrapper.find(".k-upload-status-total");
                        // Remove any messages added by Kendo from the DOM.
                        casimodo.jQuery.removeContentTextNodes($status);
                        $status.append("Fehler");
                        $status.css("color", "red");
                        casimodo.ui.showError("Fehler. Mindestens eine Datei konnte nicht gespeichert werden.")
                    },
                    complete: $.proxy(this.onCompleted, this)
                }).data("kendoUpload");

                this._components.modeSelector = this._$uploadArea
                    .find("input.mo-file-upload-mode-selector")
                    .kendoDropDownList({
                        height: 50,
                        dataValueField: "mode",
                        dataTextField: "displayName",
                        autoBind: true,
                        valuePrimitive: true,
                        dataSource: [{ mode: MODE_DEFAULT, displayName: "Standard" }, { mode: MODE_EMAIL, displayName: "E-Mail" }],
                        change: function (e) {
                            // The user selected an upload mode.
                            self._uploadMode = this.value();
                            self._onUploadModeChanged();
                        }
                    }).data("kendoDropDownList");
                this._components.modeSelector.select(0);

                kendo.bind(this._$uploadArea.find(".mo-file-upload-mode-panel"), this._scope);

                $upload.show();
            };

            return MoFileUploadViewModel;
        })();

        // Kendo treeview with tree lines: https://www.telerik.com/forums/treelines
        // jsbin: http://jsbin.com/ewiduv/1/edit?html,output

        ui.MoFolderTreeViewModel = (function () {
            var MoFolderTreeViewModel = function (options) {
                var self = this;

                this._options = options;
                // options.isFileSystemTemplateEnabled (default: false)

                this.$area = options.$area;
                this.moFileCollectionViewModel = options.moFileCollectionViewModel || null;
                this.isRecycleBinEnabled = typeof options.isRecycleBinEnabled !== "undefined" ? !!options.isRecycleBinEnabled : true;

                this.owner = null;
                this.expandedKeys = {};
                this.selectedKeys = {};
                this.schema = {
                    key: "Id",
                    children: "folders"
                };
                this.isInitialLoad = true;

                this.isAddEnabled = true;
                this.isDeleteEnabled = true;
                this.isRenameEnabled = true;

                this.isExpandAllOnLoadedEnabled = typeof options.isExpandAllOnLoadedEnabled !== "undefined"
                    ? options.isExpandAllOnLoadedEnabled
                    : false;

                this._isComponentInitialized = false;
                this._components = {};
                this._initComponent();
            };

            var fn = MoFolderTreeViewModel.prototype;

            fn.getItemById = function (id) {
                var items = this._items;
                for (var i = 0; i < items.length; i++)
                    if (items[i].Id === id)
                        return items[i];

                return null;
            };

            fn.getFolderPath = function (folderId) {
                var item = this._containerDict[folderId];
                if (!item)
                    return null;

                var path = null;

                do {
                    path = item.Name + (path ? "/" + path : "");
                    item = this._containerDict[item.ParentId];
                } while (item);

                return path;
            };

            fn.setFileCollectionViewModel = function (files) {
                this.moFileCollectionViewModel = files;
            };

            fn.setOwner = function (owner) {
                this.owner = owner;
                this.moFileCollectionViewModel.setOwner(owner);
            };

            fn.clear = function () {
                this.isInitialLoad = true;
                this.expandedKeys = {};
                this.selectedKeys = {};
                if (this._components.kendoTreeView) {
                    this._components.kendoTreeView.setDataSource(this.createDataSource([]));
                }
            };

            fn.createDataSource = function (items) {
                return casimodo.data.createMoTreeViewDataSource(items);
            };

            fn.select = function () {
                return this._components.kendoTreeView.dataItem(this._components.kendoTreeView.select());
            };

            fn.getByNode = function (node) {
                if (!node || !node.length)
                    return null;
                return this._components.kendoTreeView.dataItem(node);
            };

            fn.selectByNode = function (node) {
                return this._components.kendoTreeView.dataItem(node);
            };

            fn.refresh = function (selection) {
                return this.refreshFolders(selection);
            };

            fn.setTagFilter = function (tagId) {
                this._filterTagId = tagId;

                this.moFileCollectionViewModel.setTagFilter(tagId);
            };

            fn.refreshFolders = function (selection) {

                var self = this;

                return new Promise(function (resolve, reject) {

                    saveTreeViewState(self, self._components.kendoTreeView);

                    if (selection) {

                        var selectId = typeof selection === "string" ? selection : selection.Id;

                        if (selectId) {
                            self.selectedKeys = {};
                            self.selectedKeys[selectId] = true;
                        }
                    }

                    self._root = null;
                    self._items = null;

                    var query = "/odata/Mos/";
                    if (self._options.isRecycleBinEnabled)
                        query += "QueryWithRecycleBin()";
                    else
                        query += "Query()";

                    query += "?$select=Id,Name,ParentId,TypeId,IsContainer,Role,ModifiedOn,CreatedOn,IsDeleted,IsDeletable,IsReadOnly";
                    query += "&$expand=Permissions($select=RoleId)";
                    //query += "&$orderby=Name&";
                    query += "&$filter=OwnerId eq " + self.owner.Id;

                    if (self._filterTagId) {
                        query += " and (IsContainer eq true or Tags/any(tag: tag/Id eq " + self._filterTagId + "))";
                    }

                    casimodo.oDataQuery(query)
                        .then(function (items) {

                            self._items = items;

                            // Add internal sort property in order to have
                            // the management and recycle bin displayed at last positions.
                            // KABU TODO: Add a dedicated sort property to the Mo class.
                            items.forEach(function (x) {
                                if (x.Role === "RecycleBin")
                                    x._sortValue = 2;
                                else if (x.Role === "ManagementDocRoot")
                                    x._sortValue = 1;
                                else
                                    x._sortValue = 0;
                            });

                            items = _buildMoTreeViewHierarchy(self, items);
                            items = _restoreTreeViewState(self, items, self.isInitialLoad);

                            self._components.kendoTreeView.setDataSource(self.createDataSource(items));

                            // Select node on right mouse click.
                            self._components.kendoTreeView.wrapper.on('mousedown', '.k-item', function (event) {
                                if (event.which === 3) {
                                    event.stopPropagation();
                                    self._components.kendoTreeView.select(this);
                                }
                            });

                            // Ensure selection.
                            if (!self.select()) {
                                // Select first node.
                                var nodes = self._components.kendoTreeView.dataSource.data();
                                if (nodes.length)
                                    self._components.kendoTreeView.select(self._components.kendoTreeView.findByUid(nodes[0].uid));
                            }

                            if (self.isInitialLoad) {
                                self.isInitialLoad = false;
                                if (self.isExpandAllOnLoadedEnabled) {
                                    self._components.kendoTreeView.expand(".k-item");
                                }
                            }

                            self.changed();

                            resolve();
                        })
                        .catch((ex) => reject(ex));
                });
            };

            fn.changed = function () {
                this.onSelectionChanged();
            };

            fn.onSelectionChanged = function (e) {

                // Show items of container.
                if (!this.moFileCollectionViewModel) return;

                this.moFileCollectionViewModel._resetFileUpload();

                var folder = e ? this.selectByNode(e.node) : this.select();
                if (!folder) {
                    // Hide upload component if no container is selected.
                    this.moFileCollectionViewModel.clear();
                    this.moFileCollectionViewModel._setFileUploadVisible(false);

                    return;
                }

                var isInRecycleBin = this.isInRecycleBin(folder);

                // Hide upload-component if in recycle-bin.
                this.moFileCollectionViewModel._setFileUploadVisible(!isInRecycleBin);

                var filters = [
                    { field: "ParentId", operator: "eq", value: folder.Id },
                    { field: "IsContainer", operator: "eq", value: false }
                ];

                if (this._filterTagId)
                    filters.push({ customExpression: "Tags/any(tag: tag/Id eq " + this._filterTagId + ")" });

                this.moFileCollectionViewModel.applyFilter(filters);
            };

            fn.isRoot = function (folder) {
                return folder.Role === "LocalDocRoot";
            };

            fn.isInRecycleBin = function (folder) {
                return folder.IsDeleted || folder.Role === "RecycleBin";
            };

            fn._initComponent = function () {
                if (this._isComponentInitialized) return;
                this._isComponentInitialized = true;

                var self = this;

                // Refresh-all button.    
                this.$area.find(".mo-file-system-refresh-all-command").on("click", function () {
                    self.refresh();
                });

                // Folder treeview
                var $tree = this.$area.find("div.mo-folder-tree");

                this._components.kendoTreeView = $tree.kendoTreeView({
                    template: kendomodo.ui.templates.get("MoTreeView"),
                    select: $.proxy(self.onSelectionChanged, self),
                    dataTextField: "Name",
                    dataSource: self.createDataSource([]),
                    dataBound: function (e) {
                        if (self.isExpandAllOnLoadedEnabled && e.node)
                            this.expand(e.node.find(".k-item"));
                    }
                }).data("kendoTreeView");

                // Folder context menu.
                var $folderContextMenu = this.$area.find("ul.mo-folder-context-menu");

                if ($folderContextMenu.length) {
                    this._components.kendoContextMenu = $folderContextMenu.kendoContextMenu({
                        target: $tree,
                        filter: ".k-in",
                        animation: kendomodo.getDefaultContextMenuAnimation(),
                        open: function (e) {

                            var folder = self.select();
                            if (!folder || self.isInRecycleBin(folder)) {
                                e.preventDefault();
                                return;
                            }

                            var isroot = self.isRoot(folder);

                            var $menu = e.sender.wrapper;
                            var $addChildAction = $menu.find("li[data-name='AddChildFolder']");
                            var $renameAction = $menu.find("li[data-name='RenameFolder']");
                            var $deleteAction = $menu.find("li[data-name='MoveFolderToRecycleBin']");
                            var $selectFileSystemTemplateAction = $menu.find("li[data-name='SelectFileSystemTemplate']");

                            _enableContextMenuItem($addChildAction, self.isAddEnabled);
                            _enableContextMenuItem($renameAction, !isroot && self.isRenameEnabled && !folder.IsReadOnly);
                            // KABU TODO: Can't use IsDeletable, due to a bug. I.e. IsDeletable is always false.
                            _enableContextMenuItem($deleteAction, !isroot /* && folder.IsDeletable */ && self.isDeleteEnabled && !folder.IsReadOnly);
                            _enableContextMenuItem($selectFileSystemTemplateAction, self._options.isFileSystemTemplateEnabled && isroot);
                        },
                        select: function (e) {

                            var folder = self.select();
                            if (!folder) {
                                // KABU TODO: Can this really happen or is this superfluous?
                                e.preventDefault();
                                return;
                            }

                            var name = $(e.item).data("name");

                            if (name === "RenameFolder") {

                                _openMoCoreEditor(self, {
                                    mode: "modify",
                                    itemId: folder.Id,

                                    operation: "Rename",
                                    item: folder,
                                    // KABU TODO: LOCALIZE
                                    title: "Umbennen",
                                    errorMessage: "Der Ordner konnte nicht umbenannt werden.",
                                    success: function (result) {
                                        self.refresh(result.Id);
                                    }
                                });
                            }
                            else if (name === "AddChildFolder") {

                                _openMoCoreEditor(self, {
                                    mode: "create",
                                    itemId: null,

                                    operation: "AddPlainFolder",
                                    owner: self.owner,
                                    parentId: folder.Id,
                                    // KABU TODO: LOCALIZE
                                    title: "Neuer Ordner",
                                    errorMessage: "Der Ordner konnte nicht erstellt werden.",
                                    success: function (result) {
                                        self.refresh(result.Id);
                                    }
                                });
                            }
                            else if (name === "MoveFolderToRecycleBin") {

                                var node = self._components.kendoTreeView.select();

                                // Confirm deletion and delete.
                                kendomodo.ui.openDeletionConfirmationDialog(
                                    "Wollen Sie den Ordner '" + folder.Name + "' und dessen Inhalt wirklich löschen?")
                                    .then(function (result) {

                                        if (result !== true)
                                            return;

                                        _executeServerMoOperation(self, {
                                            operation: "MoveToRecycleBin",
                                            owner: self.owner,
                                            item: folder,
                                            errorMessage: "Der Ordner konnte nicht in den Papierkorb verschoben werden.",
                                            success: function (result) {

                                                var prev = self.getByNode($(node).prev());
                                                var next = self.getByNode($(node).next());
                                                var parent = self.getByNode(self._components.kendoTreeView.parent(node));

                                                self.refresh(prev || next || parent);
                                            }
                                        });
                                    });
                            }
                            else if (name === "SelectFileSystemTemplate") {
                                // Open Tag selector. Show only tags assignable to file-Mos.

                                kendomodo.ui.openById("8f25bf5c-c20f-4337-8072-1e397cdcf976", {
                                    title: "Dateisystem-Vorlage wählen",
                                    filters: [{ field: "ApplicableToOwnerId", operator: "eq", value: self.owner.TypeId }],
                                    width: 600,
                                    minHeight: 500,
                                    finished: function (result) {
                                        if (result.isOk) {
                                            _executeServerMoOperation(self, {
                                                operation: "ApplyFileSystemTemplate",
                                                owner: self.owner,
                                                item: folder,
                                                templateId: result.value,
                                                errorMessage: "Das Dateisystem konnte nicht erstellt werden.",
                                                success: function (result) {
                                                    self.refresh();
                                                }
                                            });
                                        }
                                    }
                                });
                            }
                        }
                    }).data("kendoContextMenu");
                }
            };

            // Private functions

            function _enableContextMenuItem($item, enabled) {
                if (!$item || !$item.length)
                    return;

                if (enabled)
                    $item.removeClass("k-hidden");
                else
                    $item.addClass("k-hidden");
            }

            function saveTreeViewState(vm, treeview) {

                vm.expandedKeys = Object.create(null);
                vm.selectedKeys = Object.create(null);

                treeview.wrapper.find(".k-item").each(function () {
                    var item = treeview.dataItem(this);

                    if (item.expanded)
                        vm.expandedKeys[item[vm.schema.key]] = true;

                    if (item.selected)
                        vm.selectedKeys[item[vm.schema.key]] = true;
                });
            }

            function _buildMoTreeViewHierarchy(self, mos) {

                var containers = [];
                var roots = [];
                var mo, parent;
                var i;
                self._root = null;
                self._containerDict = {};

                for (i = 0; i < mos.length; i++) {
                    mo = mos[i];

                    if (!mo.ParentId) {
                        mo._parent = null;
                        // KABU TODO: IMPORTANT: Rename all root Mos from "Dokumente" to "Dateien" in DB.
                        // KABU TODO: TEMP-HACK: Rename locally.
                        if (mo.Name === "Dokumente")
                            mo.Name = "Dateien";

                        self._root = mo;

                        roots.push(mo);
                    }

                    if (!mo.IsContainer) continue;

                    self._containerDict[mo.Id] = mo;
                    containers.push(mo);
                    mo.folders = [];
                    //mo.hasChildren = false;
                    mo.files = [];
                }

                for (i = 0; i < mos.length; i++) {
                    mo = mos[i];

                    if (!mo.ParentId) continue;

                    parent = self._containerDict[mo.ParentId];

                    if (typeof parent === "undefined") {
                        alert("Parent folder not found (ParentId: " + mo.ParentId + ")");
                    }

                    if (mo.IsContainer) {
                        parent.folders.push(mo);
                        //parent.hasChildren = true;
                    }
                    else {
                        parent.files.push(mo);
                    }
                }

                // Sort folders.
                containers.forEach(function (parent) {
                    parent.folders.sort(function (x, y) {
                        if (x._sortValue > y._sortValue) return 1;
                        if (x._sortValue < y._sortValue) return -1;

                        // Default sort: by name.
                        return x.Name.localeCompare(y.Name);
                    });
                });

                return roots;
            }

            function _restoreTreeViewState(vm, items, expandAll) {

                var context = {
                    schema: vm.schema,
                    expandedKeys: vm.expandedKeys || {},
                    expandedCount: vm.expandedKeys ? Object.keys(vm.expandedKeys).length : 0,
                    selectedKeys: vm.selectedKeys || {},
                    selectedCount: vm.selectedKeys ? Object.keys(vm.selectedKeys).length : 0
                };

                if (context.expandedCount || context.selectedCount)
                    _restoreTreeViewStateCore(context, items, expandAll);

                return items;
            }

            function _restoreTreeViewStateCore(context, items, expandAll) {
                if (!items || !items.length) return false;

                var isPathSelected = false;
                var item, children;
                for (var i = 0; i < items.length; i++) {

                    item = items[i];

                    if (context.expandedCount && context.expandedKeys[item[context.schema.key]]) {
                        item.expanded = true;
                        context.expandedCount--;
                    }
                    else if (expandAll) {
                        if (item.Role !== "RecycleBin")
                            item.expanded = true;
                    }

                    if (context.selectedCount && context.selectedKeys[item[context.schema.key]]) {
                        item.selected = true;
                        isPathSelected = true;
                        context.selectedCount--;
                    }

                    if (!expandAll && !context.expandedCount && !context.selectedCount)
                        return isPathSelected;

                    children = item[context.schema.children];
                    if (children && children.length) {
                        if (_restoreTreeViewStateCore(context, children, expandAll) && !item.expanded) {
                            item.expanded = true;
                            isPathSelected = true;
                        }
                    }
                }

                return isPathSelected;
            }

            return MoFolderTreeViewModel;
        })();

        function _openMoCoreEditor(self, context) {

            // Open Mo core editor dialog.
            kendomodo.ui.openById("9f6650df-db11-44fb-818b-9282a2d3ea64",
                {
                    mode: context.mode,
                    itemId: context.itemId,

                    title: context.title,
                    maximize: false,
                    scrollable: false,
                    minWidth: 600,
                    minHeight: 100,

                    finished: function (result) {
                        if (result.isOk) {
                            _executeServerMoOperation(self, context, {
                                item: result.item
                            });
                        }
                    }
                });
        }

        function _executeServerMoOperation(self, context, edit) {

            var oDataMethod = context.operation;
            var params = [];

            if (context.operation === "Rename") {
                params = [
                    { name: "id", value: edit.item.Id },
                    { name: "name", value: edit.item.Name }];
            }
            else if (context.operation === "AddPlainFolder") {
                params = [
                    { name: "ownerId", value: context.owner.Id },
                    { name: "parentId", value: context.parentId },
                    { name: "name", value: edit.item.Name }
                ];
            }
            else if (context.operation === "MoveToRecycleBin") {
                params = [
                    { name: "recycleBinOwnerId", value: context.owner.Id },
                    { name: "id", value: context.item.Id }
                ];
            }
            else if (context.operation === "AddTag") {
                params = [
                    { name: "id", value: context.item.Id },
                    { name: "tagId", value: context.tagId }
                ];
            }
            else if (context.operation === "ApplyFileSystemTemplate") {
                params = [
                    { name: "ownerId", value: context.owner.Id },
                    { name: "id", value: context.item.Id },
                    { name: "templateId", value: context.templateId }
                ];
            }
            else if (context.operation === "SaveExistingFilesToFolder") {
                params = [
                    { name: "folderId", value: context.folderId },
                    { name: "files", value: context.files }
                ];
            }
            else throw new MoFileSystemException("Unexpected MoFileSystem operation '" + context.operation + "'.");

            var errorMessage = context.errorMessage || "Fehler. Der Vorgang wurde abgebrochen.";

            kendomodo.ui.progress(true);

            return casimodo.oDataAction("/odata/Mos", oDataMethod, params)
                .then(function (result) {
                    // KABU TODO: REMOVE
                    //if (!result || !result.Id)
                    //    kendomodo.ui.showErrorDialog(errorMessage);
                    //else
                    context.success(result);
                })
                .catch(function (ex) {
                    kendomodo.ui.openErrorDialog(errorMessage);
                })
                .finally(function () {
                    kendomodo.ui.progress(false);
                });
        }

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));