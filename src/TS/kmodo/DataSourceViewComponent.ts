namespace kmodo {

    export interface DataSourceViewEvent extends ComponentEvent {
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

    export interface EditorValidationError {
        prop: string;
        message: string;
        showError?: boolean;
    }

    export interface EditorValidationResult {
        valid: boolean;
        errors: EditorValidationError[];
    }

    export abstract class DataSourceViewComponent extends FilterableViewComponent {
        protected _options: DataSourceViewOptions;
        dataSource: kendo.data.DataSource = null;

        private _isAuthFetched = false;
        protected selectionMode: string;

        constructor(options: DataSourceViewOptions) {
            super(options);

            this.keyName = "Id";
            this.auth.canView = false;

            this.on("clear", e => {
                if (this.dataSource)
                    this.dataSource.data([]);

                this.setCurrent(null);
            });

            this.on("scopeChanged", e => {
                if (e.field === "item")
                    this.onCurrentChanged();
            });
        }

        getSelectionMode(): string {
            return this.selectionMode;
        }

        async refresh(): Promise<void> {
            this._ensureViewInitialized();
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

        itemById(id: string): kendo.data.ObservableObject {
            return this.dataSource.get(id);
        }

        itemsAsJSON(): any[] {
            return this.items().map(x => (x as kendo.data.ObservableObject).toJSON());
        }

        protected setCurrent(value: any): void {
            if (this.getCurrent() === value)
                return;

            // NOTE: onCurrentItemChanged will be called
            // automatically via _onScopeChanged() whenever scope.item changes.
            this.getModel().set("item", value ? kendo.observable(value) : null);
        }

        getCurrent(): any {
            // TODO: Think about renaming "item" to "current".
            return this.getModel().get("item") || null;
        }

        private onCurrentChanged(): void {
            if (this.selectionMode === "single") {
                this.trigger("currentChanged", { sender: this, item: this.getCurrent() });
            }
        }

        protected _startRefresh(): void {
            kmodo.progress(true, this.$view);
        }

        protected _endRefresh(): void {
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

            const queries = cmodo.componentRegistry.get(this._options.id).getAuthQueries();

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

        protected _applyAuth(): void {
            // NOP - stub
        }

        private _filterCore(): Promise<void> {
            if (!this.auth.canView)
                return Promise.resolve();

            return new Promise((resolve, reject) => {
                if (!this.dataSource) {
                    resolve();
                    return;
                }

                const filters = this._getEffectiveFilters();

                this.dataSource.one('change', e => {
                    resolve();
                });

                if (filters.length) {
                    // NOTE: This will make the data-source read from the server instantly.
                    //   I.e. no need to call dataSource.read() explicitely, which would result in a superflous request.
                    this.dataSource.filter(filters);
                }
                else {
                    // No filter. Clear currently active filter on the data source.

                    // TODO: REVISIT: Do we want to modify the data source's existing behavior
                    //   w.r.t. filter events?
                    //   See https://www.telerik.com/forums/any-filtering-event
                    //   See https://stackoverflow.com/questions/20446071/i-want-to-display-the-applied-filter-criteria-on-the-kendo-ui-grid

                    if (this._options.isLocalData) {
                        // Clear active filter.
                        // This will automatically trigger a read.
                        this.dataSource.filter([]);
                    }
                    else {
                        const activeFilter = this.dataSource.filter();
                        if (activeFilter && activeFilter.filters && activeFilter.filters.length) {
                            // Clear active filter.
                            // This will automatically trigger a read.
                            this.dataSource.filter([]);
                        }
                        else {
                            // The data source's filter is already empty. Just read.
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
                // NOTE: We define a local data transport in this case.
                //   See https://docs.telerik.com/kendo-ui/framework/datasource/crud#local-or-custom-transport-crud-operations
                return {
                    schema: {
                        model: this.createDataModel()
                    },
                    transport: createLocalDataSourceTransport(this._options.localData || []),
                    pageSize: 20
                }
            } else if (this._options.dataSourceOptions) {
                // Use provided data source options factory.
                return this._options.dataSourceOptions({ sender: this });
            } else {
                // Return default options using data model and transport factories.
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

        createDataSource(): kendo.data.DataSource {
            if (this.dataSource)
                return this.dataSource;

            const dataSourceOptions = this.createDataSourceOptions();

            dataSourceOptions.filter = { filters: this._getEffectiveFilters() };

            // Extend the data model with custom computed fields.
            if (this._options.computedFields) {
                for (const prop in this._options.computedFields)
                    dataSourceOptions.schema.model[prop] = this._options.computedFields[prop];
            }

            this.extendDataSourceOptions(dataSourceOptions);

            this.dataSource = new kendo.data.DataSource(dataSourceOptions);

            return this.dataSource;
        }

        protected extendDataSourceOptions(options: kendo.data.DataSourceOptions): void {
            // NOP
        }

        /**
            Called by the kendo.DataSource on event "requestStart".
        */
        protected onDataSourceRequestStart(e: kendo.data.DataSourceRequestStartEvent): void {
            if (this._isDebugLogEnabled)
                console.debug("- DS.requestStart: type: '%s'", e.type);
        }

        /**
            Called by the kendo.DataSource on event "requestEnd".
        */
        protected onDataSourceRequestEnd(e: kendo.data.DataSourceRequestEndEvent): void {
            if (typeof e.type === "undefined") {
                // This happens when there was an error.
            } else if (e.type === "read") {
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

            this._showErrors([{ prop: "", message: message }]);

            return message;
        }

        protected _showErrors(errors: EditorValidationError[]): void {
            if (!errors || !errors.length)
                return;

            // Try finding local validaton summary.
            let $errorBox = this.$view.find(".km-form-validation-summary").first();
            if (!$errorBox.length) {
                // Try finding ancestor validaton summary.
                $errorBox = this.$view.closest(".km-form-validation-summary");
            }
            if (!$errorBox.length)
                return;

            $errorBox.empty();

            if (!errors.length) {
                $errorBox.hide(100);
                return;
            }

            // NOTE: we are using kendo's template here because the message could
            //  originate from the server and thus be HTML. Although I now adjusted
            //  the server to not return HTML. Keep this though.
            const template = kendo.template("<li>#=message #</li>");

            for (const error of errors) {
                $errorBox.append(template({
                    message: error.prop ? `${error.prop}: ${error.message}` : error.message
                }));
            }

            $errorBox.show(100);
        }

        protected _addCommandFilter(cmd: ComponentCommand): void {
            // NOTE: command filters are treated as external filters.
            this.filter.setCoreNode(cmd.filter._id, cmd.filter);
        }

        protected _removeCommandFilter(cmd: ComponentCommand): void {
            // NOTE: command filters are treated as external filters.
            this.filter.removeCoreNode(cmd.filter._id);
        }
    }
}
