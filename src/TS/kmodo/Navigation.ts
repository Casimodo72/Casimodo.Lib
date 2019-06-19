namespace kmodo {

    interface NavigateToViewEventOptions {
        editing?: Function;
        dataChanging?: Function;
        dataChange?: Function;
        validating?: Function;
        saved?: Function;
    }

    export interface NavigateToViewResultEvent {
        sender: any;
        result: NavigateToViewResult;
    }

    export interface NavigateToViewResult {
        isOk?: boolean;
        isDeleted?: boolean;
        value?: any;
        items?: any[];
    }

    export interface NavigateToViewOptions {
        itemId?: string;
        item?: any;
        mode?: string;
        canDelete?: boolean;
        events?: NavigateToViewEventOptions;
        finished?: (result: any) => void;
        finished2?: (e: NavigateToViewResultEvent) => void;
        value?: any;
        filters?: DataSourceFilterNode[];
        filterCommands?: any;
        params?: any;
        title?: string;
        message?: string;
        vm?: any;

        maximize?: boolean;
        resizable?: boolean;
        draggable?: boolean;
        scrollable?: boolean;

        width?: number;
        minWidth?: number;
        maxWidth?: number;
        height?: number;
        minHeight?: number;
        maxHeight?: number;

        options?: any;
    }

    export function navigate(part: string, itemId: string) {
        _openCore(cmodo.componentRegistry.items.find(x => x.part === part && x.role === "Page"),
            { itemId: itemId } as NavigateToViewOptions,
            null);
    }

    export function openById(viewId: string, options: NavigateToViewOptions, finished?: (result: any) => void) {
        _openCore(cmodo.componentRegistry.items.find(x => x.id === viewId), options, finished);
    }

    export function onPageNaviEvent(e: JQueryEventObject) {
        e.preventDefault();
        e.stopPropagation();

        const $el = $(e.currentTarget);
        const part = $el.data("navi-part");
        const id = $el.data("navi-id");

        kmodo.navigate(part, id);

        return false;
    }

    type CachedComponentInfo = {
        id: string;
        component: any;
        window?: kendo.ui.Window;
    }

    const _componentCache = new Array<CachedComponentInfo>();

    function _openCore(reg: cmodo.ComponentInfo, options: NavigateToViewOptions, finished?: (result: any) => void) {
        if (!reg) return;

        options = options || {} as NavigateToViewOptions;

        finished = finished || null;

        if (!options.finished)
            options.finished = finished;

        if (reg.role === "Page") {
            let listReg = reg;

            // Try to find a matching "List" view in same group.
            listReg = cmodo.componentRegistry.items
                .find(x => x.part === reg.part && x.group === reg.group && x.role === "List");

            if (!listReg)
                return;

            cmodo.navigationArgs.add({
                id: listReg.id,
                itemId: options.itemId
            });

            window.open(reg.url, "");
        }
        else if (reg.role === "Editor") {
            const args = new cmodo.DialogArgs(reg.id);
            args.mode = options.mode;
            args.canDelete = !!options.canDelete;
            args.itemId = options.itemId;
            args.value = options.value;
            args.filters = options.filters || null;
            args.params = options.params;
            args.title = options.title || null;
            args.message = options.message || null;

            const vm = _createComponent(reg, options);
            options.vm = vm;
            vm.setArgs(args);

            _openModalWindow(reg, options, args, () => {
                vm.start();
            });
        }
        else if (reg.role === "Lookup" || reg.role === "List" || reg.role === "Details") {
            const args = new cmodo.DialogArgs(reg.id);
            args.params = options.params;
            args.filters = options.filters;
            args.filterCommands = options.filterCommands;
            args.item = options.item;
            args.title = options.title || null;

            const vm = _createComponent(reg, options);
            options.vm = vm;
            vm.setArgs(args);

            _openModalWindow(reg, options, args, () => {
                vm.refresh();
            });
        }
    };

    function _createComponent(reg: cmodo.ComponentInfo, options: NavigateToViewOptions): any {
        let component = null;
        let cachedEntry: CachedComponentInfo = null;

        if (reg.isCached) {
            cachedEntry = _componentCache.find(x => x.id === reg.id);
            if (cachedEntry)
                component = cachedEntry.component;
        }

        if (!component) {
            component = cmodo.componentRegistry.createComponent(reg, false, options.options);
        }

        if (reg.isCached && !cachedEntry) {
            _componentCache.push({ id: reg.id, component: component });
        }

        if (options.events) {
            const eves = options.events;
            if (reg.isCached)
                throw new Error("Event assignment not allowed for cached view models.");

            if (eves.editing)
                component.on("editing", eves.editing);

            if (eves.dataChanging)
                component.on("dataChanging", eves.dataChanging);

            if (eves.dataChange)
                component.on("dataChange", eves.dataChange);

            if (eves.validating)
                component.on("validating", eves.validating);

            if (eves.saved)
                component.on("saved", eves.saved);
        }

        return component;
    };

    function _openModalWindow(reg: cmodo.ComponentInfo, options: NavigateToViewOptions,
        args: cmodo.DialogArgs, loaded: Function) {

        let wnd: kendo.ui.Window = null;
        let cachedEntry: CachedComponentInfo = null;
        let initialCreate = true;

        if (reg.isCached) {
            cachedEntry = _componentCache.find(x => x.id === reg.id);
            if (cachedEntry)
                wnd = cachedEntry.window || null;

            initialCreate = !wnd;
        }

        let ownd: any = null;

        if (!wnd) {
            const top = 1;

            ownd = Object.assign({}, reg);

            // Width
            ownd.width = options.width || ownd.width || null;
            ownd.minWidth = options.minWidth || ownd.minWidth || 500;
            ownd.maxWidth = options.maxWidth || ownd.maxWidth || null;

            // Height            
            ownd.minHeight = options.minHeight || ownd.minHeight || null;
            ownd.maxHeight = options.maxHeight || ownd.maxHeight || null;
            ownd.height = options.height || ownd.height || ownd.minHeight || ownd.maxHeight || null;

            // See docs: https://docs.telerik.com/kendo-ui/api/javascript/ui/window

            // TODO: REMOVE?
            // const onResize = e => {
            //    if (wnd.options.isMaximized)
            //        return;
            //};

            const canApplyWidth = false;

            const owindow: kendo.ui.WindowOptions = {
                visible: false,
                animation: getDefaultDialogWindowAnimation(),
                modal: true,
                resizable: options.resizable === false ? false : true,
                draggable: options.draggable === false ? false : true,
                scrollable: options.scrollable === false ? false : true,
                pinned: false,

                width: null,
                minWidth: ownd.minWidth,
                maxWidth: null,

                height: ownd.height,
                minHeight: ownd.minHeight,
                maxHeight: ownd.maxHeight,

                position: {
                    top: top
                },
                actions: [
                    // "Pin",
                    // "Minimize",
                    "Maximize",
                    "Close"
                ],
                close: e => {
                    if (e.userTriggered === true) {
                        // TODO: Should we hand control over this to
                        //   the component?
                        // This occurs when the user closes the dialog
                        //  via the dialog window title bar's close button.
                        args.isOk = false;
                        args.isCancelled = true;
                    }
                    if (reg.isCached) {
                        // NOP
                    }
                    else {
                        if (options.finished) {
                            options.finished(args);
                        }

                        _triggerOptionsFinished2(options, args);
                    }
                },
                deactivate: e => {

                    // TODO: REMOVE? $(window).off("resize", onResize);
                    if (!reg.isCached)
                        e.sender.destroy();
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
                .kendoWindow(owindow)
                .data('kendoWindow');
        }

        if (reg.isCached) {
            cachedEntry.window = wnd;

            if (options.finished) {
                wnd.one("close", e => {
                    if (e.userTriggered === true) {
                        // TODO: Should we hand control over this to
                        //   the component?
                        // This occurs when the user closes the dialog
                        //  via the dialog window title bar's close button.
                        args.isOk = false;
                        args.isCancelled = true;
                    }

                    options.finished(args);

                    _triggerOptionsFinished2(options, args);
                });
            }
        }

        setModalWindowBehavior(wnd);

        _windoRestrictVertically(wnd);

        kmodo.progress(true);

        if (!initialCreate) {
            _centerKendoWindowHorizontally(wnd);

            if (reg.maximize === true || options.maximize === true)
                wnd.maximize();

            kmodo.progress(false);

            wnd.open();

            setTimeout(function () {
                loaded();
            }, 1);

        }
        else {
            wnd.one("refresh", e => {
                _centerKendoWindowHorizontally(wnd);

                if (reg.maximize === true || options.maximize === true)
                    wnd.maximize();

                if (options.vm)
                    options.vm.createView();

                kmodo.progress(false);

                loaded();

                setTimeout(function () {
                    wnd.open();
                }, 20);
            });

            wnd.refresh({ url: reg.url, cache: false });
        }
    }

    function _triggerOptionsFinished2(options: NavigateToViewOptions, args: cmodo.DialogArgs): void {
        if (options.finished2) {
            const eve: NavigateToViewResultEvent = {
                sender: options.vm,
                result: Object.assign({}, args)
            };

            options.finished2(eve);
        }
    }

    function _windoRestrictVertically(wnd: kendo.ui.Window) {
        const wrapper = wnd.wrapper;
        const $browserWindow = $(window);
        const zoomLevel = kendo.support.zoomLevel; // TODO: zoomLevel was a function.

        const h = $browserWindow.height() / zoomLevel - parseInt(wrapper.css('padding-top'), 10) - 6;

        if (wnd.options.maxHeight && h >= wnd.options.maxHeight)
            return;

        wrapper.css({
            height: h,
            maxHeight: h,
            minHeight: ""
        });
        wnd.options.height = h;
        wnd.options.maxHeight = h;
    }

    function _centerKendoWindowHorizontally(wnd: kendo.ui.Window) {
        const position = wnd.options.position;
        const $wnd = wnd.wrapper;
        const $browserWindow = $(window);
        // const zoomLevel = kendo.support.zoomLevel; // TODO: zoomLevel was a function.
        let scrollLeft = 0;

        // TODO: Why do we center when maximized?
        // if (that.options.isMaximized) {
        //    return that;
        // }

        if (!wnd.options.pinned) {
            // scrollTop = documentWindow.scrollTop();
            scrollLeft = $browserWindow.scrollLeft();
        }
        const newLeft = scrollLeft + Math.max(0, ($browserWindow.width() - $wnd.width()) / 2);
        // newTop = scrollTop + Math.max(0, (documentWindow.height() - wrapper.height() - parseInt(wrapper.css('paddingTop'), 10)) / 2);
        $wnd.css({
            left: newLeft,
            // top: newTop
        });
        // position.top = newTop;
        position.left = newLeft;

        return wnd;
    }
}