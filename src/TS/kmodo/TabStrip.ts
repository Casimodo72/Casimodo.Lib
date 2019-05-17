
namespace kmodo {

    export interface ITabPageContentComponent extends kmodo.IViewComponent {
        setFilter?(filter: DataSourceFilterOneOrMany): void;
    }

    export interface IDetailTabPageContentComponent extends ITabPageContentComponent {
    }

    export interface IMasterTabPageContentComponent extends ITabPageContentComponent {
        getCurrent(): any;
    }

    export interface TabPageEventBase {
        tab: TabPage;
        componentAsAny: any;
        component: ITabPageContentComponent;
    }

    export interface TabPageEvent extends TabPageEventBase {
        sender: TabStrip;
    }

    export interface DetailTabPageEvent extends TabPageEventBase {
        sender: MasterDetailTabStrip;
        component: IDetailTabPageContentComponent;
        mcomponent?: IMasterTabPageContentComponent;
        master: any;
    }

    export interface TabPageOptionsBase<
        TComponent extends ITabPageContentComponent,
        TTabEvent extends TabPageEvent> {
        canShow?: boolean;
        icomponent?: cmodo.ComponentInfo;
        component?: TComponent | (() => TComponent);
        name?: string;
        master?: boolean;
        init?: (e: TTabEvent) => void;
        filtering?: (e: TTabEvent) => void;
        filters?: (e: TTabEvent) => DataSourceFilterNode[];
        enter?: (e: TTabEvent) => void;
        leave?: (e: TTabEvent) => void;
        leaveOrClear?: (e: TTabEvent) => void;
        autoClear?: boolean;
        autoRefresh?: boolean;
        clear?: (e: TTabEvent) => void;
        isEnabled?: boolean;
    }

    export interface TabPageOptions extends TabPageOptionsBase<ITabPageContentComponent, TabPageEvent> {
    }

    export interface DetailTabPageOptions extends TabPageOptionsBase<IDetailTabPageContentComponent, DetailTabPageEvent> {
        isMasterAffected?: boolean;
    }

    export class TabPage extends cmodo.ComponentBase {
        owner: TabStrip;
        icomponent: cmodo.ComponentInfo;
        iauth: cmodo.AuthQuery;
        component: ITabPageContentComponent;
        name: string;
        master: boolean;
        init: (e: any) => void;
        filtering: (e: any) => void;
        filters: (e: any) => DataSourceFilterNode[];
        enter: (e: any) => void;
        autoClear: boolean = true;
        autoRefresh: boolean = true;
        clear: (e: any) => void;
        leave: (e: any) => void;
        leaveOrClear: (e: any) => void;
        isEnabled: boolean;
        _isInitPerformed: boolean;
        _isRefreshPending: boolean;
        _componentFactory: () => any;
        _canShow: boolean;

        constructor(options: TabPageOptions) {
            super();

            this.owner = null;
            this.icomponent = options.icomponent || null;
            this.name = options.name;

            this._componentFactory = null;
            if (typeof options.component === "function")
                this._componentFactory = options.component;
            else
                this.component = options.component || null;

            this._canShow = typeof options.canShow !== "undefined" ? !!options.canShow : true;
            this.master = !!options.master || false;
            this.filtering = options.filtering || null;
            this.filters = options.filters || null;
            this.init = options.init || null;
            this.enter = options.enter || null;
            this.autoClear = options.autoClear !== undefined ? options.autoClear : this.autoClear;
            this.autoRefresh = options.autoRefresh !== undefined ? options.autoRefresh : this.autoRefresh;
            this.clear = options.clear || null;
            this.leave = options.leave || null;
            this.leaveOrClear = options.leaveOrClear || null;
            this.isEnabled = true;

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
            if (this.component && this.component.getModel())
                kendo.bind(this.getContentElem(), this.component.getModel());
        }

        bind($elem: JQuery): void {
            if ($elem && $elem.length && this.component && this.component.getModel())
                kendo.bind($elem, this.component.getModel());
        }
    }

    export class DetailTabPage extends TabPage {
        isMasterAffected: boolean;

        constructor(options: DetailTabPageOptions) {
            super(options);

            this.isMasterAffected = options.isMasterAffected || false;
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
        protected _kendoTabStrip: kendo.ui.TabStrip;
        protected _currentTab: TabPage;

        constructor(options: TabStripOptions) {
            super();

            this._options = options;

            this.tabs = options.tabs || [];
            for (let tab of this.tabs) {
                tab.owner = this;

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

            cmodo.authContext.one("read", e => this._onAuthContextRead(e));
        }

        _bindMaster(tab: TabPage): void {
            throw new Error("This operation is suported only on master-detail tab-strips.");
        }

        private _onAuthContextRead(e): void {
            // Apply auth.
            const auth = e.auth as cmodo.AuthActionManager;
            let tab: TabPage;
            let iauth: cmodo.AuthQuery;
            for (let i = 0; i < this.tabs.length; i++) {
                tab = this.tabs[i];
                iauth = tab.iauth;
                if (!iauth)
                    continue;

                const part = auth.part(iauth.Part, iauth.Group);
                tab.canShow(part.can("View", iauth.VRole));
            }

            this._start();
        }

        protected _start(): void {
            // NOP
        }

        // Hide the tab-strip so that only the page contents are visible.
        hideTabStrip(): void {
            this._kendoTabStrip.element.children("ul").hide();
        }

        private _initComponent(options: TabStripOptions): void {
            this._kendoTabStrip = options.$component.kendoTabStrip({
                show: e => this.onTabActivated(e),
                animation: getDefaultTabControlAnimation()
            }).data("kendoTabStrip");

            this._computeTabContentSize();

            $(window).resize(() => {
                this._computeTabContentSize();
            });
        }

        private _computeTabContentSize(): void {
            // Due to Kendo's annoying tabstrip design decision we can't simple
            // set the tab pages "height: 100%" (because that would result in
            // those pages to have the same hight as the whole Kendo tabstrip).
            // We have to adjust the pages programmatically on window resize.
            // https://docs.telerik.com/kendo-ui/controls/navigation/tabstrip/how-to/expand-height
            const $tabstrip = this._kendoTabStrip.element;
            const $tabs = $tabstrip.children(".k-content");
            const $visibleTab = $tabs.filter(":visible");
            const height = $tabstrip.innerHeight()
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
            for (let tab of this.tabs) {
                if (!tab.isEnabled)
                    this.toggleTabs(false, tab.name);
            }
        }

        removeAllTabs(): void {
            for (let i = 0; i < this.tabs.length; i++)
                this.removeTab(this.tabs[i].name);
        }

        removeTab(name): void {

            const index = this.indexOfTab(name);
            if (index === -1)
                return;

            const tab = this.getTab(index);

            this._tryClearTab(tab, true);

            this.tabs.splice(index, 1);

            this._removeTabElem(name);
        }

        protected _tryClearTab(tab: TabPage, force: boolean = false): void {
            if (tab.autoClear && tab.component && tab.component.clear)
                tab.component.clear();
        }

        private _removeTabElem(name: string): void {
            this._kendoTabStrip.remove(this.getIndexOfTabElementByName(name));
        }

        setTabVisible(name: string, value: boolean): void {
            const tab = this.getTab(name);
            if (value && !tab._canShow)
                return;

            this._toggleTabCore(this.getTabElem(name), value);
        }

        private getIndexOfTabElementByName(name: string): number {
            const elements = this._kendoTabStrip.items();
            for (let i = 0; i < elements.length; i++) {
                if ($(elements[i]).data("name") === name)
                    return i;
            }

            return -1;
        }

        toggleTabs(visible: boolean, name?: string): void {
            let tab;
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
            return $(this._kendoTabStrip.contentElement(this.getIndexOfTabElementByName(name)));
        }

        getTabElem(name: string): JQuery {
            return $(this._kendoTabStrip.items()[this.getIndexOfTabElementByName(name)]);
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
            this._kendoTabStrip.select(this.getIndexOfTabElementByName(name));
        }

        selectTabAt(index: number): void {
            this._kendoTabStrip.select(index);
        }

        onTabActivated(e): void {
            const name = $(e.item).data("name");

            this._onTabActivatedCore(name);
        }

        private async _onTabActivatedCore(name: string): Promise<boolean> {
            const tab = this.getTab(name);
            if (!tab)
                return false;

            if (!this._canEnterTab(tab))
                return false;

            // Enter tab page.

            const prevTab = this._currentTab;
            this._currentTab = tab;

            // Init tag page once.
            if (!tab._isInitPerformed)
                this._initTab(tab);

            const tabEvent = this._createTabEvent(tab);

            // Apply filters
            this._setTabFilter(tab, tabEvent);

            if (tab.autoRefresh)
                await tab.component.refresh();

            // Trigger enter event
            if (tab.enter)
                tab.enter(tabEvent);

            this.trigger("currentTabChanged", tabEvent);

            // Leave previously selected tab.

            if (prevTab) {

                if (this._canClearTab(prevTab))
                    this._tryClearTab(prevTab);

                if (prevTab.leave)
                    prevTab.leave(this._createTabEvent(prevTab));

                if (prevTab.leaveOrClear)
                    prevTab.leaveOrClear(this._createTabEvent(prevTab));
            }

            if (tab.master && tab._isRefreshPending && tab.autoRefresh) {
                tab._isRefreshPending = false;
                if (tab.component)
                    tab.component.refresh();
            }

            return true;
        }

        protected _setTabFilter(tab: TabPage, event: any): void {
            if (!tab.component || (!tab.filters && !tab.filtering))
                return;

            if (tab.filtering) {
                tab.filtering(event);
            }

            if (tab.filters && typeof tab.component.setFilter === "function") {
                tab.component.setFilter(tab.filters(event));
            }
        }

        protected _initTabComponent(tab: TabPage): void {
            if (tab.icomponent) {
                tab.component = tab.icomponent.create(true);
            } else if (tab._componentFactory) {
                tab.component = tab._componentFactory() || null;
            }
        }

        protected _initTab(tab: TabPage): void {
            if (tab._isInitPerformed)
                return;

            tab._isInitPerformed = true;

            this._initTabComponent(tab);

            if (tab.init)
                tab.init(this._createTabEvent(tab));
        }

        protected _createTabEvent(tab: TabPage): TabPageEvent {
            return {
                sender: this,
                tab: tab,
                componentAsAny: tab.component,
                component: tab.component
            };
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
        protected tabs: DetailTabPage[];
        mtab: TabPage;
        mcomponent: IMasterTabPageContentComponent;

        constructor(options: MasterDetailTabStripOptions) {

            if (typeof options.hideAll === "undefined")
                options.hideAll = true;

            super(options);

            this.mcomponent = null;
            this.mtab = this.tabs.find(x => x.master === true);
            if (!this.mtab)
                throw new Error("Master tab is missing.");

            this._initTab(this.mtab as DetailTabPage);
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
            if (this.mcomponent && this.mcomponent.getModel())
                kendo.bind(tab.getContentElem(), this.mcomponent.getModel());
        }

        setMasterComponent(component): void {
            this.mcomponent = component;
            // KABU TODO: VERY VERY IMPORTANT: React on current *selected* item changed,
            //   because after we add new item, no item is selected, but the current
            //   item still references the last selected item.

            this.mcomponent.on("currentChanged", e => {

                // On selected master item changed.
                // Show/hide tabs if the master item is selected/not selected.
                this.toggleTabs(!!this.mcomponent.getCurrent());

                // Clear all tabs.
                for (const tab of this.tabs) {

                    if (tab.master) return;

                    if (!tab._isInitPerformed) return;

                    if (!tab.isEnabled) return;

                    // Clear component.
                    this._tryClearTab(tab);

                    if (tab.clear)
                        tab.clear(this._createTabEvent(tab));

                    if (tab.leaveOrClear)
                        tab.leaveOrClear(this._createTabEvent(tab));
                }
            });
        }

        getCurrentMasterItem(): any | null {
            return this.mcomponent.getCurrent();
        }

        // override
        protected _initTab(tab: DetailTabPage): void {
            if (tab._isInitPerformed)
                return;

            tab._isInitPerformed = true;

            this._initTabComponent(tab);

            if (tab.master && tab.component)
                this.setMasterComponent(tab.component);

            if (tab.isMasterAffected && tab.component && !tab.master) {
                // If the component affects the master then
                // refresh the master component whenever some data
                // is saved.
                tab.component.on("saved", e => {
                    this.mtab._isRefreshPending = true;
                });
            }

            if (tab.init)
                tab.init(this._createTabEvent(tab));
        }

        protected _canHideTab(tab: TabPage): boolean {
            return !tab.master;
        }

        // override
        protected _createTabEvent(tab: TabPage): DetailTabPageEvent {
            const eve = super._createTabEvent(tab) as DetailTabPageEvent;
            eve.mcomponent = this.mcomponent;
            eve.master = this.mcomponent.getCurrent();

            return eve;
        }

        // override
        protected _canClearTab(tab): boolean {
            // Don't clear the master component.
            return !tab.master;
        }

        protected _canEnterTab(tab): boolean {
            // Enter tabs only if enabled and master data is selected.
            return tab.isEnabled && this.mcomponent.getCurrent() !== null;
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