
namespace kmodo {

    export interface TabPageContentComponent extends kmodo.IViewComponent {
    }

    export interface MasterTabPageContentComponent extends TabPageContentComponent {
        getCurrent(): any;
    }

    export interface TabPageEventBase {
        tab: TabPage;
        vm: ViewComponent;
    }

    export interface TabPageEvent extends TabPageEventBase {
        sender: TabStrip;
    }

    export interface DetailTabPageEvent extends TabPageEventBase {
        sender: MasterDetailTabStrip;
        master: any;
    }

    export interface TabPageOptions {
        icomponent?: cmodo.ComponentRegItem;
        canShow?: boolean;
        vm?: TabPageContentComponent | (() => TabPageContentComponent);
        name?: string;
        master?: boolean;
        filters?: (e: TabPageEvent) => kendo.data.DataSourceFilterItem[];
        init?: (e: TabPageEvent) => void;
        enter?: (e: TabPageEvent) => void;
        clear?: (e: TabPageEvent) => void;
        leave?: (e: TabPageEvent) => void;
        leaveOrClear?: (e: TabPageEvent) => void;
        isEnabled?: boolean;
        isMasterAffected?: boolean;
    }

    export interface DetailTabPageOptions extends TabPageOptions {
        filters?: (e: DetailTabPageEvent) => kendo.data.DataSourceFilterItem[];
        init?: (e: DetailTabPageEvent) => void;
        enter?: (e: DetailTabPageEvent) => void;
        clear?: (e: DetailTabPageEvent) => void;
        leave?: (e: DetailTabPageEvent) => void;
        leaveOrClear?: (e: DetailTabPageEvent) => void;
    }

    export class TabPage extends cmodo.ComponentBase {
        owner: TabStrip;
        icomponent: cmodo.ComponentRegItem;
        iauth: cmodo.AuthQuery;
        vm: TabPageContentComponent;
        name: string;
        master: boolean;
        filters: any;
        init: Function;
        enter: Function;
        clear: Function;
        leave: Function;
        leaveOrClear: Function;
        isEnabled: boolean;
        isMasterAffected: boolean;
        _isInitPerformed: boolean;
        _isRefreshPending: boolean;
        _getVmOnDemand: Function;
        _canShow: boolean;
        // KABU TODO: REMOVE becase the index will change anyway if tabs are removed/added/moved.
        //index: number;

        constructor(options: TabPageOptions) {
            super();

            this.owner = null;
            this.icomponent = options.icomponent || null;
            this.name = options.name;

            this._getVmOnDemand = null;
            if (typeof options.vm === "function")
                this._getVmOnDemand = options.vm;
            else
                this.vm = options.vm || null;

            //this.enabled = typeof options.enabled !== "undefined" ? options.enabled : true;
            //this.visible = typeof options.visible !== "undefined" ? options.visible : true;
            this._canShow = typeof options.canShow !== "undefined" ? !!options.canShow : true;
            // KABU TODO: REMOVE becase the index will change anyway if tabs are removed/added/moved.
            //this.index = options.index || null;
            this.master = !!options.master || false;
            this.filters = options.filters || null;
            this.init = options.init || null;
            this.enter = options.enter || null;
            this.clear = options.clear || null;
            this.leave = options.leave || null;
            this.leaveOrClear = options.leaveOrClear || null;
            this.isEnabled = true;
            this.isMasterAffected = options.isMasterAffected || false;
            this._isInitPerformed = false;
            this._isRefreshPending = false;
        }

        getContentElem(): JQuery {
            return this.owner.getTabContentElem(this.name);
        }

        canShow(value: boolean): TabPage {
            this._canShow = !!value;
            return this;
        }

        visible(value: boolean): TabPage {
            this.owner.setTabVisible(this.name, value);
            return this;
        }

        bindMaster(): void {
            this.owner._bindMaster(this);
        }

        bindModel(): void {
            if (this.vm && this.vm.getModel())
                kendo.bind(this.getContentElem(), this.vm.getModel());
        }

        bind($elem: JQuery): void {
            if ($elem !== null && this.vm && this.vm.getModel())
                kendo.bind($elem, this.vm.getModel());
        }
    }

    export class DetailTabPage extends TabPage {
        constructor(options: DetailTabPageOptions) {
            super(options);
        }
    }

    export interface TabStripOptions {
        hideAll?: boolean;
        tabs: TabPage[];
        $component: JQuery;
    }

    export class TabStrip extends cmodo.ComponentBase {
        protected _options: TabStripOptions;
        protected tabs: TabPage[];
        protected component: kendo.ui.TabStrip;
        protected _currentTab: TabPage;

        constructor(options: TabStripOptions) {
            super();

            var self = this;

            this._options = options;

            this.tabs = options.tabs || [];
            for (let tab of this.tabs) {
                tab.owner = self;

                // Set authorization info.
                if (tab.icomponent) {
                    tab.iauth = tab.icomponent.getAuthQueries()[0] || null;
                    if (tab.iauth)
                        cmodo.authContext.addQueries([tab.iauth]);
                }
            }
            this._currentTab = null;

            this._initComponent(options);

            if (options.hideAll)
                this.hideAllTabs();

            cmodo.authContext.one("read", (e) => self._onAuthContextRead(e));
        }

        _bindMaster(tab: TabPage): void {
            throw new Error("This operation is suported only on master-detail tab-strips.");
        }

        private _onAuthContextRead(e): void {
            // Apply auth.
            var auth = e.auth as cmodo.AuthActionManager;
            var tab: TabPage;
            var iauth: cmodo.AuthQuery;
            for (let i = 0; i < this.tabs.length; i++) {
                tab = this.tabs[i];
                iauth = tab.iauth;
                if (!iauth)
                    continue;

                var part = auth.part(iauth.Part, iauth.Group);
                tab.canShow(part.can("View", iauth.VRole));
            }

            this._start();
        }

        protected _start(): void {
            // NOP
        }

        // Hide the tab-strip so that only the page contents are visible.
        hideTabStrip(): void {
            this.component.element.children("ul").hide();
        }

        private _initComponent(options: TabStripOptions): void {
            var self = this;
            this.component = options.$component.kendoTabStrip({
                show: $.proxy(self.onTabActivated, self),
                animation: getDefaultTabControlAnimation()
            }).data("kendoTabStrip");

            this._computeTabContentSize();

            $(window).resize(function () {
                self._computeTabContentSize();
            });
        }

        private _computeTabContentSize(): void {
            // Due to Kendo's annoying tabstrip design decision we can't simple
            // set the tab pages "height: 100%" (because that would result in
            // those pages to have the same hight as the whole Kendo tabstrip).
            // We have to adjust the pages programmatically on window resize.
            // https://docs.telerik.com/kendo-ui/controls/navigation/tabstrip/how-to/expand-height
            var $tabstrip = this.component.element;
            var $tabs = $tabstrip.children(".k-content");
            var $visibleTab = $tabs.filter(":visible");
            var height = $tabstrip.innerHeight()
                - $tabstrip.children(".k-tabstrip-items").outerHeight()
                - parseFloat($visibleTab.css("padding-top"))
                - parseFloat($visibleTab.css("padding-bottom"))
                - parseFloat($visibleTab.css("border-top-width"))
                - parseFloat($visibleTab.css("border-bottom-width"))
                - parseFloat($visibleTab.css("margin-bottom"));
            $tabs.height(height);
        }

        hideAllTabs(): void {
            this.toggleTabs(false);
        }

        hideDisabledTabs(): void {
            var self = this;
            for (let tab of this.tabs) {
                if (!tab.isEnabled)
                    self.toggleTabs(false, tab.name);
            }
        }

        removeAllTabs(): void {
            for (let i = 0; i < this.tabs.length; i++)
                this.removeTab(this.tabs[i].name);
        }

        removeTab(name): void {

            var index = this.indexOfTab(name);
            if (index === -1)
                return;

            var tab = this.getTab(index);

            if (tab.vm && tab.vm.clear)
                tab.vm.clear();

            this.tabs.splice(index, 1);

            this._removeTabElem(name);
        }

        private _removeTabElem(name: string): void {
            this.component.remove(this.getIndexOfTabElementByName(name));
        }

        setTabVisible(name: string, value: boolean): void {
            var tab = this.getTab(name);
            if (value && !tab._canShow)
                return;

            this._toggleTabCore(this.getTabElem(name), value);
        }

        private getIndexOfTabElementByName(name: string): number {
            var elements = this.component.items();
            for (let i = 0; i < elements.length; i++) {
                if ($(elements[i]).data("name") === name)
                    return i;
            }

            return -1;
        }

        toggleTabs(visible: boolean, name?: string): void {
            var tab;
            for (let i = 0; i < this.tabs.length; i++) {
                tab = this.tabs[i];

                if (name && name !== tab.name)
                    continue;

                if (visible && !tab._canShow)
                    continue;

                if (!visible && !this._canHideTab(tab))
                    continue;

                this._toggleTabCore(this.getTabElem(tab.name), visible);
            }
        }



        private _toggleTabCore($tab: JQuery, visible: boolean): void {
            $tab.attr("style", "display:" + (visible ? "inline-block" : "none"));
        }

        getTabContentElem(name: string): JQuery {
            return $(this.component.contentElement(this.getIndexOfTabElementByName(name)));
        }

        getTabElem(name: string): JQuery {
            return $(this.component.items()[this.getIndexOfTabElementByName(name)]);
        }

        getTab(nameOrIndex: number | string): TabPage {

            if (typeof nameOrIndex === "string") {
                for (let i = 0; i < this.tabs.length; i++) {
                    if (this.tabs[i].name === nameOrIndex)
                        return this.tabs[i];
                }
            }
            else if (nameOrIndex < this.tabs.length) {
                return this.tabs[nameOrIndex];
            }

            return null;
        }

        indexOfTab(name: string): number {
            for (let i = 0; i < this.tabs.length; i++) {
                if (this.tabs[i].name === name)
                    return i;
            }

            return -1;
        }

        selectTab(name: string): void {
            this.component.select(this.getIndexOfTabElementByName(name));
        }

        selectTabAt(index: number): void {
            this.component.select(index);
        }

        onTabActivated(e): void {
            var name = $(e.item).data("name");

            this._onTabActivatedCore(name);
        }

        private _onTabActivatedCore(name: string): boolean {

            var tab = this.getTab(name);
            if (!tab)
                return false;

            if (!this._canEnterTab(tab))
                return false;

            // Enter tab.
            var prevTab = this._currentTab;
            this._currentTab = tab;

            if (!tab._isInitPerformed)
                this._initTab(tab);

            var tabEvent = this._createTabEvent(tab);

            if (tab.vm && typeof tab.filters === "function" && typeof tab.vm["applyFilter"] === "function") {
                tab.vm["applyFilter"](tab.filters(tabEvent));
            }

            if (typeof tab.enter === "function")
                tab.enter(tabEvent);

            this.trigger("currentTabChanged", tabEvent);

            // Leave previously selected tab.

            if (prevTab) {

                if (this._canClearTab(prevTab) && prevTab.vm && prevTab.vm.clear)
                    prevTab.vm.clear();

                if (typeof prevTab.leave === "function")
                    prevTab.leave(this._createTabEvent(prevTab));

                if (typeof prevTab.leaveOrClear === "function")
                    prevTab.leaveOrClear(this._createTabEvent(prevTab));
            }

            if (tab.master && tab._isRefreshPending) {
                tab._isRefreshPending = false;
                if (tab.vm)
                    tab.vm.refresh();
            }

            return true;
        }

        protected _initTab(tab: TabPage): void {
            if (tab._isInitPerformed)
                return;

            tab._isInitPerformed = true;

            if (typeof tab.init === "function")
                tab.init(this._createTabEvent(tab));
        }

        protected _createTabEvent(tab: TabPage): any {
            return { sender: this, tab: tab };
        }

        protected _canHideTab(tab: TabPage): boolean {
            return true;
        }

        protected _canClearTab(tab: TabPage): boolean {
            return true;
        }

        protected _canEnterTab(tab: TabPage): boolean {
            return tab.isEnabled;
        }
    }

    export interface MasterDetailTabStripOptions extends TabStripOptions {
        tabs: DetailTabPage[];
    }

    export class MasterDetailTabStrip extends TabStrip {

        mtab: TabPage;
        mvm: MasterTabPageContentComponent;

        constructor(options: MasterDetailTabStripOptions) {

            if (typeof options.hideAll === "undefined")
                options.hideAll = true;

            super(options);

            this.mvm = null;
            this.mtab = this.tabs.find(x => x.master === true);
            if (!this.mtab)
                throw new Error("Master tab is missing.");

            this._initTab(this.mtab);
        }

        protected _start(): void {
            // KABU TODO: REMOVE?
            //if (this.mtab && this.mtab._canShow) {
            //    this.toggleTabs(true, this.mtab.name);
            //    this.selectTab(this.mtab.name);
            //}
        }

        // override
        _bindMaster(tab: TabPage): void {
            if (this.mvm && this.mvm.getModel())
                kendo.bind(tab.getContentElem(), this.mvm.getModel());
        }

        setMasterViewModel(vm): void {
            var self = this;

            this.mvm = vm;
            // KABU TODO: VERY VERY IMPORTANT: React on current *selected* item changed,
            //   because after we add new item, no item is selected, but the current
            //   item still references the last selected item.

            this.mvm.on("currentItemChanged", function (e) {

                // On selected master item changed.
                // Show/hide tabs if the master item is selected/not selected.
                self.toggleTabs(self.mvm.getCurrent());

                // Clear all tabs.
                for (let tab of self.tabs) {

                    if (tab.master) return;

                    if (!tab._isInitPerformed) return;

                    if (!tab.isEnabled) return;

                    // Clear view model.
                    if (tab.vm && tab.vm.clear)
                        tab.vm.clear();

                    if (typeof tab.clear === "function")
                        tab.clear(self._createTabEvent(tab));

                    if (typeof tab.leaveOrClear === "function")
                        tab.leaveOrClear();
                }
            });
        }

        getCurrentMasterItem(): any | null {
            return this.mvm.getCurrent();
        }

        protected _initTab(tab: TabPage): void {
            var self = this;

            if (tab._isInitPerformed)
                return;

            tab._isInitPerformed = true;

            if (tab.icomponent) {
                tab.vm = tab.icomponent.vm();
            }
            else if (tab._getVmOnDemand) {
                tab.vm = tab._getVmOnDemand() || null;
            }

            if (tab.master && tab.vm)
                this.setMasterViewModel(tab.vm);

            if (tab.isMasterAffected && tab.vm && !tab.master) {
                // If the component affects the master then
                // refresh the master component whenever some data
                // is saved.
                tab.vm.on("saved", function (e) {
                    self.mtab._isRefreshPending = true;
                });
            }

            if (typeof tab.init === "function")
                tab.init(this._createTabEvent(tab));
        }

        protected _canHideTab(tab: TabPage): boolean {
            return !tab.master;
        }

        protected _createTabEvent(tab: TabPage): any {
            return { sender: this, tab: tab, vm: tab.vm, matervm: this.mvm, master: this.mvm.getCurrent() };
        }

        protected _canClearTab(tab): boolean {
            // Don't clear the master's view model.
            return !tab.master;
        }

        protected _canEnterTab(tab): boolean {
            // Enter tabs only if enabled and master item is selected.
            return tab.isEnabled && this.mvm.getCurrent() !== null;
        }
    }

    // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    function getDefaultTabControlAnimation(): kendo.ui.TabStripAnimation {
        return {
            open: {
                effects: "fadeIn", duration: 30
            }
        };
    };
}