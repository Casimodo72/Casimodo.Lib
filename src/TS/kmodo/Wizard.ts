namespace kmodo {

    interface WizardPageOptions {
        owner?: any;
        name: string;
        commands: WizardCommand[] | (() => WizardCommand[]);
        enter?: any;
        isEnabled?: boolean;
    }

    export class WizardPageViewModel {
        owner: any;
        name: string;
        commands: WizardCommand[] | (() => WizardCommand[]);
        enter: any;
        isEnabled: boolean;
        tab: TabPage;

        constructor(options: WizardPageOptions) {

            this.owner = null;
            this.name = options.name;
            this.commands = options.commands;
            this.enter = options.enter;
            if (typeof options.isEnabled !== "undefined")
                this.isEnabled = !!options.isEnabled;
            else
                this.isEnabled = true;
            this.tab = new TabPage({
                name: options.name
            });
        }

        canShow(value): boolean {
            return false;
        }

        visible(value): boolean {
            return true;
        }
    }

    interface WizardOptions {
        $component: JQuery;
        close?: Function;
        cancel?: Function;
        finish: Function;
        pages: WizardPageViewModel[];
    }

    interface WizardCommand extends kmodo.ObservableObject {
        name: string;
        text?: string;
        isEnabled?: boolean;
        isVisible?: boolean;
        onTriggered?: Function;
    }

    export class WizardViewModel extends cmodo.ComponentBase {
        $component: JQuery;
        currentPage: any;
        _commonPageCommandHandlers: any;
        _pages: WizardPageViewModel[];
        _commands: WizardCommand[];
        _tabStrip: TabStrip;
        commandsViewModel: kendo.data.ObservableObject;

        constructor(options: WizardOptions) {
            super();
            let self = this;
            this.$component = options.$component;
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

            this._createCommands();
            this.createView(options);
        }

        start(): void {
            let startPage = Enumerable.from(this._pages).firstOrDefault();
            if (!startPage)
                return;

            this._moveToPage(startPage.name);
        }

        setPageEnabled(name: string, value: boolean): void {
            let page = this.getPage(name);
            if (!page)
                return;

            page.isEnabled = !!value;
        }

        setPageCommandEnabled(name: string, value: boolean): void {
            let cmd = this._getCommand(name);
            if (!cmd)
                return;

            cmd.set("isEnabled", !!value);
        }

        moveToNextPage(): void {
            this._moveToNextPage();
        }

        private _moveToNextPage(): boolean {
            if (!this.currentPage)
                return false;

            let pages = Enumerable.from(this._pages);
            let idx = pages.indexOf(this.currentPage) + 1;

            // Get first enabled next page.
            let nextPage: WizardPageViewModel;
            while ((nextPage = pages.elementAtOrDefault(idx)) !== null && !nextPage.isEnabled)
                idx++;
            if (!nextPage)
                return false;

            this._moveToPage(nextPage.name);

            return true;
        }

        private _moveToPreviousPage(): boolean {
            if (!this.currentPage)
                return false;

            let pages = Enumerable.from(this._pages);
            let idx = pages.indexOf(this.currentPage) - 1;

            // Get first enabled previous page.
            let prevPage;
            while ((prevPage = pages.elementAtOrDefault(idx)) !== null && !prevPage.isEnabled)
                idx--;
            if (!prevPage)
                return false;

            this._moveToPage(prevPage.name);

            return true;
        }

        private _moveToPage(name: string) {
            let page = this.getPage(name);
            if (!page)
                return;

            this._tabStrip.selectTab(name);
        }

        private _executeCommand(name: string): void {
            if (!this.currentPage)
                return;

            let cmd = this._getCommand(name);

            if (cmd.name === "back") {
                this._moveToPreviousPage();
            }
            else if (cmd.name === "next") {
                this._moveToNextPage();
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
        }

        getPage(name: string): WizardPageViewModel {
            return this._pages.find((x) => x.name === name);
        }

        private _hideAllCommands(): void {
            this._commands.forEach(function (x) {
                x.set("isVisible", false);
                x.set("isEnabled", false);
            });
        }

        private _getCommand(name: string): WizardCommand {
            return this._commands.find((x) => x.name === name);
        }

        private _createCommands(): void {
            this._commands = [];

            let commands = {};
            this._addCommand(commands, "back", "Zurück");
            this._addCommand(commands, "next", "Weiter");
            this._addCommand(commands, "finish", "Fertig stellen");
            this._addCommand(commands, "cancel", "Abbrechen");
            this._addCommand(commands, "close", "Schließen");

            this.commandsViewModel = kendo.observable(commands);
        }

        private _addCommand(commands: any, name: string, text: string): void {
            let cmd = kmodo.ObservableHelper.observable<WizardCommand>({
                name: name,
                text: text,
                isEnabled: name === "back" || name === "cancel" ? true : false,
                isVisible: false,
                onTriggered: () => this._executeCommand(name)
                // KABU TODO: REMOVE?
                //enable: function (value) {
                //    this.set("isEnabled", value);
                //},
                //show: function (value) {
                //    this.set("isVisible", value);
                //}
            });

            this._commands.push(cmd);

            commands[name] = cmd;
        }

        _onPageActivated(name: string): void {
            let self = this;
            this._hideAllCommands();

            let page = this.getPage(name);
            if (!page)
                return;

            this.currentPage = page;

            if (typeof page.commands === "function") {
                let commands = page.commands();
                if (commands) {
                    commands.forEach(function (x) {
                        let cmd = self._getCommand(x.name);
                        if (cmd) {
                            // Set text
                            if (typeof x.text !== "undefined") {
                                cmd.set("text", x.text);
                            }

                            // Set enabled
                            let enabled = false;
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
        }

        private createView(options: WizardOptions) {
            let self = this;

            // Init kendo tabstrip.
            let $tabStripElem = options.$component.children("div.kendomodo-wizard-control").first();
            if ($tabStripElem.length) {
                this._tabStrip = new TabStrip({
                    $component: $tabStripElem,
                    tabs: this._pages.map(function (x) { return x.tab; })
                });

                this._tabStrip.on("currentTabChanged", function (e) {
                    self._onPageActivated(e.tab.name);
                });

                this._tabStrip.hideTabStrip();
            }

            // Bind commands.
            let $commands = this.$component.children("div.kendomodo-wizard-commands").first();
            if ($commands.length) {
                this._commands.forEach(function (cmd) {
                    $commands.append('<a data-role="button" data-bind="visible:' + cmd.name + '.isVisible, enabled:' + cmd.name + '.isEnabled, events: { click:' + cmd.name + '.onTriggered }"><span data-bind="text:' + cmd.name + '.text"></span></a>');
                });
                kendo.bind($commands, this.commandsViewModel);
            }
        }
    }
}
