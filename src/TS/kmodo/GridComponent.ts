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

    interface GridState {
        gridDataBindAction: string;
        lastCurrentItemId: string;
        gridLastScrollTop: number;
        isRestoringExpandedRows: boolean;
        isEditing: boolean;
    }

    export interface GridComponentOptions extends DataSourceViewOptions {
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
        useLocalDataSource?: boolean;
        localData?: any[];
        useRemoveCommand?: boolean;
        baseFilters?: kendo.data.DataSourceFilterItem[];
        bindRow?: boolean | Function;
        gridOptions: (e: DataSourceViewEvent) => kendo.ui.GridOptions;
    }

    export interface GridEvent extends DataSourceViewEvent {
        sender: GridComponent;
    }

    export class GridComponent extends DataSourceViewComponent {
        private _kendoGrid: kendo.ui.Grid;
        protected _options: GridComponentOptions;
        private expandedKeys: string[];
        private _customCommands: CustomCommand[];
        private _customRowCommands: any[];
        private _state: GridState;
        public selectionManager: GridSelectionManager;
        private _isItemRemoveCommandEnabled: boolean;
        private _$toolbar: JQuery;
        private _tagsFilterSelector: kendo.ui.MultiSelect;
        private _dialogWindow: kendo.ui.Window = null;
        // NOTE: createComponentOptionsOverride can be provided via view args.
        private createComponentOptionsOverride: (options: kendo.ui.GridOptions) => kendo.ui.GridOptions;

        constructor(options: GridComponentOptions) {
            super(options);

            // TODO: REMOVE: this._options = super._options as GridComponentOptions;

            let self = this;

            let componentArgs = cmodo.componentArgs.consume(this._options.id);
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
                this.on("dataBound", $.proxy(self.selectionManager._onDataSourceDataBound, self.selectionManager));
            }

            //this.componentOptions = null;

            this._tagsFilterSelector = null;
        }

        // override
        protected createDataSourceOptions(): kendo.data.DataSourceOptions {

            if (this._options.useLocalDataSource) {
                return {
                    schema: {
                        model: this.createDataModel()
                    },
                    data: this._options.localData || [],
                    pageSize: 0
                }
            }
            else return super.createDataSourceOptions();
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
            let self = this;

            this.refresh()
                .then(function () {
                    if (id)
                        self._trySetCurrentItemById(id);
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
                    let content = this.grid().content;
                    this._state.gridLastScrollTop = content.length ? content[0].scrollTop : null;
                }

                // Save current item. It will be restored after the refresh.
                let item = this.getCurrentItem();
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

            let action = this._state.gridDataBindAction;

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

                this.grid().content[0].scrollTop = this._state.gridLastScrollTop;
            }

            this.trigger('dataBound', e);
        }

        private _trySetCurrentItemById(id) {
            let self = this;

            let item = this.getCurrentItem();
            if (item && item[this.keyName] === id)
                // This item is already the current item.
                return true;

            item = this.dataSource.view().find(x => x[self.keyName] === id);
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
            //let detailRow = e.detailRow;
            //kendo.fx(detailRow).fadeIn().play();               

            if (!this._state.isRestoringExpandedRows) {

                let id = this.grid().dataItem(e.masterRow)[this.keyName];
                if (id)
                    this.expandedKeys.push(id);
            }

            this.trigger('detailExpanding', e);
        }

        /**
            Handler for kendo.ui.Grid's event "detailCollapse".
        */
        private onComponentDetailCollapsing(e) {

            // KABU TODO: Animation doesn't work, although advertised by Telerik.
            /*
            let detailRow = e.detailRow;
            setTimeout(function () {
                kendo.fx(detailRow).fadeOut().duration(375).play();
            }, 0);
            */

            if (!this._state.isRestoringExpandedRows) {
                let id = this.grid().dataItem(e.masterRow)[this.keyName];
                let idx = this.expandedKeys.indexOf(id);
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

            let self = this;

            if (!expandedKeys || !expandedKeys.length) return [];

            let restored: string[] = [];

            this.foreachGridRow(($row, item) => {

                let id: string = item[keyname];

                if (expandedKeys.indexOf(id) !== -1) {
                    self.grid().expandRow($row);
                    restored.push(id);
                }
            });

            return restored;
        }

        foreachGridRow(action: ($row: JQuery, item: any) => void): void {
            let self = this;

            this.grid().tbody.find("tr[role=row]").each((idx, elem) => {
                let $row = $(elem);
                action($row, self.grid().dataItem($row));
            });
        }

        private _initGridRows(): void {
            let self = this;

            this.foreachGridRow(($row, item) => {
                self._initRowExpander(item, $row);
                self._initRowEditing(item, $row);
                self._initRowImageCells(item, $row);

                if (self._options.bindRow) {
                    if (typeof self._options.bindRow === "function")
                        self._options.bindRow({
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

            // KABU TODO: REMOVE? Not used. KEEP, may be usefull someday.
            // hideGridManipulationColumnsDefault(grid);
            // hideGridManipulationColumnsLocked(grid);
        }

        private _initRowEditing(item, $row: JQuery): void {

            let $btn = $row.find(".k-grid-custom-edit").first();
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
                let $btn = $(elem);
                let uri = $btn.data("file-uri");

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

            //$row.find(".k-hierarchy-cell").empty();
        }

        // overwrite
        protected _applyAuth(): void {
            let self = this;

            //if (!this.auth.canView)
            //    this.grid().wrapper.hide();

            // Show "add" button in toolbar based on authorization.
            // See http://www.telerik.com/forums/disable-toolbar-button-on-kendo-ui-grid
            if (this.auth.canCreate) {
                this.grid().element.find('.k-grid-toolbar .k-grid-add').removeClass("hide");

                let $createBtn = this.grid().element.find('.k-grid-toolbar .k-grid-custom-add');
                $createBtn.removeClass("hide");

                $createBtn.on("click", function (e) {
                    if (self._state.isEditing)
                        return false;

                    self.add();

                    return false;
                });
            }
        }

        // Navigation to specific data item ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /** 
           Grid view pages can be opened in order to display only a single specific
           item using "cmodo.navigationArgs".
           This will process such navigational arguments, filter the data and modify the UI.
        */
        processNavigation(): any {
            let self = this;

            if (!this._hasBaseFilter(KEY_FILTER_ID))
                return this;

            this.one("dataBound", (e) => {
                // Select the single row.
                self.grid().select(self.grid().items().first());
            });

            // Hide manual filter editors, because we want to display only one specific data item.
            this.grid().tbody.find(".k-filter-row")
                .hide();

            // Display button for deactivation of the "specific-item" filter.
            let $command = this._$toolbar.find(".kmodo-clear-guid-filter-command");

            // Clear single object filter on demand.
            // Views can be called with a item GUID filter which loads only a single specific object.
            // This is used for navigation from other views to a specific object.
            // In order to remove that filter, a "Clear GUID filter" command is placed on the toolbar.      
            $command.on("click", (e) => {

                // Hide the command button.
                $(e.currentTarget).hide(100);

                // Show the grid's filters which were hidden since we displayed only a single object.
                self.grid().thead.find(".k-filter-row")
                    .show(100);

                // Remove single entity filter and reload.
                self._removeBaseFilter(KEY_FILTER_ID);
                self.refresh();
            });

            $command.text("Filter: Navigation");

            $command.addClass("active-toggle-button")
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
            let self = this;

            if (this._state.isEditing)
                return false;

            if (!this._options.editor || !this._options.editor.url)
                return false;

            let item = this.getCurrentItem();

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

            kmodo.openById(this._options.editor.id,
                {
                    mode: mode,
                    itemId: item ? item[this.keyName] : null,
                    // Allow deletion if authorized.
                    canDelete: true,
                    editing: function (e) {
                        self.trigger("editing", e);
                    },
                    finished: function (result) {
                        self._state.isEditing = false;

                        if (result.isOk) {
                            self.refresh()
                                .then(function () {
                                    if (mode === "create" && result.value)
                                        self._trySetCurrentItemById(result.value);
                                });
                        }
                        else if (result.isDeleted) {
                            self.refresh();
                        }
                    }
                });

            return true;
        }

        // Component initialization ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  

        private _initOptions(): kendo.ui.GridOptions {
            let options = this.createGridOptions();

            if (this.createComponentOptionsOverride)
                options = this.createComponentOptionsOverride(options);

            // KABU TODO: VERY IMPORTANT: Check if this works.
            // This is used by the MoFileExplorerView.
            let extraOptions = this._options["componentOptions"];
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
                    //hidden: true,
                    width: 30,
                    //attributes: { 'class': 'list-item-remove-command' },
                    template: kmodo.templates.get("RowRemoveCommandGridCell"),
                    groupable: false,
                    filterable: false,
                    sortable: false
                });
                this._isItemRemoveCommandEnabled = true;
            }

            return options;
        }

        // TODO: REMOVE
        //optionsUseLocalDataSource
        //optionsUseItemRemoveCommand(): void { }
        // TODO: REMOVE
        //optionsSetLocalData(data): void {
        //    this.componentOptions.dataSource.data(data);
        //}

        private _gridGetRowByContent($el: JQuery): JQuery {
            return $el.closest("tr[role=row]");
        }

        private _isTaggable(): boolean {
            return !!this._options.tagsEditorId; // || this._options.isTaggable;
        }

        registerCustomCommand(name: string, execute: Function): void {
            this._customCommands.push({
                name: name,
                execute: execute || null
            });
        }

        private _executeCustomCommand(name: string): void {
            let cmd = this._customCommands.find(x => x.name === name);
            if (!cmd)
                return;

            if (!cmd.execute)
                return;

            cmd.execute();
        }

        private _executeRowCommand(name: string, dataItem: any): void {
            let cmd = this._customRowCommands.find(x => x.name === name);
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
                this.args.filters !== this._options.filters)
                filters = filters.concat(this.args.filters);

            return filters;
        }

        private _hasNonRemovableCompanyFilter(): boolean {

            return null !== kmodo.findDataSourceFilter(this._getAllConsumerFilters(),
                function (filter) {
                    // KABU TODO: MAGIC Company type ID
                    return filter.targetTypeId === "59a58131-960d-4197-a537-6fbb58d54b8a" &&
                        !filter.deactivatable;
                });
        }

        private _updateTagFilterSelector(): void {
            if (this._tagsFilterSelector) {
                let companyFilter = this._findBaseFilter(COMPANY_FILTER_ID);
                let companyId = companyFilter ? companyFilter.value : null;
                let filters = buildTagsDataSourceFilters(this._options.dataTypeId, companyId);

                this._tagsFilterSelector.dataSource.filter(filters);
            }
        }

        createView(): void {

            // Skip if the component was provided externally.

            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

            let self = this;

            // Add base filters from options.
            if (this._options.baseFilters) {
                let baseFilters = this._baseFilters;
                let optionFilters = this._options.baseFilters;
                for (let x of optionFilters) {
                    if (Array.isArray(x))
                        baseFilters.push(...x);
                    else
                        baseFilters.push(x);
                }

            }

            // Evaluate navigation args
            let naviArgs = cmodo.navigationArgs.consume(this._options.id);
            if (naviArgs && naviArgs.itemId) {
                this._setBaseFilter(KEY_FILTER_ID, { field: this.keyName, value: naviArgs.itemId });
            }

            let isCompanyChangeable = !this._hasNonRemovableCompanyFilter();

            if (self._options.isGlobalCompanyFilterEnabled &&
                isCompanyChangeable &&
                // Don't filter by global company if navigating to a specific entity.
                !self._hasBaseFilter(KEY_FILTER_ID)) {

                let companyId = cmodo.getGlobalInitialCompanyId();
                if (companyId)
                    self._setBaseFilter(COMPANY_FILTER_ID, { field: "CompanyId", value: companyId });
            }

            let $component = this._options.$component || null;

            // Find the element of the grid.
            if (!$component)
                $component = $("#grid-" + this._options.id);

            let kendoGridOptions = this._initOptions();

            // Create the Kendo Grid.
            this._kendoGrid = $component.kendoGrid(kendoGridOptions).data('kendoGrid');
            this.setDataSource(this._kendoGrid.dataSource);

            if (kendoGridOptions.selectable === "row") {

                this.grid().tbody.on("mousedown", "tr[role=row]", (e) => {
                    if (e.which === 3) {
                        // Select grid row also on right mouse click.
                        e.stopPropagation();
                        self.grid().select($(e.currentTarget));
                        return false;
                    }
                    return true;
                });
            }

            if (this._isItemRemoveCommandEnabled) {
                this.grid().tbody.on("click", ".list-item-remove-command", (e) => {
                    e.stopPropagation();

                    let $row = self._gridGetRowByContent($(e.currentTarget));
                    let item = self.getItemByRow($row);

                    self.trigger("item-remove-command-fired", { sender: self, item: item, $row: $row });

                    return false;
                });
            }

            // Toolbar commands
            let $toolbar = this._$toolbar = self.grid().wrapper.find(".km-grid-toolbar-content");

            // Refresh command
            $toolbar.find(".k-grid-refresh").on("click", function (e) {
                self.refresh();
                // KABU TODO: REMOVE: self.grid().dataSource.read();
            });

            let initialCompanyId: string = null;
            if (self._options.isCompanyFilterEnabled) {

                let $companySelector = $toolbar.find(".km-grid-company-filter-selector");
                if ($companySelector.length) {

                    if (!isCompanyChangeable) {
                        // The component was instructed to operate on a specific Company.
                        // Do not allow changing that Company.
                        $companySelector.remove();
                    }
                    else {
                        // Use the globally provided current Company if configured to do so.
                        initialCompanyId =
                            (self._options.isGlobalCompanyFilterEnabled &&
                                // Don't filter by global company if navigating to a specific entity.
                                !self._hasBaseFilter(KEY_FILTER_ID))
                                ? cmodo.getGlobalInitialCompanyId()
                                : null;

                        kmodo.createCompanySelector(
                            $companySelector,
                            {
                                companyId: initialCompanyId,
                                changed: (companyId) => {
                                    if (companyId)
                                        self._setBaseFilter(COMPANY_FILTER_ID, { field: "CompanyId", value: companyId });
                                    else
                                        self._removeBaseFilter(COMPANY_FILTER_ID);

                                    self.refresh();

                                    self._updateTagFilterSelector();

                                }
                            }
                        );
                    }
                }
            }

            if (self._options.isTagsFilterEnabled) {
                let $selector = $toolbar.find(".km-grid-tags-filter-selector");
                if ($selector.length) {
                    self._tagsFilterSelector = kmodo.createMoTagFilterSelector(
                        $selector,
                        {
                            // TODO: VERY IMPORTANT: Needs to be updated
                            //  when the company filter changes.
                            filters: kmodo.buildTagsDataSourceFilters(self._options.dataTypeId, initialCompanyId),
                            changed: function (tagIds) {
                                if (tagIds && tagIds.length) {
                                    let expression = tagIds.map(x => "ToTags/any(totag: totag/Tag/Id eq " + x + ")").join(" and ");
                                    self._setBaseFilter(TAGS_FILTER_ID, { expression: expression });
                                }
                                else
                                    self._removeBaseFilter(TAGS_FILTER_ID);

                                self.refresh();
                            }
                        }
                    );
                }
            }

            // Export menu
            let $toolsMenu = $toolbar.find(".km-grid-tools-menu");
            $toolsMenu.kendoMenu({
                openOnClick: true,
                select: function (e) {
                    let name = $(e.item).data("name");

                    // Close menu.
                    e.sender.wrapper.find('.k-animation-container').css('display', 'none');
                    e.sender.close();

                    if (name === "ExportToExcel")
                        self.exportDataTo("excel");
                    else if (name === "ExportToPdf")
                        self.exportDataTo("pdf");
                }
            });

            // Init custom command buttons on the grid's toolbar.

            $toolbar.find(".custom-command").on("click", (e) => {
                self._executeCustomCommand($(e.currentTarget).attr("data-command-name"));
            });

            if (!this._options.isLookup) {

                this.grid().tbody.on("click", ".k-grid-custom-edit", (e) => {
                    if (self._state.isEditing)
                        return true;

                    self._gridSelectByRow(self._gridGetRowByContent($(e.currentTarget)));

                    self.edit();

                    return false;
                });

                // EditTags context menu action.
                if (this._options.hasRowContextMenu) {

                    let $contextMenu = $component.parent().find("#row-context-menu-grid-" + this._options.id);
                    $contextMenu.kendoContextMenu({
                        target: $component,
                        filter: "tr[role=row]",
                        open: function (e) {
                            //let menu = e.sender;
                            //let name = $(e.item).data("name");
                            //let dataItem = self.grid().dataItem(e.target);

                            // KABU TODO: IMPORTANT: Apply auth.
                            //kmodo.enableContextMenuItems(menu, "EditTags", xyz);
                        },
                        select: (e) => {
                            // let menu = e.sender;
                            let name = $(e.item).data("name");
                            let dataItem = self.grid().dataItem(e.target) as any;

                            // KABU TODO: This may be just a temporary hack.
                            //   We could/should use a service instead in the future.
                            if (name === "EditTags" && self._isTaggable()) {
                                // Open tags (MoTags) editor.
                                kmodo.openById(self._options.tagsEditorId,
                                    {
                                        itemId: dataItem[self.keyName],
                                        filters: kmodo.buildTagsDataSourceFilters(self._options.dataTypeId, dataItem.CompanyId as string),
                                        // TODO: LOCALIZE
                                        title: "Markierungen bearbeiten",

                                        minWidth: 400,
                                        minHeight: 500,

                                        finished: function (result) {
                                            if (result.isOk) {
                                                self.refresh();
                                            }
                                        }
                                    });
                            }
                            else {
                                // Custom row commands.
                                self._executeRowCommand(name, dataItem);
                            }
                        }
                    });
                }
            }

            this.grid().tbody.on("click", "span.page-navi", kmodo.onPageNaviEvent);

            if (!this._options.isDialog &&
                this.grid().options.scrollable === true) {

                $(window).resize((e) => {
                    this.updateSize();
                });

            }

            if (this._options.isLookup)
                this._initComponentAsLookup();
        }

        updateSize() {
            // NOTE: The grid's scroll area is not being updated (the k-grid-content has a fixed height)
            //   when the window is being resized. As a workaround the user has to refresh the grid.
            //   For a possible calculation on window resize see: https://www.telerik.com/forums/window-resize-c4fcceedd72c

            let $grid = this.grid().wrapper;

            let gridHeight = $grid.outerHeight(true);

            let extraContentSize = 0;
            $grid.find(">div:not(.k-grid-content)").each((idx, elem) => {
                extraContentSize += $(elem).outerHeight(true);
            });

            let contentHeight = gridHeight - extraContentSize;

            $grid.find(".k-grid-content").first().height(contentHeight);
        }

        _initComponentAsLookup() {
            let self = this;

            // Get dialog arguments and set them on the view model.
            if (!this.args)
                this.setArgs(cmodo.dialogArgs.consume(this._options.id));

            this._dialogWindow = kmodo.findKendoWindow(this.grid().wrapper);
            this._initDialogWindowTitle();

            // KABU TODO: IMPORTANT: There was no time yet to develop a
            //   decorator for dialog functionality. That's why the grid view model
            //   itself has to take care of the dialog commands which are located
            //   *outside* the grid widget.
            let $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", (e) => {
                if (!self.getCurrentItem())
                    return false;

                self.args.buildResult();
                self.args.isCancelled = false;
                self.args.isOk = true;

                self._dialogWindow.close();

                return false;
            });

            $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", (e) => {
                self.args.isCancelled = true;
                self.args.isOk = false;

                self._dialogWindow.close();
                return false;
            });

            let $toolbarLeft = this._$toolbar.find(".km-grid-tools").first();

            let filterCommands = this.args.filterCommands;
            if (filterCommands && filterCommands.length) {

                // Add deactivatable filter buttons to the grid's toolbar.
                // Those represent initially active filters as defined by the caller.
                // The filter/button will be removed by pressing the button.

                for (let i = 0; i < filterCommands.length; i++) {
                    let cmd = filterCommands[i];

                    if (cmd.deactivatable) {
                        let $btn = $("<button class='k-button active-toggle-button'>" + cmd.title + "</button>");
                        $btn.on("click", function (e) {

                            // Remove that filter by field name.
                            // KABU TODO: This means that we can't remove custom/complex filter expressions.
                            self.removeFilterByFieldName(cmd.field);

                            // Hide the command button.
                            $(e.currentTarget).hide(100);

                            // Refresh since filter has changed.
                            self.refresh();
                        });
                        // Append button to grid's toolbar.
                        $toolbarLeft.append($btn);
                    }
                }
            }

            // On row double-click: set dialog result and close the window.
            this.grid().tbody.on("dblclick", "tr[role=row]", function (e) {
                e.preventDefault();
                e.stopPropagation();

                let row = e.currentTarget;

                // If the selected row has a data item...
                if (self.grid().dataItem(row)) {
                    let wnd = kmodo.findKendoWindow(self.grid().wrapper);
                    if (wnd) {
                        self.args.buildResult();
                        self.args.isCancelled = false;
                        self.args.isOk = true;

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
            let filters = this.args.filters;
            if (filters && filters.length) {

                let $thead = self.grid().thead;

                for (let item of filters) {
                    let fieldName = (item as any).field;
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
        private gridViewModel: GridComponent;
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
            let self = this;

            this._performBatchUpdate(function () {
                self.selection = [];
                self._selectionDataItems = [];

                if (self._$allSelector)
                    self._$allSelector.prop("checked", false);

                for (let isel of self._iselectors) {
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

            let isel = this._iselectors.find(function (x) { return x.id === id; });
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
            let self = this;

            let isel = this._iallSelector;
            if (isel.isInitialized)
                return;

            isel.isInitialized = true;

            isel.$selector = this.getKendoGrid().thead.find("input.all-list-items-selector").first();
            isel.isExisting = isel.$selector.length === 1;

            if (isel.isExisting) {
                isel.$selector.on("change", function () {
                    self._onAllSelectorChanged(isel.$selector);
                });
            }
        }

        reinitializeSelectors(): void {
            this._initSelectors();
        }

        private _initSelectors(): void {
            let self = this;

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
                    let $row = $(elem);
                    let $selector = $row.find("input.list-item-selector");
                    let $selectorVisual = $row.find("label.list-item-selector");

                    let item = self.getKendoGrid().dataItem($row);
                    let id = item[self.keyName];
                    let isSelected = self._getIsSelectedById(id);

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

                for (let isel of self._iselectors) {

                    if (self.customSelectorInitializer) {
                        // Hook for the consumer in order to provide custom logic for the
                        // initial states of a selector such as isSelected, isEnabled and isVisible.
                        let custom = self.customSelectorInitializer(isel);

                        if (custom.isVisible !== undefined)
                            isel.isVisible = custom.isVisible;

                        if (custom.isSelected !== undefined)
                            isel.isSelected = custom.isSelected;

                        if (custom.isEnabled !== undefined)
                            isel.isEnabled = custom.isEnabled;
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
                }
            }
            finally {
                self._isUpdatingBatch = false;
                self._isUpdatingSelectors = false;
                self._isSelectorBindingPending = false;
            }
        }

        private _getDataSource(): kendo.data.DataSource {
            return this.gridViewModel.dataSource;
        }

        private _updateSelectedViewStates(): void {
            let self = this;
            for (let isel of this._iselectors) {
                isel.isSelected = self._getIsSelectedById(isel.id);
                isel.$selector.prop("checked", isel.isSelected);
            }
        }

        _onAllSelectorChanged($selector: JQuery): void {

            if (this._isUpdatingSelectors)
                return;

            let self = this;

            this._performBatchUpdate(function () {

                let add = $selector.prop("checked") === true;

                let items = self._getDataSource().data();
                items.forEach((item) => {
                    if (add)
                        self._addDataItem(item);
                    else
                        self._removeDataItem(item);
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
            let index = this.selection.indexOf(item[this.keyName]);
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

            let index = this.selection.indexOf(id);
            if (index === -1)
                return;

            let item = this._selectionDataItems[index];

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