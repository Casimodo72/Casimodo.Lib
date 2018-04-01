"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        ui._componentCache = [];

        ui.navigate = function (part, itemId) {
            ui._openCore(casimodo.ui.componentRegistry.items.find(x => x.part === part && x.role === "Page"), { itemId: itemId });
        };

        ui.openById = function (viewId, options, finished) {
            ui._openCore(casimodo.ui.componentRegistry.items.find(x => x.id === viewId), options, finished);
        };

        ui._openCore = function (reg, options, finished) {
            var self = this;

            if (!reg) return;

            options = options || {};
            finished = finished || null;

            if (!options.finished)
                options.finished = finished;

            var args;
            var vm;

            if (reg.role === "Page") {
                var listReg = reg;

                // TODO: REMOVE

                // Try to find a matching "List" view in same group.
                listReg = casimodo.ui.componentRegistry.items
                    .find(x => x.part === reg.part && x.group === reg.group && x.role === "List");

                if (!listReg)
                    return;

                casimodo.ui.navigationArgs.add({
                    id: listReg.id,
                    filters: [{ field: "Id", operator: "eq", value: options.itemId }]
                });

                window.open(reg.url, "");
            }
            else if (reg.role === "Editor") {
                args = new casimodo.ui.DialogArgs(reg.id);
                args.mode = options.mode;
                args.itemId = options.itemId;
                args.value = options.value;
                args.params = options.params;
                args.title = options.title || null;

                vm = self._createViewModel(reg, options);
                options.vm = vm;
                vm.setArgs(args);

                ui._openModalWindow(reg, options, args, function () {

                    vm.start();
                });
            }
            else if (reg.role === "Lookup" || reg.role === "List") {
                args = new casimodo.ui.DialogArgs(reg.id);
                
                args.filters = options.filters;
                args.filterCommands = options.filterCommands;               
                args.item = options.item;                
                args.title = options.title || null;

                vm = self._createViewModel(reg, options);
                options.vm = vm;
                vm.setArgs(args);

                ui._openModalWindow(reg, options, args, function () {

                    vm.refresh();
                });
            }
        };

        ui._createViewModel = function (reg, options) {

            var vm = null;
            var cachedEntry = null;

            if (reg.isCached) {
                cachedEntry = this._componentCache.find(x => x.id === reg.id);
                if (cachedEntry)
                    vm = cachedEntry.vm;
            }

            if (!vm)
                vm = casimodo.ui.componentRegistry.createViewModelOnly(reg);

            if (reg.isCached && !cachedEntry) {
                kendomodo.ui._componentCache.push({ id: reg.id, vm: vm });
            }

            if (typeof options.editing === "function") {
                if (reg.isCached)
                    throw new Error("Event assignment not allowed for cached view models.");

                vm.on("editing", options.editing);
            }

            return vm;
        };

        ui._openModalWindow = function (reg, options, args, loaded) {

            var wnd = null;
            var cachedEntry = null;
            var initialCreate = true;

            if (reg.isCached) {
                cachedEntry = this._componentCache.find(x => x.id === reg.id);
                if (cachedEntry)
                    wnd = cachedEntry.window || null;

                initialCreate = !wnd;
            }

            if (!wnd) {
                var top = 1;

                var ownd = reg;

                // Width
                ownd.width = options.width || ownd.width || null;
                ownd.minWidth = options.minWidth || ownd.minWidth || 500;
                ownd.maxWidth = options.maxWidth || ownd.maxWidth || null;

                // Height            
                ownd.minHeight = options.minHeight || ownd.minHeight || null;
                //if (ownd.minHeight > maxHeight)
                //    ownd.minHeight = maxHeight;

                ownd.maxHeight = options.maxHeight || ownd.maxHeight || null; //_windowComputeEstimatedMaxHeight();
                //if (ownd.maxHeight > maxHeight)
                //    ownd.maxHeight = maxHeight;

                ownd.height = options.height || ownd.height || ownd.minHeight || ownd.maxHeight || null;
                //if (ownd.height > maxHeight)
                //    ownd.height = maxHeight;

                // See docs: https://docs.telerik.com/kendo-ui/api/javascript/ui/window

                //var onResize = function (e) {
                //    if (wnd.options.isMaximized)
                //        return;
                //};

                var canApplyWidth = false;

                var owindow = {
                    visible: false,
                    animation: kendomodo.getDefaultDialogWindowAnimation(),
                    modal: true,
                    resizable: options.resizable === false ? false : true,
                    draggable: options.draggable === false ? false : true,
                    scrollable: options.scrollable === false ? false : true,
                    pinned: false,
                    height: ownd.height,
                    minHeight: ownd.minHeight,
                    maxHeight: ownd.maxHeight,
                    position: {
                        top: top
                    },
                    actions: [
                        //"Pin",
                        //"Minimize",
                        "Maximize",
                        "Close"
                    ],
                    close: function (e) {
                        if (reg.isCached) {
                            // NOP
                        }
                        else {
                            if (options.finished)
                                options.finished(args);
                        }
                    },
                    deactivate: function () {

                        // $(window).off("resize", onResize);
                        if (!reg.isCached)
                            this.destroy();
                    }
                };

                if (canApplyWidth) {
                    // NOTE: Must not set max-width on the window itself because
                    //  otherwise maximizing will not work anymore.
                    //  We set max-width on the component's root element instead further down.
                    owindow.minWidth = ownd.minWidth;
                    owindow.maxWidth = ownd.maxWidth;
                    owindow.width = ownd.width;
                }

                wnd = $("<div></div>")
                    //.appendTo(casimodo.ui.findDialogContainer())
                    .kendoWindow(owindow)
                    .data('kendoWindow');
            }

            if (reg.isCached) {
                cachedEntry.window = wnd;

                if (options.finished) {
                    wnd.one("close", function (e) {
                        options.finished(args);
                    });
                }
            }

            kendomodo.setModalWindowBehavior(wnd);

            //wnd.content("<img src='content/bootstrap/loading-image.gif' style='" +
            //    //"vertical-align: middle; display: inline-block; position: relative;"+
            //    "display: block; margin: 0 auto; vertical-align: middle;" +
            //    "' alt='Lade...'/>");

            //wnd.wrapper.css({
            //    "overflow-y": "auto"
            //});

            _windoRestrictVertically(wnd);

            kendomodo.ui.progress(true);

            if (!initialCreate) {
                _centerKendoWindowHorizontally(wnd);

                if (options.maximize === true)
                    wnd.maximize();

                kendomodo.ui.progress(false);

                wnd.open();

                setTimeout(function () {
                    loaded();
                }, 1);

            }
            else {
                wnd.one("refresh", function (e) {

                    //if (options.maximize === false) {
                    //    _centerKendoWindowHorizontally(wnd);
                    //    //wnd.center();
                    //}
                    if (!reg.isCached) {
                        var componentRoot = wnd.wrapper.find(".component-root").first();
                        if (ownd.maxWidth)
                            componentRoot.css("max-width", ownd.maxWidth);
                        if (ownd.minWidth && (ownd.maxWidth === null || ownd.minWidth <= ownd.maxWidth))
                            componentRoot.css("min-width", ownd.minWidth);
                    }

                    _centerKendoWindowHorizontally(wnd);

                    if (options.maximize === true)
                        wnd.maximize();

                    if (options.vm)
                        options.vm.createComponent();

                    kendomodo.ui.progress(false);

                    loaded();

                    setTimeout(function () {
                        wnd.open();
                    }, 20);

                    //if (options.maximize === false)
                    //    $(window).on("resize", onResize);

                });

                wnd.refresh({ url: reg.url, cache: false });
            }
        };

        function _windowComputeEstimatedMaxHeight() {
            var wnd = $(window),
                zoomLevel = kendo.support.zoomLevel();

            return wnd.height() / zoomLevel - 30 - 6;
        }

        function _windoRestrictVertically(that) {
            var wrapper = that.wrapper,
                wnd = $(window),
                zoomLevel = kendo.support.zoomLevel();

            //w = wnd.width() / zoomLevel;
            var h = wnd.height() / zoomLevel - parseInt(wrapper.css('padding-top'), 10) - 6;

            if (that.options.maxHeight && h >= that.options.maxHeight)
                return;

            wrapper.css({
                //width: w,
                height: h,
                maxHeight: h,
                minHeight: ""
            });
            //that.options.width = w;
            that.options.height = h;
            that.options.maxHeight = h;
            //that.resize();
        }

        function _centerKendoWindowHorizontally(that) {
            var position = that.options.position,
                wrapper = that.wrapper,
                documentWindow = $(window),
                zoomLevel = kendo.support.zoomLevel(),
                //scrollTop = 0,
                scrollLeft = 0,
                //newTop,
                newLeft;

            //if (that.options.isMaximized) {
            //    return that;
            //}

            if (!that.options.pinned) {
                //scrollTop = documentWindow.scrollTop();
                scrollLeft = documentWindow.scrollLeft();
            }
            newLeft = scrollLeft + Math.max(0, (documentWindow.width() - wrapper.width()) / 2);
            //newTop = scrollTop + Math.max(0, (documentWindow.height() - wrapper.height() - parseInt(wrapper.css('paddingTop'), 10)) / 2);
            wrapper.css({
                left: newLeft,
                //top: newTop
            });
            //position.top = newTop;
            position.left = newLeft;

            return that;
        }

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
