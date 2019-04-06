namespace kmodo {

    interface WizardPageOptions {
        owner?: any;
        name: string;
        commands: WizardCommand[] | (() => WizardCommand[]);
        init?: () => void;
        enter?: () => void;
        leave?: () => void;
        validate?: () => boolean;
        isEnabled?: boolean;
    }

    export class WizardPage {
        owner: any = null;
        name: string = "";
        commands: WizardCommand[] | (() => WizardCommand[]) = null;
        initialized = false;
        init?: () => void = null;
        enter: () => void = null;
        leave?: () => void = null;
        validate: () => boolean = null;
        isEnabled = true;
        tab: TabPage = null;

        constructor(options: WizardPageOptions) {
            this.name = options.name;
            this.commands = options.commands;
            this.init = options.init;
            this.enter = options.enter;
            this.leave = options.leave;
            this.validate = options.validate;
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
        pages: WizardPage[];
    }

    interface WizardCommand extends kmodo.ObservableObject {
        name: string;
        text?: string;
        enabled?: boolean;
        visible?: boolean;
        onTriggered?: Function;
    }

    export class Wizard extends cmodo.ComponentBase {
        $component: JQuery;
        currentPage: WizardPage = null;
        _commonPageCommandHandlers: any;
        _pages: WizardPage[];
        _commands: WizardCommand[] = [];
        _tabStrip: TabStrip;
        commandsViewModel: kendo.data.ObservableObject;

        constructor(options: WizardOptions) {
            super();

            this.$component = options.$component;

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
            for (const page of this._pages) {
                page.owner = this;
            }

            this._createCommands();
            this.createView(options);
        }

        start(): void {
            const startPage = Enumerable.from(this._pages).firstOrDefault();
            if (!startPage)
                return;

            this._moveToPage(startPage.name);
        }

        setPageEnabled(name: string, value: boolean): void {
            const page = this.getPage(name);
            if (!page)
                return;

            page.isEnabled = !!value;
        }

        setPageCommandEnabled(name: string, value: boolean): void {
            const cmd = this._getCommand(name);
            if (!cmd)
                return;

            cmd.set("isEnabled", !!value);
        }

        moveToNextPage(): boolean {
            return this._moveToNextPage();
        }

        private _canLeaveCurrentPage(): boolean {
            if (!this.currentPage)
                return false;

            if (this.currentPage.validate) {
                if (!this.currentPage.validate()) {
                    return false;
                }
            }

            return true;
        }

        private _moveToNextPage(): boolean {
            if (!this._canLeaveCurrentPage()) {
                return false;
            }

            const pages = Enumerable.from(this._pages);
            let idx = pages.indexOf(this.currentPage) + 1;

            // Get first enabled next page.
            let nextPage: WizardPage;
            while ((nextPage = pages.elementAtOrDefault(idx)) !== null && !nextPage.isEnabled)
                idx++;
            if (!nextPage)
                return false;

            this._moveToPage(nextPage.name);

            return true;
        }

        private _moveToPreviousPage(): boolean {
            if (!this._canLeaveCurrentPage()) {
                return false;
            }

            const pages = Enumerable.from(this._pages);
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
            const page = this.getPage(name);
            if (!page)
                return;

            this._tabStrip.selectTab(name);
        }

        private _executeCommand(name: string): boolean {
            if (!this.currentPage)
                return false;

            const cmd = this._getCommand(name);

            if (cmd.name === "back") {
                return this._moveToPreviousPage();
            }
            else if (cmd.name === "next") {
                return this._moveToNextPage();
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

            return true;
        }

        getPage(name: string): WizardPage {
            return this._pages.find((x) => x.name === name);
        }

        private _hideAllCommands(): void {
            for (const cmd of this._commands) {
                cmd.set("isVisible", false);
                cmd.set("isEnabled", false);
            }
        }

        private _getCommand(name: string): WizardCommand {
            return this._commands.find((x) => x.name === name);
        }

        private _createCommands(): void {
            this._commands = [];

            const commands = {};
            this._addCommand(commands, "back", "Zurück");
            this._addCommand(commands, "next", "Weiter");
            this._addCommand(commands, "finish", "Fertig stellen");
            this._addCommand(commands, "cancel", "Abbrechen");
            this._addCommand(commands, "close", "Schließen");

            this.commandsViewModel = kendo.observable(commands);
        }

        private _addCommand(commands: any, name: string, text: string): void {
            const cmd = kmodo.ObservableHelper.observable<WizardCommand>({
                name: name,
                text: text,
                enabled: name === "back" || name === "cancel" ? true : false,
                visible: false,
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
            this._hideAllCommands();

            if (this.currentPage) {
                if (this.currentPage.leave) {
                    this.currentPage.leave();
                }
            }

            const page = this.getPage(name);
            if (!page)
                return;

            this.currentPage = page;

            if (typeof page.commands === "function") {
                const commands = page.commands();
                if (commands) {
                    for (const x of commands) {
                        const cmd = this._getCommand(x.name);
                        if (cmd) {
                            // Set text
                            if (typeof x.text !== "undefined") {
                                cmd.set("text", x.text);
                            }

                            // Set enabled
                            let enabled = false;
                            if (typeof x.enabled !== "undefined") {
                                // Explicitely defined by the consumer.
                                enabled = !!x.enabled;
                            }
                            else if (x.name === "back" || x.name === "cancel" || x.name === "close") {
                                // Special commands are implicitely enabled by default.
                                enabled = true;
                            }
                            cmd.set("isEnabled", enabled);

                            // Set visibility
                            cmd.set("isVisible", true);
                        }
                    }
                }
            }

            if (!page.initialized && page.init) {
                page.initialized = true;
                page.init();
            }

            if (page.enter) {
                page.enter();
            }

            this.trigger("currentPageChanged", { sender: this, page: this.currentPage });
        }

        private createView(options: WizardOptions) {
            // Init kendo tabstrip.
            const $tabStripElem = options.$component.children("div.kendomodo-wizard-control").first();
            if ($tabStripElem.length) {
                this._tabStrip = new TabStrip({
                    $component: $tabStripElem,
                    tabs: this._pages.map(function (x) { return x.tab; })
                });

                this._tabStrip.on("currentTabChanged", (e) => {
                    this._onPageActivated(e.tab.name);
                });

                this._tabStrip.hideTabStrip();
            }

            // Bind commands.
            const $commands = this.$component.children("div.kendomodo-wizard-commands").first();
            if ($commands.length) {
                for (const cmd of this._commands) {
                    $commands.append('<a data-role="button" data-bind="visible:' + cmd.name + '.isVisible, enabled:' + cmd.name + '.isEnabled, events: { click:' + cmd.name + '.onTriggered }"><span data-bind="text:' + cmd.name + '.text"></span></a>');
                }

                kendo.bind($commands, this.commandsViewModel);
            }
        }
    }
}
