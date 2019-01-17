
// KABU TODO: Eliminate references to hard-coded geoassistant.MoTypeKeys.Manager/File GUID.
namespace kmodo {

    class MoFileSystemException extends Error {
        constructor(message) {
            super(message);
            // KABU TODO: Eval if we need to set the name.
            //this.name = "MoFileSystemException";
        }
    }

    export interface MoFolderTreeOptions {
        $area: JQuery;
        isFileSystemTemplateEnabled: boolean;
        isRecycleBinEnabled?: boolean;
        isExpandAllOnLoadedEnabled?: boolean;
        moFileCollectionViewModel?: MoFileCollectionViewModel;
    }

    interface InteralMoFolderTreeOptions {
        $area: JQuery;
        isFileSystemTemplateEnabled: boolean;
        isRecycleBinEnabled: boolean;
        isExpandAllOnLoadedEnabled: boolean;
        moFileCollectionViewModel?: MoFileCollectionViewModel;
    }

    interface MoFolderTreeDataSourceSchema {
        key: string;
        children: string;
    }

    export interface MoFolderTreeEntity extends kendo.data.Node {
        Id: string;
        Name: string;
        ParentId: string;
        IsDeleted: boolean;
        Role: string;
        IsReadOnly: boolean;
        selected?: boolean;
        expanded?: boolean;
    }

    export interface FileWithTagsInfo {
        FileId: string;
        FileName: string;
        TagIds: string[];
    }

    export interface MoFileEntity extends kendo.data.ObservableObject {
        Id: string;
        Name: string;
        ParentId: string;
        IsDeleted: boolean;
        Role: string;
        IsReadOnly: boolean;
        File: {
            FileName: string;
            FileExtension: string;
            fileDownloadUrl: string;
        }
    }

    class MoFolderTreeViewModel {
        private options: InteralMoFolderTreeOptions;
        private owner: MoFileTreeOwner;
        private expandedKeys: Object;
        private selectedKeys: Object;
        private schema: MoFolderTreeDataSourceSchema;
        private isInitialLoad: boolean;
        private isAddEnabled: boolean;
        private isDeleteEnabled: boolean;
        private isRenameEnabled: boolean;
        private _isComponentInitialized: boolean;
        private _folderKendoTreeView: kendo.ui.TreeView;
        private moFileCollectionViewModel: MoFileCollectionViewModel;
        //private _kendoContextMenu: kendo.ui.ContextMenu;
        private _items: MoFolderTreeEntity[];
        private _containerDict: Object = {};
        private _filterTagIds: string[];
        //private _root: MoFolderTreeEntity;

        constructor(options: MoFolderTreeOptions) {

            // options.isFileSystemTemplateEnabled (default: false)

            this._setOptions(options);

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

            this._isComponentInitialized = false;
            this._initComponent();
        }

        private _setOptions(options: MoFolderTreeOptions): void {
            this.options = {
                $area: options.$area,
                isExpandAllOnLoadedEnabled: typeof options.isExpandAllOnLoadedEnabled !== "undefined" ? options.isExpandAllOnLoadedEnabled : false,
                isRecycleBinEnabled: typeof options.isRecycleBinEnabled !== "undefined" ? options.isRecycleBinEnabled : true,
                isFileSystemTemplateEnabled: typeof options.isFileSystemTemplateEnabled !== "undefined" ? options.isFileSystemTemplateEnabled : false
            };
        }

        getItemById(id: string): any {
            return this._items.find(x => x.Id === id);
        }

        getFolderPath(folderId: string): string {
            var item = this._containerDict[folderId] as MoFolderTreeEntity;
            if (!item)
                return null;

            var path: string = null;

            do {
                path = item.Name + (path ? "/" + path : "");
                item = this._containerDict[item.ParentId];
            } while (item);

            return path;
        }

        setFileCollectionViewModel(files: MoFileCollectionViewModel): void {
            this.moFileCollectionViewModel = files;
        }

        setOwner(owner: any): void {
            this.owner = owner;
            this.moFileCollectionViewModel.setOwner(owner);
        }

        clear(): void {
            this.isInitialLoad = true;
            this.expandedKeys = {};
            this.selectedKeys = {};
            if (this._folderKendoTreeView) {
                this._folderKendoTreeView.setDataSource(this.createDataSource([]));
            }
        }

        createDataSource(items: MoFolderTreeEntity[]): kendo.data.HierarchicalDataSource {
            return createMoTreeViewDataSource(items);
        }

        select(): MoFolderTreeEntity {
            return this._folderKendoTreeView.dataItem(this._folderKendoTreeView.select()) as MoFolderTreeEntity;
        }

        getByNode($node: JQuery): Object {
            if (!$node || !$node.length)
                return null;
            return this._folderKendoTreeView.dataItem($node);
        }

        selectByNode($node: JQuery): Object {
            return this._folderKendoTreeView.dataItem($node);
        }

        refresh(selection?: Object): Promise<void> {
            return this.refreshFolders(selection);
        }

        setTagsFilter(tagIds: string[]): void {
            this._filterTagIds = tagIds;

            // TODO: REMOVE: this.moFileCollectionViewModel.setTagsFilter(tagIds);
        }

        refreshFolders(selection): Promise<void> {

            var self = this;

            return new Promise(function (resolve, reject) {

                _saveTreeViewState(self, self._folderKendoTreeView);

                if (selection) {

                    var selectId = typeof selection === "string" ? selection : selection.Id;

                    if (selectId) {
                        self.selectedKeys = {};
                        self.selectedKeys[selectId] = true;
                    }
                }

                //self._root = null;
                self._items = null;

                var query = "/odata/Mos/";
                if (self.options.isRecycleBinEnabled)
                    query += "QueryWithRecycleBin()";
                else
                    query += "Query()";

                query += "?$select=Id,Name,ParentId,TypeId,IsContainer,Role,ModifiedOn,CreatedOn,IsDeleted,IsDeletable,IsReadOnly";
                query += "&$expand=Permissions($select=RoleId)";
                //query += "&$orderby=Name&";
                query += "&$filter=OwnerId eq " + self.owner.Id;

                if (self._filterTagIds && self._filterTagIds.length) {
                    var tagsExpression = self._filterTagIds.map(x => "Tags/any(tag: tag/Id eq " + x + ")").join(" and ");
                    query += " and (IsContainer eq true or (" + tagsExpression + "))";
                }

                cmodo.oDataQuery(query)
                    .then(function (rawItems) {

                        self._items = rawItems as MoFolderTreeEntity[];

                        // Add internal sort property in order to have
                        // the management and recycle bin displayed at last positions.
                        // KABU TODO: Add a dedicated sort property to the Mo class.
                        for (let x of self._items as any[]) {
                            if (x.Role === "RecycleBin")
                                x._sortValue = 2;
                            else if (x.Role === "ManagementDocRoot")
                                x._sortValue = 1;
                            else
                                x._sortValue = 0;
                        }

                        self._items = self._buildMoTreeViewHierarchy(self._items);
                        self._items = self._restoreTreeViewState(self._items, self.isInitialLoad);

                        self._folderKendoTreeView.setDataSource(self.createDataSource(self._items));

                        // Select node on right mouse click.
                        self._folderKendoTreeView.wrapper.on('mousedown', '.k-item', (e) => {
                            if (e.which === 3) {
                                e.stopPropagation();
                                self._folderKendoTreeView.select(e.currentTarget);
                            }
                        });

                        // Ensure selection.
                        if (!self.select()) {
                            // Select first node.
                            var nodes = self._folderKendoTreeView.dataSource.data();
                            if (nodes.length)
                                self._folderKendoTreeView.select(self._folderKendoTreeView.findByUid(nodes[0].uid));
                        }

                        if (self.isInitialLoad) {
                            self.isInitialLoad = false;
                            if (self.options.isExpandAllOnLoadedEnabled) {
                                self._folderKendoTreeView.expand(".k-item");
                            }
                        }

                        self.changed();

                        resolve();
                    })
                    .catch((ex) => reject(ex));
            });
        }

        changed(): void {
            this.onSelectionChanged();
        }

        onSelectionChanged(e?: any): void {

            // Show items of container.
            if (!this.moFileCollectionViewModel) return;

            this.moFileCollectionViewModel._resetFileUpload();

            var folder = (e ? this.selectByNode(e.node) : this.select()) as MoFolderTreeEntity;
            if (!folder) {
                // Hide upload component if no container is selected.
                this.moFileCollectionViewModel.clear();
                this.moFileCollectionViewModel._setFileUploadVisible(false);

                return;
            }

            var isInRecycleBin = this.isInRecycleBin(folder);

            // Hide upload-component if in recycle-bin.
            this.moFileCollectionViewModel._setFileUploadVisible(!isInRecycleBin);

            var filters: kendo.data.DataSourceFilterItem[] = [
                { field: "ParentId", operator: "eq", value: folder.Id },
                { field: "IsContainer", operator: "eq", value: false }
            ];

            if (this._filterTagIds && this._filterTagIds.length) {
                var tagsExpression = this._filterTagIds.map(x => "Tags/any(tag: tag/Id eq " + x + ")").join(" and ");
                (filters as any[]).push({ customExpression: tagsExpression });
            }

            this.moFileCollectionViewModel.applyFilter(filters);
        }

        isRoot(folder: MoFolderTreeEntity): boolean {
            return folder.Role === "LocalDocRoot";
        }

        isInRecycleBin(folder: MoFolderTreeEntity): boolean {
            return folder.IsDeleted || folder.Role === "RecycleBin";
        }

        private _initComponent(): void {
            if (this._isComponentInitialized) return;
            this._isComponentInitialized = true;

            var self = this;

            // Refresh-all button.    
            this.options.$area.find(".mo-file-system-refresh-all-command").on("click", function () {
                self.refresh();
            });

            // Folder treeview
            var $folderTree = this.options.$area.find("div.mo-folder-tree");

            this._folderKendoTreeView = $folderTree.kendoTreeView({
                template: cmodo.templates.get("MoTreeView"),
                select: $.proxy(self.onSelectionChanged, self),
                dataTextField: "Name",
                dataSource: self.createDataSource([]),
                dataBound: (e) => {
                    if (self.options.isExpandAllOnLoadedEnabled && e.node)
                        e.sender.expand(e.node.find(".k-item"));
                }
            }).data("kendoTreeView");

            // Folder context menu.
            var $folderContextMenu = this.options.$area.find("ul.mo-folder-context-menu");

            if ($folderContextMenu.length) {
                //this._kendoContextMenu =
                $folderContextMenu.kendoContextMenu({
                    target: $folderTree,
                    filter: ".k-in",
                    animation: kmodo.getDefaultContextMenuAnimation(),
                    open: function (e) {

                        var folder = self.select() as MoFolderTreeEntity;
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
                        _enableContextMenuItem($selectFileSystemTemplateAction, self.options.isFileSystemTemplateEnabled && isroot);
                    },
                    select: function (e) {

                        var folder = self.select() as MoFolderTreeEntity;
                        if (!folder) {
                            // KABU TODO: Can this really happen or is this superfluous?
                            e.preventDefault();
                            return;
                        }

                        var name = $(e.item).data("name") as string;

                        if (name === "RenameFolder") {

                            _openMoCoreEditor({
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

                            _openMoCoreEditor({
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

                            var node = self._folderKendoTreeView.select();

                            // Confirm deletion and delete.
                            kmodo.openDeletionConfirmationDialog(
                                "Wollen Sie den Ordner '" + folder.Name + "' und dessen Inhalt wirklich löschen?")
                                .then(function (result) {

                                    if (result !== true)
                                        return;

                                    _executeServerMoOperation({
                                        operation: "MoveToRecycleBin",
                                        owner: self.owner,
                                        item: folder,
                                        errorMessage: "Der Ordner konnte nicht in den Papierkorb verschoben werden.",
                                        success: function (result) {

                                            var prev = self.getByNode($(node).prev());
                                            var next = self.getByNode($(node).next());
                                            var parent = self.getByNode(self._folderKendoTreeView.parent(node));

                                            self.refresh(prev || next || parent);
                                        }
                                    });
                                });
                        }
                        else if (name === "SelectFileSystemTemplate") {
                            // Open Tag selector. Show only tags assignable to file-Mos.

                            kmodo.openById("8f25bf5c-c20f-4337-8072-1e397cdcf976", {
                                title: "Dateisystem-Vorlage wählen",
                                filters: [{ field: "ApplicableToOwnerId", operator: "eq", value: self.owner.TypeId }],
                                width: 600,
                                minHeight: 500,
                                finished: function (result) {
                                    if (result.isOk) {
                                        _executeServerMoOperation({
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
        }

        private _buildMoTreeViewHierarchy(mos: Object[]) {

            var containers = [];
            var roots = [];
            var mo, parent;
            //this._root = null;
            this._containerDict = {};

            for (let i = 0; i < mos.length; i++) {
                mo = mos[i];

                if (!mo.ParentId) {
                    mo._parent = null;
                    // KABU TODO: IMPORTANT: Rename all root Mos from "Dokumente" to "Dateien" in DB.
                    // KABU TODO: TEMP-HACK: Rename locally.
                    if (mo.Name === "Dokumente")
                        mo.Name = "Dateien";

                    //this._root = mo;

                    roots.push(mo);
                }

                if (!mo.IsContainer) continue;

                this._containerDict[mo.Id] = mo;
                containers.push(mo);
                mo.folders = [];
                //mo.hasChildren = false;
                mo.files = [];
            }

            for (let i = 0; i < mos.length; i++) {
                mo = mos[i];

                if (!mo.ParentId) continue;

                parent = this._containerDict[mo.ParentId];

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
            for (let parent of containers) {
                parent.folders.sort(function (x, y) {
                    if (x._sortValue > y._sortValue) return 1;
                    if (x._sortValue < y._sortValue) return -1;

                    // Default sort: by name.
                    return x.Name.localeCompare(y.Name);
                });
            }

            return roots;
        }

        private _restoreTreeViewState(items: any[], expandAll: boolean): any[] {

            var context = {
                schema: this.schema,
                expandedKeys: this.expandedKeys || {},
                expandedCount: this.expandedKeys ? Object.keys(this.expandedKeys).length : 0,
                selectedKeys: this.selectedKeys || {},
                selectedCount: this.selectedKeys ? Object.keys(this.selectedKeys).length : 0
            };

            if (context.expandedCount || context.selectedCount)
                this._restoreTreeViewStateCore(context, items, expandAll);

            return items;
        }

        private _restoreTreeViewStateCore(context: any, items: any[], expandAll: boolean): boolean {
            if (!items || !items.length)
                return false;

            var isPathSelected = false;
            var item, children;
            for (let i = 0; i < items.length; i++) {

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
                    if (this._restoreTreeViewStateCore(context, children, expandAll) && !item.expanded) {
                        item.expanded = true;
                        isPathSelected = true;
                    }
                }
            }

            return isPathSelected;
        }
    }

    const UPLOAD_MODE_DEFAULT = "default",
        UPLOAD_MODE_EMAIL = "email-extraction";

    // The exposed observable model.
    interface MoFileUploadModel extends kendo.data.ObservableObject {
        isEmailCreateFolderEnabled: boolean;
        isEmailStorageEnabled: boolean;
        isEmailBodyExtractionEnabled: boolean;
        isEmailAttachmentExtractionEnabled: boolean;
    }

    interface MoFileUploadOptions {
        $area: JQuery;
        moFileCollectionViewModel: MoFileCollectionViewModel;
    }

    class MoFileUploadComponent {
        private options: MoFileUploadOptions;
        //private $area: JQuery;
        //private moFileCollectionViewModel: MoFileCollectionViewModel;
        private _baseUrl: string;
        private _uploadMode: string;
        private _scope: MoFileUploadModel;
        private _isRefreshingAfterUpload: boolean;
        private _isComponentInitialized: boolean;
        private _kendoUpload: kendo.ui.Upload;
        private _modeSelector: kendo.ui.DropDownList;
        private _$uploadArea: JQuery;

        constructor(options: MoFileUploadOptions) {
            this.options = options;
            this._baseUrl = "api/FileUpload/UploadFileToFolder/";
            this._uploadMode = UPLOAD_MODE_DEFAULT;
            this._scope = kendo.observable({
                isEmailCreateFolderEnabled: false,
                isEmailStorageEnabled: false,
                isEmailBodyExtractionEnabled: false,
                isEmailAttachmentExtractionEnabled: true
            }) as MoFileUploadModel;

            this._isRefreshingAfterUpload = false;
            this._isComponentInitialized = false;
            this._initComponent();
        }

        reset(): void {
            if (this._isRefreshingAfterUpload)
                // The folder tree or file list components will request a reset
                //   when they are being refreshed immediately after files were uploaded.
                //   We don't want to reset in this case. Thus skip.
                return;

            var $elem = this._kendoUpload.wrapper;
            $elem.find(".k-upload-files").remove();
            $elem.find(".k-upload-status").remove();
            $elem.find(".k-upload.k-header").addClass("k-upload-empty");
            $elem.find(".k-upload-button").removeClass("k-state-focused");
            this.showResetButton(false);
        }

        onSelecting(e): void {
            this.reset();
            this.trySelectFolder(e);
        }

        onUploading(e): void {

            var folder = this.trySelectFolder(e);
            if (!folder) {
                cmodo.showError("Wählen Sie zuerst einen Ordner bevor Sie Dateien hochladen.");

                // Cancel.
                e.preventDefault();

                return;
            }

            e.sender.options.async.saveUrl = this._baseUrl + folder.Id + "?mode=" + this._uploadMode;

            // Validate
            var files = e.files;
            var file: any;
            for (let i = 0; i < files.length; i++) {
                file = files[i];

                if (this._uploadMode === UPLOAD_MODE_EMAIL) {
                    if (!file.extension || file.extension.toLowerCase() !== ".msg") {

                        cmodo.showError("Die Datei wurde nicht hinzugefügt. " +
                            "In diesem Modus können nur 'MSG' (Outlook E-Mail) Dateien hinzugefügt werden.");

                        // Cancel.
                        e.preventDefault();

                        return;
                    }
                }
            }

            if (this._uploadMode === UPLOAD_MODE_EMAIL) {
                e.sender.options.async.saveUrl +=
                    "&isEmailCreateFolderEnabled=" + this._scope.isEmailCreateFolderEnabled +
                    "&isEmailStorageEnabled=" + this._scope.isEmailStorageEnabled +
                    "&isEmailBodyExtractionEnabled=" + this._scope.isEmailBodyExtractionEnabled +
                    "&isEmailAttachmentExtractionEnabled=" + this._scope.isEmailAttachmentExtractionEnabled;
            }

            kmodo.useHeaderRequestVerificationToken(e.XMLHttpRequest);           
        }

        trySelectFolder(e): MoFolderTreeEntity {
            var folder = this.options.moFileCollectionViewModel.selectFolder();
            if (!folder || !folder.Id) {
                e.preventDefault();

                cmodo.showError("Bitte wählen Sie zuerst einen Order aus bevor Sie eine Datei hochladen.");

                return null;
            }

            return folder;
        }

        onCompleted(e): void {
            var self = this;
            this.showResetButton(true);

            this._isRefreshingAfterUpload = true;
            try {
                var refreshFunc = null;
                // Refresh collection of files.
                if (this.options.moFileCollectionViewModel._moFolderTreeViewModel)
                    refreshFunc = function () { return self.options.moFileCollectionViewModel._moFolderTreeViewModel.refresh(); };
                else
                    refreshFunc = function () { return self.options.moFileCollectionViewModel.refresh(); };

                refreshFunc()
                    .finally(function () {
                        self._isRefreshingAfterUpload = false;
                    });
            }
            catch (ex) {
                this._isRefreshingAfterUpload = false;
            }
        }

        _onUploadModeChanged(): void {
            // TODO: Show options panel for mode "email-extraction".
            var $emailModeProps = this.options.$area.find("div.mo-file-upload-mode-email-prop");
            if (this._uploadMode === UPLOAD_MODE_EMAIL)
                $emailModeProps.show(100);
            else
                $emailModeProps.hide(100);
        }

        showResetButton(visible: boolean): void {
            var self = this;
            var $btn = this.options.$area.find(".kmodo-upload-reset-command");

            if (visible && !$btn.length) {
                // Add reset button
                $btn = this._kendoUpload.wrapper
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

        visible(value: boolean): void {
            this._kendoUpload.enable(value);
            if (value)
                this._$uploadArea.show(100);
            else
                this._$uploadArea.hide(100);
        }

        private _initComponent(): void {
            if (this._isComponentInitialized) return;
            this._isComponentInitialized = true;

            var self = this;

            this._$uploadArea = this.options.$area.find(".mo-file-upload-area");
            var $upload = this._$uploadArea.find("input.mo-file-upload");

            if (!this.options.moFileCollectionViewModel.isAddEnabled) {
                $upload.remove();
                return;
            }

            this._kendoUpload = $upload.kendoUpload({
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
                    cmodo.jQueryRemoveContentTextNodes($status);
                    $status.append("Fehler");
                    $status.css("color", "red");
                    cmodo.showError("Fehler. Mindestens eine Datei konnte nicht gespeichert werden.")
                },
                complete: $.proxy(this.onCompleted, this)
            }).data("kendoUpload");

            this._modeSelector = this._$uploadArea
                .find("input.mo-file-upload-mode-selector")
                .kendoDropDownList({
                    height: 50,
                    dataValueField: "mode",
                    dataTextField: "displayName",
                    autoBind: true,
                    valuePrimitive: true,
                    dataSource: [{ mode: UPLOAD_MODE_DEFAULT, displayName: "Standard" }, { mode: UPLOAD_MODE_EMAIL, displayName: "E-Mail" }],
                    change: function (e) {
                        // The user selected an upload mode.
                        self._uploadMode = e.sender.value();
                        self._onUploadModeChanged();
                    }
                }).data("kendoDropDownList");
            this._modeSelector.select(0);

            kendo.bind(this._$uploadArea.find(".mo-file-upload-mode-panel"), this._scope);

            $upload.show();
        }
    }

    class MoFileCollectionViewModel extends cmodo.ComponentBase {
        private $area: JQuery;
        private filesGridViewModel: GridComponent;
        _moFolderTreeViewModel: MoFolderTreeViewModel;
        private _fileUploadViewModel: MoFileUploadComponent;
        //private _kendoContextMenu: kendo.ui.ContextMenu;
        private isUploadEnabled: boolean;
        isAddEnabled: boolean;
        private isDeleteEnabled: boolean;
        private isRenameEnabled: boolean;
        private owner: any;
        private _isComponentInitialized: boolean;
        //private _filterTagIds: string[];

        constructor(options) {
            super();

            this.$area = options.$area;
            this._moFolderTreeViewModel = options.moFolderTreeViewModel;
            this.filesGridViewModel = options.filesGridViewModel;
            this.isUploadEnabled = true;
            if (typeof options.isUploadEnabled !== "undefined")
                this.isUploadEnabled = options.isUploadEnabled;

            this.isAddEnabled = true;
            this.isDeleteEnabled = true;
            this.isRenameEnabled = true;
            this.owner = null;
            this._bindCoreEvents();

            this._isComponentInitialized = false;
            this._initComponent();
        }

        private _bindCoreEvents(): void {
            var self = this;
            this.filesGridViewModel.on("dataBound", function (e) { self.trigger("dataBound", e); });
            this.filesGridViewModel.selectionManager.on("selectionChanged", function (e) { self.trigger("selectionChanged", e); });
            this.filesGridViewModel.selectionManager.on("selectionItemAdded", function (e) { self.trigger("selectionItemAdded", e); });
            this.filesGridViewModel.selectionManager.on("selectionItemRemoved", function (e) { self.trigger("selectionItemRemoved", e); });
        }
        // TODO: REMOVE: 
        //setTagsFilter(tagIds: string[]): void {
        //    this._filterTagIds = tagIds;
        //}

        getDataSource(): kendo.data.DataSource {
            return this.getKendoGrid().dataSource;
        }

        getKendoGrid(): kendo.ui.Grid {
            return this.filesGridViewModel.component;
        }

        setOwner(owner: any): void {
            this.owner = owner;
        }

        selectFolder(): MoFolderTreeEntity {
            return this._moFolderTreeViewModel.select() as MoFolderTreeEntity;
        }

        select(e): MoFileEntity {
            return this.getKendoGrid().dataItem($(e.target).closest("tr")) as MoFileEntity;
        }

        clearSelection(): void {
            this.filesGridViewModel.selectionManager.clearSelection();
        }

        // Show the checkbox column.
        showSelectors(): void {
            this.filesGridViewModel.selectionManager.showSelectors();
        }

        hideSelectors(): void {
            this.filesGridViewModel.selectionManager.hideSelectors();
        }

        applyFilter(filter): void {
            this.filesGridViewModel.applyFilter(filter);
        }

        clear(): void {
            this.filesGridViewModel.clear();
        }

        // Reloads data.
        refresh(selectId?: string): Promise<void> {
            var self = this;

            return new Promise(function (resolve, reject) {

                self.getDataSource().read()
                    .then(function () {

                        if (!selectId) return;

                        // Selected the file with the provided ID.

                        var item = self.getDataSource().get(selectId);
                        if (!item) return;

                        var filesKendoGrid = self.getKendoGrid();
                        // KABU TODO: VERY IMPORTANT: Check if selectable exists on kendo grid.
                        if (typeof (filesKendoGrid as any).selectable !== "undefined")
                            filesKendoGrid.select(filesKendoGrid.tbody.find("tr[data-uid='" + item.uid + "']"));

                        resolve();
                    })
                    .fail((ex) => reject(ex));
            });
        }

        _resetFileUpload(): void {
            if (!this._fileUploadViewModel)
                return;

            this._fileUploadViewModel.reset();
        }

        _setFileUploadVisible(value: boolean): void {
            if (!this._fileUploadViewModel)
                return;

            this._fileUploadViewModel.visible(value);
        };

        private _initComponent(): void {
            if (this._isComponentInitialized) return;
            this._isComponentInitialized = true;

            var self = this;

            // File context menu widget
            var $fileList = this.$area.find("div.mo-file-list");
            var $fileContextMenu = this.$area.find("ul.mo-file-context-menu");
            if ($fileList.length && $fileContextMenu.length) {
                // this._kendoContextMenu =
                $fileContextMenu.kendoContextMenu({
                    target: $fileList,
                    filter: "div.file-tile",
                    animation: kmodo.getDefaultContextMenuAnimation(),
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
                        //var downloadAction = $menu.find("li[data-name='DownloadFile']");
                        //var tagAction = $menu.find("li[data-name='EditFileTags']");
                        var $copyImageToPdfAction = $menu.find("li[data-name='CopyImageToPdf']");

                        _enableContextMenuItem(renameAction, self.isRenameEnabled && !file.IsReadOnly);
                        // KABU TODO: Can't use IsDeletable, due to a bug. I.e. IsDeletable is always false.
                        _enableContextMenuItem(deleteAction, self.isDeleteEnabled && /* file.IsDeletable && */ !file.IsReadOnly);

                        var extension = file.File.FileExtension;
                        _enableContextMenuItem($copyImageToPdfAction, extension === "png" || extension === "jpg");
                    },
                    select: function (e) {

                        var file = self.select(e);
                        if (!file) {
                            e.preventDefault();
                            return;
                        }

                        var name = $(e.item).data("name");

                        if (name === "DownloadFile") {
                            cmodo.downloadFile(file.File.fileDownloadUrl, file.File.FileName);
                        }
                        else if (name === "RenameFile") {

                            _openMoCoreEditor({
                                mode: "modify",
                                itemId: file.Id,

                                operation: "Rename",
                                item: file,
                                fileName: cmodo.removeFileNameExtension(file.File.FileName, file.File.FileExtension),
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
                            kmodo.openDeletionConfirmationDialog(
                                "Wollen Sie die Datei '" + file.Name + "' wirklich löschen?")
                                .then(function (result) {

                                    if (result !== true)
                                        return;

                                    _executeServerMoOperation({
                                        operation: "MoveToRecycleBin",
                                        owner: self.owner,
                                        item: file,
                                        // KABU TODO: LOCALIZE
                                        title: "Löschen",
                                        errorMessage: "Die Datei konnte nicht in den Papierkorb verschoben werden.",
                                        success: function (result) {

                                            self._moFolderTreeViewModel.refresh();
                                        }
                                    });

                                });
                        }
                        else if (name === "EditFileTags") {

                            // Open Mo file tags editor.
                            kmodo.openById("844ed81d-dbbb-4278-abf4-2947f11fa4d3",
                                {
                                    // KABU TODO: MAGIC Mo file type ID.
                                    filters: buildTagsDataSourceFilters("6773cd3a-2179-4c88-b51f-d22a139b5c60", self.owner.CompanyId),
                                    itemId: file.Id,
                                    title: "Datei-Markierungen bearbeiten",
                                    message: file.File.FileName,
                                    minWidth: 400,
                                    minHeight: 500,

                                    finished: function (result) {
                                        if (result.isOk) {
                                            self._moFolderTreeViewModel.refresh();
                                        }
                                    }
                                });
                        }
                        else if (name === "CopyImageToPdf") {
                            _executeServerMoOperation({
                                operation: "CopyImageToPdf",
                                owner: null,
                                item: file,
                                // KABU TODO: LOCALIZE
                                title: "PDF aus Bild-Datei erzeugen",
                                errorMessage: "Das Erzeugen der PDF Datei schlug fehl.",
                                success: function (result) {

                                    self._moFolderTreeViewModel.refresh();
                                }
                            });
                        }
                    }
                }).data("kendoContextMenu");
            }

            // File upload
            if (this.isUploadEnabled) {
                if (this.$area.find("input.mo-file-upload").length) {
                    this._fileUploadViewModel = new MoFileUploadComponent({
                        $area: this.$area,
                        moFileCollectionViewModel: this
                    });
                }
            }
            else {
                // Remove file upload panel.
                this.$area.find(".mo-file-upload-area").remove();
            }
        };
    }

    export interface MoFileExplorerOptions extends ViewComponentOptions {
        $area: JQuery;
        owners?: (() => MoFileTreeOwnerDefinition[]) | MoFileTreeOwnerDefinition[];
        isRecycleBinEnabled?: boolean;
        isFileSystemTemplateEnabled?: boolean;
        areFileSelectorsVisible?: boolean;
        isUploadEnabled?: boolean;
    }

    interface InternalMoFileExplorerOptions {
        $area: JQuery;
        owners: MoFileTreeOwnerDefinition[];
        isRecycleBinEnabled: boolean;
        isFileSystemTemplateEnabled: boolean;
        areFileSelectorsVisible: boolean;
        isUploadEnabled: boolean;
    }

    export interface MoFileTreeOwnerDefinition {
        Kind: string;       
        Name: string;      
        TypeId: string;
        Id?: string;
        CompanyId?: string;
    }

    export interface MoFileTreeOwnerSetting {
        Id: string;
        Name: string;
        CompanyId: string;
    }

    export interface MoFileTreeOwner extends kendo.data.ObservableObject {
        Kind: string;
        Id: string;
        Name: string;
        CompanyId: string;
        TypeId?: string;
    }

    interface MoFileTreeOnwersModel extends kendo.data.ObservableObject {
        current: MoFileTreeOwner;
        items: MoFileTreeOwner[];
        changed: Function;
    }

    export class MoFileExplorerViewModel extends ViewComponent {
        private options: InternalMoFileExplorerOptions;
        private _treeView: MoFolderTreeViewModel;
        _filesView: MoFileCollectionViewModel;
        private _tagFilterSelector: kendo.ui.MultiSelect;
        private _filesGrid: GridComponent;
        private _lastCompanyId: string;
        private owners: MoFileTreeOnwersModel;

        constructor(options: MoFileExplorerOptions) {
            super(options);

            this._setOptions(options);

            // NOTE: _initComponent() will be called on demand via initComponent().
        }

        private _setOptions(options: MoFileExplorerOptions): void {
            this.options = {
                $area: options.$area,
                owners: (typeof options.owners === "function")
                    ? options.owners()
                    : (options.owners || []),
                isRecycleBinEnabled: typeof options.isRecycleBinEnabled !== "undefined" ? options.isRecycleBinEnabled : true,
                isFileSystemTemplateEnabled: typeof options.isFileSystemTemplateEnabled !== "undefined" ? options.isFileSystemTemplateEnabled : false,
                areFileSelectorsVisible: typeof options.areFileSelectorsVisible !== "undefined" ? options.areFileSelectorsVisible : false,
                isUploadEnabled: typeof options.isUploadEnabled !== "undefined" ? options.isUploadEnabled : false
            };
        }

        initComponent(): void {
            this._initComponent(this.options);
        }

        refresh(): Promise<void> {
            return this._treeView.refresh();
        }

        clear(): void {
            if (!this._isComponentInitialized)
                return;
            this._filesView.clearSelection();
            this._treeView.clear();
            this._filesView.clear();
        }

        private _getCurrentOwner(): MoFileTreeOwner {
            return this.owners.current;
        }

        activateOwner(ownerKind?: string): void {
            if (!this._getCurrentOwner() && !ownerKind) {
                // Activate first owner.
                this._activateOwnerAt(0);
                return;
            }

            if (!this._getCurrentOwner() && ownerKind) {
                // Set current owner by provided owner kind.
                this._activateOwnerAt(this._getDisplayedOwners().findIndex(x => x.Kind === ownerKind));
                return;
            }

            if (this._getCurrentOwner() && !ownerKind) {
                // Reactivate current owner.
                this._onCurrentOwnerChanged();
                return;
            }
        }

        private _activateOwnerAt(index: number): void {
            if (index < 0 || index >= this.owners.items.length)
                return;

            this.owners.set("current", this.owners.items[index]);
            this._onCurrentOwnerChanged();
        }

        getCurrentOwner(): MoFileTreeOwner {
            return this._getCurrentOwner();
        }

        getIsCurrentOwnerValid(): boolean {
            var owner = this._getCurrentOwner();
            return !!owner && !!owner.Id;
        }

        clearAllOwnerValues(): void {
            this._getDisplayedOwners().forEach(x => {
                x.set("Id", null);
                x.set("Name", "?");
                x.set("CompanyId", null);
            });
        }

        setOwnerValues(ownerKind: string, value: MoFileTreeOwnerSetting): void {
            var owner = this._getDisplayedOwners().find(x => x.Kind === ownerKind);
            if (!owner)
                return;

            owner.set("Id", value.Id);
            owner.set("Name", value.Name);
            owner.set("CompanyId", value.CompanyId || null);
        }

        _getDisplayedOwners(): MoFileTreeOwner[] {
            return this.owners.items;
        }

        _onCurrentOwnerChanged(): void {
            var owner = this._getCurrentOwner();

            // Clear the folders tree view model. This will also enable
            // expansion of all folders after the next refresh.
            this._treeView.clear();
            this._filesView.clear();
            // Set the selected owner.
            this._treeView.setOwner(owner);
            // Don't refresh initially, but alwas refresh subsequently, so that the
            // consumer can have control over the first refresh.
            if (this._isComponentInitialized) {
                this._treeView.refresh();
            }

            // Update available tags.
            if (this._tagFilterSelector &&
                this._lastCompanyId !== owner.CompanyId) {

                this._lastCompanyId = owner.CompanyId;

                this._tagFilterSelector.dataSource.filter(
                    // KABU TODO: MAGIC Mo file type ID.
                    buildTagsDataSourceFilters("6773cd3a-2179-4c88-b51f-d22a139b5c60", owner.CompanyId)
                );
            }
        }

        // TODO: REMOVE
        //_onTagFilterChanged(): void {
        //    // KABU TODO: NOP? var tagFilter = this._componentModels.tags.current;
        //}

        getCurrentFolder(): MoFolderTreeEntity {
            return this._treeView.select();
        }

        _getFilesSelectionManager(): GridSelectionManager {
            return this._filesGrid.selectionManager;
        }

        getFoldersViewModel(): MoFolderTreeViewModel {
            return this._treeView;
        }

        getFilesViewModel(): MoFileCollectionViewModel {
            return this._filesView;
        }

        saveExistingFilesToFolder(folderId: string, fileInfos: FileWithTagsInfo[]): void {
            var self = this;

            // Confirm and save.
            var owner = this.getCurrentOwner();
            var ownerName = owner.Name;
            var path = this.getFoldersViewModel().getFolderPath(folderId);

            kmodo.openConfirmationDialog(
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

                    _executeServerMoOperation({
                        operation: "SaveExistingFilesToFolder",
                        owner: owner,
                        folderId: folderId,
                        files: fileInfos,
                        errorMessage: "Fehler beim Speichern. Der Vorgang wurde abgebrochen. Es wurde nichts gespeichert.",
                        success: function (result) {
                            self._treeView.refresh();
                            kmodo.openInfoDialog("Die Dateien wurden im Verzeichnis \"" + path + "\" gespeichert.");
                        }
                    });
                });
        }

        private _initComponent(options: InternalMoFileExplorerOptions): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            var self = this;

            // Folder tree view for Mo folders to select from.
            this._treeView = new MoFolderTreeViewModel({
                $area: options.$area,
                isRecycleBinEnabled: !!options.isRecycleBinEnabled, // NOTE: Recycle bin won't be fetched
                isExpandAllOnLoadedEnabled: true,
                isFileSystemTemplateEnabled: options.isFileSystemTemplateEnabled
            });

            // Grid view for Mo files to select from.
            this._createFilesView(options);

            this._treeView.setFileCollectionViewModel(this._filesView);

            // Combobox for selection of Mo file system owner (i.e. either Project, ProjectSegment or Contract).
            // NOTE: This has to be initialized after the source files view models have been created
            // because is will immediately call "setOwner" on those view models.
            this.owners = kendo.observable({
                current: null,
                items: this.options.owners,
                // KABU TODO: VERY IMPORTANT: Eval if changed is really called.
                changed: function (e) {
                    //var item = e.sender.dataItem();
                    self._onCurrentOwnerChanged();
                }
            }) as MoFileTreeOnwersModel;
            kendo.bind(options.$area.find("input.mo-file-system-owner-selector"), this.owners);

            var $selector = options.$area.find("input.mo-file-system-tag-filter-selector");
            if ($selector.length) {
                this._tagFilterSelector = createMoTagFilterSelector(
                    $selector,
                    {
                        // KABU TODO: MAGIC Mo file type ID.
                        filters: buildTagsDataSourceFilters("6773cd3a-2179-4c88-b51f-d22a139b5c60"),
                        changed: function (tagIds) {
                            self._treeView.setTagsFilter(tagIds);
                            self._treeView.refresh();
                        }
                    }
                );
            }
        }

        private _createFilesView(options: InternalMoFileExplorerOptions): void {
            // View for Mo files.

            this._filesGrid = this._createFilesGrid(options);

            this._filesView = new MoFileCollectionViewModel({
                $area: options.$area,
                moFolderTreeViewModel: this._treeView,
                // Assign generated grid view model for the kendo grid
                // (its code is in the generated JS file "mo.forFiles.list.vm.generated.js").
                filesGridViewModel: this._filesGrid,
                isUploadEnabled: options.isUploadEnabled
            });
        }

        _createFilesGrid(options: InternalMoFileExplorerOptions): GridComponent {

            var filesViewOptions: any = {
                $component: this.options.$area.find("div.mo-file-list"),
                selectionMode: this.options.areFileSelectorsVisible ? "multiple" : undefined,
                // KABU TODO: IMPORTANT: Use selectionMode only. Don't operate on the kendo grid here.
                componentOptions: !this.options.areFileSelectorsVisible
                    ? { selectable: "row" }
                    : undefined
            };

            // This is the generated grid view model for the kendo grid.
            var filesGrid = cmodo.componentRegistry.getById("4f846a0e-fe28-451a-a5bf-7a2bcb9f3712").vm(filesViewOptions);

            if (options.areFileSelectorsVisible)
                filesGrid.selectionManager.showSelectors();

            return filesGrid;
        }
    }

    function _enableContextMenuItem($item: JQuery, enabled: boolean): void {
        if (!$item || !$item.length)
            return;

        if (enabled)
            $item.removeClass("k-hidden");
        else
            $item.addClass("k-hidden");
    }

    // Kendo treeview with tree lines: https://www.telerik.com/forums/treelines
    // jsbin: http://jsbin.com/ewiduv/1/edit?html,output 

    function _saveTreeViewState(vm, treeview: kendo.ui.TreeView): void {

        vm.expandedKeys = Object.create(null);
        vm.selectedKeys = Object.create(null);

        treeview.wrapper.find(".k-item").each((idx, elem) => {

            var item = treeview.dataItem(elem) as MoFolderTreeEntity;

            if (item.expanded)
                vm.expandedKeys[item[vm.schema.key]] = true;

            if (item.selected)
                vm.selectedKeys[item[vm.schema.key]] = true;
        });
    }

    function _openMoCoreEditor(context: any): void {

        // Open Mo core editor dialog.
        kmodo.openById("9f6650df-db11-44fb-818b-9282a2d3ea64",
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
                        _executeServerMoOperation(context, {
                            item: result.item
                        });
                    }
                }
            });
    }

    function _executeServerMoOperation(context: any, edit?: any) {

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
        else if (context.operation === "CopyImageToPdf") {
            params = [
                { name: "id", value: context.item.Id }
            ];
        }
        else throw new MoFileSystemException("Unexpected MoFileSystem operation '" + context.operation + "'.");

        var errorMessage = context.errorMessage || "Fehler. Der Vorgang wurde abgebrochen.";

        kmodo.progress(true);

        return cmodo.oDataAction("/odata/Mos", oDataMethod, params)
            .then(function (result) {
                // KABU TODO: REMOVE
                //if (!result || !result.Id)
                //    kmodo.showErrorDialog(errorMessage);
                //else
                context.success(result);
            })
            .catch(function (ex) {
                kmodo.openErrorDialog(errorMessage);
            })
            .finally(function () {
                kmodo.progress(false);
            });
    }

    function createMoTreeViewDataSource(items: Object[]): kendo.data.HierarchicalDataSource {
        return new kendo.data.HierarchicalDataSource({
            data: items || [],
            schema: {
                model: {
                    id: "Id",
                    children: "folders",
                    hasChildren: function (item) {
                        return item.folders && item.folders.length;
                    },
                    fields: {
                        Id: { validation: { required: true }, editable: false, defaultValue: '00000000-0000-0000-0000-000000000000', },
                        Name: { validation: { required: true }, defaultValue: "" },
                        ParentId: {},
                        TypeId: {},
                        IsContainer: { type: 'boolean' },
                        Role: {},
                        CreatedOn: { type: 'date' },
                        ModifiedOn: { type: 'date' }
                        //Index: { type: 'number' }
                    }
                }
            }
        });
    }

    // KABU TODO: REMOVE? Not used
    /*
    function createMoFolderDataSource(items): kendo.data.DataSource {
        return new kendo.data.DataSource({
            data: items || [],
            schema: {
                model: {
                    id: "Id",
                    fields: {
                        Id: { validation: { required: true }, editable: false, defaultValue: '00000000-0000-0000-0000-000000000000', },
                        Name: { validation: { required: true }, defaultValue: "" },
                        ParentId: {},
                        TypeId: {},
                        IsContainer: { type: 'boolean' },
                        Role: {},
                        CreatedOn: { type: 'date' },
                        ModifiedOn: { type: 'date' }
                        //Index: { type: 'number' }
                    }
                }
            }
        });
    }
    */
}