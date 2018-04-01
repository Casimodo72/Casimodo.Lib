"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var GridViewSelectionManager = (function () {

            function GridViewSelectionManager(options) {
                this.gridViewModel = options.gridViewModel;

                this.keyName = this.gridViewModel.keyName;

                // List of selected data item IDs.
                this.selection = [];
                // List of selected data items.
                this._selectionDataItems = [];
                // Infos of currently visible selectors.
                this._iselectors = [];
                // Info of the "select all" selector.
                this._iallSelector = {
                    isInitialized: false,
                    isExisting: false,
                    $selector: null
                };

                this._isSelectorInitializationPending = false;
                this._isSelectorBindingPending = false;

                this._events = new casimodo.EventManager(this);

            }

            var fn = GridViewSelectionManager.prototype;

            fn.on = function (eventName, func) {
                this._events.on(eventName, func);
            };

            fn.trigger = function (eventName, e) {
                this._events.trigger(eventName, e);
            };

            fn.clearSelection = function () {
                var self = this;

                this._performBatchUpdate(function () {
                    self.selection = [];
                    self._selectionDataItems = [];

                    if (self._$allSelector)
                        self._$allSelector.prop("checked", false);

                    self._iselectors.forEach(function (isel) {
                        isel.isSelected = false;
                        isel.$selector.prop("checked", false);
                    });
                });
            };

            fn.getSelectedDataItems = function () {
                return this._selectionDataItems;
            };

            fn.showSelectors = function () {
                if (!this._getIsEnabled())
                    return;

                this._isSelectorsVisible = true;

                if (this._isSelectorInitializationPending)
                    this._initSelectors();

                this.kendoGrid.showColumn(0);
            };

            fn.hideSelectors = function () {
                if (!this._getIsEnabled())
                    return;

                this._isSelectorsVisible = false;

                this.kendoGrid.hideColumn(0);
            };

            fn.deselectedById = function (id) {

                var isel = this._iselectors.find(function (x) { return x.id === id; });
                if (isel && isel.isSelected) {
                    isel.$selector.prop("checked", false).change();
                }

                // Since the data item with the provided ID might not be currently being displayed
                // we need to try to remove it from the selection list.
                this._removeDataItemById(id);
            };

            fn._getIsEnabled = function () {
                return this.gridViewModel.selectionMode === "multiple";
            };

            fn._onDataSourceDataBound = function (e) {
                if (!this._getIsEnabled())
                    return;

                this._isSelectorBindingPending = true;
                this._initSelectors();
            };

            fn._initAllSelector = function () {
                var self = this;

                var isel = this._iallSelector;
                if (isel.isInitialized)
                    return;

                isel.isInitialized = true;

                isel.$selector = this.kendoGrid.thead.find("input.all-list-items-selector").first();
                isel.isExisting = isel.$selector.length === 1;

                if (isel.isExisting) {
                    isel.$selector.on("change", function () {
                        self._onAllSelectorChanged(isel.$selector);
                    });
                }
            }

            fn.reinitializeSelectors = function () {
                this._initSelectors();
            };

            fn._initSelectors = function () {
                var self = this;

                if (!self._getIsEnabled())
                    return;

                if (!self._isSelectorsVisible) {
                    self._isSelectorInitializationPending = true;
                    return;
                }

                self._isSelectorInitializationPending = false;

                self._isUpdatingSelectors = true;
                self._isUpdatingBatch = true;
                try {
                    self._iselectors = [];

                    // The "all items selector" needs to be initialized only once
                    // because it resides in the grid's header and is not destroyed/re-created when new data is fetched.
                    self._initAllSelector();
                    if (self._iallSelector.isExisting) {
                        self._iallSelector.$selector.prop("checked", false);
                    }

                    // Build selector infos.
                    this.kendoGrid.tbody.find("tr[role='row']").each(function () {
                        var $row = $(this);
                        var $selector = $row.find("input.list-item-selector");
                        var $selectorVisual = $row.find("label.list-item-selector");

                        var item = self.kendoGrid.dataItem($row);
                        var id = item[self.keyName];
                        var isSelected = self._getIsSelectedById(id);

                        self._iselectors.push({
                            id: id,
                            item: item,
                            isSelected: isSelected,
                            isEnabled: true,
                            isVisible: true,
                            $row: $row,
                            $selector: $selector,
                            $selectorVisual: $selectorVisual
                        });
                    });

                    self._iselectors.forEach(function (isel) {

                        if (self.customSelectorInitializer) {
                            // Hook for the consumer in order to provide custom logic for the
                            // initial states of a selector such as isSelected, isEnabled and isVisible.
                            var custom = self.customSelectorInitializer(isel);

                            if (typeof custom.isVisible !== "undefined")
                                isel.isVisible = !!custom.isVisible;

                            if (typeof custom.isSelected !== "undefined")
                                isel.isSelected = !!custom.isSelected;

                            if (typeof custom.isEnabled !== "undefined")
                                isel.isEnabled = !!custom.isEnabled;
                        }

                        // Sanity check: we don't want items to be selected if the
                        // selector is not visible since this would lead to confusion.
                        // KABU TODO: Should we also remove from self.selection here?
                        if (isel.isSelected && !isel.isVisible)
                            isel.isSelected = false;

                        if (isel.isSelected)
                            self._addDataItem(isel.item);
                        else
                            self._removeDataItemById(isel.id);

                        // Set selected state.
                        isel.$selector.prop("checked", isel.isSelected);

                        // Set selector visibility.
                        if (isel.isVisible)
                            isel.$selectorVisual.show();
                        else
                            isel.$selectorVisual.hide();

                        if (self._isSelectorBindingPending) {
                            // Listen to selector's change event.
                            isel.$selector.on("change", function () {
                                self._onSelectorChanged(isel);
                            });
                        }
                    });
                }
                finally {
                    self._isUpdatingBatch = false;
                    self._isUpdatingSelectors = false;
                    self._isSelectorBindingPending = false;
                }
            };

            fn._getDataSource = function () {
                return this.gridViewModel.dataSource;
            };

            fn._updateSelectedViewStates = function () {
                var self = this;
                self._iselectors.forEach(function (isel) {

                    isel.isSelected = self._getIsSelectedById(isel.id);
                    isel.$selector.prop("checked", isel.isSelected);
                });
            };

            fn._onAllSelectorChanged = function ($selector) {

                if (this._isUpdatingSelectors)
                    return;

                var self = this;

                this._performBatchUpdate(function () {

                    var add = $selector.prop("checked") === true;

                    var items = self._getDataSource().data();
                    items.forEach(function (item) {
                        if (add)
                            self._addDataItem(item);
                        else
                            self._removeDataItem(item);
                    });
                });
            };

            fn._onSelectorChanged = function (isel) {

                if (this._isUpdatingSelectors)
                    return;

                isel.isSelected = !!isel.$selector.prop("checked");

                if (isel.isSelected)
                    this._addDataItem(isel.item);
                else
                    this._removeDataItem(isel.item);
            };

            fn._performBatchUpdate = function (action) {
                this._isUpdatingSelectors = true;
                this._isUpdatingBatch = true;
                try {

                    action();

                    this._updateSelectedViewStates();
                    this.trigger("selectionChanged", { items: this._selectionDataItems });
                }
                finally {
                    this._isUpdatingBatch = false;
                    this._isUpdatingSelectors = false;
                }
            };

            fn._addDataItem = function (item) {
                // Add item to selection.
                var index = this.selection.indexOf(item[this.keyName]);
                if (index !== -1)
                    return;

                this.selection.push(item[this.keyName]);
                this._selectionDataItems.push(item);

                if (!this._isUpdatingBatch) {
                    // Hand over as non-observable copy.
                    this.trigger("selectionItemAdded", { item: item.toJSON() });
                }
            };

            fn._removeDataItem = function (item) {
                this._removeDataItemById(item[this.keyName]);
            };

            fn._removeDataItemById = function (id) {

                var index = this.selection.indexOf(id);
                if (index === -1)
                    return false;

                var item = this._selectionDataItems[index];

                this.selection.splice(index, 1);
                this._selectionDataItems.splice(index, 1);

                if (!this._isUpdatingBatch) {
                    // Hand over as non-observable copy.
                    this.trigger("selectionItemRemoved", { item: item.toJSON() });
                }
            };

            fn._getIsSelectedById = function (id) {
                return this.selection.indexOf(id) !== -1;
            };

            return GridViewSelectionManager;
        })();
        ui.GridViewSelectionManager = GridViewSelectionManager;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
