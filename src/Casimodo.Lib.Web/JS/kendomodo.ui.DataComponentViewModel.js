"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {
        var DataComponentViewModel = (function (_super) {
            casimodo.__extends(DataComponentViewModel, _super);

            function DataComponentViewModel(options) {
                _super.call(this, options);

                this.dataSource = null;
                this.dataSourceOptions = null;

                this.auth.canView = false;

                this._isAuthFetched = false;

                this.on("clear", function (e) {
                    if (this.dataSource)
                        this.dataSource.data([]);

                    this.setCurrentItem(null);
                });

                this.on("scopeChanged", function (e) {
                    if (e.field === "item")
                        this.onCurrentItemChanged();
                });
            }

            var fn = DataComponentViewModel.prototype;

            // Current item ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            fn.setCurrentItem = function (value) {
                if (this.scope.item === value)
                    return;

                // NOTE: onCurrentItemChanged will be called
                // automatically via _onScopeChanged() whenever scope.item changes.
                this.scope.set("item", value ? kendo.observable(value) : null);
            };

            fn.getCurrentItem = function () {
                return this.scope.item;
            };

            fn.onCurrentItemChanged = function () {
                if (this.selectionMode === "single") {
                    this.trigger("currentItemChanged", { sender: this, item: this.getCurrentItem() });
                }
            };

            // Data load ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            fn.processNavigation = function () {
                // NOP
                return this;
            };

            fn.refresh = function () {
                var self = this;

                return new Promise(function (resolve, reject) {
                    self._startRefresh();
                    resolve();
                })
                    .then(() => self._fetchAuth())
                    .then(() => self._filterCore())
                    .finally(() => {
                        self._endRefresh();
                    });
            };

            fn._startRefresh = function () {
                kendomodo.ui.progress(true, this.$view);
            };

            fn._endRefresh = function () {
                kendomodo.ui.progress(false, this.$view);
            };

            fn._fetchAuth = function () {
                var self = this;

                var promise = Promise.resolve();

                if (this._isAuthFetched)
                    return promise;

                // NOTE: Implicitely required if auth is undefined.
                if (this._options.isAuthRequired === false) {

                    this.auth.canCreate = true;
                    this.auth.canModify = true;
                    this.auth.canDelete = true;

                    this._isAuthFetched = true;

                    return promise;
                }

                var queries = casimodo.ui.componentRegistry.getById(this._options.id).getAuthQueries();

                return promise
                    .then(() => casimodo.getActionAuth(queries))
                    .then(function (manager) {

                        self._isAuthFetched = true;

                        var part = manager.part(self._options.part, self._options.group);
                        self.auth.canView = part.can("View", self._options.role);
                        self.auth.canCreate = part.can("Create", "Editor");
                        self.auth.canModify = part.can("Modify", "Editor");
                        self.auth.canDelete = part.can("Delete", "Editor");

                        self._applyAuth();
                    });
            };

            fn._applyAuth = function () {
                // NOP
            };

            fn._filterCore = function () {
                var self = this;

                if (!this.auth.canView)
                    return Promise.resolve();

                return new Promise((resolve, reject) => {

                    if (!self.dataSource) {
                        resolve();
                        return;
                    }

                    var filters = [];
                    var i;
                    var baseFilters = self.getBaseFilters();
                    for (i = 0; i < baseFilters.length; i++)
                        filters.push(baseFilters[i]);

                    var customFilters = self.filters || [];
                    for (i = 0; i < customFilters.length; i++)
                        filters.push(customFilters[i]);

                    self.dataSource.one('change', function (e) {
                        resolve();
                    });

                    if (filters.length) {
                        // NOTE: This will make the data-source read from the server instantly.
                        //   I.e. no need to call dataSource.read() explicitely, which would result in a superflous request.
                        self.dataSource.filter(filters);
                    }
                    else {
                        // The VM has an empty filter. Clear any filter on the data source.
                        var activeFilter = self.dataSource.filter();
                        if (activeFilter && activeFilter.filters && activeFilter.filters.length) {
                            // Clear active filter.
                            self.dataSource.filter([]);
                        }
                        else {
                            // The data source's filter is alreay empty. Just read.
                            self.dataSource.read();
                        }
                    }
                });
            };

            // kendo.data.DataSource ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            fn.createDataSource = function () {
                if (this.dataSource)
                    return this.dataSource;

                var options = this.createDataSourceOptions();

                if (this.filters.length)
                    options.filter = { filters: this.filters };

                // Extend with custom data model fields.
                if (this._options.fields) {
                    for (var prop in this._options.fields)
                        options.schema.model.fields[prop] = this._options.fields[prop];
                }

                this.extendDataSourceOptions(options);

                this.dataSource = new kendo.data.DataSource(options);

                return this.dataSource;
            };

            fn.extendDataSourceOptions = function (options) {
                // NOP
            };

            /**
                Called by the kendo.DataSource on event "requestEnd".
            */
            fn.onDataSourceRequestEnd = function (e) {

                if (typeof e.type === "undefined") {
                    // This happens when there was an error.
                }
                else if (e.type === "read") {
                    // KABU TODO: IMPORTANT: ASP Web Api returns 200 instead of 401
                    //   on unauthorized OData requests. Dunny why and dunny how to fix the ASP side.
                    //   Thus we look for e.error as a hopefully temporary workaround.
                    if (e.response.error) {
                        alert("The server returned an error while trying to read data.");

                        this.dataSource.data([]);
                        return;
                    }

                    var items = e.response.value;
                    if (!items.length)
                        return;

                    // Enhance File model.
                    kendomodo.extendDisplayableMoFiles(items);
                }
            };

            fn.onDataSourceError = function (e) {
                if (this._isDebugLogEnabled)
                    console.debug("- ds.error");

                // NOTE: On errors the data source will trigger "requestEnd" first and then "error".

                // Displays server errors in the grid's pop up editor.
                // Data source error handler: http://demos.telerik.com/aspnet-mvc/grid/editing-popup
                return kendomodo.onServerErrorOData(e);
            };

            fn.setDataSource = function (value) {
                this.dataSource = value;
            };

            fn.setFilter = function (filters) {
                this.filters = filters || [];
                fixFilters(this.filters);
            };

            fn.getBaseFilters = function () {
                return [];
            };

            // NOTE: Operates only on the first level of filters.
            fn.removeFilterByFieldName = function (fieldName) {
                for (var i = 0; i < this.filters.length; i++) {
                    var item = this.filters[i];
                    if (item.field === fieldName) {
                        this.filters.splice(i, 1);

                        return;
                    }
                }
            };

            function fixFilters(filters) {
                var item;
                for (var i = 0; i < filters.length; i++) {
                    item = filters[i];
                    // Set implicit "eq" operator.
                    if (typeof item.operator === "undefined" || item.operator === null)
                        item.operator = "eq";
                }
            }

            fn.applyFilter = function (filters) {
                if (typeof filters !== "undefined")
                    this.setFilter(filters);

                if (!this.component) {
                    // NOTE: The view model might not have a component at all, 
                    // or the component does not have a data source itself.
                    this.createComponent();

                    if (this.component)
                        alert("Applying filters before component was created.");
                }

                this.refresh();
            };

            fn.items = function () {
                if (this.dataSource)
                    return this.dataSource.data();

                return new kendo.data.ObservableArray([]);
            };

            return DataComponentViewModel;

        })(kendomodo.ui.ComponentViewModel);
        ui.DataComponentViewModel = DataComponentViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));