"use strict";
var kendomodo;
(function (kendomodo) {
    (function (ui) {

        ui.EmailAttachmentsViewModel = (function () {

            var EmailAttachmentsViewModel = function (options) {
                // KABU TODO: Use a deep copy of provided options.
                this.options = options;
                // KABU TODO: MoFileExplorerViewModel options:
                // options.areFileSelectorsVisible (default: false)
                // options.isRecycleBinEnabled (default: true)

                if (typeof options.owners === "function")
                    this.options.owners = options.owners();
                if (!this.options.owners)
                    this.options.owners = [];

                this.options.isFileSystemTemplateEnabled = false;

                this.currentOwnerKind = "ProjectSeries";

                this._attachmentsDataSource = new kendo.data.DataSource({
                    data: [],
                    pageSize: 7
                });

                this._isComponentInitialized = false;
                this._components = {};
                // NOTE: _initComponent() on demand using initComponent().
            };

            var fn = EmailAttachmentsViewModel.prototype;

            fn.initComponent = function () {
                this._initComponent(this.options);
            };

            // TODO: REMOVE
            //fn.setOwner = function (owner) {
            //    this._components.fileExplorer.setOwner(owner);
            //};

            fn.activateOwner = function (ownerKind) {
                this._components.fileExplorer.activateOwner(ownerKind);
            };

            fn.clearSelection = function () {
                if (!this._isComponentInitialized)
                    return;
                this._getSelectionManager().clearSelection();
                this._attachmentsDataSource.data([]);
            };

            fn.getAttachments = function () {
                return this._attachmentsDataSource.data();
            };

            fn.insertAttachments = function (attachments) {
                if (!attachments)
                    return;

                var self = this;

                attachments.forEach(function (item) {
                    self._attachmentsDataSource.insert(item);
                });
            };

            fn.initInitialAttachments = function () {
                // Add the existing attachments to the selectionManager of the files Grid view model.
                var selectionManager = this._getSelectionManager();

                this.getAttachments().forEach(function (item) {
                    selectionManager._addDataItem(item);
                });
            };

            fn.refresh = function () {
                this._components.fileExplorer.refresh();
            };

            fn.getAttachmentById = function (id) {
                return this.getAttachments().find(function (x) { return x.Id === id; });
            };

            fn.getAttachmentByUid = function (uid) {
                return this.getAttachments().find(function (x) { return x.uid === uid; });
            };

            fn._getSelectionManager = function () {
                return this._components.fileExplorer._getSelectionManager();
            };

            fn._removeAttachment = function (item) {
                if (!item)
                    return;

                // Deselect the row in the source files view.           
                this._getSelectionManager().deselectedById(item.Id);

                // If this item is not currently being displayed in the source files view,
                // then it won't be automatically remove from attachments.
                // Thus also explicitely try to remove from attachments.
                this._attachmentsDataSource.remove(item);
            };

            fn._initComponent = function (options) {
                if (this._isComponentInitialized)
                    return;
                this._isComponentInitialized = true;

                var self = this;

                // Init attachment list view.
                this._components.attachmentsKendoListView = options.$area
                    .find("div.mo-email-attachment-list-view")
                    .kendoListView({
                        dataSource: this._attachmentsDataSource,
                        template: options.attachmentFileTemplate
                    }).data("kendoListView");

                // Init "remove attachment" buttons on attachment tiles.
                this._components.attachmentsKendoListView.wrapper.on('click', ".list-item-remove-command", function (e) {
                    var uid = $(e.currentTarget).closest("div[data-uid]").first().data("uid");
                    self._removeAttachment(self.getAttachmentByUid(uid));
                });

                // Init pager for attachment list view.
                this._components.attachmentsKendoPager = options.$area
                    .find("div.mo-email-attachment-list-pager")
                    .kendoPager({
                        messages: { empty: "Leer" },
                        dataSource: this._attachmentsDataSource
                    }).data("kendoPager");

                // Init file explorer.
                this._components.fileExplorer = new kendomodo.ui.MoFileExplorerViewModel(this.options);
                this._components.fileExplorer.initComponent();

                var filesViewModel = this._components.fileExplorer._components.filesViewModel;

                // Handle file selection changes.
                filesViewModel.on("selectionChanged", function (e) {
                    self._attachmentsDataSource.data(e.items);
                });

                filesViewModel.on("selectionItemAdded", function (e) {
                    // Add file to attachments.
                    var item = self.getAttachmentById(e.item.Id);
                    if (!item)
                        self._attachmentsDataSource.insert(0, e.item);
                });

                filesViewModel.on("selectionItemRemoved", function (e) {
                    // Remove file from attachments.
                    var item = self.getAttachmentById(e.item.Id);
                    if (item)
                        self._attachmentsDataSource.remove(item);
                });
            };

            return EmailAttachmentsViewModel;
        })();

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));