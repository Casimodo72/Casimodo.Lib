
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

    interface KendoTargetElementEvent {
        target?: Element;
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
        private _isViewInitialized: boolean;
        private _folderKendoTreeView: kendo.ui.TreeView;
        private moFileCollectionViewModel: MoFileCollectionViewModel;
        private _items: MoFolderTreeEntity[];
        private _containerDict: Object = {};
        private _filterTagIds: string[];

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

            this._isViewInitialized = false;
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
            let item = this._containerDict[folderId] as MoFolderTreeEntity;
            if (!item)
                return null;

            let path: string = null;

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

        isEmpty(): boolean {
            return !this._items || this._items.length === 0;
        }

        refreshFolders(selection): Promise<void> {
            return new Promise((resolve, reject) => {

                _saveTreeViewState(this, this._folderKendoTreeView);

                if (selection) {

                    const selectId = typeof selection === "string" ? selection : selection.Id;

                    if (selectId) {
                        this.selectedKeys = {};
                        this.selectedKeys[selectId] = true;
                    }
                }

                this._items = null;

                let query = "/odata/Mos/";
                if (this.options.isRecycleBinEnabled)
                    query += "QueryWithRecycleBin()";
                else
                    query += "Query()";

                query += "?$select=Id,Name,ParentId,TypeId,IsContainer,Role,ModifiedOn,CreatedOn,IsDeleted,IsNotDeletable,IsReadOnly";
                query += "&$expand=Permissions($select=RoleId)";
                //query += "&$orderby=Name&";
                query += "&$filter=OwnerId eq " + this.owner.Id;

                if (this._filterTagIds && this._filterTagIds.length) {
                    const tagsExpression = this._filterTagIds.map(x => "ToTags/any(totag: totag/TagId eq " + x + ")").join(" and ");
                    query += " and (IsContainer eq true or (" + tagsExpression + "))";
                }

                cmodo.oDataQuery(query)
                    .then((rawItems) => {

                        this._items = rawItems as MoFolderTreeEntity[];

                        // Add internal sort property in order to have
                        // the management and recycle bin displayed at last positions.
                        // KABU TODO: Add a dedicated sort property to the Mo class.
                        for (let x of this._items as any[]) {
                            if (x.Role === "RecycleBin")
                                x._sortValue = 2;
                            else if (x.Role === "ManagementDocRoot")
                                x._sortValue = 1;
                            else
                                x._sortValue = 0;
                        }

                        this._items = this._buildMoTreeViewHierarchy(this._items);
                        this._items = this._restoreTreeViewState(this._items, this.isInitialLoad);

                        this._folderKendoTreeView.setDataSource(this.createDataSource(this._items));

                        // Select node on right mouse click.
                        this._folderKendoTreeView.wrapper.on('mousedown', '.k-item', e => {
                            if (e.which === 3) {
                                e.stopPropagation();
                                this._folderKendoTreeView.select(e.currentTarget);
                            }
                        });

                        // Ensure selection.
                        if (!this.select()) {
                            // Select first node.
                            const nodes = this._folderKendoTreeView.dataSource.data();
                            if (nodes.length)
                                this._folderKendoTreeView.select(this._folderKendoTreeView.findByUid(nodes[0].uid));
                        }

                        if (this.isInitialLoad) {
                            this.isInitialLoad = false;
                            if (this.options.isExpandAllOnLoadedEnabled) {
                                this._folderKendoTreeView.expand(".k-item");
                            }
                        }

                        this.changed();

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

            const folder = (e ? this.selectByNode(e.node) : this.select()) as MoFolderTreeEntity;
            if (!folder) {
                // Hide upload component if no container is selected.
                this.moFileCollectionViewModel.clear();
                this.moFileCollectionViewModel._setFileUploadVisible(false);

                return;
            }

            const isInRecycleBin = this.isInRecycleBin(folder);

            // Hide upload-component if in recycle-bin.
            this.moFileCollectionViewModel._setFileUploadVisible(!isInRecycleBin);

            const filters: DataSourceFilterNode[] = [
                { field: "ParentId", operator: "eq", value: folder.Id },
                { field: "IsContainer", operator: "eq", value: false }
            ];

            if (this._filterTagIds && this._filterTagIds.length) {
                const tagsExpression = this._filterTagIds.map(x => "ToTags/any(totag: totag/TagId eq " + x + ")").join(" and ");
                (filters as any[]).push({ customExpression: tagsExpression });
            }

            this.moFileCollectionViewModel.setFilter(filters);
            this.moFileCollectionViewModel.refresh();
        }

        isRoot(folder: MoFolderTreeEntity): boolean {
            return folder.Role === "LocalDocRoot";
        }

        isInRecycleBin(folder: MoFolderTreeEntity): boolean {
            return folder.IsDeleted || folder.Role === "RecycleBin";
        }

        private _initComponent(): void {
            if (this._isViewInitialized) return;
            this._isViewInitialized = true;

            // Refresh-all button.    
            this.options.$area.find(".mo-file-system-refresh-all-command").on("click", () => {
                this.refresh();
            });

            // Folder treeview
            const $folderTree = this.options.$area.find("div.mo-folder-tree");

            this._folderKendoTreeView = $folderTree.kendoTreeView({
                template: cmodo.templates.get("MoTreeView"),
                select: e => this.onSelectionChanged(e),
                dataTextField: "Name",
                dataSource: this.createDataSource([]),
                dataBound: e => {
                    if (this.options.isExpandAllOnLoadedEnabled && e.node)
                        e.sender.expand(e.node.find(".k-item"));
                }
            }).data("kendoTreeView");

            // Folder context menu.
            const $folderContextMenu = this.options.$area.find("ul.mo-folder-context-menu");
            if ($folderContextMenu.length) {
                //this._kendoContextMenu =
                $folderContextMenu.kendoContextMenu({
                    target: $folderTree,
                    filter: ".k-in",
                    animation: kmodo.getDefaultContextMenuAnimation(),
                    open: e => {
                        const folder = this.select() as MoFolderTreeEntity;
                        if (!folder || this.isInRecycleBin(folder)) {
                            e.preventDefault();
                            return;
                        }

                        const isroot = this.isRoot(folder);

                        const $menu = e.sender.wrapper;
                        const $addChildAction = $menu.find("li[data-name='AddChildFolder']");
                        const $renameAction = $menu.find("li[data-name='RenameFolder']");
                        const $deleteAction = $menu.find("li[data-name='MoveFolderToRecycleBin']");
                        const $selectFileSystemTemplateAction = $menu.find("li[data-name='SelectFileSystemTemplate']");

                        _enableContextMenuItem($addChildAction, this.isAddEnabled);
                        _enableContextMenuItem($renameAction, !isroot && this.isRenameEnabled && !folder.IsReadOnly);
                        // TODO: Can't use IsNotDeletable, due to a bug. I.e. IsNotDeletable is always true.
                        // TODO: VERY IMPORTANT: FIX IsNotDeletable states in DB.
                        _enableContextMenuItem($deleteAction, !isroot /* && !folder.IsNotDeletable */ && this.isDeleteEnabled && !folder.IsReadOnly);
                        _enableContextMenuItem($selectFileSystemTemplateAction, this.options.isFileSystemTemplateEnabled && isroot);
                    },
                    select: e => {
                        const folder = this.select() as MoFolderTreeEntity;
                        if (!folder) {
                            // KABU TODO: Can this really happen or is this superfluous?
                            e.preventDefault();
                            return;
                        }

                        const name = $(e.item).data("name") as string;

                        if (name === "RenameFolder") {

                            _openMoCoreEditor({
                                mode: "modify",
                                itemId: folder.Id,

                                operation: "Rename",
                                item: folder,
                                // KABU TODO: LOCALIZE
                                title: "Umbennen",
                                errorMessage: "Der Ordner konnte nicht umbenannt werden.",
                                success: (result) => {
                                    this.refresh(result.Id);
                                }
                            });
                        }
                        else if (name === "AddChildFolder") {

                            _openMoCoreEditor({
                                mode: "create",
                                itemId: null,

                                operation: "AddPlainFolder",
                                owner: this.owner,
                                parentId: folder.Id,
                                // KABU TODO: LOCALIZE
                                title: "Neuer Ordner",
                                errorMessage: "Der Ordner konnte nicht erstellt werden.",
                                success: (result) => {
                                    this.refresh(result.Id);
                                }
                            });
                        }
                        else if (name === "MoveFolderToRecycleBin") {
                            const node = this._folderKendoTreeView.select();

                            // Confirm deletion and delete.
                            kmodo.openDeletionConfirmationDialog(
                                "Wollen Sie den Ordner '" + folder.Name + "' und dessen Inhalt wirklich löschen?")
                                .then((result) => {

                                    if (result !== true)
                                        return;

                                    _executeServerMoOperation({
                                        operation: "MoveToRecycleBin",
                                        owner: this.owner,
                                        item: folder,
                                        errorMessage: "Der Ordner konnte nicht in den Papierkorb verschoben werden.",
                                        success: (result) => {

                                            const prev = this.getByNode($(node).prev());
                                            const next = this.getByNode($(node).next());
                                            const parent = this.getByNode(this._folderKendoTreeView.parent(node));

                                            this.refresh(prev || next || parent);
                                        }
                                    });
                                });
                        }
                        else if (name === "SelectFileSystemTemplate") {

                            kmodo.openById("8f25bf5c-c20f-4337-8072-1e397cdcf976", {
                                title: "Dateisystem-Vorlage wählen",
                                filters: [{ field: "ApplicableToOwnerId", operator: "eq", value: this.owner.TypeId }],
                                width: 600,
                                minHeight: 500,
                                finished: (result) => {
                                    if (result.isOk) {
                                        _executeServerMoOperation({
                                            operation: "ApplyFileSystemTemplate",
                                            owner: this.owner,
                                            item: folder,
                                            templateId: result.value,
                                            errorMessage: "Das Dateisystem konnte nicht erstellt werden.",
                                            success: (result) => {
                                                this.refresh();
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
            const containers = [];
            const roots = [];
            let mo, parent;
            this._containerDict = {};

            for (let i = 0; i < mos.length; i++) {
                mo = mos[i];

                if (!mo.ParentId) {
                    mo._parent = null;
                    // KABU TODO: IMPORTANT: Rename all root Mos from "Dokumente" to "Dateien" in DB.
                    // KABU TODO: TEMP-HACK: Rename locally.
                    if (mo.Name === "Dokumente")
                        mo.Name = "Dateien";

                    roots.push(mo);
                }

                if (!mo.IsContainer) continue;

                this._containerDict[mo.Id] = mo;
                containers.push(mo);
                mo.folders = [];
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
                parent.folders.sort((x, y) => {
                    if (x._sortValue > y._sortValue) return 1;
                    if (x._sortValue < y._sortValue) return -1;

                    // Default sort: by name.
                    return x.Name.localeCompare(y.Name);
                });
            }

            return roots;
        }

        private _restoreTreeViewState(items: any[], expandAll: boolean): any[] {
            const context = {
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

            let isPathSelected = false;
            let item;
            let children;
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
        private _isViewInitialized: boolean;
        private _kendoUpload: kendo.ui.Upload;
        private _kendoUploadModeSelector: kendo.ui.DropDownList;
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
            this._isViewInitialized = false;
            this.createView();
        }

        reset(): void {
            if (this._isRefreshingAfterUpload)
                // The folder tree or file list components will request a reset
                //   when they are being refreshed immediately after files were uploaded.
                //   We don't want to reset in this case. Thus skip.
                return;

            const $elem = this._kendoUpload.wrapper;
            $elem.find(".k-upload-files").remove();
            $elem.find(".k-upload-status").remove();
            $elem.find(".k-upload.k-header").addClass("k-upload-empty");
            $elem.find(".k-upload-button").removeClass("k-state-focused");
            this.showResetButton(false);
        }

        onSelecting(e: kendo.ui.UploadSelectEvent): void {
            this.reset();
            this.getCurrentFolder(e);
        }

        onUploading(e: kendo.ui.UploadUploadEvent): void {
            const folder = this.getCurrentFolder(e);
            if (!folder) {
                cmodo.showError("Wählen Sie zuerst einen Ordner bevor Sie Dateien hochladen.");

                // Cancel.
                e.preventDefault();

                return;
            }

            e.sender.options.async.saveUrl = this._baseUrl + folder.Id + "?mode=" + this._uploadMode;

            // Validate
            const files = e.files;
            for (let i = 0; i < files.length; i++) {
                const file = files[i];

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

        getCurrentFolder(e): MoFolderTreeEntity {
            const folder = this.options.moFileCollectionViewModel.getCurrentFolder();
            if (!folder || !folder.Id) {
                e.preventDefault();

                cmodo.showError("Bitte wählen Sie zuerst einen Order aus bevor Sie eine Datei hochladen.");

                return null;
            }

            return folder;
        }

        onCompleted(e: kendo.ui.UploadEvent): void {
            this.showResetButton(true);

            this._isRefreshingAfterUpload = true;
            try {
                let refreshFunc = null;
                // Refresh collection of files.
                if (this.options.moFileCollectionViewModel._moFolderTreeViewModel)
                    refreshFunc = () => this.options.moFileCollectionViewModel._moFolderTreeViewModel.refresh();
                else
                    refreshFunc = () => this.options.moFileCollectionViewModel.refresh();

                refreshFunc()
                    .finally(() => {
                        this._isRefreshingAfterUpload = false;
                    });
            }
            catch (err) {
                this._isRefreshingAfterUpload = false;
            }
        }

        _onUploadModeChanged(): void {
            // TODO: Show options panel for mode "email-extraction".
            const $emailModeProps = this.options.$area.find("div.mo-file-upload-mode-email-prop");
            if (this._uploadMode === UPLOAD_MODE_EMAIL)
                $emailModeProps.show(100);
            else
                $emailModeProps.hide(100);
        }

        showResetButton(visible: boolean): void {
            let $btn = this.options.$area.find(".kmodo-upload-reset-command");

            if (visible && !$btn.length) {
                // Add reset button
                $btn = this._kendoUpload.wrapper
                    .find(".k-dropzone")
                    .append('<button type="button" class="k-button k-upload-action kmodo-upload-reset-command" aria-label="Bereinigen"><span class="k-icon k-i-close k-i-x" title="Bereinigen"></span></button>');

                $btn.on("click", () => {
                    this.reset();
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

        private createView(): void {
            if (this._isViewInitialized) return;
            this._isViewInitialized = true;

            this._$uploadArea = this.options.$area.find(".mo-file-upload-area");
            const $upload = this._$uploadArea.find("input.mo-file-upload");

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
                select: e => this.onSelecting(e),
                upload: e => this.onUploading(e),
                async: {
                    saveUrl: this._baseUrl,
                    autoUpload: true
                },
                success: e => {
                    // Hide the irritating warning (or info) icon on success.
                    //$upload.find(".k-icon.k-warning").hide();
                },
                error: e => {
                    const $status = e.sender.wrapper.find(".k-upload-status-total");
                    // Remove any messages added by Kendo from the DOM.
                    cmodo.jQueryRemoveContentTextNodes($status);
                    $status.append("Fehler");
                    $status.css("color", "red");
                    cmodo.showError("Fehler. Mindestens eine Datei konnte nicht gespeichert werden.")
                },
                complete: e => this.onCompleted(e)
            }).data("kendoUpload");

            this._kendoUploadModeSelector = this._$uploadArea
                .find("input.mo-file-upload-mode-selector")
                .kendoDropDownList({
                    height: 50,
                    dataValueField: "mode",
                    dataTextField: "displayName",
                    autoBind: true,
                    valuePrimitive: true,
                    dataSource: [{ mode: UPLOAD_MODE_DEFAULT, displayName: "Standard" }, { mode: UPLOAD_MODE_EMAIL, displayName: "E-Mail" }],
                    change: e => {
                        // The user selected an upload mode.
                        this._uploadMode = e.sender.value();
                        this._onUploadModeChanged();
                    }
                }).data("kendoDropDownList");
            this._kendoUploadModeSelector.select(0);

            kendo.bind(this._$uploadArea.find(".mo-file-upload-mode-panel"), this._scope);

            $upload.show();
        }
    }

    class MoFileCollectionViewModel extends cmodo.ComponentBase {
        private $area: JQuery;
        private filesGridViewModel: Grid;
        _moFolderTreeViewModel: MoFolderTreeViewModel;
        private _fileUploadViewModel: MoFileUploadComponent;
        private isUploadEnabled: boolean;
        isAddEnabled: boolean;
        private isDeleteEnabled: boolean;
        private isRenameEnabled: boolean;
        private owner: any;
        private _isViewInitialized: boolean;

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

            this._isViewInitialized = false;
            this.createView();
        }

        private _bindCoreEvents(): void {
            this.filesGridViewModel.on("dataBound", e => { this.trigger("dataBound", e); });
            this.filesGridViewModel.selectionManager.on("selectionChanged", e => { this.trigger("selectionChanged", e); });
            this.filesGridViewModel.selectionManager.on("selectionItemAdded", e => { this.trigger("selectionItemAdded", e); });
            this.filesGridViewModel.selectionManager.on("selectionItemRemoved", e => { this.trigger("selectionItemRemoved", e); });
        }
        // TODO: REMOVE: 
        //setTagsFilter(tagIds: string[]): void {
        //    this._filterTagIds = tagIds;
        //}

        getDataSource(): kendo.data.DataSource {
            return this.getKendoGrid().dataSource;
        }

        getKendoGrid(): kendo.ui.Grid {
            return this.filesGridViewModel.getKendoGrid();
        }

        setOwner(owner: any): void {
            this.owner = owner;
        }

        getCurrentFolder(): MoFolderTreeEntity {
            return this._moFolderTreeViewModel.select() as MoFolderTreeEntity;
        }

        getFileByEventTarget(e: KendoTargetElementEvent): MoFileEntity {
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

        setFilter(filter: DataSourceFilterOneOrMany): void {
            this.filesGridViewModel.setFilter(filter);
        }

        clear(): void {
            this.filesGridViewModel.clear();
        }

        // Reloads data.
        async refresh(selectId?: string): Promise<void> {
            await this.filesGridViewModel.refresh();

            if (!selectId)
                return;

            // Select the file with the provided ID.
            this.filesGridViewModel.trySetCurrentById(selectId);
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

        private createView(): void {
            if (this._isViewInitialized) return;
            this._isViewInitialized = true;

            // File context menu widget
            const $fileList = this.$area.find("div.mo-file-list");
            const $fileContextMenu = this.$area.find("ul.mo-file-context-menu");
            if ($fileList.length && $fileContextMenu.length) {
                $fileContextMenu.kendoContextMenu({
                    target: $fileList,
                    filter: "div.file-tile",
                    animation: kmodo.getDefaultContextMenuAnimation(),
                    open: e => {
                        const file = this.getFileByEventTarget(e);
                        if (!file || file.IsDeleted) {
                            e.preventDefault();
                            return;
                        }

                        this.filesGridViewModel.gridSelectByItem(file);

                        const $menu = e.sender.wrapper;
                        const renameAction = $menu.find("li[data-name='RenameFile']");
                        const deleteAction = $menu.find("li[data-name='DeleteFile']");
                        // const downloadAction = $menu.find("li[data-name='DownloadFile']");
                        // const tagAction = $menu.find("li[data-name='EditFileTags']");
                        const $copyImageToPdfAction = $menu.find("li[data-name='CopyImageToPdf']");

                        _enableContextMenuItem(renameAction, this.isRenameEnabled && !file.IsReadOnly);
                        // TODO: Can't use IsNotDeletable, due to a bug. I.e. IsNotDeletable is always true.
                        // TODO: VERY IMPORTANT: FIX IsNotDeletable in DB.
                        _enableContextMenuItem(deleteAction, this.isDeleteEnabled && /* !file.IsNotDeletable && */ !file.IsReadOnly);

                        const extension = file.File.FileExtension;
                        _enableContextMenuItem($copyImageToPdfAction, extension === "png" || extension === "jpg");
                    },
                    select: e => {
                        const file = this.getFileByEventTarget(e);
                        if (!file)
                            return false;

                        const name = $(e.item).data("name");

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
                                success: (result) => {
                                    this.refresh(result.Id);
                                }
                            });
                        }
                        else if (name === "DeleteFile") {

                            // Confirm deletion and delete.
                            kmodo.openDeletionConfirmationDialog(
                                "Wollen Sie die Datei '" + file.Name + "' wirklich löschen?")
                                .then((result) => {

                                    if (result !== true)
                                        return;

                                    _executeServerMoOperation({
                                        operation: "MoveToRecycleBin",
                                        owner: this.owner,
                                        item: file,
                                        // KABU TODO: LOCALIZE
                                        title: "Löschen",
                                        errorMessage: "Die Datei konnte nicht in den Papierkorb verschoben werden.",
                                        success: (result) => {

                                            this._moFolderTreeViewModel.refresh();
                                        }
                                    });

                                });
                        }
                        else if (name === "EditFileTags") {
                            // Open Mo Tags editor. Show only tags assignable to file-Mos.
                            // TODO: MAGIC component ID.
                            kmodo.openById("27325a97-268c-4064-aebd-46b74474bcbd",
                                {
                                    // TODO: MAGIC Mo file type ID.
                                    filters: buildTagsDataSourceFilters("6773cd3a-2179-4c88-b51f-d22a139b5c60", this.owner.CompanyId),
                                    itemId: file.Id,
                                    title: "Datei-Tags bearbeiten",
                                    message: file.File.FileName,
                                    minWidth: 400,
                                    minHeight: 500,

                                    finished: (result) => {
                                        if (result.isOk) {
                                            this._moFolderTreeViewModel.refresh();
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
                                success: (result) => {

                                    this._moFolderTreeViewModel.refresh();
                                }
                            });
                        }

                        return false;
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
        private _filesGrid: Grid;
        private _lastCompanyId: string;
        private owners: MoFileTreeOnwersModel;
        private _$fileSystemInitiatorBtn: JQuery;
        private _$tagSelectorIcon: JQuery;

        constructor(options: MoFileExplorerOptions) {
            super(options);

            this._setOptions(options);

            this._$fileSystemInitiatorBtn = null;

            // createView will be called on-demand.
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

        createView(): void {
            this._createView(this.options);
        }

        refresh(): Promise<void> {
            return this._refreshTreeView();
        }

        private _refreshTreeView(): Promise<void> {
            return this._treeView.refresh()
                .then(() => {
                    if (this._treeView.isEmpty()) {
                        this._$fileSystemInitiatorBtn.show();
                        if (this._tagFilterSelector) {
                            this._$tagSelectorIcon.hide();
                            this._tagFilterSelector.wrapper.hide();
                        }
                    }
                    else {
                        this._$fileSystemInitiatorBtn.hide();

                        if (this._tagFilterSelector) {
                            this._$tagSelectorIcon.show();
                            this._tagFilterSelector.wrapper.show();
                        }
                    }
                });
        }

        clear(): void {
            if (!this._isViewInitialized)
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
            const owner = this._getCurrentOwner();
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
            const owner = this._getDisplayedOwners().find(x => x.Kind === ownerKind);
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
            const owner = this._getCurrentOwner();

            // Clear the folders tree view model. This will also enable
            // expansion of all folders after the next refresh.
            this._treeView.clear();
            this._filesView.clear();
            // Set the selected owner.
            this._treeView.setOwner(owner);
            // Don't refresh initially, but always refresh subsequently, so that the
            // consumer can have control over the first refresh.
            if (this._isViewInitialized) {
                this._refreshTreeView();
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
            // Confirm and save.
            const owner = this.getCurrentOwner();
            const ownerName = owner.Name;
            const path = this.getFoldersViewModel().getFolderPath(folderId);

            kmodo.openConfirmationDialog(
                "Ziel: " + ownerName + " \"" + path + "\"\n\n" +
                "Wollen Sie die Dateien wirklich in diesem Verzeichnis speichern?",
                //"Verzeichnis: \"" + path + "\"\n" +
                //"Besitzer: \"" + ownerName + "\"",
                {
                    title: "Dateien speichern",
                    kind: "warning"
                })
                .then((result) => {

                    if (result !== true)
                        return;

                    _executeServerMoOperation({
                        operation: "SaveExistingFilesToFolder",
                        owner: owner,
                        folderId: folderId,
                        files: fileInfos,
                        errorMessage: "Fehler beim Speichern. Der Vorgang wurde abgebrochen. Es wurde nichts gespeichert.",
                        success: (result) => {
                            this._refreshTreeView();
                            kmodo.openInfoDialog("Die Dateien wurden im Verzeichnis \"" + path + "\" gespeichert.");
                        }
                    });
                });
        }

        private _createView(options: InternalMoFileExplorerOptions): void {
            if (this._isViewInitialized)
                return;
            this._isViewInitialized = true;

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
                changed: e => {
                    // const item = e.sender.dataItem();
                    this._onCurrentOwnerChanged();
                }
            }) as MoFileTreeOnwersModel;
            kendo.bind(options.$area.find("input.mo-file-system-owner-selector"), this.owners);

            this._$fileSystemInitiatorBtn = options.$area.find(".mo-file-system-initialize");
            this._$fileSystemInitiatorBtn.on("click", () => {
                kmodo.progress(true, this.$view);
                cmodo.oDataAction("odata/Mos", "CreateFilesystem",
                    [
                        { name: "ownerId", value: this.getCurrentOwner().Id }
                    ])
                    .finally(() => {
                        this._$fileSystemInitiatorBtn.hide();
                        kmodo.progress(false, this.$view);
                        this.refresh();
                    });
            });

            this._$tagSelectorIcon = options.$area.find(".mo-file-system-tag-icon");

            const $selector = options.$area.find("input.mo-file-system-tag-filter-selector");
            if ($selector.length) {
                this._tagFilterSelector = createMoTagFilterSelector(
                    $selector,
                    {
                        // KABU TODO: MAGIC Mo file type ID.
                        filters: buildTagsDataSourceFilters("6773cd3a-2179-4c88-b51f-d22a139b5c60"),
                        changed: (tagIds) => {
                            this._treeView.setTagsFilter(tagIds);
                            this._refreshTreeView();
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

        _createFilesGrid(options: InternalMoFileExplorerOptions): Grid {
            const filesViewOptions: any = {
                $component: this.options.$area.find("div.mo-file-list"),
                selectionMode: this.options.areFileSelectorsVisible ? "multiple" : undefined,
                // KABU TODO: IMPORTANT: Use selectionMode only. Don't operate on the kendo grid here.
                componentOptions: !this.options.areFileSelectorsVisible
                    ? { selectable: "row" }
                    : undefined
            };

            // This is the generated grid view model for the kendo grid.
            const filesGrid = cmodo.componentRegistry.getById("4f846a0e-fe28-451a-a5bf-7a2bcb9f3712").vm(filesViewOptions);

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
            const item = treeview.dataItem(elem) as MoFolderTreeEntity;

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

                finished: (result) => {
                    if (result.isOk) {
                        _executeServerMoOperation(context, {
                            item: result.item
                        });
                    }
                }
            });
    }

    function _executeServerMoOperation(context: any, edit?: any) {
        const oDataMethod = context.operation;
        let params = [];

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

        const errorMessage = context.errorMessage || "Fehler. Der Vorgang wurde abgebrochen.";

        kmodo.progress(true);

        return cmodo.oDataAction("/odata/Mos", oDataMethod, params)
            .then((result) => {
                context.success(result);
            })
            .catch((err) => {
                kmodo.openErrorDialog(errorMessage);
            })
            .finally(() => {
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
                    hasChildren: (item) => {
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