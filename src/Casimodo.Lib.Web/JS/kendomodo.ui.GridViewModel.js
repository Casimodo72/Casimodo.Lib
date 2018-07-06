"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        // kendo.ui.Grid:    https://docs.telerik.com/kendo-ui/api/javascript/ui/grid
        // kendo.DataSource: https://docs.telerik.com/kendo-ui/api/javascript/data/datasource

        var GridViewModel = (function (_super) {
            casimodo.__extends(GridViewModel, _super);

            function GridViewModel(options) {
                _super.call(this, options);
                this._super = _super;

                var self = this;

                var componentArgs = casimodo.ui.componentArgs.consume(this._options.id);
                if (componentArgs) {
                    if (componentArgs.createComponentOptionsOverride)
                        this.createComponentOptionsOverride = componentArgs.createComponentOptionsOverride;
                }

                this.expandedKeys = [];

                this.selectionMode = options.selectionMode || "single";

                var state = this._state = {
                    gridDataBindAction: null,
                    lastCurrentItemId: null,
                    gridLastScrollTop: null,
                    isRestoringExpandedRows: false,
                    isEditing: false
                };

                this.selectionManager = new kendomodo.ui.GridViewSelectionManager({
                    gridViewModel: this
                });

                if (this.selectionMode === "multiple") {
                    this.on("dataBound", $.proxy(self.selectionManager._onDataSourceDataBound, self.selectionManager));
                }

                this.componentOptions = null;
                this._onListItemRemoveCommandClicked = null;
            }

            var fn = GridViewModel.prototype;

            fn.createReadQuery = function () {
                this._throwAbstractFunc("createReadQuery");
            };

            fn.createDataSourceOptions = function () {
                this._throwAbstractFunc("createDataSourceOptions");
            };

            fn.createDataModel = function () {
                this._throwAbstractFunc("createDataModel");
            };

            fn.extendDataSourceOptions = function (options) {
                // Attach event handlers.
                options.error = this._eve(this.onDataSourceError);
                options.requestEnd = this._eve(this.onDataSourceRequestEnd);
            };

            // overwrite
            fn.setComponent = function (value) {
                this.component = value;
                this.setDataSource(this.component.dataSource);
                this.selectionManager.kendoGrid = value;
            };

            fn._startRefresh = function () {
                // NOP. Don't use busy indicator because that's already done by the kendo grid.
            };

            fn._endRefresh = function () {
                // NOP. Don't use busy indicator because that's already done by the kendo grid.
            };

            /**
                Handler for kendo.ui.Grid's event "dataBinding".
                @param {any} e - Event 
            */

            fn.onComponentDataBinding = function (e) {

                if (this._isDebugLogEnabled)
                    console.debug("- data binding: action: '%s'", e.action)

                this._state.gridDataBindAction = e.action;

                if (e.action === "rebind") {
                    // This is a refresh.

                    if (this.component.options.scrollable) {
                        // Compute current scroll top position. This will be restored after the refresh.
                        var content = this.component.content;
                        this._state.gridLastScrollTop = content.length ? content[0].scrollTop : null;
                    }

                    // Save current item. It will be restored after the refresh.
                    var item = this.getCurrentItem();
                    this._state.lastCurrentItemId = item ? item[this.keyName] : null;;
                    this.setCurrentItem(null);
                }

                this.trigger('dataBinding', e);
            };

            /**
                Handler for kendo.ui.Grid's event "dataBound".
                @param {any} e - Event 
            */
            fn.onComponentDataBound = function (e) {

                if (this._isDebugLogEnabled)
                    console.debug("- data bound");

                var action = this._state.gridDataBindAction;

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

                    this.component.content[0].scrollTop = this._state.gridLastScrollTop;
                }

                this.trigger('dataBound', e);
            };

            fn._trySetCurrentItemById = function (id) {
                var self = this;

                var item = this.getCurrentItem();
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
                this.component.clearSelection();

                return false;
            };

            fn.gridSelectByItem = function (item) {
                this._gridSelectByItem(item);
            };

            fn._gridSelectByItem = function (item) {
                this.component.select(this._gridGetRowByItem(item));
            };

            fn._gridGetRowByItem = function (item) {
                return this.component.tbody.find("tr[role=row][data-uid='" + item.uid + "']");
            };

            fn._gridSelectByRow = function ($row) {
                this.component.select($row);
            };

            fn.getItemByRow = function ($row) {
                return this.component.dataItem($row);
            };

            /**
                Handler for kendo.ui.Grid's event "change".
                Triggered when the grid's row(s) selection changed.
                @param {any} e - Event 
            */
            fn.onComponentChanged = function (e) {
                if (this._isDebugLogEnabled)
                    console.debug("- changed");

                this.trigger('changed', e);

                if (this.selectionMode === "single")
                    this.setCurrentItem(this.component.dataItem(this.component.select()));
            };

            /**
                Handler for kendo.ui.Grid's event "detailInit".
            */
            fn.onComponentDetailInit = function (e) {
                // This must be performed, otherwise Kendo grid inline detail template bindings will not work.
                // See http://jsfiddle.net/jeastburn/5MU4r/
                kendo.bind(e.detailCell, e.data);

                this.trigger('detailInit', e);
            };

            /**
                Handler for kendo.ui.Grid's event "detailExpand".
            */
            fn.onComponentDetailExpanding = function (e) {

                // KABU TODO: Animation doesn't work, although advertised by Telerik.
                //var detailRow = e.detailRow;
                //kendo.fx(detailRow).fadeIn().play();               

                if (!this._state.isRestoringExpandedRows) {

                    var id = this.component.dataItem(e.masterRow)[this.keyName];
                    if (id)
                        this.expandedKeys.push(id);
                }

                this.trigger('detailExpanding', e);
            };

            /**
                Handler for kendo.ui.Grid's event "detailCollapse".
            */
            fn.onComponentDetailCollapsing = function (e) {

                // KABU TODO: Animation doesn't work, although advertised by Telerik.
                /*
                var detailRow = e.detailRow;
                setTimeout(function () {
                    kendo.fx(detailRow).fadeOut().duration(375).play();
                }, 0);
                */

                if (!this._state.isRestoringExpandedRows) {
                    var id = this.component.dataItem(e.masterRow)[this.keyName];
                    var idx = this.expandedKeys.indexOf(id);
                    if (idx !== -1)
                        this.expandedKeys.splice(idx, 1);
                }

                this.trigger('detailCollapsing', e);
            };

            /**
                Restores previously expanded rows.
                See http://www.telerik.com/forums/saving-expanded-detail-rows
            */
            fn._restoreExpandedRows = function (e, expandedKeys, keyname) {

                var self = this;

                if (!expandedKeys || !expandedKeys.length) return [];

                var restored = [];

                this.foreachGridRow(function ($row, item) {

                    var id = item[keyname];

                    if (expandedKeys.indexOf(id) !== -1) {
                        self.component.expandRow($row);
                        restored.push(id);
                    }
                });

                return restored;
            }

            fn.foreachGridRow = function (action) {
                var self = this;

                this.component.tbody.find("tr[role=row]").each(function () {
                    var $row = $(this);

                    action($row, self.component.dataItem($row));
                });
            };

            fn._initGridRows = function (grid) {
                var self = this;

                this.foreachGridRow(function ($row, item) {
                    self._initRowExpander(item, $row);
                    self._initRowEditing(item, $row);
                    self._initRowImageCells(item, $row);
                });

                if (this._options.isRowAlwaysExpanded) {
                    this.component.wrapper.find(".k-hierarchy-cell").empty().hide();
                    this.component.wrapper.find(".k-detail-row > td:first-child").hide();
                }

                // KABU TODO: REMOVE? Not used. KEEP, may be usefull someday.
                // hideGridManipulationColumnsDefault(grid);
                // hideGridManipulationColumnsLocked(grid);
            };

            fn._initRowEditing = function (item, $row) {
                var self = this;

                var $btn = $row.find(".k-grid-custom-edit").first();
                if (!$btn.length)
                    return;

                // Editing is supported only for the "single" selectionMode.
                if (this.selectionMode === "single" && this._canModifyItem(item))
                    $btn.show();
                else
                    $btn.remove();
            };

            fn._canModifyItem = function (item) {
                return item && this.auth.canModify === true && item.IsReadOnly === false;
            };

            fn._initRowImageCells = function (item, $row) {
                // KABU TODO: DISABLED - REMOVE? Not used. Was intended for "PhotoCellTemplate".
                //   But currently we don't display images anymore in the grid.
                //   KEEP: maybe we can use this in the future.

                $row.find(".kendomodo-button-show-image").each(function (elem) {
                    var $btn = $(elem);
                    var uri = $btn.data("file-uri");

                    $btn.magnificPopup({
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

            fn._initRowExpander = function (item, $row) {
                if (!this._options.isRowAlwaysExpanded)
                    return;

                if (!$row.hasClass("k-master-row"))
                    return;

                this.component.expandRow($row);

                //$row.find(".k-hierarchy-cell").empty();
            };

            // overwrite
            fn._applyAuth = function () {
                var self = this;

                //if (!this.auth.canView)
                //    this.component.wrapper.hide();

                // Show "add" button in toolbar based on authorization.
                // See http://www.telerik.com/forums/disable-toolbar-button-on-kendo-ui-grid
                if (this.auth.canCreate) {
                    this.component.element.find('.k-grid-toolbar .k-grid-add').removeClass("hide");

                    var $createBtn = this.component.element.find('.k-grid-toolbar .k-grid-custom-add');
                    $createBtn.removeClass("hide");

                    $createBtn.on("click", function (e) {
                        e.preventDefault();
                        e.stopPropagation();

                        if (self._state.isEditing)
                            return false;

                        self.add();

                        return false;
                    });
                }
            };

            // Navigation to specific data item ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            /** 
               Grid view pages can be opened in order to display only a single specific
               item using "casimodo.ui.navigationArgs".
               This will process such navigational arguments, filter the data and modify the UI.
            */
            fn.processNavigation = function () {
                var self = this;

                if (!this._hasBaseFilter(KEY_FILTER_ID))
                    return this;

                this.one("dataBound", function (e) {
                    // Select the single row.
                    self.component.select(self.component.items().first());
                });

                // Hide manual filter editors, because we want to display only one specific data item.
                this.component.tbody.find(".k-filter-row")
                    .hide();

                // Display button for deactivation of the "specific-item" filter.
                var $command = this._$toolbar.find(".kmodo-clear-guid-filter-command");

                // Clear single object filter on demand.
                // Views can be called with a item GUID filter which loads only a single specific object.
                // This is used for navigation from other views to a specific object.
                // In order to remove that filter, a "Clear GUID filter" command is placed on the toolbar.      
                $command.on("click", function (e) {

                    // Hide the command button.
                    $(this).hide(100);

                    // Show the grid's filters which were hidden since we displayed only a single object.
                    self.component.thead.find(".k-filter-row")
                        .show(100);

                    // Remove single entity filter and reload.
                    self._removeBaseFilter(KEY_FILTER_ID);
                    self.refresh();
                });

                $command.text("Filter: Navigation");

                $command.addClass("active-filter-toggle-button")
                    .show();

                return this;
            };

            fn.add = function () {
                return this._addOrEdit("create");
            };

            fn.edit = function () {
                return this._addOrEdit("modify");
            };

            fn._addOrEdit = function (mode) {
                var self = this;

                if (this._state.isEditing)
                    return;

                if (!this._options.editor || !this._options.editor.url)
                    return false;

                var item = this.getCurrentItem();

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

                kendomodo.ui.openById(this._options.editor.id,
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
            };

            // Component initialization ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  

            fn.initComponentOptions = function () {
                if (this.componentOptions)
                    return this.componentOptions;

                this.componentOptions = this.createComponentOptions();

                if (this._options.isDetailsEnabled === false)
                    delete this.componentOptions.detailTemplate;

                // Attach event handlers.
                this.componentOptions.dataBinding = this._eve(this.onComponentDataBinding);
                this.componentOptions.dataBound = this._eve(this.onComponentDataBound);
                this.componentOptions.change = this._eve(this.onComponentChanged);

                if (this.componentOptions.detailTemplate) {
                    this.componentOptions.detailInit = this._eve(this.onComponentDetailInit);
                    this.componentOptions.detailExpand = this._eve(this.onComponentDetailExpanding);
                    this.componentOptions.detailCollapse = this._eve(this.onComponentDetailCollapsing);
                }

                return this.componentOptions;
            };

            fn.optionsUseItemRemoveCommand = function () {
                this.componentOptions.columns.push({
                    field: 'ListItemRemoveCommand',
                    title: ' ',
                    //hidden: true,
                    width: 30,
                    //attributes: { 'class': 'list-item-remove-command' },
                    template: kendomodo.ui.templates.get("RowRemoveCommandGridCell"),
                    groupable: false,
                    filterable: false,
                    sortable: false
                });

                this._isItemRemoveCommandEnabled = true;
            };

            fn.optionsUseLocalDataSource = function (data) {
                this.componentOptions.dataSource = new kendo.data.DataSource({
                    schema: {
                        model: this.createDataModel()
                    },
                    data: data || [],
                    pageSize: 0
                });
            };

            fn.optionsSetLocalData = function (data) {
                this.componentOptions.dataSource.data(data);
            };

            fn._gridGetRowByContent = function ($el) {
                return $el.closest("tr[role=row]");
            };

            fn._isTaggable = function () {
                return !!this._options.tagsEditorId; // || this._options.isTaggable;
            };

            fn.exportDataTo = function (target) {
                if (target === "excel")
                    this.component.saveAsExcel();
                else if (target === "pdf")
                    this.component.saveAsPDF();
            };

            fn._getAllConsumerFilters = function () {

                var filters = this._options.filters || [];

                if (this.args && this.args.filters && this.args.filters !== this._options.filters)
                    filters = filters.concat(this.args.filters);

                return filters;
            }

            fn._hasNonRemovableCompanyFilter = function () {

                return !!kendomodo.findDataSourceFilter(this._getAllConsumerFilters(),
                    function (filter) {
                        // KABU TODO: MAGIC Company type ID
                        return filter.targetTypeId === "59a58131-960d-4197-a537-6fbb58d54b8a" &&
                            !filter.deactivatable;
                    });
            };

            fn._updateTagFilterSelector = function () {

                if (this._tagsFilterSelector) {
                    var companyFilter = this._findBaseFilter(COMPANY_FILTER_ID);
                    var companyId = companyFilter ? companyFilter.value : null;
                    var filters = kendomodo.buildTagsDataSourceFilters(this._options.dataTypeId, companyId);

                    this._tagsFilterSelector.dataSource.filter(filters);
                }
            };

            fn._getCurrentCompanyId = function () {

            };

            var KEY_FILTER_ID = "5883120a-b1a6-4ac8-81a2-1d23028daebe";
            var COMPANY_FILTER_ID = "b984dd7b-5d7a-48bf-b7f1-db26522c63df";
            var TAGS_FILTER_ID = "2bd9e0d8-7b2d-4c1e-90c0-4d7eac6d01a4";

            fn.createComponent = function () {
                if (this.component)
                    return this.component;

                var self = this;

                this.initBaseFilters();

                // Evaluate navigation args
                var naviArgs = casimodo.ui.navigationArgs.consume(this._options.id);
                if (naviArgs && naviArgs.itemId) {
                    this._setBaseFilter(KEY_FILTER_ID, { field: this.keyName, value: naviArgs.itemId });
                }

                var isCompanyChangeable = !this._hasNonRemovableCompanyFilter();

                if (self._options.isGlobalCompanyFilterEnabled &&
                    isCompanyChangeable &&
                    // Don't filter by global company if navigating to a specific entity.
                    !self._hasBaseFilter(KEY_FILTER_ID)) {

                    var companyId = casimodo.getGlobalInitialCompanyId();
                    if (companyId)
                        self._setBaseFilter(COMPANY_FILTER_ID, { field: "CompanyId", value: companyId });
                }

                var $component = this._options.$component || null;

                // Find the element of the grid.
                if (!$component)
                    $component = $("#grid-" + this._options.id);

                var kendoGridOptions = this.initComponentOptions();

                // Create the Kendo Grid.
                var kendoGrid = $component.kendoGrid(kendoGridOptions).data('kendoGrid');

                this.setComponent(kendoGrid);

                //var validator = $component.kendoValidator({
                //    errorTemplate: '<span class="">' + '<span class="k-icon k-warning"> </span> #=message#</span>',
                //}).data('kendoValidator');

                if (kendoGridOptions.selectable && kendoGridOptions.selectable.mode === "row") {

                    this.component.tbody.on("mousedown", "tr[role=row]", function (e) {
                        if (e.which === 3) {
                            // Select grid row also on right mouse click.
                            e.stopPropagation();
                            self.component.select($(this));
                            return false;
                        }
                    });
                }

                if (this._isItemRemoveCommandEnabled) {
                    this.component.tbody.on("click", ".list-item-remove-command", function (e) {
                        e.stopPropagation();

                        var $row = self._gridGetRowByContent($(this));
                        var item = self.getItemByRow($row);

                        self.trigger("item-remove-command-fired", { sender: this, item: item, $row: $row });

                        return false;
                    });
                }

                // Toolbar commands
                var $toolbar = this._$toolbar = self.component.wrapper.find(".km-grid-toolbar-content");

                // Refresh command
                $toolbar.find(".k-grid-refresh").on("click", function (e) {
                    self.refresh();
                    // KABU TODO: REMOVE: self.component.dataSource.read();
                });

                var initialCompanyId = null;
                if (self._options.isCompanyFilterEnabled) {

                    var $companySelector = $toolbar.find(".km-grid-company-filter-selector");
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
                                    ? casimodo.getGlobalInitialCompanyId()
                                    : null;

                            self._companyFilterSelector = kendomodo.ui.createCompanySelector(
                                $companySelector,
                                function (companyId) {
                                    if (companyId)
                                        self._setBaseFilter(COMPANY_FILTER_ID, { field: "CompanyId", value: companyId });
                                    else
                                        self._removeBaseFilter(COMPANY_FILTER_ID);

                                    self.refresh();

                                    self._updateTagFilterSelector();

                                },
                                { CompanyId: initialCompanyId }
                            );
                        }
                    }
                }

                if (self._options.isTagsFilterEnabled) {
                    var $selector = $toolbar.find(".km-grid-tags-filter-selector");
                    if ($selector.length) {
                        self._tagsFilterSelector = kendomodo.ui.createMoTagFilterSelector(
                            $selector,
                            {
                                changed: function (tagId) {
                                    if (tagId) {
                                        var expression = "Tags/any(tag: tag/Id eq " + tagId + ")";
                                        self._setBaseFilter(TAGS_FILTER_ID, { expression: expression });
                                    }
                                    else
                                        self._removeBaseFilter(TAGS_FILTER_ID);

                                    self.refresh();
                                },
                                filters: kendomodo.buildTagsDataSourceFilters(self._options.dataTypeId, initialCompanyId)
                            }
                        );
                    }
                }

                // Export menu
                var $toolsMenu = $toolbar.find(".km-grid-tools-menu");
                $toolsMenu.kendoMenu({
                    openOnClick: true,
                    select: function (e) {
                        var name = $(e.item).data("name");

                        e.sender.close();

                        if (name === "ExportToExcel")
                            self.exportDataTo("excel");
                        else if (name === "ExportToPdf")
                            self.exportDataTo("pdf");
                    }
                });
                //$toolsMenu.find(">li").first().find("span.k-link").first().css("padding", "0px").css("padding-top", "6px");

                // Init custom command buttons on the grid's toolbar.
                // KABU TODO: CLARIFY: What was "self.extension" about?
                if (this.extension) {
                    $toolbar.find(".custom-command").on("click", function (e) {
                        var elem = $(this);
                        var name = elem.attr("name");
                        self.executeCustomCommand({ source: elem, name: name });
                    });
                }

                if (!this._options.isLookup) {

                    this.component.tbody.on("click", ".k-grid-custom-edit", function (e) {
                        if (self._state.isEditing)
                            return;

                        self._gridSelectByRow(self._gridGetRowByContent($(this)));

                        self.edit();

                        return false;
                    });

                    // EditTags context menu action.
                    if (this._isTaggable()) {

                        var $contextMenu = $component.parent().find("#tags-context-menu-grid-" + this._options.id);
                        $contextMenu.kendoContextMenu({
                            target: $component,
                            filter: "tr[role=row]",
                            open: function (e) {
                                var menu = e.sender;
                                var name = $(e.item).data("name");
                                var grid = self.component;
                                var dataItem = grid.dataItem(e.target);

                                // KABU TODO: IMPORTANT: Apply auth.
                                //kendomodo.enableContextMenuItems(menu, "EditTags", xyz);
                            },
                            select: function (e) {
                                var menu = e.sender;
                                var name = $(e.item).data("name");
                                var grid = self.component;
                                var dataItem = grid.dataItem(e.target);

                                // KABU TODO: This may be just a temporary hack.
                                //   We could / should use a service instead in the future.
                                if (name === "EditTags") {
                                    // Open tags (MoTags) editor.
                                    kendomodo.ui.openById(self._options.tagsEditorId,
                                        {
                                            itemId: dataItem[self.keyName],
                                            filters: kendomodo.buildTagsDataSourceFilters(self._options.dataTypeId, dataItem.CompanyId),
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
                            }
                        });
                    }
                }

                this.component.tbody.on("click", "span.page-navi", kendomodo.ui.onPageNaviEvent);

                if (!this._options.isDialog &&
                    this.component.options.scrollable === true) {
                    // NOTE: The grid's scroll area is not being updated (the k-grid-content has a fixed height)
                    //   when the window is being resized. As a workaround the user has to refresh the grid.
                    //   For a possible calculation on window resize see: https://www.telerik.com/forums/window-resize-c4fcceedd72c

                    $(window).resize(function (e) {
                        var $grid = self.component.wrapper;

                        var gridHeight = $grid.outerHeight(true);

                        var extraContentSize = 0;
                        $grid.find(">div:not(.k-grid-content)").each(function () {
                            extraContentSize += $(this).outerHeight(true);
                        });

                        var contentHeight = gridHeight - extraContentSize;

                        $grid.find(".k-grid-content").first().height(contentHeight);
                    });

                }

                if (this._options.isLookup)
                    this._initComponentAsLookup();
            };

            fn._initComponentAsLookup = function () {
                var self = this;

                // Get dialog arguments and set them on the view model.
                if (!this.args)
                    this.setArgs(casimodo.ui.dialogArgs.consume(this._options.id));

                this._dialogWindow = kendomodo.findKendoWindow(this.component.wrapper);
                this._initDialogWindowTitle();

                // KABU TODO: IMPORTANT: There was no time yet to develop a
                //   decorator for dialog functionality. That's why the grid view model
                //   itself has to take care of the dialog commands which are located
                //   *outside* the grid widget.
                var $dialogCommands = $('#dialog-commands-' + this._options.id);
                // Init OK/Cancel buttons.
                $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", function () {
                    if (!self.getCurrentItem())
                        return false;

                    self.args.buildResult();
                    self.args.isCancelled = false;
                    self.args.isOk = true;

                    self._dialogWindow.close();
                });

                $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", function () {
                    self.args.isCancelled = true;
                    self.args.isOk = false;

                    self._dialogWindow.close();
                });

                var $toolbarLeft = this._$toolbar.find(".km-grid-tools").first();

                var filterCommands = this.args.filterCommands;
                if (filterCommands && filterCommands.length) {

                    // Add deactivatable filter buttons to the grid's toolbar.
                    // Those represent initially active filters as defined by the caller.
                    // The filter/button will be removed by pressing the button.

                    for (var i = 0; i < filterCommands.length; i++) {
                        var cmd = filterCommands[i];

                        if (cmd.deactivatable) {
                            var $btn = $("<button class='k-button active-filter-toggle-button'>" + cmd.title + "</button>");
                            $btn.on("click", function (e) {

                                // Remove that filter by field name.
                                // KABU TODO: This means that we can't remove custom/complex filter expressions.
                                self.removeFilterByFieldName(cmd.field);

                                // Hide the command button.
                                $(this).hide(100);

                                // Refresh since filter has changed.
                                self.refresh();
                            });
                            // Append button to grid's toolbar.
                            $toolbarLeft.append($btn);
                        }
                    }
                }

                // On row double-click: set dialog result and close the window.
                this.component.tbody.on("dblclick", "tr[role=row]", function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    var row = e.currentTarget;
                    var selectedItem = self.component.dataItem(row);
                    if (selectedItem) {
                        var wnd = kendomodo.findKendoWindow(self.component.wrapper);
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
                var filters = this.args.filters;
                if (filters && filters.length) {

                    var $thead = self.component.thead;

                    filters.forEach(function (item) {
                        // KABU TODO: Should we remove those cells instead of just hiding them?
                        //   Dunno, maybe the kendo.ui.Grid uses this UI in order to construct its
                        //   filter expression. Experiment with it.

                        // Hide column menu where one can also change the filter.
                        var cell = $thead.find("th.k-header[data-field='" + item.field + "'] > a.k-header-column-menu")
                            .first()
                            .css("visibility", "hidden");

                        // Hide filter cell.
                        cell = $thead.find(".k-filtercell[data-field='" + item.field + "']")
                            .first()
                            .css("visibility", "hidden");
                    });
                }
            };

            fn._initDialogWindowTitle = function () {
                var title = "";

                if (this.args.title) {
                    title = this.args.title;
                }
                else {
                    title = this._options.title || "";

                    if (this._options.isLookup)
                        title += " wählen";
                }

                this._dialogWindow.title(title);
            };

            function _throwGeneratedFunc(name) {
                throw new Error("Not implemented. The function '" + name + "' is expected to be overwritten by generated VM code.");
            }

            // Obsolete or postponed ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // KABU TODO: REMOVE? KEEP, may be usefull someday.
            // See http://www.telerik.com/forums/way-to-show-remind-user-that-a-column-is-applying-a-filter-
            /*
            kendomodo.hideGridManipulationColumnsDefault = function (grid) {
                var filter = grid.dataSource.filter();
                grid.thead.find(".k-header-column-menu.k-state-active").removeClass("k-state-active");
                if (filter) {
                    var filteredMembers = {};
                    this._setFilteredMembers(filter, filteredMembers);
                    grid.thead.find("th[data-field]").each(function () {
                        var cell = $(this);
                        var filtered = filteredMembers[cell.data("field")];
                        if (filtered) {
                            cell.find(".k-header-column-menu").addClass("k-state-active");
                        }
                    });
                }
            };
            */

            // KABU TODO: REMOVE? KEEP, may be usefull someday.
            // See http://www.telerik.com/forums/way-to-show-remind-user-that-a-column-is-applying-a-filter-
            /*
            fn._hideGridManipulationColumnsLocked = function (grid) {
                var filter = grid.dataSource.filter();
             
                grid.thead.find(".k-header-column-menu.k-state-active").removeClass("k-state-active");
                grid.wrapper.find(".k-grid-header-locked .k-header-column-menu.k-state-active").removeClass("k-state-active");
             
                if (filter) {
                    var filteredMembers = {};
                    this._setFilteredMembers(filter, filteredMembers);
             
                    var heads = grid.thead.find("th[data-field]");
                    var frozenHeads = grid.wrapper.find(".k-grid-header-locked th[data-field]");
             
                    heads.add(frozenHeads).each(function () {
                        var cell = $(this);
                        var filtered = filteredMembers[cell.data("field")];
                        if (filtered) {
                            cell.find(".k-header-column-menu").addClass("k-state-active");
                        }
                    });
                }
            };
            */

            // KABU TODO: REMOVE? KEEP, may be usefull someday.
            // See http://www.telerik.com/forums/way-to-show-remind-user-that-a-column-is-applying-a-filter-
            /*
            fn._setFilteredMembers = function (filter, members) {
                if (filter.filters) {
                    for (var i = 0; i < filter.filters.length; i++) {
                        this._setFilteredMembers(filter.filters[i], members);
                    }
                }
                else {
                    members[filter.field] = true;
                }
            };
            */

            return GridViewModel;

        })(kendomodo.ui.DataComponentViewModel);
        ui.GridViewModel = GridViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
