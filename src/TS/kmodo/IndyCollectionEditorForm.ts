namespace kmodo {

    export interface IndyCollectionEditorFormOptions extends ViewComponentOptions {
        title?: string;      
        saveBaseUrl: string;
        saveMethod: string;
        isLocalTargetData?: boolean;
        localTargetData?: any[];
        isCustomSave?: boolean;
        sourceListId: string;
        targetListId: string;
        targetContainerQuery: string;
        targetContainerListField: string;
    }

    export class IndyCollectionEditorForm extends ViewComponent {
        protected _options: IndyCollectionEditorFormOptions;
        dataSource: kendo.data.DataSource = null;
        connector: SelectableFromCollectionConnector = null;
        sourceGridViewModel: Grid;
        targetGridViewModel: Grid;
        private _dialogWindow: kendo.ui.Window;

        constructor(options: IndyCollectionEditorFormOptions) {
            super(options);

            // TODO: REMOVE: this._options = super._options as IndyCollectionEditorFormComponentOptions;
        }

        save(): Promise<any> {
            if (this._options.isCustomSave)
                return Promise.resolve();

            const baseUrl = this._options.saveBaseUrl;
            const method = this._options.saveMethod;
            const key = this.keyName;
            const itemIds = this.targetGridViewModel.dataSource.data().map(x => x[key]);
            const params = [
                { name: "id", value: this.args.itemId },
                { name: "itemIds", value: itemIds }

            ];
            return cmodo.oDataAction(baseUrl, method, params);
        }

        start(): void {
            this.args.buildResult = () => {
                this.args.items = this.targetGridViewModel.dataSource.data();
            };

            const filters = this._getEffectiveFilters();
            if (filters.length) {
                this.sourceGridViewModel.setFilter(filters);
            }
            this.sourceGridViewModel.createView();
            this.sourceGridViewModel.selectionManager.showSelectors();

            this.targetGridViewModel.createView();

            this.connector.init();

            this.sourceGridViewModel.refresh();

            if (!this._options.isLocalTargetData) {
                cmodo.oDataQuery(this._options.targetContainerQuery + "&$filter=" + this.keyName + " eq " + this.args.itemId)
                    .then((items) => {
                        const item = items[0];
                        if (!item)
                            return;

                        this.targetGridViewModel.dataSource.data(item[this._options.targetContainerListField]);
                        this.connector.processInitialTargetItems();
                    });
            }
            else {
                this.targetGridViewModel.refresh()
                    .then(() => {
                        this.connector.processInitialTargetItems();
                    });
            }
        }

        private _createGrids(): void {

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
                isLocalData: this._options.isLocalTargetData,
                localData: this._options.localTargetData || null,
                useRemoveCommand: true
            });

            this.targetGridViewModel.on("item-remove-command-fired", e => {
                this.connector.remove(e.item);
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

        createView(): void {
            if (this._isViewInitialized)
                return;
            this._isViewInitialized = true;

            this.$view = $("#indylist-editor-view-" + this.getViewId());

            if (this.args.message) {
                this.$view.find(".indylist-editor-header").append("<span class='indylist-editor-header-message'>" + this.args.message + "</span>");
            }

            this._createGrids();

            if (this._options.isDialog)
                this._initViewAsDialog();
        }

        _initViewAsDialog(): void {

            // Get dialog arguments and set them on the view model.
            if (!this.args)
                this.setArgs(cmodo.dialogArgs.consume(this._options.id));

            this._dialogWindow = kmodo.findKendoWindow(this.$view);
            this._initDialogWindowTitle();

            const $dialogCommands = $('#dialog-commands-' + this.getViewId()).first();

            // Init OK/Cancel buttons.
            const $okBtn = $dialogCommands.find('button.ok-button').first();
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
            $okBtn.on("click", () => {
                kmodo.progress(true);
                this.save()
                    .then(() => {
                        this.args.buildResult();
                        this.args.isCancelled = false;
                        this.args.isOk = true;

                        this._dialogWindow.close();
                    })
                    .finally(() => {
                        kmodo.progress(false);
                    });
            });

            $dialogCommands.find('button.cancel-button').first().on("click", () => {
                this.args.isCancelled = true;
                this.args.isOk = false;

                this._dialogWindow.close();
            });
        }

        private _initDialogWindowTitle(): void {
            let title = "";
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
        source: Grid;
        target: Grid;
    }

    export class SelectableFromCollectionConnector extends cmodo.ComponentBase {

        keyName: string;
        sourceGridViewModel: Grid;
        targetGridViewModel: Grid;

        constructor(options: SelectableFromCollectionConnectorOptions) {
            super();

            this.keyName = options.keyName || "Id";
            this.sourceGridViewModel = options.source;
            this.targetGridViewModel = options.target;
        }

        init(): void {
            const selectionManager = this._getSourceSelectionManager();
            const targetDataSource = this._getTargetDataSource();
            const key = this.keyName;

            // Handle source selection changes.
            selectionManager.on("selectionChanged", e => {
                targetDataSource.data(e.items);
            });

            selectionManager.on("selectionItemAdded", e => {
                // Add file to target.
                const item = this.getTargetItemById(e.item[key]);
                if (!item) {
                    targetDataSource.insert(0, e.item);
                }
            });

            selectionManager.on("selectionItemRemoved", e => {
                // Remove file from target.
                const item = this.getTargetItemById(e.item[key]);
                if (item) {
                    targetDataSource.remove(item);
                }
            });
        }

        clear(): void {
            this._getSourceSelectionManager().clearSelection();
            this._getTargetDataSource().data([]);
        }

        processInitialTargetItems(): void {
            // Add the initially existing data items to the selectionManager of the source items view model.

            const selectionManager = this._getSourceSelectionManager();
            const targetDataSource = this._getTargetDataSource();

            targetDataSource.data().forEach(function (item) {
                selectionManager._addDataItem(item);
            });
        }

        private getTargetItemById(id: string): any {
            const key = this.keyName;
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