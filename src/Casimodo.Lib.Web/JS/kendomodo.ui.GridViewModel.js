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
                        this.space.createComponentOptionsOverride = componentArgs.createComponentOptionsOverride;
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

                    self._initRowEditing(item, $row);
                    self._initRowImageCells(item, $row);
                });

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

                var args = casimodo.ui.navigationArgs.consume(this._options.id);
                if (!args)
                    return this;

                this.one("dataBound", function (e) {
                    // Select the single row.
                    self.component.select(self.component.items().first());
                });

                // Hide manual filter editors, because we want to display only one specific data item.
                this.component.tbody.find(".k-filter-row")
                    .hide();

                this.setArgs(args);

                // Display button for deactivation of the "specific-item" filter.
                this.component.wrapper.find(".k-grid-toolbar .kmodo-clear-guid-filter-command")
                    .addClass("active-filter-toggle-button")
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
                        }
                    });

                return true;
            };

            // Component initialization ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~           

            fn.createComponent = function () {
                if (this.component)
                    return this.component;

                var self = this;

                var $component = this._options.$component || null;

                // Find the element of the grid.
                if (!$component)
                    $component = $('#grid-' + this._options.id);

                // Create the grid component.
                // KABU TODO: createComponentOptions() is located in the CSHTML file because
                //   we still need Razor for some of kendo.ui.Grid's column definitions.
                var kendoGridOptions = this.space.createComponentOptions();
                // Attach event handlers.
                kendoGridOptions.dataBinding = this._eve(this.onComponentDataBinding);
                kendoGridOptions.dataBound = this._eve(this.onComponentDataBound);
                kendoGridOptions.change = this._eve(this.onComponentChanged);

                if (kendoGridOptions.detailTemplate) {
                    kendoGridOptions.detailInit = this._eve(this.onComponentDetailInit);
                    kendoGridOptions.detailExpand = this._eve(this.onComponentDetailExpanding);
                    kendoGridOptions.detailCollapse = this._eve(this.onComponentDetailCollapsing);
                }

                var kendoGrid = $component.kendoGrid(kendoGridOptions).data('kendoGrid');
                this.space.component = kendoGrid;
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
                        }
                    });

                }

                // Toolbar commands
                var $toolbar = self.component.wrapper.find(".k-grid-toolbar > .toolbar");

                // Refresh command
                $toolbar.find(".k-grid-refresh").on("click", function (e) {
                    self.refresh();
                    // KABU TODO: REMOVE: self.component.dataSource.read();
                });

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
                    // Clear single object filter on demand.
                    // Views can be called with a item GUID filter which loads only a single specific object.
                    // This is used for navigation from other views to a specific object.
                    // In order to remove that filter, a "Clear GUID filter" command is placed on the toolbar.      
                    $toolbar.find(".kmodo-clear-guid-filter-command").on("click", function (e) {

                        // Hide the command button.
                        $(this).hide(100);

                        // Show the grid's filters which were hidden since we displayed only a single object.
                        self.component.thead.find(".k-filter-row")
                            .show(100);

                        // Clear filters and reload.
                        self.applyFilter(null);
                    });


                    this.component.tbody.on("click", ".k-grid-custom-edit", function (e) {
                        if (self._state.isEditing)
                            return;

                        self._gridSelectByRow($(this).closest("tr[role=row]"));

                        self.edit();

                        return false;
                    });
                }

                this.component.tbody.on("click", "span.page-navi", function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    var $el = $(this);
                    var part = $el.data("navi-part");
                    var id = $el.data("navi-id");

                    kendomodo.ui.navigate(part, id);

                    return false;
                });

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

                var filterCommands = this.args.filterCommands;
                if (typeof filterCommands !== "undefined" && filterCommands.length) {

                    // Add deactivatable filter buttons to the grid's toolbar.
                    // Those represent initially active filters as defined by the caller.
                    // The filter/button will be removed by pressing the button.

                    var $toolbar = this.component.wrapper.find(".k-grid-toolbar > .toolbar");

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
                            $toolbar.append($btn);
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
