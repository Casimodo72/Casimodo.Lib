/// <reference path="../casimodo/Transport.ts" />

namespace kmodo {

    export interface IndyCollectionEditorFormComponentOptions extends ViewComponentOptions {
        title?: string;
        isCustomSave?: boolean;
        saveBaseUrl: string;
        saveMethod: string;
        isLocalTargetData?: string;
        localTargetData?: any[];
        sourceListId: string;
        targetListId: string;
        targetContainerQuery: string;
        targetContainerListField: string;
    }

    export class IndyCollectionEditorFormComponent extends ViewComponent {
        protected _options: IndyCollectionEditorFormComponentOptions;
        dataSource: kendo.data.DataSource = null;
        connector: SelectableFromCollectionConnector = null;
        sourceGridViewModel: GridComponent;
        targetGridViewModel: GridComponent;
        private _dialogWindow: kendo.ui.Window;

        constructor(options: IndyCollectionEditorFormComponentOptions) {
            super(options);

            // TODO: REMOVE: this._options = super._options as IndyCollectionEditorFormComponentOptions;
        }

        save(): Promise<any> {

            if (this._options.isCustomSave)
                return Promise.resolve();

            var baseUrl = this._options.saveBaseUrl;
            var method = this._options.saveMethod;
            var key = this.keyName;
            var itemIds = this.targetGridViewModel.dataSource.data().map(x => x[key]);
            var params = [
                { name: "id", value: this.args.itemId },
                { name: "itemIds", value: itemIds }

            ];
            return cmodo.oDataAction(baseUrl, method, params);
        }

        start(): void {
            var self = this;

            this.args.buildResult = function () {
                self.args.items = self.targetGridViewModel.dataSource.data();
            };

            if (this.args.filters)
                this.sourceGridViewModel.setFilter(this.args.filters);
            this.sourceGridViewModel.createComponent();
            this.sourceGridViewModel.selectionManager.showSelectors();

            this.targetGridViewModel.createComponent();

            this.connector.init();

            this.sourceGridViewModel.refresh();

            if (!this._options.isLocalTargetData) {
                cmodo.oDataQuery(this._options.targetContainerQuery + "&$filter=" + this.keyName + " eq " + this.args.itemId)
                    .then(function (items) {
                        var item = items[0];
                        if (!item)
                            return;

                        self.targetGridViewModel.dataSource.data(item[self._options.targetContainerListField]);
                        self.connector.processInitialTargetItems();
                    });
            }
            else {
                this.targetGridViewModel.refresh()
                    .then(function () {
                        self.connector.processInitialTargetItems();
                    });
            }
        }

        private _createGrids(): void {
            var self = this;

            this.sourceGridViewModel = cmodo.componentRegistry.getById(this._options.sourceListId).vmOnly({
                $component: this.$view.find(".indylist-source-view").first(),
                selectionMode: "multiple",
                isAuthRequired: false,
                isDialog: false, isLookup: false, isDetailsEnabled: false,
                editor: null
            });

            this.targetGridViewModel = cmodo.componentRegistry.getById(this._options.targetListId).vmOnly({
                $component: this.$view.find(".indylist-target-view").first(),
                isAuthRequired: false,
                isDialog: false, isLookup: false, isDetailsEnabled: false,
                editor: null,
                // KABU TODO: VERY IMPORTANT: Eval if those new options work as expected.
                useLocalDataSource: this._options.isLocalTargetData,
                localData: this._options.localTargetData || null,
                useRemoveCommand: true
            });

            // KABU TODO: REMOVE:
            // this.targetGridViewModel.initComponentOptions();
            // this.targetGridViewModel.optionsUseLocalDataSource(this._options.localTargetData);            
            // if (this._options.isLocalTargetData)
            //    this.targetGridViewModel.optionsSetLocalData(this._options.localTargetData || []);
            //this.targetGridViewModel.optionsUseItemRemoveCommand();

            this.targetGridViewModel.on("item-remove-command-fired", function (e) {
                self.connector.remove(e.item);
            });

            this.connector = new SelectableFromCollectionConnector({
                keyName: this.keyName,
                source: this.sourceGridViewModel,
                target: this.targetGridViewModel
            });
        }

        getViewId(): string {
            return this._options.viewId || this._options.id;
        }

        createComponent(): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            this.$view = $("#indylist-editor-view-" + this.getViewId());

            if (this.args.message) {
                this.$view.find(".indylist-editor-header").append("<span class='indylist-editor-header-message'>" + this.args.message + "</span>");
            }

            this._createGrids();

            if (this._options.isDialog)
                this._initComponentAsDialog();
        }

        _initComponentAsDialog(): void {
            var self = this;

            // Get dialog arguments and set them on the view model.
            if (!this.args)
                this.setArgs(cmodo.dialogArgs.consume(this._options.id));

            this._dialogWindow = kmodo.findKendoWindow(this.$view);
            this._initDialogWindowTitle();

            var $dialogCommands = $('#dialog-commands-' + this.getViewId()).first();

            // Init OK/Cancel buttons.
            var $okBtn = $dialogCommands.find('button.ok-button').first();
            if (this._options.isLocalTargetData) {
                // Change save button text to "OK" because the text "Save" might
                // indicate to the consumer that the result will be automatically
                // saved to DB - which is not the case for local target data.
                $okBtn.text("OK");
            }
            else {
                $okBtn.text("Speichern");
            }
            // $okBtn.off("click.dialog-ok").on("click.dialog-ok", function () {
            $okBtn.on("click", function () {
                kmodo.progress(true);
                self.save()
                    .then(function () {
                        self.args.buildResult();
                        self.args.isCancelled = false;
                        self.args.isOk = true;

                        self._dialogWindow.close();
                    })
                    .finally(function () {
                        kmodo.progress(false);
                    });
            });
            // $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", function () {
            $dialogCommands.find('button.cancel-button').first().on("click", function () {
                self.args.isCancelled = true;
                self.args.isOk = false;

                self._dialogWindow.close();
            });
        }

        private _initDialogWindowTitle(): void {
            var title = "";
            if (!this._dialogWindow)
                return;
            if (this.args.title) {
                title = this.args.title;
            }
            else {
                title = this._options.title || "";

                if (this._options.isLookup)
                    title += " bearbeiten";
            }

            this._dialogWindow.title(title);
        }
    }

    interface SelectableFromCollectionConnectorOptions {
        keyName?: string;
        source: GridComponent;
        target: GridComponent;
    }

    export class SelectableFromCollectionConnector extends cmodo.ComponentBase {

        keyName: string;
        sourceGridViewModel: GridComponent;
        targetGridViewModel: GridComponent;

        constructor(options: SelectableFromCollectionConnectorOptions) {
            super();

            this.keyName = options.keyName || "Id";
            this.sourceGridViewModel = options.source;
            this.targetGridViewModel = options.target;
        }

        init(): void {
            var self = this;
            var selectionManager = this._getSourceSelectionManager();
            var targetDataSource = this._getTargetDataSource();
            var key = this.keyName;

            // Handle source selection changes.
            selectionManager.on("selectionChanged", function (e) {
                targetDataSource.data(e.items);
            });

            selectionManager.on("selectionItemAdded", function (e) {
                // Add file to target.
                var item = self.getTargetItemById(e.item[key]);
                if (!item)
                    targetDataSource.insert(0, e.item);
            });

            selectionManager.on("selectionItemRemoved", function (e) {
                // Remove file from target.
                var item = self.getTargetItemById(e.item[key]);
                if (item)
                    targetDataSource.remove(item);
            });
        }

        clear(): void {
            this._getSourceSelectionManager().clearSelection();
            this._getTargetDataSource().data([]);
        }

        processInitialTargetItems(): void {
            // Add the initially existing data items to the selectionManager of the source items view model.

            var selectionManager = this._getSourceSelectionManager();
            var targetDataSource = this._getTargetDataSource();

            targetDataSource.data().forEach(function (item) {
                selectionManager._addDataItem(item);
            });
        }

        private getTargetItemById(id: string): any {
            var key = this.keyName;
            return this._getTargetDataSource().data().find(function (x) { return x[key] === id; });
        }

        remove(item: any): void {
            if (!item)
                return;

            // Deselect the row in the source files view.           
            this._getSourceSelectionManager().deselectedById(item[this.keyName]);

            // If this item is not currently being displayed in the source files view,
            // then it won't be automatically removed from the target data source.
            // Thus we also explicitely try to remove from the target dadta source.
            this._getTargetDataSource().remove(item);
        }

        private _getSourceSelectionManager(): GridSelectionManager {
            return this.sourceGridViewModel.selectionManager;
        }

        private _getTargetDataSource(): kendo.data.DataSource {
            return this.targetGridViewModel.dataSource;
        }
    }
}