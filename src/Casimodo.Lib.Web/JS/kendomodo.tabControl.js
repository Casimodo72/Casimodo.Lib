"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var TabControlPageViewModel = (function () {

            function TabControlPageViewModel(options) {

                this.owner = null;
                this.icomponent = options.icomponent || null;
                this.name = options.name;
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
                this._isInitPerformed = false;
            }

            var fn = TabControlPageViewModel.prototype;

            fn.getContentElem = function () {
                return this.owner.getTabContentElem(this.name);
            };

            fn.canShow = function (value) {
                if (typeof value === "undefined")
                    return this._canShow;

                this._canShow = !!value;

                return this;
            };

            fn.visible = function (value) {
                this.owner.setTabVisible(this.name, value);
                return this;
            };

            return TabControlPageViewModel;

        })();

        ui.TabControlPageViewModel = TabControlPageViewModel;

        var TabControlViewModel = (function () {

            function TabControlViewModel(options) {
                var self = this;

                this._options = options;

                this.tabs = options.tabs || [];
                this.tabs.forEach(function (tab) {
                    tab.owner = self;

                    // Set authorization info.
                    if (tab.icomponent) {
                        tab.iauth = tab.icomponent.getAuthQueries()[0] || null;
                        if (tab.iauth)
                            casimodo.authContext.addQueries([tab.iauth]);
                    }
                });
                this._currentTab = null;

                this._events = new casimodo.EventManager();
                this._initComponent(options);

                if (options.hideAll)
                    this.hideAllTabs();                         

                casimodo.authContext.one("read", (e) => self._onAuthContextRead(e));
            }

            var fn = TabControlViewModel.prototype;

            fn._onAuthContextRead = function (e) {
                // Apply auth.
                var auth = e.auth;
                var tab;
                var iauth;
                for (var i = 0; i < this.tabs.length; i++) {
                    tab = this.tabs[i];
                    iauth = tab.iauth;
                    if (!iauth)
                        continue;

                    var part = auth.part(iauth.Part, iauth.Group);
                    tab.canShow(part.can("View", iauth.VRole));
                }

                this._start();
            };

            fn._start = function () {
                // NOP
            };

            fn.on = function (eventName, func) {
                this._events.on(eventName, func);
            };

            fn.one = function (eventName, func) {
                this._events.one(eventName, func);
            };

            fn.trigger = function (eventName, e) {
                this._events.trigger(eventName, e, this);
            };

            // Hide the tab-strip so that only the page contents are visible.
            fn.hideTabStrip = function () {
                this.component.element.children("ul").hide();
            };

            fn._initComponent = function (options) {
                var self = this;
                this.component = options.$component.kendoTabStrip({
                    show: $.proxy(self.onTabActivated, self),
                    animation: kendomodo.getDefaultTabControlAnimation()
                }).data("kendoTabStrip");
            };

            fn.hideAllTabs = function () {
                this.toggleTabs(false);
            };

            fn.hideDisabledTabs = function () {
                var self = this;
                self.tabs.forEach(function (tab) {
                    if (!tab.isEnabled)
                        self.toggleTabs(false, tab.name);
                });
            };

            fn.removeAllTabs = function () {
                for (var i = 0; i < this.tabs.length; i++)
                    this.removeTab(this.tabs[i].name);
            };

            fn.removeTab = function (name) {

                var index = this.indexOfTab(name);
                if (index === -1)
                    return;

                var tab = this.getTab(index);

                if (tab.vm && tab.vm.clear)
                    tab.vm.clear();

                this.tabs.splice(index, 1);

                _removeTabElem(this, name);
            };

            function _removeTabElem(self, name) {
                self.component.remove(getIndexOfTabElementByName(self, name));
            }

            fn.setTabVisible = function (name, value) {
                var tab = this.getTab(name);
                if (value && !tab._canShow)
                    return;

                this._toggleTabCore(this.getTabElem(name), value);
            };

            function getIndexOfTabElementByName(self, name) {
                var elements = self.component.items();
                for (var i = 0; i < elements.length; i++) {
                    if ($(elements[i]).data("name") === name)
                        return i;
                }

                return -1;
            }

            fn.toggleTabs = function (visible, name) {
                var tab;
                for (var i = 0; i < this.tabs.length; i++) {
                    tab = this.tabs[i];

                    if (name && name !== tab.name)
                        continue;

                    if (visible && !tab._canShow)
                        continue;

                    if (!visible && !this._canHideTab(tab))
                        continue;

                    this._toggleTabCore(this.getTabElem(tab.name), visible);
                }
            };

            fn._canHideTab = function (tab) {
                return true;
            };

            fn._toggleTabCore = function ($tab, visible) {
                $tab.attr("style", "display:" + (visible ? "inline-block" : "none"));
            };

            fn.getTabContentElem = function (name) {
                return $(this.component.contentElement(getIndexOfTabElementByName(this, name)));
            };

            fn.getTabElem = function (name) {
                return $(this.component.items()[getIndexOfTabElementByName(this, name)]);
            };

            fn.getTab = function (nameOrIndex) {

                if (typeof nameOrIndex === "string") {
                    for (var i = 0; i < this.tabs.length; i++) {
                        if (this.tabs[i].name === nameOrIndex)
                            return this.tabs[i];
                    }
                }
                else if (nameOrIndex < this.tabs.length) {
                    return this.tabs[nameOrIndex];
                }

                return null;
            };

            fn.indexOfTab = function (name) {
                for (var i = 0; i < this.tabs.length; i++) {
                    if (this.tabs[i].name === name)
                        return i;
                }

                return -1;
            };

            fn.selectTab = function (name) {
                this.component.select(getIndexOfTabElementByName(this, name));
            };

            fn.selectTabAt = function (index) {
                this.component.select(index);
            };

            fn.onTabActivated = function (e) {
                var name = $(e.item).data("name");

                this._onTabActivatedCore(name);
            };

            fn._onTabActivatedCore = function (name) {

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

                if (tab.vm && typeof tab.filters === "function")
                    tab.vm.applyFilter(tab.filters(tabEvent));

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

                return true;
            };

            fn._initTab = function (tab) {
                if (tab._isInitPerformed)
                    return;

                tab._isInitPerformed = true;

                if (typeof tab.init === "function")
                    tab.init(this._createTabEvent(tab));
            };

            fn._createTabEvent = function (tab) {
                return { sender: this, tab: tab };
            };

            fn._canClearTab = function (tab) {
                return true;
            };

            fn._canEnterTab = function (tab) {
                return tab.isEnabled;
            };

            return TabControlViewModel;
        })();

        ui.TabControlViewModel = TabControlViewModel;

        // TabControlPageSpaceViewModel ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        var TabControlPageSpaceViewModel = (function (_super) {

            casimodo.__extends(TabControlPageSpaceViewModel, _super);

            function TabControlPageSpaceViewModel(options) {

                _super.call(this, options);

                this.space = null;
                this._getSpaceOnDemand = null;
                if (typeof options.space === "function")
                    this._getSpaceOnDemand = options.space;
                else
                    this.space = options.space || null;

                this.master = !!options.master || false;
            }

            var fn = TabControlPageSpaceViewModel.prototype;

            fn.bindMaster = function () {
                kendo.bind(this.getContentElem(), this.owner.mvm.scope);
            };

            fn.bindModel = function () {
                kendo.bind(this.getContentElem(), this.vm.scope);
            };

            fn.bind = function ($elem) {
                if ($elem === null || this.vm === null || !this.vm.scope)
                    return;

                kendo.bind($elem, this.vm.scope);
            };

            return TabControlPageSpaceViewModel;

        })(TabControlPageViewModel);

        ui.TabControlPageSpaceViewModel = TabControlPageSpaceViewModel;

        // TabControlSpaceViewModel ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        var TabControlSpaceViewModel = (function (_super) {

            casimodo.__extends(TabControlSpaceViewModel, _super);

            function TabControlSpaceViewModel(options) {

                _super.call(this, options);
            }

            var fn = TabControlSpaceViewModel.prototype;

            return TabControlSpaceViewModel;
        })(TabControlViewModel);

        ui.TabControlSpaceViewModel = TabControlSpaceViewModel;

        // MasterDetailTabControlSpaceViewModel ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        var MasterDetailTabControlSpaceViewModel = (function (_super) {
            casimodo.__extends(MasterDetailTabControlSpaceViewModel, _super);

            function MasterDetailTabControlSpaceViewModel(options) {

                if (typeof options.hideAll === "undefined")
                    options.hideAll = true;

                _super.call(this, options);

                if (this._options.masterSpace)
                    throw new Error("Master space is obsolete.");

                this.mvm = null;
                this.mtab = this.tabs.find(x => x.master === true);
                if (!this.mtab)
                    throw new Error("Master tab is missing.");

                this._initTab(this.mtab);
            }

            var fn = MasterDetailTabControlSpaceViewModel.prototype;

            fn._start = function () {
                //if (this.mtab && this.mtab._canShow) {
                //    this.toggleTabs(true, this.mtab.name);
                //    this.selectTab(this.mtab.name);
                //}
            };

            fn.setMasterViewModel = function (vm) {
                var self = this;

                this.mvm = vm;
                // KABU TODO: VERY VERY IMPORTANT: React on current *selected* item changed,
                //   because after we add new item, no item is selected, but the current
                //   item still references the last selected item.
                this.mvm.on("currentItemChanged", function (e) {

                    // On selected master item changed.
                    // Show/hide tabs if the master item is selected/not selected.
                    self.toggleTabs(self.mvm.scope.item !== null);

                    // Clear all tabs.
                    self.tabs.forEach(function (tab) {

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
                    });
                });
            };

            fn.getCurrentMasterItem = function () {
                return this.mvm.scope.item || null;
            };

            fn._initTab = function (tab) {
                if (tab._isInitPerformed)
                    return;

                tab._isInitPerformed = true;

                if (tab.icomponent) {
                    tab.space = tab.icomponent.space();
                    tab.vm = tab.space.vm;
                }
                else {
                    if (tab._getSpaceOnDemand)
                        tab.space = tab._getSpaceOnDemand() || null;

                    if (tab.space && tab.space.vm)
                        tab.vm = tab.space.vm;
                }

                if (tab.master && tab.vm)
                    this.setMasterViewModel(tab.vm);

                if (typeof tab.init === "function")
                    tab.init(this._createTabEvent(tab));
            };

            fn._canHideTab = function (tab) {
                return !tab.master;
            };

            fn._createTabEvent = function (tab) {
                return { sender: this, tab: tab, space: tab.space, vm: tab.vm, master: this.mvm.scope.item };
            };

            fn._canClearTab = function (tab) {
                // Don't clear the master's view model.
                return !tab.master;
            };

            fn._canEnterTab = function (tab) {
                // Enter tabs only if enabled and master item is selected.
                return tab.isEnabled && this.mvm.scope.item !== null;
            };

            return MasterDetailTabControlSpaceViewModel;

        })(TabControlSpaceViewModel);

        ui.MasterDetailTabControlSpaceViewModel = MasterDetailTabControlSpaceViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
