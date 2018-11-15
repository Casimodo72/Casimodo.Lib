/// <reference path="DataSourceViewComponent.ts" />

namespace kmodo {

    export interface FormReadOnlyViewOptions extends DataSourceViewOptions {
        editor?: EditorInfo;
    }

    export class ReadOnlyFormComponent extends DataSourceViewComponent {
        protected _options: FormReadOnlyViewOptions;
        private $toolbar: JQuery;
        private _renderers: CustomPropViewComponentInfo[];
        private _customCommands: any[];

        constructor(options: FormReadOnlyViewOptions) {
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
            return {
                type: 'odata-v4',
                schema: {
                    model: this.createDataModel()
                },
                transport: this.createDataSourceTransportOptions(),
                pageSize: 1,
                serverPaging: true,
                serverSorting: true,
                serverFiltering: true,
            };
        }

        // override
        protected extendDataSourceOptions(options) {
            // Attach event handlers.
            options.change = this._eve(this.onDataSourceChanged);
            options.error = this._eve(this.onDataSourceError);
            options.requestStart = this._eve(this.onDataSourceRequestStart);
            options.requestEnd = this._eve(this.onDataSourceRequestEnd);
        }

        private onDataSourceChanged(e) {

            var item = this.dataSource.data()[0] || null;

            this.setCurrentItem(item);

            this._renderers.forEach(function (ed) {

                // Perform setValue() on code-mirror editor in order to
                //   populate it with the current textarea's value.
                //   This is needed because code-mirror does not do that automatically.
                if (ed.type === "CodeMirror") {
                    ed.component.setValue(ed.$el.val());
                }
            });
        }

        // overwrite
        protected _applyAuth() {
            var self = this;

            if (!this.auth.canView) {
                this.$view.children().remove();
                this.$view.prepend("<div style='color:red'>This view is not permitted.</div>");
                // this.component.wrapper.hide();
            }

            // Init edit button.
            var $editBtn = this.$toolbar.find(".edit-command");
            if (this.auth.canModify && this._options.editor) {

                $editBtn.on("click", (e) => {
                    self._openEditor();
                });
                $editBtn.show();
            }
            else {
                // Editing not permitted. Remove edit button.
                $editBtn.remove();
            }
        }

        private _openEditor() {
            var self = this;

            if (!this.auth.canModify)
                throw new Error("Modification is not permitted.");

            if (!this._options.editor || !this._options.editor.url)
                return;

            var item = self.getCurrentItem();
            if (item === null)
                return;

            kmodo.openById(this._options.editor.id,
                {
                    mode: "modify",
                    itemId: item[this.keyName],
                    // NOTE: We don't allow deletion via the read-only detail views,
                    //   but only via the grid view model.
                    canDelete: false,
                    finished: function (result) {
                        if (result.isOk) {
                            // Trigger a saved event if the data was modified and saved.
                            self.trigger("saved");
                        }

                        self.refresh();
                    }
                });
        }

        private _prepareView() {
            this.$view.find('.remove-on-Read').remove();
        }

        private _executeCustomCommand(name: string) {
            var cmd = this._customCommands.find(x => x.name === name);
            if (!cmd)
                return;

            if (!cmd.execute)
                return;

            cmd.execute();
        }

        // override
        createComponent() {

            if (this.component) return;

            var self = this;

            // Create dummy component object.
            this.setComponent({});

            this.$view = $("#details-view-" + this._options.id);

            this._prepareView();

            this._initTextRenderers();

            // Toolbar commands.
            this.$toolbar = this.$view.find(".details-view-toolbar");
            // Refresh command.
            this.$toolbar.find(".refresh-command").on("click", (e) => {
                kmodo.progress(true, self.$view);
                self.refresh()
                    .finally(function () {
                        kmodo.progress(false, self.$view);
                    });
            });
            // Custom commands.
            this.$toolbar.find(".custom-command").on("click", (e) => {
                var commandName = $(this).attr("data-command-name");
                self._executeCustomCommand(commandName);
            });

            this.setTitle(this._options.title);
        }

        private _initTextRenderers() {
            var self = this;

            this.$view.find("textarea[data-use-renderer]").each((index, elem) => {
                var $el = $(elem);
                var type = $el.data("use-renderer");

                if (type === "scss" || type === "html") {

                    // https://codemirror.net/doc/manual.html
                    var renderer = CodeMirror.fromTextArea(elem as HTMLTextAreaElement, {
                        mode: self._getCodeMirrorMode(type),
                        lineNumbers: true,
                        indentUnit: 4,
                        indentWithTabs: true
                    });

                    // Register editor.
                    self._renderers.push({ type: "CodeMirror", component: renderer, $el: $el });
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