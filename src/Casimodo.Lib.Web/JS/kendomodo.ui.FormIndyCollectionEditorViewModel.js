"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var SelectableFromCollectionConnector = (function (_super) {
            casimodo.__extends(SelectableFromCollectionConnector, _super);

            function SelectableFromCollectionConnector(options) {
                _super.call(this, options);

                this.keyName = options.keyName || "Id";
                this.sourceGridViewModel = options.source;
                this.targetGridViewModel = options.target;
            }

            var fn = SelectableFromCollectionConnector.prototype;

            fn.init = function () {
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
            };

            fn.processInitialTargetItems = function () {
                // Add the initially existing data items to the selectionManager of the source items view model.

                var selectionManager = this._getSourceSelectionManager();
                var targetDataSource = this._getTargetDataSource();

                targetDataSource.data().forEach(function (item) {
                    selectionManager._addDataItem(item);
                });
            };

            fn.getTargetItemById = function (id) {
                var key = this.keyName;
                return this._getTargetDataSource().data().find(function (x) { return x[key] === id; });
            };

            fn.remove = function (item) {
                if (!item)
                    return;

                // Deselect the row in the source files view.           
                this._getSourceSelectionManager().deselectedById(item[this.keyName]);

                // If this item is not currently being displayed in the source files view,
                // then it won't be automatically removed from the target data source.
                // Thus we also explicitely try to remove from the target dadta source.
                this._getTargetDataSource.remove(item);
            };

            fn.clear = function () {
                this._getSourceSelectionManager().clearSelection();
                this._getTargetDataSource().data([]);
            };

            fn._getSourceSelectionManager = function () {
                return this.sourceGridViewModel.selectionManager;
            };

            fn._getTargetDataSource = function () {
                return this.targetGridViewModel.dataSource;
            };

            return SelectableFromCollectionConnector;

        })(casimodo.ObservableObject);
        ui.SelectableFromCollectionConnector = SelectableFromCollectionConnector;

        var FormIndyCollectionEditorViewModel = (function (_super) {
            casimodo.__extends(FormIndyCollectionEditorViewModel, _super);

            function FormIndyCollectionEditorViewModel(options) {
                _super.call(this, options);

                this.$view = null;
                this._isComponentInitialized = false;
                this.dataSource = null;
                this.connector = null;
            }

            var fn = FormIndyCollectionEditorViewModel.prototype;

            fn.save = function () {

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
                return casimodo.oDataAction(baseUrl, method, params);
            };

            fn.start = function () {
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
                    casimodo.oDataQuery(this._options.targetContainerQuery + "&$filter=" + this.keyName + " eq " + this.args.itemId)
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
            };

            fn._createViewModels = function () {
                var self = this;

                this.sourceGridViewModel = casimodo.ui.componentRegistry.getById(this._options.sourceListId)
                    .vmOnly({
                        $component: this.$view.find(".indylist-source-view").first(),
                        selectionMode: "multiple",
                        isAuthRequired: false,
                        isDialog: false, isLookup: false, isDetailsEnabled: false,
                        editor: null
                    });

                this.targetGridViewModel = casimodo.ui.componentRegistry.getById(this._options.targetListId)
                    .vmOnly({
                        $component: this.$view.find(".indylist-target-view").first(),
                        isAuthRequired: false,
                        isDialog: false, isLookup: false, isDetailsEnabled: false,
                        editor: null
                    });

                this.targetGridViewModel.initComponentOptions();
                this.targetGridViewModel.optionsUseLocalDataSource(this._options.localTargetData);
                //if (this._options.isLocalTargetData)
                //    this.targetGridViewModel.optionsSetLocalData(this._options.localTargetData || []);

                this.targetGridViewModel.optionsUseItemRemoveCommand();
                this.targetGridViewModel.on("item-remove-command-fired", function (e) {
                    self.connector.remove(e.item);
                });

                this.connector = new SelectableFromCollectionConnector({
                    keyName: this.keyName,
                    source: this.sourceGridViewModel,
                    target: this.targetGridViewModel
                });
            };

            fn.getViewId = function () {
                return this._options.viewId || this._options.id;
            };

            fn.createComponent = function () {
                if (this._isComponentInitialized)
                    return;
                this._isComponentInitialized = true;

                var self = this;

                this.$view = $("#indylist-editor-view-" + this.getViewId());

                if (this.args.message) {
                    this.$view.find(".indylist-editor-header").append("<span class='indylist-editor-header-message'>" + this.args.message + "</span>");
                }

                this._createViewModels();

                if (this._options.isDialog)
                    this._initComponentAsDialog();
            };

            fn._initComponentAsDialog = function () {
                var self = this;

                // Get dialog arguments and set them on the view model.
                if (!this.args)
                    this.setArgs(casimodo.ui.dialogArgs.consume(this._options.id));

                this._dialogWindow = kendomodo.findKendoWindow(this.$view);
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
                    kendomodo.ui.progress(true);
                    self.save()
                        .then(function () {
                            self.args.buildResult();
                            self.args.isCancelled = false;
                            self.args.isOk = true;

                            self._dialogWindow.close();
                        })
                        .finally(function () {
                            kendomodo.ui.progress(false);
                        });
                });
                // $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", function () {
                $dialogCommands.find('button.cancel-button').first().on("click", function () {
                    self.args.isCancelled = true;
                    self.args.isOk = false;

                    self._dialogWindow.close();
                });
            };

            fn._initDialogWindowTitle = function () {
                var title = "";

                if (this.args.title) {
                    title = this.args.title;
                }
                else {
                    title = this._options.title || "";

                    if (this._options.isLookup)
                        title += " bearbeiten";
                }

                this._dialogWindow.title(title);
            };

            return FormIndyCollectionEditorViewModel;

        })(kendomodo.ui.ComponentViewModel);
        ui.FormIndyCollectionEditorViewModel = FormIndyCollectionEditorViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));