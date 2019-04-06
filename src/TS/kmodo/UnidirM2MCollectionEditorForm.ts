namespace kmodo {

    export interface UnidirM2MCollectionEditorFormOptions extends ViewComponentOptions {
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

    export class UnidirM2MCollectionEditorForm extends ViewComponent {
        protected _options: UnidirM2MCollectionEditorFormOptions;
        dataSource: kendo.data.DataSource = null;
        connector: SelectableFromCollectionConnector = null;
        sourceGridViewModel: GridComponent;
        targetGridViewModel: GridComponent;
        private _dialogWindow: kendo.ui.Window;

        constructor(options: UnidirM2MCollectionEditorFormOptions) {
            super(options);
        }

        save(): Promise<any> {

            if (this._options.isCustomSave)
                return Promise.resolve();

            const baseUrl = this._options.saveBaseUrl;
            const method = this._options.saveMethod;
            const itemIds = this.targetGridViewModel.dataSource.data().map(x => x[this.keyName]);
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

            if (this.args.filters)
                this.sourceGridViewModel.setFilter(this.args.filters);
            this.sourceGridViewModel.createView();
            this.sourceGridViewModel.selectionManager.showSelectors();

            this.targetGridViewModel.createView();

            this.connector.init();

            this.sourceGridViewModel.refresh();

            if (!this._options.isLocalTargetData) {
                cmodo.oDataQuery(this._options.targetContainerQuery + "&$filter=" + this.keyName + " eq " + this.args.itemId)
                    .then(items => {
                        const item = items[0];
                        if (!item)
                            return;

                        const steps = this._options.targetContainerListField.split(".");
                        // First step points to the "ToTags" list.
                        let list = item[steps[0]];
                        // Second step points to the Tag itself. Select all tags from list.
                        list = list.map(x => x[steps[1]]);

                        this.targetGridViewModel.dataSource.data(list);
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

            this.targetGridViewModel.on("item-remove-command-fired", (e) => {
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
}