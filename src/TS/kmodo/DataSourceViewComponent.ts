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
        isLocalData?: boolean;
        localData?: any[];
        readQuery?: string;
        dataModel?: (e: DataSourceViewEvent) => kendo.data.DataSourceSchemaModelWithFieldsObject;
        extendDataModel?: (e: DataSourceViewEvent) => any;
        dataSourceOptions?: (e: DataSourceViewEvent) => kendo.data.DataSourceOptions;
        transport?: (e: DataSourceViewEvent) => kendo.data.DataSourceTransport;
        isCustomSave?: boolean;

    }

    export abstract class DataSourceViewComponent extends ViewComponent {
        protected _options: DataSourceViewOptions;
        dataSource: kendo.data.DataSource;
        protected _baseFilters: ExtKendoDataSourceFilterItem[];
        private _isAuthFetched = false;
        protected selectionMode: string;

        constructor(options: DataSourceViewOptions) {
            super(options);

            this.keyName = "Id";
            this.dataSource = null;
            this._baseFilters = [];
            this.auth.canView = false;

            this.on("clear", e => {
                if (this.dataSource)
                    this.dataSource.data([]);

                this.setCurrentItem(null);
            });

            this.on("scopeChanged", e => {
                if (e.field === "item")
                    this.onCurrentItemChanged();
            });
        }

        getSelectionMode() {
            return this.selectionMode;
        }

        async refresh(): Promise<void> {
            this._startRefresh();
            try {
                await this._fetchAuth();
                await this._filterCore();
            } finally {
                this._endRefresh();
            }
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

            if (!this._isComponentInitialized) {
                // NOTE: The view model might not have a component at all, 
                // or the component does not have a data source itself.
                this.createView();

                if (this._isComponentInitialized)
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
            const promise = Promise.resolve();

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

            const queries = cmodo.componentRegistry.getById(this._options.id).getAuthQueries();

            return promise
                .then(() => cmodo.getActionAuth(queries))
                .then((manager: cmodo.AuthActionManager) => {
                    this._isAuthFetched = true;

                    const part = manager.part(this._options.part, this._options.group);
                    this.auth.canView = part.can("View", this._options.role);
                    this.auth.canCreate = part.can("Create", "Editor");
                    this.auth.canModify = part.can("Modify", "Editor");
                    this.auth.canDelete = part.can("Delete", "Editor");

                    this._applyAuth();
                });
        }

        protected _applyAuth() {
            // NOP
        }

        private _filterCore(): Promise<void> {
            if (!this.auth.canView)
                return Promise.resolve();

            return new Promise((resolve, reject) => {
                if (!this.dataSource) {
                    resolve();
                    return;
                }

                const filters = [];

                const baseFilters = this.getBaseFilters();
                for (const x of baseFilters)
                    filters.push(x);

                const customFilters = this.filters;
                for (const x of customFilters)
                    filters.push(x);

                this.dataSource.one('change', e => {
                    resolve();
                });

                if (filters.length) {
                    // NOTE: This will make the data-source read from the server instantly.
                    //   I.e. no need to call dataSource.read() explicitely, which would result in a superflous request.
                    this.dataSource.filter(filters);
                }
                else {
                    if (this._options.isLocalData) {
                        // Clear active filter.
                        this.dataSource.filter([]);
                    }
                    else {
                        // The VM has an empty filter. Clear any filter on the data source.
                        const activeFilter = this.dataSource.filter();
                        if (activeFilter && activeFilter.filters && activeFilter.filters.length) {
                            // Clear active filter.
                            this.dataSource.filter([]);
                        }
                        else {
                            // The data source's filter is alreay empty. Just read.
                            this.dataSource.read();
                        }
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

            const dataModel = this._options.dataModel({ sender: this });

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
            if (this._options.isLocalData) {
                // Return local data data source options.
                // TODO: IMPORTANT: We have to define local data transport.
                //   See https://docs.telerik.com/kendo-ui/framework/datasource/crud#local-or-custom-transport-crud-operations
                return {
                    schema: {
                        model: this.createDataModel()
                    },
                    transport: createLocalDataSourceTransport(this._options.localData || []),
                    // data: this._options.localData || [],
                    pageSize: 20
                }
            } else if (this._options.dataSourceOptions) {
                // Try to use data source options factory.
                return this._options.dataSourceOptions({ sender: this });
            } else {
                // Return default options + data model and transport factories.
                return {
                    type: 'odata-v4',
                    schema: {
                        model: this.createDataModel()
                    },
                    transport: this.createDataSourceTransportOptions(),
                    // Use max 20 items per page.
                    pageSize: 20,
                    serverPaging: true,
                    serverSorting: true,
                    serverFiltering: true,
                };
            }
        }

        // TODO: REMOVE
        /*
        private _initDataSourceOptionsAsLocal(dsopts: kendo.data.DataSourceOptions): kendo.data.DataSourceOptions {
            if (!this._options.isLocalData)
                return dsopts;
 
            // Modify data source related options.
            delete dsopts.type;
            delete dsopts.transport;
            delete dsopts.serverPaging;
            delete dsopts.serverSorting;
            delete dsopts.serverFiltering;
            dsopts.data = this._options.localData || [];
 
            return dsopts;
        }
        */

        createDataSource(): kendo.data.DataSource {
            if (this.dataSource)
                return this.dataSource;

            const options = this.createDataSourceOptions();

            if (this.filters.length)
                options.filter = { filters: this.filters };

            // Extend the data model with custom computed fields.
            if (this._options.computedFields) {
                for (const prop in this._options.computedFields)
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
        protected onDataSourceRequestStart(e: kendo.data.DataSourceRequestStartEvent) {
            if (this._isDebugLogEnabled)
                console.debug("- DS.requestStart: type: '%s'", e.type);
        }

        /**
            Called by the kendo.DataSource on event "requestEnd".
        */
        protected onDataSourceRequestEnd(e: kendo.data.DataSourceRequestEndEvent) {

            if (typeof e.type === "undefined") {
                // This happens when there was an error.
            }
            else if (e.type === "read") {
                if (this._options.isLocalData) {
                    // NOTE: In case of local data the e.response will be the data array.
                    // NOP
                } else {
                    // KABU TODO: IMPORTANT: ASP Web Api returns 200 instead of 401
                    //   on unauthorized OData requests. Dunny why and dunny how to fix the ASP side.
                    //   Thus we look for e.error as a hopefully temporary workaround.
                    if (e.response.error) {
                        cmodo.showError("The server returned an error while trying to read data.");

                        this.dataSource.data([]);
                        return;
                    }

                    const items = e.response.value;
                    if (!items.length)
                        return;

                    // Enhance model (e.g. the file model).
                    cmodo.extendTransportedData(items);
                }
            }
        }

        protected onDataSourceError(e: kendo.data.DataSourceErrorEvent): string {
            // DataSource error event: https://docs.telerik.com/kendo-ui/api/javascript/data/datasource/events/error

            if (this._isDebugLogEnabled)
                console.debug("- DS.error");

            // NOTE: On errors the Kendo DataSource will trigger "requestEnd" first and then "error".

            // Data source error handler: http://demos.telerik.com/aspnet-mvc/grid/editing-popup

            const message = cmodo.getODataErrorMessageFromJQueryXHR(e.xhr);
            cmodo.showError(message);

            let $errorBox = this.$view.find(".km-form-validation-summary").first();
            if (!$errorBox.length)
                $errorBox = this.$view.closest(".km-form-validation-summary");
            if ($errorBox.length) {
                $errorBox.empty();
                const template = kendo.template("<li>#=message #</li>");
                $errorBox.append(template({
                    message: message
                }));
                $errorBox.show(100);
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
            const filters = this.getBaseFilters();

            let filter = filters.find(x => x._filterId === id);
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
            const filters = this.getBaseFilters();
            const idx = filters.findIndex(x => x._filterId === id);
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
                const item = this.filters[i];
                if ((item as any).field === fieldName) {
                    this.filters.splice(i, 1);

                    return;
                }
            }
        }

        private _fixFilters(filters: Array<any>) {
            for (const item of filters) {
                // Set implicit "eq" operator.
                if (typeof item.operator === "undefined" || item.operator === null)
                    item.operator = "eq";
            }
        }
    }
}