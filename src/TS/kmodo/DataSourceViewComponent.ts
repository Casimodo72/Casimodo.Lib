/// <reference path="ViewComponent.ts" />
/// <reference path="../cmodo/ComponentRegistry.ts" />
/// <reference path="../cmodo/Data.ts" />

namespace kmodo {

    export function progress(isbusy: boolean, $el?: JQuery) {
        if (!$el || !$el.length)
            $el = $(window.document.body);

        kendo.ui.progress($el, isbusy);
    }

    export interface DataSourceFilterOptions {
        _filterId?: string;
        field?: string;
        operator?: string;
        value?: any;
        expression?: string;
    }

    export interface DataSourceViewEvent extends ViewComponentEvent {
        sender: DataSourceViewComponent;
        data?: any;
    }

    export interface DataSourceViewOptions extends ViewComponentOptions {
        dataTypeName?: string;
        dataTypeId?: string;
        readQuery?: string;
        dataModel: (e: DataSourceViewEvent) => any;
        extendDataModel?: (e: DataSourceViewEvent) => any;
        dataSourceOptions?: (e: DataSourceViewEvent) => kendo.data.DataSourceOptions;
        transport?: (e: DataSourceViewEvent) => kendo.data.DataSourceTransport;
        isCustomSave?: boolean;

    }

    export abstract class DataSourceViewComponent extends ViewComponent {
        protected _options: DataSourceViewOptions;
        dataSource: kendo.data.DataSource;
        protected _baseFilters: ExtKendoDataSourceFilterItem[];
        private _isAuthFetched: boolean;
        protected selectionMode: string;

        constructor(options: DataSourceViewOptions) {
            super(options);

            var self = this;

            this.keyName = "Id";
            this.dataSource = null;
            this._baseFilters = [];

            this.auth.canView = false;

            this._isAuthFetched = false;

            this.on("clear", function (e) {
                if (self.dataSource)
                    self.dataSource.data([]);

                self.setCurrentItem(null);
            });

            this.on("scopeChanged", function (e) {
                if (e.field === "item")
                    self.onCurrentItemChanged();
            });
        }

        getSelectionMode() {
            return this.selectionMode;
        }

        refresh(): Promise<void> {
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
        }

        items(): kendo.data.ObservableArray {
            if (this.dataSource)
                return this.dataSource.data();

            return new kendo.data.ObservableArray([]);
        }

        setFilter(filter: kendo.data.DataSourceFilter | kendo.data.DataSourceFilter[]): void {
            super.setFilter(filter);
            this._fixFilters(this.filters);
        }

        applyFilter(filter?: kendo.data.DataSourceFilter | kendo.data.DataSourceFilter[]): Promise<void> {
            if (typeof filter !== "undefined")
                this.setFilter(filter);

            if (!this.component) {
                // NOTE: The view model might not have a component at all, 
                // or the component does not have a data source itself.
                this.createView();

                if (this.component)
                    alert("Applying filters before component was created.");
            }

            return this.refresh();
        }

        processNavigation() {
            // NOP
            return this;
        }

        protected setCurrentItem(value) {
            if (this.getModel().get("item") === value)
                return;

            // NOTE: onCurrentItemChanged will be called
            // automatically via _onScopeChanged() whenever scope.item changes.
            this.getModel().set("item", value ? kendo.observable(value) : null);
        }

        getCurrent(): any | null {
            return this.getModel().get("item") || null;
        }

        protected getCurrentItem(): any {
            return this.getModel().get("item");
        }

        private onCurrentItemChanged() {
            if (this.selectionMode === "single") {
                this.trigger("currentItemChanged", { sender: this, item: this.getCurrentItem() });
            }
        }

        protected _startRefresh() {
            kmodo.progress(true, this.$view);
        }

        protected _endRefresh() {
            kmodo.progress(false, this.$view);
        }

        private _fetchAuth(): Promise<void> {
            var self = this;

            var promise = Promise.resolve();

            if (this._isAuthFetched)
                return promise;

            // NOTE: Implicitely required if auth is undefined.
            if (this._options.isAuthRequired === false) {

                this.auth.canView = true;
                this.auth.canCreate = true;
                this.auth.canModify = true;
                this.auth.canDelete = true;

                this._isAuthFetched = true;

                return promise;
            }

            var queries = cmodo.componentRegistry.getById(this._options.id).getAuthQueries();

            return promise
                .then(() => cmodo.getActionAuth(queries))
                .then(function (manager: cmodo.AuthActionManager) {

                    self._isAuthFetched = true;

                    var part = manager.part(self._options.part, self._options.group);
                    self.auth.canView = part.can("View", self._options.role);
                    self.auth.canCreate = part.can("Create", "Editor");
                    self.auth.canModify = part.can("Modify", "Editor");
                    self.auth.canDelete = part.can("Delete", "Editor");

                    self._applyAuth();
                });
        }

        protected _applyAuth() {
            // NOP
        }

        private _filterCore(): Promise<void> {
            var self = this;

            if (!this.auth.canView)
                return Promise.resolve();

            return new Promise((resolve, reject) => {

                if (!self.dataSource) {
                    resolve();
                    return;
                }

                var filters = [];

                var baseFilters = self.getBaseFilters();
                for (let x of baseFilters)
                    filters.push(x);

                var customFilters = self.filters;
                for (let x of customFilters)
                    filters.push(x);

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
        }

        // kendo.data.DataSource ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        createReadQuery(): string {
            return this._options.readQuery || null;
        }

        createDataModel(): any {
            if (!this._options.dataModel)
                return null;

            var dataModel = this._options.dataModel({ sender: this });

            if (this._options.extendDataModel)
                this._options.extendDataModel({ sender: this, data: { model: dataModel } });

            return dataModel;
        }

        protected createDataSourceTransportOptions(): kendo.data.DataSourceTransport {
            if (!this._options.transport)
                return null;

            return this._options.transport({ sender: this });
        }

        protected createDataSourceOptions(): kendo.data.DataSourceOptions {
            if (!this._options.dataSourceOptions)
                return null;

            return this._options.dataSourceOptions({ sender: this });
        }

        createDataSource() {
            if (this.dataSource)
                return this.dataSource;

            var options = this.createDataSourceOptions();

            if (this.filters.length)
                options.filter = { filters: this.filters };

            // Extend the data model with custom computed fields.
            if (this._options.computedFields) {
                for (var prop in this._options.computedFields)
                    options.schema.model[prop] = this._options.computedFields[prop];
            }

            this.extendDataSourceOptions(options);

            this.dataSource = new kendo.data.DataSource(options);

            return this.dataSource;
        }

        protected extendDataSourceOptions(options: kendo.data.DataSourceOptions) {
            // NOP
        }

        /**
            Called by the kendo.DataSource on event "requestStart".
        */
        protected onDataSourceRequestStart(e) {
            if (this._isDebugLogEnabled)
                console.debug("- EDITOR: DS.requestStart: type: '%s'", e.type);
        }

        /**
            Called by the kendo.DataSource on event "requestEnd".
        */
        protected onDataSourceRequestEnd(e) {

            if (typeof e.type === "undefined") {
                // This happens when there was an error.
            }
            else if (e.type === "read") {
                // KABU TODO: IMPORTANT: ASP Web Api returns 200 instead of 401
                //   on unauthorized OData requests. Dunny why and dunny how to fix the ASP side.
                //   Thus we look for e.error as a hopefully temporary workaround.
                if (e.response.error) {
                    cmodo.showError("The server returned an error while trying to read data.");

                    this.dataSource.data([]);
                    return;
                }

                var items = e.response.value;
                if (!items.length)
                    return;

                // Enhance File model.
                cmodo.extendTransportedData(items);
            }
        }

        protected onDataSourceError(e): string {
            if (this._isDebugLogEnabled)
                console.debug("- ds.error");

            // NOTE: On errors the data source will trigger "requestEnd" first and then "error".

            // Displays server errors in the grid's pop up editor.
            // Data source error handler: http://demos.telerik.com/aspnet-mvc/grid/editing-popup
            var message = cmodo.getResponseErrorMessage("odata", e.xhr);

            var result = cmodo._tryCleanupHtml(message);
            if (result.ok)
                message = result.html;

            cmodo.showError(message);

            // KABU TODO: ELIMINATE and move into the view models.
            var $errorBox = $("#validation-errors-box");
            if ($errorBox) {
                $errorBox.empty();
                var template = result.ok
                    ? kendo.template("<li>#=message #</li>")
                    : kendo.template("<li>#:message #</li>");
                $errorBox.append(template({
                    message: message
                }));
            }

            return message;
        }

        public setDataSource(value: kendo.data.DataSource) {
            this.dataSource = value;
        }

        protected initBaseFilters() {
            // NOP
        }

        private getBaseFilters(): ExtKendoDataSourceFilterItem[] {
            return this._baseFilters;
        }

        protected _setBaseFilter(id: string, data: DataSourceFilterOptions): void {

            let filters = this.getBaseFilters();

            var filter = filters.find(x => x._filterId === id);
            if (!filter) {
                filter = {
                    _filterId: id,
                    operator: "eq"
                };
                filters.push(filter);
            }

            if (typeof data.field !== "undefined")
                filter.field = data.field;
            if (typeof data.value !== "undefined")
                filter.value = data.value;
            if (typeof data.expression !== "undefined")
                filter.customExpression = data.expression;
        }

        protected _removeBaseFilter(id: string): void {
            var filters = this.getBaseFilters();
            var idx = filters.findIndex(x => x._filterId === id);
            if (idx !== -1)
                filters.splice(idx, 1);
        }

        protected _hasBaseFilter(id: string): boolean {
            return -1 !== this.getBaseFilters().findIndex(x => x._filterId === id);
        }

        protected _findBaseFilter(id: string): ExtKendoDataSourceFilterItem {
            return this.getBaseFilters().find(x => x._filterId === id);
        }

        // NOTE: Operates only on the first level of filters.
        protected removeFilterByFieldName(fieldName: string): void {
            for (let i = 0; i < this.filters.length; i++) {
                var item = this.filters[i];
                if ((item as any).field === fieldName) {
                    this.filters.splice(i, 1);

                    return;
                }
            }
        }

        private _fixFilters(filters: Array<any>) {
            var item;
            for (let i = 0; i < filters.length; i++) {
                item = filters[i];
                // Set implicit "eq" operator.
                if (typeof item.operator === "undefined" || item.operator === null)
                    item.operator = "eq";
            }
        }
    }
}