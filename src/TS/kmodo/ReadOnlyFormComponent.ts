namespace kmodo {

    export interface FormReadOnlyFormOptions extends DataSourceViewOptions {
        editor?: EditorInfo;
    }

    export class ReadOnlyForm extends DataSourceViewComponent {
        protected _options: FormReadOnlyFormOptions;
        private $toolbar: JQuery;
        private _renderers: CustomPropViewComponentInfo[];
        private _customCommands: any[];

        constructor(options: FormReadOnlyFormOptions) {
            super(options);

            // TODO: REMOVE: this._options = super._options as FormReadOnlyViewOptions;

            this.$view = null;
            this.$toolbar = null;
            this._renderers = [];
            this._customCommands = [];
            this.createDataSource();
        }

        registerCustomCommand(name: string, execute: Function) {
            this._customCommands.push({
                name: name,
                execute: execute || null
            });
        }

        setTitle(title: string) {
            this.$toolbar.find(".details-view-title").text(title || null);
        }

        protected createDataSourceOptions(): kendo.data.DataSourceOptions {
            return Object.assign(super.createDataSourceOptions(),
                {
                    pageSize: 1
                });
        }

        // override
        protected extendDataSourceOptions(options) {
            // Attach event handlers.
            options.change = e => this.onDataSourceChanged(e);
            options.error = e => this.onDataSourceError(e);
            options.requestStart = e => this.onDataSourceRequestStart(e);
            options.requestEnd = e => this.onDataSourceRequestEnd(e);
        }

        private onDataSourceChanged(e) {
            const item = this.dataSource.data()[0] || null;

            this.setCurrent(item);

            for (const ed of this._renderers) {

                // Perform setValue() on code-mirror editor in order to
                //   populate it with the current textarea's value.
                //   This is needed because code-mirror does not do that automatically.
                if (ed.type === "CodeMirror") {
                    ed.component.setValue(ed.$el.val());
                }
            }
        }

        // overwrite
        protected _applyAuth() {
            if (!this.auth.canView) {
                this.$view.children().remove();
                this.$view.prepend("<div style='color:red'>This view is not permitted.</div>");
                // this.component.wrapper.hide();
            }

            // Init edit button.
            const $editBtn = this.$toolbar.find(".edit-command");
            if (this.auth.canModify && this._options.editor) {
                $editBtn.on("click", e => {
                    this._openEditor();
                });
                $editBtn.show();
            }
            else {
                // Editing not permitted. Remove edit button.
                $editBtn.remove();
            }
        }

        private _openEditor() {
            if (!this.auth.canModify)
                throw new Error("Modification is not permitted.");

            if (!this._options.editor || !this._options.editor.url)
                return;

            const item = this.getCurrent();
            if (item === null)
                return;

            kmodo.openById(this._options.editor.id,
                {
                    mode: "modify",
                    itemId: item[this.keyName],
                    // NOTE: We don't allow deletion via the read-only detail views,
                    //   but only via the grid view model.
                    canDelete: false,
                    finished: (result) => {
                        if (result.isOk) {
                            // Trigger a saved event if the data was modified and saved.
                            this.trigger("saved");
                        }

                        this.refresh();
                    }
                });
        }

        private _prepareView() {
            this.$view.find('.remove-on-Read').remove();
        }

        private _executeCustomCommand(name: string) {
            const cmd = this._customCommands.find(x => x.name === name);
            if (!cmd)
                return;

            if (!cmd.execute)
                return;

            cmd.execute();
        }

        // override
        createView() {
            if (this._isViewInitialized) return
            this._isViewInitialized = true;

            this.$view = $("#details-view-" + this._options.id);

            this._prepareView();

            this._initTextRenderers();

            // Toolbar commands.
            this.$toolbar = this.$view.find(".details-view-toolbar");
            // Refresh command.
            this.$toolbar.find(".refresh-command").on("click", e => {
                kmodo.progress(true, this.$view);
                this.refresh()
                    .finally(() => {
                        kmodo.progress(false, this.$view);
                    });
            });
            // Custom commands.
            this.$toolbar.find(".custom-command").on("click", e => {
                const commandName = $(e.currentTarget).attr("data-command-name");
                this._executeCustomCommand(commandName);
            });

            this.setTitle(this._options.title);
        }

        private _initTextRenderers() {
            this.$view.find("textarea[data-use-renderer]").each((idx, elem) => {
                const $el = $(elem);
                const type = $el.data("use-renderer");

                if (type === "scss" || type === "html") {

                    // https://codemirror.net/doc/manual.html
                    const renderer = CodeMirror.fromTextArea(elem as HTMLTextAreaElement, {
                        mode: this._getCodeMirrorMode(type),
                        lineNumbers: true,
                        indentUnit: 4,
                        indentWithTabs: true
                    });

                    // Register editor.
                    this._renderers.push({ type: "CodeMirror", component: renderer, $el: $el });
                }
            });
        }

        private _getCodeMirrorMode(type: string) {
            if (type === "scss")
                return "text/x-scss";
            if (type === "html")
                return "htmlmixed";

            throw new Error("Unexpected text content type '" + type + "'.");
        }
    }
}