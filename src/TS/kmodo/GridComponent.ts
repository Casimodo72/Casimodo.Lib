namespace kmodo {
    // kendo.ui.Grid:    https://docs.telerik.com/kendo-ui/api/javascript/ui/grid
    // kendo.DataSource: https://docs.telerik.com/kendo-ui/api/javascript/data/datasource      

    // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    export function foreachGridRow(grid: kendo.ui.Grid, action: ($row: JQuery, item: any) => void) {

        grid.tbody.find("tr[role=row]").each((idx, elem) => {
            let $row = $(elem);
            action($row, grid.dataItem($row));
        });
    }

    interface InternalGridState {
        gridDataBindAction: string;
        lastCurrentId: string;
        gridLastScrollTop: number;
        isRestoringExpandedRows: boolean;
        isEditing: boolean;
    }

    export interface GridOptions extends DataSourceViewOptions {
        title?: string;
        selectionMode?: string;
        hasRowContextMenu?: boolean;
        isRowAlwaysExpanded?: boolean;
        isDetailsEnabled?: boolean;
        tagsEditorId?: string;
        filters?: DataSourceFilterNode[],
        editor?: EditorInfo;
        isTaggable?: boolean;
        isTagsFilterEnabled?: boolean;
        $component?: JQuery;
        useRemoveCommand?: boolean;
        canModify?: boolean;
        canCreate?: boolean;
        bindRow?: boolean | Function;
        gridOptions?: (e: DataSourceViewEvent) => kendo.ui.GridOptions;
        companyId?: string;
    }

    export interface GridEvent extends DataSourceViewEvent {
        sender: Grid;
    }

    export interface GridCommand extends GenericComponentCommand<Grid, GridEvent> { }

    interface IColumnInfo {
        column: kendo.ui.GridColumn;
        index: number;
    }

    interface IRowColumnsInfo {
        [name: string]: IColumnInfo;
    }

    export class Grid extends DataSourceViewComponent {
        public readonly selectionManager: GridSelectionManager;
        private _kendoGrid: kendo.ui.Grid;
        private _$commandBox: JQuery;
        protected _options: GridOptions;
        private expandedKeys: string[];
        private _filterCommands: CustomFilterCommandInfo[] = [];
        private _commands: GridCommand[];
        private _rowCommands: GridCommand[];
        private _state: InternalGridState;
        private _isItemRemoveCommandEnabled: boolean;
        private _$toolbar: JQuery;
        private _tagsFilterSelector: kendo.ui.MultiSelect;
        private _dialogWindow: kendo.ui.Window = null;
        // NOTE: createComponentOptionsOverride can be provided via view args.
        private createComponentOptionsOverride: (options: kendo.ui.GridOptions) => kendo.ui.GridOptions;

        constructor(options: GridOptions) {
            super(options);

            if (this._options.canModify === undefined)
                this._options.canModify = true;

            if (this._options.canCreate === undefined)
                this._options.canCreate = true;

            const componentArgs = cmodo.componentArgs.consume(this._options.id);
            if (componentArgs) {
                if (componentArgs.createComponentOptionsOverride)
                    this.createComponentOptionsOverride = componentArgs.createComponentOptionsOverride;
            }

            this.expandedKeys = [];

            this.selectionMode = options.selectionMode || "single";

            this._commands = [];
            this._rowCommands = [];

            this._state = {
                gridDataBindAction: null,
                lastCurrentId: null,
                gridLastScrollTop: null,
                isRestoringExpandedRows: false,
                isEditing: false
            };

            this.selectionManager = new GridSelectionManager({
                grid: this
            });

            if (this.selectionMode === "multiple") {
                this.on("dataBound", e => this.selectionManager._onDataSourceDataBound(e));
            }

            this._tagsFilterSelector = null;
        }

        private createGridOptions(): kendo.ui.GridOptions {
            if (!this._options.gridOptions)
                return null;

            return this._options.gridOptions({ sender: this });
        }

        // override
        protected extendDataSourceOptions(options: kendo.data.DataSourceOptions) {
            // Attach event handlers.
            options.error = e => this.onDataSourceError(e);
            options.requestEnd = e => this.onDataSourceRequestEnd(e);
        }

        private grid(): kendo.ui.Grid {
            return this._kendoGrid;
        }

        getKendoGrid(): kendo.ui.Grid {
            return this._kendoGrid;
        }

        // override
        protected _startRefresh() {
            // NOP. Don't use busy indicator because that's already done by the kendo grid.
        }

        // override
        protected _endRefresh() {
            // NOP. Don't use busy indicator because that's already done by the kendo grid.
        }

        refreshAndTrySetCurrentById(id) {
            this.refresh()
                .then(() => {
                    if (id)
                        this._trySetCurrentItemById(id);
                });
        }

        /**
            Handler for kendo.ui.Grid's event "dataBinding".
            @param {any} e - Event 
        */
        private onComponentDataBinding(e) {
            if (this._isDebugLogEnabled)
                console.debug("- data binding: action: '%s'", e.action)

            this._state.gridDataBindAction = e.action;

            if (e.action === "rebind") {
                // This is a refresh.

                if (this.grid().options.scrollable) {
                    // Compute current scroll top position. This will be restored after the refresh.
                    const content = this.grid().content;
                    this._state.gridLastScrollTop = content.length ? content[0].scrollTop : null;
                }

                // Save current item. It will be restored after the refresh.
                const item = this.getCurrent();
                this._state.lastCurrentId = item ? item[this.keyName] : null;;
                this.setCurrent(null);
            }

            this.trigger('dataBinding', e);
        }

        /**
            Handler for kendo.ui.Grid's event "dataBound".
            @param {any} e - Event 
        */
        private onComponentDataBound(e) {
            if (this._isDebugLogEnabled)
                console.debug("- data bound");

            const grid = this.grid();

            const action = this._state.gridDataBindAction;

            // Autofit columns.
            // TODO: Won't work if cols are rearranged by the user.
            if (this._autofitColIndexes.length) {
                for (const icol of this._autofitColIndexes) {
                    grid.autoFitColumn(icol);
                }
            }

            this._initGridRows();

            if (action === "rebind" && this._state.lastCurrentId !== null) {

                // This is a refresh. Restore last current item.
                try {
                    this._trySetCurrentItemById(this._state.lastCurrentId);
                }
                finally {
                    this._state.lastCurrentId = null;
                }
            }

            // Restore expanded inline details.
            if (this.expandedKeys && this.expandedKeys.length) {
                this._state.isRestoringExpandedRows = true;
                try {
                    this.expandedKeys = this._restoreExpandedRows(e, this.expandedKeys, this.keyName);
                }
                finally {
                    this._state.isRestoringExpandedRows = false;
                }
            }

            if (action === "rebind" && this._state.gridLastScrollTop !== null) {
                // If the view is scrollable then restore the last scroll position
                // after the grid has been refreshed. Otherwise we would always
                // jump to the top of the grid after a refresh.

                grid.content[0].scrollTop = this._state.gridLastScrollTop;
            }

            this.trigger('dataBound', e);
        }

        trySetCurrentById(id: string): boolean {
            return this._trySetCurrentItemById(id);
        }

        private _trySetCurrentItemById(id: string): boolean {
            let item = this.getCurrent();
            if (item && item[this.keyName] === id)
                // This item is already the current item.
                return true;

            item = this.dataSource.view().find(x => x[this.keyName] === id);
            if (item) {
                // This will set the current item automatically via the grid's "change" event.
                this._gridSelectByItem(item);

                return true;
            }

            // Clear current item if the specified item is not in the view.
            this.grid().clearSelection();

            return false;
        }

        gridSelectByItem(item: any): void {
            this._gridSelectByItem(item);
        }

        private _gridSelectByItem(item: any): void {
            this.grid().select(this._gridGetRowByItem(item));
        }

        private _gridGetRowByItem(item: any): JQuery {
            return this.grid().tbody.find("tr[role=row][data-uid='" + item.uid + "']");
        }

        private _gridSelectByRow($row: JQuery): void {
            this.grid().select($row);
        }

        private getItemByRow($row: JQuery): kendo.data.ObservableObject {
            return this.grid().dataItem($row);
        }

        /**
            Handler for kendo.ui.Grid's event "change".
            Triggered when the grid's row(s) selection changed.
            @param {any} e - Event 
        */
        private onComponentChanged(e): void {
            if (this._isDebugLogEnabled)
                console.debug("- changed");

            this.trigger('changed', e);

            if (this.selectionMode === "single")
                this.setCurrent(this.grid().dataItem(this.grid().select()));
        }

        /**
            Handler for kendo.ui.Grid's event "detailInit".
        */
        private onComponentDetailInit(e) {
            // This must be performed, otherwise Kendo grid inline detail template bindings will not work.
            // See http://jsfiddle.net/jeastburn/5MU4r/
            kendo.bind(e.detailCell, e.data);

            this.trigger('detailInit', e);
        }

        /**
            Handler for kendo.ui.Grid's event "detailExpand".
        */
        private onComponentDetailExpanding(e): void {
            // TODO: Animation doesn't work, although advertised by Telerik.
            // let detailRow = e.detailRow;
            // kendo.fx(detailRow).fadeIn().play();               

            if (!this._state.isRestoringExpandedRows) {

                const id = this.grid().dataItem(e.masterRow)[this.keyName];
                if (id) {
                    this.expandedKeys.push(id);
                }
            }

            this.trigger('detailExpanding', e);
        }

        /**
            Handler for kendo.ui.Grid's event "detailCollapse".
        */
        private onComponentDetailCollapsing(e): void {
            if (!this._state.isRestoringExpandedRows) {
                const id = this.grid().dataItem(e.masterRow)[this.keyName];
                const idx = this.expandedKeys.indexOf(id);
                if (idx !== -1)
                    this.expandedKeys.splice(idx, 1);
            }

            this.trigger('detailCollapsing', e);
        }

        cancelChanges(model?: any): void {
            if (model)
                this.dataSource.cancelChanges(model);
            else
                this.getKendoGrid().cancelChanges();
        }

        /**
            Restores previously expanded rows.
            See http://www.telerik.com/forums/saving-expanded-detail-rows
        */
        private _restoreExpandedRows(e, expandedKeys: string[], keyname: string): string[] {
            if (!expandedKeys || !expandedKeys.length) return [];

            const restored: string[] = [];

            this.foreachGridRow(($row, item) => {
                const id: string = item[keyname];

                if (expandedKeys.indexOf(id) !== -1) {
                    this.grid().expandRow($row);
                    restored.push(id);
                }
            });

            return restored;
        }

        foreachGridRow(action: ($row: JQuery, item: any) => void): void {
            this.grid().tbody.find("tr[role=row]").each((idx, elem) => {
                const $row = $(elem);
                action($row, this.grid().dataItem($row));
            });
        }

        private _initGridRows(): void {
            const grid = this._kendoGrid;

            const styledColumnsInfo: IRowColumnsInfo = {};
            for (const col of grid.options.columns) {
                if (col.hidden)
                    continue;

                // Only columns with custom style.
                if (!(col as any).style)
                    continue;

                const colIndex = this._getColIndex(col.field);
                if (colIndex === -1)
                    continue;

                styledColumnsInfo[col.field] = {
                    column: col,
                    index: colIndex
                };
            }

            this.foreachGridRow(($row, item) => {
                this._initRowExpander(item, $row);
                this._initRowEditing(item, $row);
                this._initRowImageCells(item, $row);

                if (this._options.bindRow) {
                    if (typeof this._options.bindRow === "function")
                        this._options.bindRow({
                            sender: this,
                            data: { $row: $row, item: item }
                        });
                    else
                        kendo.bind($row, item);
                }

                const $cells = $row.children("td");

                for (const colName in styledColumnsInfo) {
                    const icol = styledColumnsInfo[colName];

                    const style = (icol.column as any).style;
                    if (!style)
                        continue;

                    const $cell = $cells.eq(icol.index);
                    if (style.color) {
                        $cell.css("color", style.color);
                    }
                }
            });

            if (this._options.isRowAlwaysExpanded) {
                // Expand all row detail views.
                this.grid().wrapper.find(".k-hierarchy-cell").empty().hide();
                this.grid().wrapper.find(".k-detail-row > td:first-child").hide();
            }
        }

        private _getColIndex(name: string): number {
            return this.grid().element.find("th[data-field = '" + name + "']").index() || -1;
        }

        private _initRowEditing(item, $row: JQuery): void {
            const $btn = $row.find(".k-grid-custom-edit").first();
            if (!$btn.length)
                return;

            // Editing is supported only for the "single" selectionMode.
            if (this.selectionMode === "single" && this._canModifyItem(item))
                $btn.show();
            else
                $btn.remove();
        }

        private _canCreate(): boolean {
            return this._options.canCreate && this.auth.canCreate;
        }

        private _canModifyItem(item): boolean {
            return item && this._options.canModify && this.auth.canModify === true && item.IsReadOnly === false;
        }

        private _initRowImageCells(item, $row: JQuery): void {
            // TODO: DISABLED - REMOVE? Not used. Was intended for "PhotoCellTemplate".
            //   But currently we don't display images anymore in the grid.
            //   KEEP: maybe we can use this in the future.

            $row.find(".kendomodo-button-show-image").each((idx, elem) => {
                const $btn = $(elem);
                const uri = $btn.data("file-uri");

                // NOTE: Using magnific popup lib.
                ($btn as any).magnificPopup({
                    items: {
                        src: uri
                    },
                    type: 'image', // this is default type 

                    removalDelay: 300,

                    // Class that is added to popup wrapper and background
                    // make it unique to apply your CSS animations just to this exact popup
                    mainClass: 'mfp-fade',
                    preloader: false,
                    closeOnContentClick: true
                });
            });
        }

        private _initRowExpander(item, $row: JQuery): void {
            if (!this._options.isRowAlwaysExpanded)
                return;

            if (!$row.hasClass("k-master-row"))
                return;

            this.grid().expandRow($row);
        }

        // overwrite
        protected _applyAuth(): void {
            // Show "add" button in toolbar based on authorization.
            // See http://www.telerik.com/forums/disable-toolbar-button-on-kendo-ui-grid
            this._initAddCommand(this._canCreate());
        }

        private _initAddCommand(activate: boolean): void {
            // TODO: REMOVE: this.grid().element.find('.k-grid-toolbar .k-grid-add').removeClass("hide");

            const $btn = this._$toolbar.find(".k-grid-custom-add");
            if (!$btn.length)
                return;

            if (activate) {
                $btn.removeClass("hide");
                $btn.on("click", e => {
                    if (this._state.isEditing)
                        return false;

                    this.add();

                    return false;
                });
            } else {
                $btn.remove();
            }
        }

        private _initRefreshCommand(activate: boolean): void {
            const $btn = this._$toolbar.find(".k-grid-refresh");
            if (!$btn.length)
                return;

            if (activate) {
                $btn.on("click", e => {
                    this.refresh();
                });
            } else {
                $btn.remove();
            }
        }

        // Navigation to specific data item ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /** 
           Grid view pages can be opened in order to display only a single specific
           item using "cmodo.navigationArgs".
           This will process such navigational arguments, filter the data and modify the UI.
        */
        // override
        processNavigation(): Grid {
            if (!this.filter.hasCoreNode(KEY_FILTER_ID))
                return this;

            this.one("dataBound", e => {
                // Select the single row.
                this.grid().select(this.grid().items().first());
            });

            // Hide manual filter editors, because we want to display only one specific data item.
            this.grid().tbody.find(".k-filter-row")
                .hide();

            // Display button for deactivation of the "specific-item" filter.
            const $command = this._$toolbar.find(".km-clear-key-filter-command");

            // Clear single object filter on demand.
            // Views can be called with a item GUID filter which loads only a single specific object.
            // This is used for navigation from other views to a specific object.
            // In order to remove that filter, a "Clear GUID filter" command is placed on the toolbar.      
            $command.on("click", e => {
                // Hide the command button.
                $(e.currentTarget).hide(100);

                // Show the grid's filters which were hidden since we displayed only a single object.
                this.grid().thead.find(".k-filter-row")
                    .show(100);

                // Remove single entity filter and reload.
                this.filter.removeCoreNode(KEY_FILTER_ID);
                this.refresh();
            });

            $command.text("Filter: Navigation");

            $command.addClass("km-active-toggle-button")
                .show();

            return this;
        }

        add(): boolean {
            return this._addOrEdit("create");
        }

        edit(): boolean {
            return this._addOrEdit("modify");
        }

        private _addOrEdit(mode): boolean {
            if (this._state.isEditing)
                return false;

            if (!this._options.editor || !this._options.editor.url)
                return false;

            const item = this.getCurrent();

            if (mode === "create") {
                if (!this._canCreate())
                    return false;
            }
            else if (mode === "modify") {
                if (!this._canModifyItem(item))
                    return false;
            }
            else throw new Error(`Invalid edit mode "${mode}".`);

            this._state.isEditing = true;

            const editorOptions: NavigateToViewOptions = {
                mode: mode,
                itemId: item ? item[this.keyName] : null,
                // Allow deletion if authorized.
                canDelete: true,
                // Dispatch editor events
                events: {
                    editing: e => {
                        this.trigger("editing", e);
                    },
                    dataChanging: e => {
                        this.trigger("dataChanging", e);
                    },
                    dataChange: e => {
                        this.trigger("dataChange", e);
                    },
                    validating: e => {
                        this.trigger("validating", e);
                    }
                },
                finished2: e => {
                    this._state.isEditing = false;

                    if (e.result.isOk) {
                        const item = (e.sender as EditorForm).getCurrent().toJSON();

                        let postprocess: Promise<void> = null;

                        if (this._options.isLocalData) {

                            if (mode === "create") {
                                this.dataSource.insert(0, item);
                            } else {
                                // TODO: Update on edited
                                this.dataSource.pushUpdate(item);

                            }
                            postprocess = this.dataSource.sync() as unknown as Promise<void>;
                        } else {
                            postprocess = Promise.resolve();
                        }

                        postprocess
                            .then(() => this.refresh())
                            .then(() => {
                                // "e.result.value" will be the ID of the edited data item.
                                if (mode === "create" && e.result.value)
                                    this._trySetCurrentItemById(e.result.value);
                            });

                    }
                    else if (e.result.isDeleted) {
                        this.refresh();
                    }
                }
            };

            // Pass on local data options.
            if (this._options.isLocalData) {
                editorOptions.options = {
                    isLocalData: true,
                    // Only a custom save makes sense in this scenario.
                    isCustomSave: true
                };
            }

            kmodo.openById(this._options.editor.id, editorOptions);

            return true;
        }

        // Component initialization ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  

        private _initOptions(): kendo.ui.GridOptions {
            let options = this.createGridOptions();

            if (this.createComponentOptionsOverride)
                options = this.createComponentOptionsOverride(options);

            // TODO: IMPORTANT: Eval if this specific feature works.
            // This is used only by the MoFileExplorerView.
            const extraOptions = this._options["componentOptions"];
            if (extraOptions) {
                // Extend with privided extra options.
                for (let prop in extraOptions)
                    options[prop] = extraOptions[prop];
            }

            if (this._options.isDetailsEnabled === false)
                delete options.detailTemplate;

            // Attach event handlers.
            options.dataBinding = e => this.onComponentDataBinding(e);
            options.dataBound = e => this.onComponentDataBound(e);
            options.change = e => this.onComponentChanged(e);

            if (options.detailTemplate) {
                options.detailInit = e => this.onComponentDetailInit(e);
                options.detailExpand = e => this.onComponentDetailExpanding(e);
                options.detailCollapse = e => this.onComponentDetailCollapsing(e);
            }

            if (this._options.useRemoveCommand) {
                options.columns.push({
                    field: 'ListItemRemoveCommand',
                    title: ' ',
                    width: 30,
                    template: kmodo.templates.get("RowRemoveCommandGridCell"),
                    groupable: false,
                    filterable: false,
                    sortable: false
                });
                this._isItemRemoveCommandEnabled = true;
            }

            let icol = 0;
            for (const col of options.columns) {
                if (col["autofit"] === true) {
                    this._autofitColIndexes.push(icol);
                }
                icol++;
            }

            return options;
        }

        private _autofitColIndexes: number[] = [];

        private _gridGetRowByContent($el: JQuery): JQuery {
            return $el.closest("tr[role=row]");
        }

        private _isTaggable(): boolean {
            return !!this._options.tagsEditorId;
        }

        addCommand(name: string, title: string, execute: (e: GridEvent) => void) {
            const cmd: GridCommand = {
                name: name,
                title: title,
                execute: execute
            };
            this._commands.push(cmd);

            // Add command button to grid header.
            const $btn = $(`<button type='button' class='k-button btn custom-command' data-command-name='${cmd.name}'>${cmd.title}</button>`);

            $btn.on("click", e => {
                const cmdName = $(e.currentTarget).attr("data-command-name");
                this._executeCommand(cmdName);
            });

            $btn.insertBefore(this._$commandBox.children().first());
        }

        private _executeCommand(name: string, data: any = null): void {
            const cmd = this._commands.find(x => x.name === name);
            if (!cmd)
                return;

            if (!cmd.execute)
                return;

            cmd.execute({ sender: this, data: data });
        }

        addRowCommand(cmd: GridCommand) {
            this._rowCommands.push(cmd);
        }

        private _executeRowCommand(name: string, dataItem: any): void {
            const cmd = this._rowCommands.find(x => x.name === name);
            if (!cmd || !cmd.execute)
                return;

            cmd.execute({ sender: this, data: dataItem });
        }

        exportDataTo(target: string): void {
            if (target === "excel")
                this.grid().saveAsExcel();
            else if (target === "pdf")
                this.grid().saveAsPDF();
        }

        private _hasPersistentCompanyFilter(): boolean {
            return !!kmodo.findDataSourceFilter(
                this._getEffectiveFilters(),
                filter => filter._id === COMPANY_FILTER_ID && filter._persistent === true);
        }

        private _updateTagFilterSelector(): void {
            if (this._tagsFilterSelector) {
                const companyFilter = this.filter.findCoreNode(COMPANY_FILTER_ID);
                const companyId = companyFilter ? companyFilter.value : null;
                const filters = buildTagsDataSourceFilters(this._options.dataTypeId, companyId);

                this._tagsFilterSelector.dataSource.filter(filters);
            }
        }

        createView(): void {
            if (this._isViewInitialized)
                return;
            this._isViewInitialized = true;

            // Process navigation args
            const naviArgs = cmodo.navigationArgs.consume(this._options.id);
            if (naviArgs && naviArgs.itemId) {
                this.filter.setCoreNode(KEY_FILTER_ID, { field: this.keyName, value: naviArgs.itemId });
            }

            const isCompanyChangeable = !this._hasPersistentCompanyFilter();

            if (this._options.isGlobalCompanyFilterEnabled &&
                isCompanyChangeable &&
                // Don't filter by global company if navigating to a specific entity.
                !this.filter.hasCoreNode(KEY_FILTER_ID)) {

                const companyId = cmodo.getGlobalInitialCompanyId();
                if (companyId)
                    this.filter.setCoreNode(COMPANY_FILTER_ID, { field: COMPANY_REF_FIELD, value: companyId });
            }

            let $grid = this._options.$component || null;

            // Find the element of the grid.
            if (!$grid)
                $grid = $("#grid-" + this._options.id);

            this.$view = $grid;

            const kendoGridOptions = this._initOptions();

            // Create the Kendo Grid.
            this._kendoGrid = $grid.kendoGrid(kendoGridOptions).data('kendoGrid');
            this.dataSource = this._kendoGrid.dataSource;

            // Commands in grid header.
            this._$commandBox = this.$view.find(".km-grid-tools-right");

            if (kendoGridOptions.selectable === "row") {
                this.grid().tbody.on("mousedown", "tr[role=row]", e => {
                    if (e.which === 3) {
                        // Select grid row also on right mouse click.
                        e.stopPropagation();
                        this.grid().select($(e.currentTarget));
                        return false;
                    }
                    return true;
                });
            }

            if (this._isItemRemoveCommandEnabled) {
                this.grid().tbody.on("click", ".list-item-remove-command", e => {
                    e.stopPropagation();

                    const $row = this._gridGetRowByContent($(e.currentTarget));
                    const item = this.getItemByRow($row);

                    this.trigger("item-remove-command-fired", { sender: this, item: item, $row: $row });

                    return false;
                });
            }

            // Toolbar commands
            const $toolbar = this._$toolbar = this.grid().wrapper.find(".km-grid-toolbar-content");

            // Refresh command
            this._initRefreshCommand(!this._options.isLocalData);

            // Enable add command if local data.
            if (this._options.isLocalData) {
                this._initAddCommand(true);
            }

            let initialCompanyId: string = this._options.companyId || null;

            const $companyFilter = $toolbar.find(".km-grid-company-filter-selector");
            if (!this._options.isCompanyFilterEnabled || !isCompanyChangeable) {
                // Remove company filter UI.
                // NOTE: isCompanyChangeable === false means
                //   that the component was instructed to operate on a specific Company.
                //   Do not allow changing that Company.
                $companyFilter.closest(".km-grid-tool-filter").remove();
            }
            else if ($companyFilter.length) {
                // Use the globally provided current Company if configured to do so.
                initialCompanyId =
                    (this._options.isGlobalCompanyFilterEnabled &&
                        // Don't filter by global company if navigating to a specific entity.
                        !this.filter.hasCoreNode(KEY_FILTER_ID))
                        ? cmodo.getGlobalInitialCompanyId()
                        : null;

                kmodo.createCompanySelector(
                    $companyFilter,
                    {
                        companyId: initialCompanyId,
                        changed: companyId => {
                            if (companyId)
                                this.filter.setCoreNode(COMPANY_FILTER_ID, { field: COMPANY_REF_FIELD, value: companyId });
                            else
                                this.filter.removeCoreNode(COMPANY_FILTER_ID);

                            this.refresh();

                            this._updateTagFilterSelector();
                        }
                    }
                );
            }

            const $tagsFilter = $toolbar.find(".km-grid-tags-filter-selector");
            if (!this._options.isTagsFilterEnabled) {
                // Remove that filter
                $tagsFilter.closest(".km-grid-tool-filter").remove();
            }
            else if ($tagsFilter.length) {
                // Init tags filter.
                this._tagsFilterSelector = kmodo.createMoTagFilterSelector(
                    $tagsFilter,
                    {
                        // TODO: VERY IMPORTANT: Needs to be updated
                        //  when the company filter changes.
                        filters: kmodo.buildTagsDataSourceFilters(this._options.dataTypeId, initialCompanyId),
                        changed: tagIds => {
                            if (tagIds && tagIds.length) {
                                const expression = tagIds.map(x => "ToTags/any(totag: totag/TagId eq " + x + ")").join(" and ");
                                this.filter.setCoreNode(TAGS_FILTER_ID, { customExpression: expression });
                            }
                            else
                                this.filter.removeCoreNode(TAGS_FILTER_ID);

                            this.refresh();
                        }
                    }
                );
            }

            // Export menu
            const $toolsMenu = $toolbar.find(".km-grid-tools-menu");
            $toolsMenu.kendoMenu({
                openOnClick: true,
                select: e => {
                    const name = $(e.item).data("name");

                    // Close menu.
                    e.sender.wrapper.find('.k-animation-container').css('display', 'none');
                    e.sender.close();

                    if (name === "ExportToExcel")
                        this.exportDataTo("excel");
                    else if (name === "ExportToPdf")
                        this.exportDataTo("pdf");
                }
            });

            // Init custom command buttons on the grid's toolbar.

            $toolbar.find(".custom-command").on("click", e => {
                this._executeCommand($(e.currentTarget).attr("data-command-name"));
            });

            if (!this._options.isLookup) {
                this.grid().tbody.on("click", ".k-grid-custom-edit", e => {
                    if (this._state.isEditing)
                        return true;

                    this._gridSelectByRow(this._gridGetRowByContent($(e.currentTarget)));

                    this.edit();

                    return false;
                });

                // Row commands
                this._initRowContextMenu();
            }

            this.grid().tbody.on("click", "span.km-page-navi", kmodo.onPageNaviEvent);

            if (!this._options.isDialog && this.grid().options.scrollable === true) {
                $(window).resize(e => {
                    this.updateSize();
                });
            }

            if (this._options.isDialog)
                this._initViewAsDialog();
        }

        private _initRowContextMenu(): void {
            if (this._options.isTaggable && !!this._options.tagsEditorId) {
                // Insert built-in "edit tags" command.
                this._rowCommands.splice(0, 0, {
                    name: "EditTags",
                    title: "Tags(Markierungen)",
                    execute: (e) => {
                        this._editTags(e.data);
                    }
                });
            }

            //if (!this._rowCommands.length) {
            //    // No commands no context menu.
            //    return;
            //}

            // Create menu element.
            // id='row-context-menu-grid-d9d90acb-f062-4f02-bed4-7266b0fa86b4'
            const $menu = $("<ul style='text-wrap:none;min-width:150px;display:none'></ul>");
            // TODO: REMOVE: const $menu = this.$view.parent().find("#row-context-menu-grid-" + this._options.id);

            // Add row commands to context menu.
            //for (const cmd of this._rowCommands) {
            //    $menu.append(`<li data-name="${cmd.name}">${cmd.title}</li>`);
            //}

            // Add menu element as next sibling of grid element.
            $menu.insertAfter(this.$view);

            // Create context menu.
            $menu.kendoContextMenu({
                target: this._kendoGrid.element,
                filter: "tr[role=row]",
                open: e => {
                    if (!this._rowCommands.length) {
                        // No commands -> no context menu.
                        e.preventDefault();
                        return;
                    }

                    const menu = e.sender;

                    // Clear existing commands.
                    menu.element.empty();

                    // Add row commands
                    for (const cmd of this._rowCommands) {
                        menu.element.append(`<li class="k-item k-state-default k-first k-last" role="menuitem" data-name="${cmd.name}"><span class="k-link">${cmd.title}</span></li>`);                     
                    }

                    // TODO: REVISIT: Anything to do here? Maybe apply auth? Let the command enabled/disable itself?
                },
                select: e => {
                    // const menu = e.sender;
                    const name = $(e.item).data("name");
                    const dataItem = this.grid().dataItem(e.target);

                    this._executeRowCommand(name, dataItem);
                }
            });
        }

        private _editTags(dataItem: any): void {
            // Open tags (MoTags) editor.
            kmodo.openById(this._options.tagsEditorId, {
                itemId: dataItem[this.keyName],
                filters: kmodo.buildTagsDataSourceFilters(this._options.dataTypeId, dataItem[COMPANY_REF_FIELD]),
                // TODO: LOCALIZE
                title: "Tags bearbeiten",

                minWidth: 400,
                minHeight: 500,

                options: {
                    isLocalTargetData: !!this._options.isLocalData,
                    isCustomSave: !!this._options.isLocalData
                },

                finished2: e => {
                    if (e.result.isOk) {
                        if (this._options.isLocalData &&
                            typeof dataItem["ToTags"] !== "undefined" &&
                            e.result.items &&
                            e.result.items.length) {

                            // Local data: add tags to entity.
                            const tags = e.result.items.map(x => x.toJSON());
                            const tagLinks = cmodo.entityMappingService.createLinksForTags(this._options.dataTypeId, dataItem[this.keyName], tags);

                            dataItem.set("ToTags", tagLinks);
                        }

                        this.refresh();
                    }
                }
            });
        }

        updateSize() {
            // NOTE: The grid's scroll area is not being updated (the k-grid-content has a fixed height)
            //   when the window is being resized. As a workaround the user has to refresh the grid.
            //   For a possible calculation on window resize see: https://www.telerik.com/forums/window-resize-c4fcceedd72c

            const $grid = this.grid().wrapper;

            const gridHeight = $grid.outerHeight(true);

            let extraContentSize = 0;
            $grid.find(">div:not(.k-grid-content)").each((idx, elem) => {
                extraContentSize += $(elem).outerHeight(true);
            });

            const contentHeight = gridHeight - extraContentSize;

            $grid.find(".k-grid-content").first().height(contentHeight);
        }

        private _initViewAsDialog() {
            // Get dialog arguments and set them on the view model.
            if (!this.args)
                this.setArgs(cmodo.dialogArgs.consume(this._options.id));

            this._dialogWindow = kmodo.findKendoWindow(this.grid().wrapper);
            this._initDialogWindowTitle();

            // TODO: IMPORTANT: There was no time yet to develop a
            //   decorator for dialog functionality. That's why the grid view model
            //   itself has to take care of the dialog commands which are located
            //   *outside* the grid widget.
            const $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", e => {
                if (!this.getCurrent())
                    return false;

                this.args.buildResult();
                this.args.isCancelled = false;
                this.args.isOk = true;

                this._dialogWindow.close();

                return false;
            });

            $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", e => {
                this.args.isCancelled = true;
                this.args.isOk = false;

                this._dialogWindow.close();
                return false;
            });

            const $toolbarLeft = this._$toolbar.find(".km-grid-tools").first();
            this._initFilterCommands($toolbarLeft);

            // On row double-click: set dialog result and close the window.
            this.grid().tbody.on("dblclick", "tr[role=row]", e => {
                e.preventDefault();
                e.stopPropagation();

                const row = e.currentTarget;

                // If the selected row has a data item...
                if (this.grid().dataItem(row)) {
                    const wnd = kmodo.findKendoWindow(this.grid().wrapper);
                    if (wnd) {
                        this.args.buildResult();
                        this.args.isCancelled = false;
                        this.args.isOk = true;

                        // Close the window.
                        wnd.close();
                    }
                }

                return false;
            });

            // Dialogs can be called with a given set of filters.
            // Those filters could be circumvented by the user by
            // manipulating the filter cell or the filter in the column menu.
            // Thus we need to hide any filter UI of the given filter fields.
            const filters = this.args.filters;
            if (filters && filters.length) {

                const $thead = this.grid().thead;

                for (const item of filters) {
                    const fieldName = (item as any).field;
                    if (typeof fieldName === "undefined")
                        return;

                    // TODO: Should we remove those cells instead of just hiding them?
                    //   Dunno, maybe the kendo.ui.Grid uses this UI in order to construct its
                    //   filter expression. Experiment with it.

                    // Hide column menu where one can also change the filter.
                    $thead.find("th.k-header[data-field='" + fieldName + "'] > a.k-header-column-menu")
                        .first()
                        .css("visibility", "hidden");

                    // Hide filter cell.
                    $thead.find(".k-filtercell[data-field='" + fieldName + "']")
                        .first()
                        .css("visibility", "hidden");
                }
            }
        }

        private _initFilterCommands($toolbarLeft: JQuery): void {
            // Add deactivatable filter buttons to the grid's toolbar.

            const commands = this.args.filterCommands;
            if (!commands || !commands.length)
                return;

            // Ensure group prop.
            for (const cmd of commands) {
                if (!cmd.group)
                    cmd.group = null;
            }

            // Group by group.
            const commandGroups = Enumerable.from(commands).groupBy(x => x.group).toArray();

            for (const group of commandGroups) {
                const groupName = group.key;
                const groupCommands = group.getSource();

                // TODO: Multiple commands in group: Use radio buttons instead.

                let index = -1;
                for (const cmd of groupCommands) {
                    index++;

                    // If multiple filter commands in group: Add only the first.
                    const isInitiallyActive = !groupName || index === 0;

                    if (isInitiallyActive) {
                        this._addCommandFilter(cmd);
                    }

                    // Init filter command UI.
                    const $btn = this._createFilterCommandUI(cmd, isInitiallyActive);

                    const icmd: CustomFilterCommandInfo = {
                        command: cmd,
                        $btn: $btn
                    };

                    this._filterCommands.push(icmd);

                    $btn.on("click", e => {
                        const newActive = !this.filter.hasCoreNode(cmd.filter._id);

                        this._setFilterCommandActive(icmd, newActive);

                        // Deactivate other commands in same group.
                        if (newActive && cmd.group) {
                            for (const otherICmd of this._filterCommands
                                .filter(x => x !== icmd && x.command.group === cmd.group)) {

                                this._setFilterCommandActive(otherICmd, false);
                            }
                        }

                        this.refresh();
                    });

                    // Append button to toolbar.
                    $toolbarLeft.append($btn);
                }
            }
        }

        private _setFilterCommandActive(icmd: CustomFilterCommandInfo, active: boolean): void {
            if (active) {
                this._addCommandFilter(icmd.command);
                // Activate command button.
                kmodo.toggleButton(icmd.$btn, active);
            } else {
                // Deactivate filter & button.
                this._removeCommandFilter(icmd.command);

                if (icmd.command.hideOnDeactivated) {
                    // Hide the command button.
                    icmd.$btn.hide(100);
                } else {
                    // Deactivate command button.
                    kmodo.toggleButton(icmd.$btn, false);
                }
            }
        }

        private _createFilterCommandUI(cmd: ComponentCommand, active: boolean): JQuery {
            const activeClass = active ? " km-active-toggle-button" : "";
            return $(`<button class='k-button${activeClass}'>${cmd.title}</button>`);
        }

        _initDialogWindowTitle() {
            let title = "";

            if (this.args.title) {
                title = this.args.title;
            }
            else {
                title = this._options.title || "";

                if (this._options.isLookup)
                    title += " wählen";
            }

            this._dialogWindow.title(title);
        }
    }

    export function getGridRowDataItem(grid: kendo.ui.Grid, $elem: JQuery): any {
        return grid.dataItem($elem.closest("tr[role='row']"));
    }

    export function gridReferenceFilterColTemplate(args: any, valueField: string, textField: string, isNullable: boolean): void {
        args.element.kendoDropDownList({
            dataSource: args.dataSource,
            dataValueField: valueField,
            dataTextField: textField,
            optionLabel: isNullable ? " " : null,
            valuePrimitive: true
        });
    }

    interface AllSelectionInfo {
        isInitialized: boolean;
        isExisting: boolean;
        $selector: JQuery;
    }

    interface SingleSelectionInfo {
        id: string;
        item: any;
        isVisible: boolean;
        isEnabled: boolean;
        isSelected: boolean;
        $row: JQuery;
        $selector: JQuery;
        $selectorVisual: JQuery;
    }

    export interface ItemSelectionStateInfo {
        isVisible?: boolean;
        isEnabled?: boolean;
        isSelected?: boolean;
    }

    export class GridSelectionManager extends cmodo.ComponentBase {
        private _keyName: string;
        private _selection: any[];
        private _selectionDataItems: any[];
        private _iselectors: SingleSelectionInfo[];
        private _iallSelector: AllSelectionInfo;
        private _isSelectorInitializationPending: boolean;
        private _isSelectorBindingPending: boolean;
        private _grid: Grid;
        private _$allSelector: JQuery;
        private _isSelectorsVisible: boolean;
        private _isUpdatingSelectors: boolean;
        private _isUpdatingBatch: boolean;
        customSelectorInitializer: (isel: SingleSelectionInfo) => ItemSelectionStateInfo;

        constructor(options) {
            super();

            this._grid = options.grid;

            this._keyName = this._grid.keyName;

            // List of selected data item IDs.
            this._selection = [];
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
            this._isSelectorsVisible = false;
        }

        clearSelection(): void {
            this._performBatchUpdate(() => {
                this._selection = [];
                this._selectionDataItems = [];

                if (this._$allSelector)
                    this._$allSelector.prop("checked", false);

                for (let isel of this._iselectors) {
                    isel.isSelected = false;
                    isel.$selector.prop("checked", false);
                }
            });
        }

        getSelectedDataItems(): any[] {
            return this._selectionDataItems;
        }

        showSelectors(): void {
            if (!this._getIsEnabled())
                return;

            this._isSelectorsVisible = true;

            if (this._isSelectorInitializationPending)
                this._initSelectors();

            this.getKendoGrid().showColumn(0);
        }

        hideSelectors(): void {
            if (!this._getIsEnabled())
                return;

            this._isSelectorsVisible = false;

            this.getKendoGrid().hideColumn(0);
        }

        deselectedById(id: string): void {
            const isel = this._iselectors.find(x => x.id === id);
            if (isel && isel.isSelected) {
                isel.$selector.prop("checked", false).change();
            }

            // Since the data item with the provided ID might not be currently being displayed
            // we need to try to remove it from the selection list.
            this._removeDataItemById(id);
        }

        private getKendoGrid(): kendo.ui.Grid {
            return this._grid.getKendoGrid() as kendo.ui.Grid;
        }

        private _getIsEnabled(): boolean {
            return this._grid.getSelectionMode() === "multiple";
        }

        public _onDataSourceDataBound(e): void {
            if (!this._getIsEnabled())
                return;

            this._isSelectorBindingPending = true;
            this._initSelectors();
        }

        private _initAllSelector(): void {
            let isel = this._iallSelector;
            if (isel.isInitialized)
                return;

            isel.isInitialized = true;

            isel.$selector = this.getKendoGrid().thead.find("input.all-list-items-selector").first();
            isel.isExisting = isel.$selector.length === 1;

            if (isel.isExisting) {
                isel.$selector.on("change", () => {
                    this._onAllSelectorChanged(isel.$selector);
                });
            }
        }

        reinitializeSelectors(): void {
            this._initSelectors();
        }

        private _initSelectors(): void {
            if (!this._getIsEnabled())
                return;

            if (!this._isSelectorsVisible) {
                this._isSelectorInitializationPending = true;
                return;
            }

            this._isSelectorInitializationPending = false;

            this._isUpdatingSelectors = true;
            this._isUpdatingBatch = true;
            try {
                this._iselectors = [];

                // The "all items selector" needs to be initialized only once
                // because it resides in the grid's header and is not destroyed/re-created when new data is fetched.
                this._initAllSelector();
                if (this._iallSelector.isExisting) {
                    this._iallSelector.$selector.prop("checked", false);
                }

                // Build selector infos.
                this.getKendoGrid().tbody.find("tr[role='row']").each((idx, elem) => {
                    const $row = $(elem);
                    const $selector = $row.find("input.list-item-selector");
                    const $selectorVisual = $row.find("label.list-item-selector");

                    const item = this.getKendoGrid().dataItem($row);
                    const id = item[this._keyName];
                    const isSelected = this._getIsSelectedById(id);

                    this._iselectors.push({
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

                for (const isel of this._iselectors) {

                    if (this.customSelectorInitializer) {
                        // Hook for the consumer in order to provide custom logic for the
                        // initial states of a selector such as isSelected, isEnabled and isVisible.
                        const custom = this.customSelectorInitializer(isel);

                        if (custom.isVisible !== undefined)
                            isel.isVisible = custom.isVisible;

                        if (custom.isSelected !== undefined)
                            isel.isSelected = custom.isSelected;

                        if (custom.isEnabled !== undefined)
                            isel.isEnabled = custom.isEnabled;
                    }

                    // Sanity check: we don't want items to be selected if the
                    // selector is not visible since this would lead to confusion.
                    // TODO: Should we also remove from selection here?
                    if (isel.isSelected && !isel.isVisible)
                        isel.isSelected = false;

                    if (isel.isSelected)
                        this._addDataItem(isel.item);
                    else
                        this._removeDataItemById(isel.id);

                    // Set selected state.
                    isel.$selector.prop("checked", isel.isSelected);

                    // Set selector visibility.
                    if (isel.isVisible)
                        isel.$selectorVisual.show();
                    else
                        isel.$selectorVisual.hide();

                    if (this._isSelectorBindingPending) {
                        // Listen to selector's change event.
                        isel.$selector.on("change", () => {
                            this._onSelectorChanged(isel);
                        });
                    }
                }
            }
            finally {
                this._isUpdatingBatch = false;
                this._isUpdatingSelectors = false;
                this._isSelectorBindingPending = false;
            }
        }

        private _getDataSource(): kendo.data.DataSource {
            return this._grid.dataSource;
        }

        private _updateSelectedViewStates(): void {
            for (const isel of this._iselectors) {
                isel.isSelected = this._getIsSelectedById(isel.id);
                isel.$selector.prop("checked", isel.isSelected);
            }
        }

        _onAllSelectorChanged($selector: JQuery): void {
            if (this._isUpdatingSelectors)
                return;

            this._performBatchUpdate(() => {
                const add = $selector.prop("checked") === true;
                const items = this._getDataSource().data();

                items.forEach((item) => {
                    if (add)
                        this._addDataItem(item);
                    else
                        this._removeDataItem(item);
                });
            });
        }

        _onSelectorChanged(isel): void {
            if (this._isUpdatingSelectors)
                return;

            isel.isSelected = !!isel.$selector.prop("checked");

            if (isel.isSelected)
                this._addDataItem(isel.item);
            else
                this._removeDataItem(isel.item);
        }

        _performBatchUpdate(action): void {
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
        }

        _addDataItem(item: any): void {
            // Add item to selection.
            const index = this._selection.indexOf(item[this._keyName]);
            if (index !== -1)
                return;

            this._selection.push(item[this._keyName]);
            this._selectionDataItems.push(item);

            if (!this._isUpdatingBatch) {
                // Hand over as non-observable copy.
                this.trigger("selectionItemAdded", { item: item.toJSON() });
            }
        }

        _removeDataItem(item: any): void {
            this._removeDataItemById(item[this._keyName]);
        };

        _removeDataItemById(id: string): void {
            const index = this._selection.indexOf(id);
            if (index === -1)
                return;

            const item = this._selectionDataItems[index];

            this._selection.splice(index, 1);
            this._selectionDataItems.splice(index, 1);

            if (!this._isUpdatingBatch) {
                // Hand over as non-observable copy.
                this.trigger("selectionItemRemoved", { item: item.toJSON() });
            }
        }

        _getIsSelectedById(id): boolean {
            return this._selection.indexOf(id) !== -1;
        }
    }
}