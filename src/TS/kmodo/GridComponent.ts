namespace kmodo {
    // kendo.ui.Grid:    https://docs.telerik.com/kendo-ui/api/javascript/ui/grid
    // kendo.DataSource: https://docs.telerik.com/kendo-ui/api/javascript/data/datasource

    const KEY_FILTER_ID = "5883120a-b1a6-4ac8-81a2-1d23028daebe";
    const COMPANY_FILTER_ID = "b984dd7b-5d7a-48bf-b7f1-db26522c63df";
    const TAGS_FILTER_ID = "2bd9e0d8-7b2d-4c1e-90c0-4d7eac6d01a4";

    // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    export function foreachGridRow(grid: kendo.ui.Grid, action: ($row: JQuery, item: any) => void) {

        grid.tbody.find("tr[role=row]").each((idx, elem) => {
            let $row = $(elem);
            action($row, grid.dataItem($row));
        });
    }

    interface InternalGridState {
        gridDataBindAction: string;
        lastCurrentItemId: string;
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
        filters?: ViewComponentFilter[],
        editor?: EditorInfo;
        isTaggable?: boolean;
        isTagsFilterEnabled?: boolean;
        $component?: JQuery;
        useRemoveCommand?: boolean;
        baseFilters?: kendo.data.DataSourceFilterItem[];
        bindRow?: boolean | Function;
        gridOptions?: (e: DataSourceViewEvent) => kendo.ui.GridOptions;
        companyId?: string;
    }

    export interface GridEvent extends DataSourceViewEvent {
        sender: Grid;
    }

    interface NavigateToEditorFormOptions extends NavigateToViewOptions {
    }

    export class Grid extends DataSourceViewComponent {
        private _kendoGrid: kendo.ui.Grid;
        protected _options: GridOptions;
        private expandedKeys: string[];
        private _customCommands: CustomCommand[];
        private _customRowCommands: any[];
        private _state: InternalGridState;
        public selectionManager: GridSelectionManager;
        private _isItemRemoveCommandEnabled: boolean;
        private _$toolbar: JQuery;
        private _tagsFilterSelector: kendo.ui.MultiSelect;
        private _dialogWindow: kendo.ui.Window = null;
        // NOTE: createComponentOptionsOverride can be provided via view args.
        private createComponentOptionsOverride: (options: kendo.ui.GridOptions) => kendo.ui.GridOptions;

        constructor(options: GridOptions) {
            super(options);

            const componentArgs = cmodo.componentArgs.consume(this._options.id);
            if (componentArgs) {
                if (componentArgs.createComponentOptionsOverride)
                    this.createComponentOptionsOverride = componentArgs.createComponentOptionsOverride;
            }

            this.expandedKeys = [];

            this.selectionMode = options.selectionMode || "single";

            this._customCommands = [];
            this._customRowCommands = [];

            this._state = {
                gridDataBindAction: null,
                lastCurrentItemId: null,
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
            options.error = this._eve(this.onDataSourceError);
            options.requestEnd = this._eve(this.onDataSourceRequestEnd);
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
                const item = this.getCurrentItem();
                this._state.lastCurrentItemId = item ? item[this.keyName] : null;;
                this.setCurrentItem(null);
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
            if (this._autofitColIndexes.length) {
                for (const icol of this._autofitColIndexes) {
                    grid.autoFitColumn(icol);
                }
            }

            this._initGridRows();

            if (action === "rebind" && this._state.lastCurrentItemId !== null) {

                // This is a refresh. Restore last current item.
                try {
                    this._trySetCurrentItemById(this._state.lastCurrentItemId);
                }
                finally {
                    this._state.lastCurrentItemId = null;
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

        private _trySetCurrentItemById(id) {
            let item = this.getCurrentItem();
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

        gridSelectByItem(item) {
            this._gridSelectByItem(item);
        }

        _gridSelectByItem(item) {
            this.grid().select(this._gridGetRowByItem(item));
        }

        _gridGetRowByItem(item) {
            return this.grid().tbody.find("tr[role=row][data-uid='" + item.uid + "']");
        }

        _gridSelectByRow($row: JQuery) {
            this.grid().select($row);
        }

        getItemByRow($row: JQuery) {
            return this.grid().dataItem($row);
        }

        /**
            Handler for kendo.ui.Grid's event "change".
            Triggered when the grid's row(s) selection changed.
            @param {any} e - Event 
        */
        private onComponentChanged(e) {
            if (this._isDebugLogEnabled)
                console.debug("- changed");

            this.trigger('changed', e);

            if (this.selectionMode === "single")
                this.setCurrentItem(this.grid().dataItem(this.grid().select()));
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
        private onComponentDetailExpanding(e) {
            // KABU TODO: Animation doesn't work, although advertised by Telerik.
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
        private onComponentDetailCollapsing(e) {
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
            });

            if (this._options.isRowAlwaysExpanded) {
                this.grid().wrapper.find(".k-hierarchy-cell").empty().hide();
                this.grid().wrapper.find(".k-detail-row > td:first-child").hide();
            }
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

        private _canModifyItem(item): boolean {
            return item && this.auth.canModify === true && item.IsReadOnly === false;
        }

        private _initRowImageCells(item, $row: JQuery): void {
            // KABU TODO: DISABLED - REMOVE? Not used. Was intended for "PhotoCellTemplate".
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
            this._initAddCommand(this.auth.canCreate);
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
        processNavigation(): any {
            if (!this._hasBaseFilter(KEY_FILTER_ID))
                return this;

            this.one("dataBound", (e) => {
                // Select the single row.
                this.grid().select(this.grid().items().first());
            });

            // Hide manual filter editors, because we want to display only one specific data item.
            this.grid().tbody.find(".k-filter-row")
                .hide();

            // Display button for deactivation of the "specific-item" filter.
            const $command = this._$toolbar.find(".kmodo-clear-guid-filter-command");

            // Clear single object filter on demand.
            // Views can be called with a item GUID filter which loads only a single specific object.
            // This is used for navigation from other views to a specific object.
            // In order to remove that filter, a "Clear GUID filter" command is placed on the toolbar.      
            $command.on("click", (e) => {
                // Hide the command button.
                $(e.currentTarget).hide(100);

                // Show the grid's filters which were hidden since we displayed only a single object.
                this.grid().thead.find(".k-filter-row")
                    .show(100);

                // Remove single entity filter and reload.
                this._removeBaseFilter(KEY_FILTER_ID);
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
        };

        private _addOrEdit(mode): boolean {
            if (this._state.isEditing)
                return false;

            if (!this._options.editor || !this._options.editor.url)
                return false;

            const item = this.getCurrentItem();

            if (mode === "create") {
                if (!this.auth.canCreate)
                    return false;
            }
            else if (mode === "modify") {
                if (!this._canModifyItem(item))
                    return false;
            }
            else throw new Error(`Invalid edit mode "${mode}".`);

            this._state.isEditing = true;

            const editorOptions: NavigateToEditorFormOptions = {
                mode: mode,
                itemId: item ? item[this.keyName] : null,
                // Allow deletion if authorized.
                canDelete: true,
                events: {
                    editing: e => {
                        this.trigger("editing", e);
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
                            //this.dataSource.sync()
                            //    .then(() => {
                            //        // "e.result.value" will be the ID of the edited data item.
                            //        if (mode === "create" && e.result.value)
                            //            this._trySetCurrentItemById(e.result.value);
                            //    });
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

            // KABU TODO: VERY IMPORTANT: Check if this works.
            // This is used by the MoFileExplorerView.
            const extraOptions = this._options["componentOptions"];
            if (extraOptions) {
                // Extend with privided extra options.
                for (let prop in extraOptions)
                    options[prop] = extraOptions[prop];
            }

            if (this._options.isDetailsEnabled === false)
                delete options.detailTemplate;

            // Attach event handlers.
            options.dataBinding = this._eve(this.onComponentDataBinding);
            options.dataBound = this._eve(this.onComponentDataBound);
            options.change = this._eve(this.onComponentChanged);

            if (options.detailTemplate) {
                options.detailInit = this._eve(this.onComponentDetailInit);
                options.detailExpand = this._eve(this.onComponentDetailExpanding);
                options.detailCollapse = this._eve(this.onComponentDetailCollapsing);
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

        registerCustomCommand(name: string, execute: Function): void {
            this._customCommands.push({
                name: name,
                execute: execute || null
            });
        }

        private _executeCustomCommand(name: string): void {
            const cmd = this._customCommands.find(x => x.name === name);
            if (!cmd)
                return;

            if (!cmd.execute)
                return;

            cmd.execute();
        }

        private _executeRowCommand(name: string, dataItem: any): void {
            const cmd = this._customRowCommands.find(x => x.name === name);
            if (!cmd)
                return;

            if (!cmd.execute)
                return;

            cmd.execute(dataItem);
        }

        addRowCommandHandler(name: string, execute: Function): void {
            this._customRowCommands.push({
                name: name,
                execute: execute || null
            });
        }

        exportDataTo(target: string): void {
            if (target === "excel")
                this.grid().saveAsExcel();
            else if (target === "pdf")
                this.grid().saveAsPDF();
        }

        private _getAllConsumerFilters(): ViewComponentFilter[] {
            let filters = this._options.filters || [];

            // Add filters of args.
            if (this.args &&
                this.args.filters &&
                this.args.filters !== this._options.filters) {

                filters = filters.concat(this.args.filters);
            }

            return filters;
        }

        private _hasNonRemovableCompanyFilter(): boolean {
            return null !== kmodo.findDataSourceFilter(this._getAllConsumerFilters(),
                filter => {
                    return filter.targetTypeId === COMPANY_TYPE_ID &&
                        !filter.deactivatable;
                });
        }

        private _updateTagFilterSelector(): void {
            if (this._tagsFilterSelector) {
                const companyFilter = this._findBaseFilter(COMPANY_FILTER_ID);
                const companyId = companyFilter ? companyFilter.value : null;
                const filters = buildTagsDataSourceFilters(this._options.dataTypeId, companyId);

                this._tagsFilterSelector.dataSource.filter(filters);
            }
        }

        createView(): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            // Add base filters from options.
            if (this._options.baseFilters) {
                const baseFilters = this._baseFilters;
                const optionFilters = this._options.baseFilters;
                for (const x of optionFilters) {
                    if (Array.isArray(x))
                        baseFilters.push(...x);
                    else
                        baseFilters.push(x);
                }

            }

            // Evaluate navigation args
            const naviArgs = cmodo.navigationArgs.consume(this._options.id);
            if (naviArgs && naviArgs.itemId) {
                this._setBaseFilter(KEY_FILTER_ID, { field: this.keyName, value: naviArgs.itemId });
            }

            const isCompanyChangeable = !this._hasNonRemovableCompanyFilter();

            if (this._options.isGlobalCompanyFilterEnabled &&
                isCompanyChangeable &&
                // Don't filter by global company if navigating to a specific entity.
                !this._hasBaseFilter(KEY_FILTER_ID)) {

                const companyId = cmodo.getGlobalInitialCompanyId();
                if (companyId)
                    this._setBaseFilter(COMPANY_FILTER_ID, { field: "CompanyId", value: companyId });
            }

            let $component = this._options.$component || null;

            // Find the element of the grid.
            if (!$component)
                $component = $("#grid-" + this._options.id);

            const kendoGridOptions = this._initOptions();

            // Create the Kendo Grid.
            this._kendoGrid = $component.kendoGrid(kendoGridOptions).data('kendoGrid');
            this.setDataSource(this._kendoGrid.dataSource);

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
                // Remove company filter.
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
                        !this._hasBaseFilter(KEY_FILTER_ID))
                        ? cmodo.getGlobalInitialCompanyId()
                        : null;

                kmodo.createCompanySelector(
                    $companyFilter,
                    {
                        companyId: initialCompanyId,
                        changed: companyId => {
                            if (companyId)
                                this._setBaseFilter(COMPANY_FILTER_ID, { field: "CompanyId", value: companyId });
                            else
                                this._removeBaseFilter(COMPANY_FILTER_ID);

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
                                const expression = tagIds.map(x => "ToTags/any(totag: totag/Tag/Id eq " + x + ")").join(" and ");
                                this._setBaseFilter(TAGS_FILTER_ID, { expression: expression });
                            }
                            else
                                this._removeBaseFilter(TAGS_FILTER_ID);

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
                this._executeCustomCommand($(e.currentTarget).attr("data-command-name"));
            });

            if (!this._options.isLookup) {
                this.grid().tbody.on("click", ".k-grid-custom-edit", e => {
                    if (this._state.isEditing)
                        return true;

                    this._gridSelectByRow(this._gridGetRowByContent($(e.currentTarget)));

                    this.edit();

                    return false;
                });

                // Edit tags context menu action.
                if (this._options.hasRowContextMenu) {
                    const $contextMenu = $component.parent().find("#row-context-menu-grid-" + this._options.id);
                    $contextMenu.kendoContextMenu({
                        target: $component,
                        filter: "tr[role=row]",
                        open: e => {
                            // const menu = e.sender;
                            // const name = $(e.item).data("name");
                            // const dataItem = this.grid().dataItem(e.target);

                            // TODO: IMPORTANT: Apply auth.
                            // kmodo.enableContextMenuItems(menu, "EditTags", xyz);
                        },
                        select: e => {
                            // const menu = e.sender;
                            const name = $(e.item).data("name");
                            const dataItem = this.grid().dataItem(e.target);

                            // KABU TODO: This may be just a temporary hack.
                            //   We could/should use a service instead in the future.
                            if (name === "EditTags" && this._isTaggable()) {
                                // Open tags (MoTags) editor.
                                kmodo.openById(this._options.tagsEditorId,
                                    {
                                        itemId: dataItem[this.keyName],
                                        filters: kmodo.buildTagsDataSourceFilters(this._options.dataTypeId, dataItem["CompanyId"]),
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
                                                // TODO: IMPORTANT: MAGIC ToTags. Move to entity mapping service somehow.
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
                            else {
                                // Custom row commands.
                                this._executeRowCommand(name, dataItem);
                            }
                        }
                    });
                }
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

            // KABU TODO: IMPORTANT: There was no time yet to develop a
            //   decorator for dialog functionality. That's why the grid view model
            //   itself has to take care of the dialog commands which are located
            //   *outside* the grid widget.
            const $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", e => {
                if (!this.getCurrentItem())
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

            const filterCommands = this.args.filterCommands;
            if (filterCommands && filterCommands.length) {

                // Add deactivatable filter buttons to the grid's toolbar.
                // Those represent initially active filters as defined by the caller.
                // The filter/button will be removed by pressing the button.

                for (let i = 0; i < filterCommands.length; i++) {
                    const cmd = filterCommands[i];

                    if (cmd.deactivatable) {
                        const $btn = $("<button class='k-button km-active-toggle-button'>" + cmd.title + "</button>");
                        $btn.on("click", e => {

                            // Remove that filter by field name.
                            // KABU TODO: This means that we can't remove custom/complex filter expressions.
                            this.removeFilterByFieldName(cmd.field);

                            // Hide the command button.
                            $(e.currentTarget).hide(100);

                            // Refresh since filter has changed.
                            this.refresh();
                        });
                        // Append button to grid's toolbar.
                        $toolbarLeft.append($btn);
                    }
                }
            }

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

                    // KABU TODO: Should we remove those cells instead of just hiding them?
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
        private keyName: string;
        private selection: any[];
        private _selectionDataItems: any[];
        private _iselectors: SingleSelectionInfo[];
        private _iallSelector: AllSelectionInfo;
        private _isSelectorInitializationPending: boolean;
        private _isSelectorBindingPending: boolean;
        private gridViewModel: Grid;
        private _$allSelector: JQuery;
        private _isSelectorsVisible: boolean;
        private _isUpdatingSelectors: boolean;
        private _isUpdatingBatch: boolean;
        customSelectorInitializer: (isel: SingleSelectionInfo) => ItemSelectionStateInfo;

        constructor(options) {
            super();

            this.gridViewModel = options.grid;

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
            this._isSelectorsVisible = false;
        }

        clearSelection(): void {
            this._performBatchUpdate(() => {
                this.selection = [];
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
            return this.gridViewModel.getKendoGrid() as kendo.ui.Grid;
        }

        private _getIsEnabled(): boolean {
            return this.gridViewModel.getSelectionMode() === "multiple";
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
                    const id = item[this.keyName];
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
                    // KABU TODO: Should we also remove from selection here?
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
            return this.gridViewModel.dataSource;
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
            const index = this.selection.indexOf(item[this.keyName]);
            if (index !== -1)
                return;

            this.selection.push(item[this.keyName]);
            this._selectionDataItems.push(item);

            if (!this._isUpdatingBatch) {
                // Hand over as non-observable copy.
                this.trigger("selectionItemAdded", { item: item.toJSON() });
            }
        }

        _removeDataItem(item: any): void {
            this._removeDataItemById(item[this.keyName]);
        };

        _removeDataItemById(id: string): void {
            const index = this.selection.indexOf(id);
            if (index === -1)
                return;

            const item = this._selectionDataItems[index];

            this.selection.splice(index, 1);
            this._selectionDataItems.splice(index, 1);

            if (!this._isUpdatingBatch) {
                // Hand over as non-observable copy.
                this.trigger("selectionItemRemoved", { item: item.toJSON() });
            }
        }

        _getIsSelectedById(id): boolean {
            return this.selection.indexOf(id) !== -1;
        }
    }
}