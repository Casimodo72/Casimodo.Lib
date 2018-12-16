/// <reference path="../cmodo/ComponentBase.ts" />

namespace kmodo {

    export interface EmailAttachmentsViewOptions {
        $area: JQuery;
        owners?: () => MoFileTreeOwner[] | MoFileTreeOwner[];
        isFileSystemTemplateEnabled: boolean;
        areFileSelectorsVisible: boolean;
        isRecycleBinEnabled: boolean;
        attachmentFileTemplate: Function;
    }

    interface InternalEmailAttachmentsViewOptions {
        $area: JQuery;
        owners: MoFileTreeOwnerDefinition[];
        isFileSystemTemplateEnabled: boolean;
        areFileSelectorsVisible: boolean;
        isRecycleBinEnabled: boolean;
        attachmentFileTemplate: Function;
    }

    export class EmailAttachmentsViewModel extends cmodo.ComponentBase {
        private options: InternalEmailAttachmentsViewOptions;
        currentOwnerKind: string;
        _attachmentsDataSource: kendo.data.DataSource;
        _fileExplorer: MoFileExplorerViewModel;
        _attachmentsKendoListView: kendo.ui.ListView;
        _attachmentsKendoPager: kendo.ui.Pager;
        private _isComponentInitialized: boolean = false;

        constructor(options: EmailAttachmentsViewOptions) {
            super();
            this._setOptions(options);
            // KABU TODO: MoFileExplorerViewModel options:
            // options.areFileSelectorsVisible (default: false)
            // options.isRecycleBinEnabled (default: true)

            this.currentOwnerKind = "ProjectSeries";

            this._attachmentsDataSource = new kendo.data.DataSource({
                data: [],
                pageSize: 7
            });

            // NOTE: _initComponent() on demand using initComponent().
        }

        private _setOptions(options: EmailAttachmentsViewOptions): void {
            this.options = {
                $area: options.$area,
                owners: (typeof options.owners === "function")
                    ? options.owners()
                    : (options.owners || []),
                areFileSelectorsVisible: options.areFileSelectorsVisible,
                isRecycleBinEnabled: options.isRecycleBinEnabled,
                isFileSystemTemplateEnabled: false,
                attachmentFileTemplate: options.attachmentFileTemplate
            };
        }

        initComponent(): void {
            this._initComponent(this.options);
        }

        activateOwner(ownerKind: string): void {
            this._fileExplorer.activateOwner(ownerKind);
        }

        clearSelection(): void {
            if (!this._isComponentInitialized)
                return;
            this._getFilesSelectionManager().clearSelection();
            this._attachmentsDataSource.data([]);
        }

        getAttachments(): kendo.data.ObservableArray {
            return this._attachmentsDataSource.data();
        }

        insertAttachments(attachments: Object[]): void {
            if (!attachments)
                return;

            var self = this;

            for (let item of attachments)
                self._attachmentsDataSource.insert(item);
        }

        initInitialAttachments(): void {
            // Add the existing attachments to the selectionManager of the files Grid view model.
            var selectionManager = this._getFilesSelectionManager();

            this.getAttachments().forEach(function (item) {
                selectionManager._addDataItem(item);
            });
        }

        refresh(): Promise<void> {
            return this._fileExplorer.refresh();
        }

        getAttachmentById(id: string): MoFileEntity {
            return this.getAttachments().find(function (x) { return (x as MoFileEntity).Id === id; }) as MoFileEntity;
        }

        getAttachmentByUid(uid: string): MoFileEntity {
            return this.getAttachments().find(function (x) { return (x as MoFileEntity).uid === uid; }) as MoFileEntity;
        }

        private _getFilesSelectionManager(): GridSelectionManager {
            return this._fileExplorer._getFilesSelectionManager();
        }

        private _removeAttachment(item): void {
            if (!item)
                return;

            // Deselect the row in the source files view.           
            this._getFilesSelectionManager().deselectedById(item.Id);

            // If this item is not currently being displayed in the source files view,
            // then it won't be automatically remove from attachments.
            // Thus also explicitely try to remove from attachments.
            this._attachmentsDataSource.remove(item);
        }

        private _initComponent(options: InternalEmailAttachmentsViewOptions): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            var self = this;

            // Init attachment list view.
            this._attachmentsKendoListView = options.$area
                .find("div.mo-email-attachment-list-view")
                .kendoListView({
                    dataSource: this._attachmentsDataSource,
                    template: options.attachmentFileTemplate
                }).data("kendoListView");

            // Init "remove attachment" buttons on attachment tiles.
            this._attachmentsKendoListView.wrapper.on('click', ".list-item-remove-command", function (e) {
                var uid = $(e.currentTarget).closest("div[data-uid]").first().data("uid");
                self._removeAttachment(self.getAttachmentByUid(uid));
            });

            // Init pager for attachment list view.
            this._attachmentsKendoPager = options.$area
                .find("div.mo-email-attachment-list-pager")
                .kendoPager({
                    messages: { empty: "Leer" },
                    dataSource: this._attachmentsDataSource
                }).data("kendoPager");

            // Init file explorer.
            this._fileExplorer = new MoFileExplorerViewModel({
                $area: this.options.$area,
                areFileSelectorsVisible: this.options.areFileSelectorsVisible,
                isFileSystemTemplateEnabled: this.options.isFileSystemTemplateEnabled,
                isRecycleBinEnabled: this.options.isRecycleBinEnabled,
                isUploadEnabled: false,
                owners: this.options.owners
            });
            this._fileExplorer.initComponent();

            var filesView = this._fileExplorer._filesView;

            // Handle file selection changes.
            filesView.on("selectionChanged", function (e) {
                self._attachmentsDataSource.data(e.items);
            });

            filesView.on("selectionItemAdded", function (e) {
                // Add file to attachments.
                var item = self.getAttachmentById(e.item.Id);
                if (!item)
                    self._attachmentsDataSource.insert(0, e.item);
            });

            filesView.on("selectionItemRemoved", function (e) {
                // Remove file from attachments.
                var item = self.getAttachmentById(e.item.Id);
                if (item)
                    self._attachmentsDataSource.remove(item);
            });
        }
    }
}