"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var WizardPageViewModel = /** @class */ (function () {

            function WizardPageViewModel(options) {
                
                this.owner = null;
                this.name = options.name;
                this.commands = options.commands;
                this.enter = options.enter;
                if (typeof options.isEnabled !== "undefined")
                    this.isEnabled = !!options.isEnabled;
                else
                    this.isEnabled = true;
                this.tab = new kendomodo.ui.TabControlPageViewModel({
                    name: options.name
                });
            }

            var fn = WizardPageViewModel.prototype;

            fn.canShow = function (value) {

            };

            fn.visible = function (value) {

            };

            return WizardPageViewModel;

        })();

        ui.WizardPageViewModel = WizardPageViewModel;

        var WizardViewModel = (function () {

            function WizardViewModel(options) {
                var self = this;
                this._$component = options.$component;
                this.currentPage = null;
                this._commonPageCommandHandlers = {
                    close: function () {
                        window.close();
                    },
                    cancel: function () {
                        window.close();
                    },
                    finish: null
                };
                if (options.close)
                    this._commonPageCommandHandlers.close = options.close;
                if (options.cancel)
                    this._commonPageCommandHandlers.cancel = options.cancel;
                if (options.finish)
                    this._commonPageCommandHandlers.finish = options.finish;
                this._pages = options.pages || [];
                this._pages.forEach(function (x) {
                    x.owner = self;
                });

                this._events = new casimodo.EventManager();
                this._createCommands();
                this._initComponent(options);
            }

            var fn = WizardViewModel.prototype;

            fn.on = function (eventName, func) {
                this._events.on(eventName, func);
            };

            fn.trigger = function (eventName, e) {
                this._events.trigger(eventName, e, this);
            };

            fn.start = function () {
                var startPage = Enumerable.from(this._pages).firstOrDefault();
                if (!startPage)
                    return;

                this._moveToPage(startPage.name);
            };

            fn.setPageEnabled = function (name, value) {
                var page = this.getPage(name);
                if (!page)
                    return;

                page.isEnabled = !!value;
            };

            fn.setPageCommandEnabled = function (name, value) {
                var cmd = this._getCommand(name);
                if (!cmd)
                    return;

                cmd.set("isEnabled", !!value);
            };

            fn.moveToNextPage = function () {
                this._moveToNextPage();
            };

            fn._moveToNextPage = function () {
                if (!this.currentPage)
                    return false;

                var pages = Enumerable.from(this._pages);
                var idx = pages.indexOf(this.currentPage) + 1;

                // Get first enabled next page.
                var nextPage;
                while ((nextPage = pages.elementAtOrDefault(idx)) !== null && !nextPage.isEnabled)
                    idx++;
                if (!nextPage)
                    return false;

                this._moveToPage(nextPage.name);

                return true;
            };

            fn._moveToPreviousPage = function () {
                if (!this.currentPage)
                    return false;

                var pages = Enumerable.from(this._pages);
                var idx = pages.indexOf(this.currentPage) - 1;

                // Get first enabled previous page.
                var prevPage;
                while ((prevPage = pages.elementAtOrDefault(idx)) !== null && !prevPage.isEnabled)
                    idx--;
                if (!prevPage)
                    return;

                this._moveToPage(prevPage.name);

                return true;
            };

            fn._moveToPage = function (name) {
                var page = this.getPage(name);
                if (!page)
                    return;

                this._tabControl.selectTab(name);
            };

            fn._executeCommand = function (cmd) {
                if (!this.currentPage)
                    return;

                if (cmd.name === "back") {
                    this._moveToPreviousPage({ sender: this });
                }
                else if (cmd.name === "next") {
                    this._moveToNextPage({ sender: this });
                }
                else if (cmd.name === "finish") {
                    if (this._commonPageCommandHandlers.finish)
                        this._commonPageCommandHandlers.finish({ sender: this });
                }
                else if (cmd.name === "cancel") {
                    if (this._commonPageCommandHandlers.cancel)
                        this._commonPageCommandHandlers.cancel({ sender: this });
                }
                else if (cmd.name === "close") {
                    if (this._commonPageCommandHandlers.close)
                        this._commonPageCommandHandlers.close({ sender: this });
                }
            };

            fn.getPage = function (name) {
                return Enumerable.from(this._pages).first(function (x) { return x.name === name; });
            };

            fn._hideAllCommands = function () {
                this._commands.forEach(function (x) {
                    x.set("isVisible", false);
                    x.set("isEnabled", false);
                });
            };

            fn._getCommand = function (name) {
                return Enumerable.from(this._commands).first(function (x) { return x.name === name; });
            };

            fn._createCommands = function () {
                this._commands = [];

                var commands = {};
                this._addCommand(commands, "back", "Zurück");
                this._addCommand(commands, "next", "Weiter");
                this._addCommand(commands, "finish", "Fertig stellen");
                this._addCommand(commands, "cancel", "Abbrechen");
                this._addCommand(commands, "close", "Schließen");

                this.commandsViewModel = kendo.observable(commands);
            };

            fn._addCommand = function (commands, name, text) {
                var self = this;

                var cmd = kendo.observable({
                    name: name,
                    text: text,
                    isEnabled: name === "back" || name === "cancel" ? true : false,
                    isVisible: false,
                    onTriggered: function () {
                        self._executeCommand(cmd);
                    }
                    //enable: function (value) {
                    //    this.set("isEnabled", value);
                    //},
                    //show: function (value) {
                    //    this.set("isVisible", value);
                    //}
                });

                this._commands.push(cmd);

                commands[name] = cmd;
            };

            fn._onPageActivated = function (name) {
                var self = this;
                this._hideAllCommands();

                var page = this.getPage(name);
                if (!page)
                    return;

                this.currentPage = page;

                if (typeof page.commands === "function") {
                    var commands = page.commands();
                    if (commands) {
                        commands.forEach(function (x) {
                            var cmd = self._getCommand(x.name);
                            if (cmd) {
                                // Set text
                                if (typeof x.text !== "undefined") {
                                    cmd.set("text", x.text);
                                }

                                // Set enabled
                                var enabled = false;
                                if (typeof x.isEnabled !== "undefined") {
                                    // Explicitely defined by the consumer.
                                    enabled = !!x.isEnabled;
                                }
                                else if (x.name === "back" || x.name === "cancel" || x.name === "close") {
                                    // Special commands are implicitely enabled by default.
                                    enabled = true;
                                }
                                cmd.set("isEnabled", enabled);

                                // Set visibility
                                cmd.set("isVisible", true);
                            }
                        });
                    }
                }

                if (typeof page.enter === "function") {
                    page.enter();
                }

                this.trigger("currentPageChanged", { sender: this, page: this.currentPage });
            };

            fn._initComponent = function (options) {
                var self = this;

                // Init kendo tabstrip.
                var $tabControl = options.$component.children("div.kendomodo-wizard-control").first();
                if ($tabControl.length) {
                    this._tabControl = new kendomodo.ui.TabControlViewModel({
                        $component: $tabControl,
                        tabs: this._pages.map(function (x) { return x.tab; })
                    });

                    this._tabControl.on("currentTabChanged", function (e) {
                        self._onPageActivated(e.tab.name);
                    });

                    this._tabControl.hideTabStrip();
                }

                // Bind commands view model.
                var $commands = this._$component.children("div.kendomodo-wizard-commands").first();
                if ($commands.length) {
                    this._commands.forEach(function (cmd) {
                        $commands.append('<a data-role="button" data-bind="visible:' + cmd.name + '.isVisible, enabled:' + cmd.name + '.isEnabled, events: { click:' + cmd.name + '.onTriggered }"><span data-bind="text:' + cmd.name + '.text"></span></a>');
                    });
                    kendo.bind($commands, this.commandsViewModel);
                }

            };

            return WizardViewModel;
        })();

        ui.WizardViewModel = WizardViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
