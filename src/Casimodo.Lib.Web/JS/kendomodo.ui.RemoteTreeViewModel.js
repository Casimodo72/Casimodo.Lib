"use strict";
var kendomodo;
(function (kendomodo) {
    (function (ui) {

        ui.RemoteTreeViewModel = (function () {

            var RemoteTreeViewModel = function ($tree, options) {
                this.$tree = $tree;
                this.options = options;
                this.schema = options.schema;
                this.dataSource = null;
                this._node = null;
                this._events = new casimodo.EventManager(this);

                // Init events.
                if (this.options.onCurrentItemChanging)
                    this.on("currentItemChanging", this.options.onCurrentItemChanging);

                // Components
                this.tree = null;

                this._initComponent();
            };

            var fn = RemoteTreeViewModel.prototype;

            fn.onNodeSelected = function (node) {
                // NOP
            };

            fn._saveTreeViewState = function () {
                var self = this;

                var state = {
                    selectedKey: null,
                    expandedKeys: []
                };

                var item = this.select();
                if (item)
                    state.selectedKey = item.Id;

                this.tree.wrapper.find(".k-item").each(function () {
                    var item = self.tree.dataItem(this);
                    if (item.expanded) {
                        state.expandedKeys.push(item.Id);
                    }
                });

                return state;
            };

            fn._restoreTreeViewState = function (state) {
                var self = this;

                // KABU TODO: Do we really need to disable animations here?
                this.tree.setOptions({ animation: false });

                // Expand nodes
                this.tree.expandPath(state.expandedKeys, function () {

                    // Restore animation
                    self.tree.setOptions({ animation: kendo.ui.TreeView.fn.options.animation });

                    // Re-select last selected node.
                    if (state.selectedKey) {
                        var item = self.tree.dataSource.get(state.selectedKey);
                        if (item)
                            self.tree.select(self.tree.findByUid(item.uid));
                    }
                });
            };

            fn.refresh = function () {
                var self = this;
                var state = this._saveTreeViewState();

                this.tree.dataSource.read().then(function () {
                    self._restoreTreeViewState(state);
                });
            };

            fn.select = function () {
                return this.tree.dataItem(this._node);
            };

            fn.createDataSource = function (items) {
                var self = this;
                var dataSourceOptions = {
                    type: "odata-v4",
                    transport: {
                        read: {
                            url: self.options.url
                        }
                    },
                    schema: {
                        model: self.options.model,
                        children: dataSourceOptions
                    },
                    serverFiltering: true,
                    serverPaging: true,
                    serverSorting: true
                };

                this.dataSource = new kendo.data.HierarchicalDataSource(dataSourceOptions);

                return this.dataSource;
            };

            fn._initComponent = function () {
                var self = this;

                this.tree = this.$tree.kendoTreeView({
                    template: self.options.template || null,
                    select: function (e) {
                        self.trigger("currentItemChanging", e);

                        if (e.defaultPrevented)
                            return;

                        self._node = e.node;
                        self.trigger("currentItemChanged");

                    },
                    dataTextField: self.options.model.text,
                    dataSource: self.createDataSource()
                }).data("kendoTreeView");
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

            return RemoteTreeViewModel;
        })();

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
