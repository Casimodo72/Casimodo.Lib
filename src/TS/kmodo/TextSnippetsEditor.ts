namespace kmodo {

    // KABU TODO: VERY IMPORTANT: DO NOT CHANGE any names here,
    // because we are using this also in the Angular mobile client project,
    // which is not TypeScript and thus will break silently otherwise.

    // See issue: https://www.telerik.com/forums/defining-a-kendo-model-in-typescript
    const SnippetItem = kendo.data.Model.define({
        fields: {
            index: { type: "number" },
            selected: { type: "boolean" },
            value: { type: "string" },
        },
        onChanged: function (e) {
            // NOTE: this is the observable
            if (e.field === "selected") {
                this.trigger("change", { field: "background" });
                this.trigger("change", { field: "borderWidth" });
                this.trigger("change", { field: "borderColor" });
            }
        },
        background: function () { return this.selected ? "#fff1bc" : "white" },
        borderWidth: function () { return this.selected ? "1.5px" : "0.5px" },
        borderColor: function () { return this.selected ? "orange" : "gray" }
    });

    interface TextItem extends kendo.data.ObservableObject {
        id: string;
        value: string;
        selected: boolean;
        index: number;
    }

    interface TextItemContainerLiteral {
        id: string;
        value: string;
        selected: boolean;
        items: TextItem[];
    }

    interface TextItemContainer extends kendo.data.ObservableObject {
        id: string;
        value: string;
        selected: boolean;
        items: TextItem[];
    }

    interface TextItemEntity extends kendo.data.ObservableObject {
        Id: string;
        Value1: string;
        IsContainer: boolean;
    }

    interface ViewModel extends kmodo.ComponentViewModel {
        values: TextItem[];
        containers: TextItemContainer[];
        deleteAllValues: Function;
        deselectAllValues: Function;
    }

    interface TextSnippetsEditorArgsParams {
        typeId: string;
    }

    interface TextSnippetsEditorArgs extends kmodo.ViewComponentArgs {
        params: TextSnippetsEditorArgsParams;
    }

    export class TextSnippetsEditor extends kmodo.ViewComponent {
        private _dialogWindow: kendo.ui.Window;
        protected args: TextSnippetsEditorArgs;

        constructor(options: kmodo.ViewComponentOptions) {
            super(options);

            this._options = {
                id: "64dcc132-7123-4855-a38f-864cb97d6a27",
                isDialog: true,
                isLookup: true
            };

            this.$view = null;
            this._dialogWindow = null;

            this.scope = kendo.observable({
                values: [],
                container: null,
                containers: [],

                deselectAllValues: function () {
                    this.values.forEach(function (x) {
                        x.set("selected", false);
                    });
                },
                onValueClicked: function (e) {
                    const wasSelected = e.data.selected;
                    this.deselectAllValues();

                    if (!wasSelected)
                        e.data.set("selected", true);
                },
                deleteSelectedValue: function (e) {
                    const selected = this.values.find(x => x.selected);
                    if (selected) {
                        this.values.splice(selected.index, 1);
                        if (this.values.length) {

                            // Set new indexes
                            for (let i = selected.index; i < this.values.length; i++) {
                                this.values[i].set("index", i);
                            }

                            // Select the next or previous sibling.
                            let index = selected.index;
                            while (index >= this.values.length)
                                index--;
                            this.values[index].set("selected", true);
                        }
                    }
                },
                deleteAllValues: function (e) {
                    this.values.empty();
                },
                onSelectContainer: function (e) {
                    this.deselectAllContainers();
                    this.set("container", e.data);
                    this.container.set("selected", true);
                },
                deselectAllContainers: function () {
                    this.containers.forEach((x) => {
                        x.set("selected", false);
                    });
                },
                onUseSnippet: e => {
                    this.insertSnippetValue(e.data.value);
                },
                onDeleteSnippet: function (e) {
                    alert("onDeleteSnippet");
                },
            });
        }

        getModel(): ViewModel {
            return super.getModel() as ViewModel;
        }

        setArgs(args: TextSnippetsEditorArgs): void {

            this.args = args;

            this.args.isCancelled = false;
            this.args.isOk = false;

            this.args.buildResult = this.buildResult.bind(this);
        }

        private buildResult(): void {
            let value: string = "";
            const values: string[] = this.getModel().values.map(x => x.value);

            values.forEach((val) => {
                value += val;
                if (this.args.mode === "List") {
                    value += "\r\n";
                }
                else {
                    value += " ";
                }
            });

            this.args.value = value && value.length ? value : null;
        }

        private initValue(value: string): void {

            this.getModel().deleteAllValues();

            if (!value) return;

            if (this.args.mode === "List") {
                // Split text into list items
                this.addValueList(value.match(/[^\r\n]+/g).map(val => val.trim()));

            }
            else {
                // Try to split into snippet items.
                const snippets = this.getAllSnippetStrings()
                    // Longer snippets first
                    .sort((a, b) => -1 * (a.length - b.length));

                const values: string[] = [];

                this.initValuesCore(value, values, snippets);

                this.addValueList(values);
            }
        }

        private initValuesCore(value: string, values: string[], snippets: string[]): void {
            const matches = snippets.some((snippet) => {
                const index = value.indexOf(snippet);
                if (index !== -1) {

                    const pre = value.substring(0, index).trim();
                    if (pre.length) {
                        this.initValuesCore(pre, values, snippets);
                    }

                    values.push(snippet);

                    const tail = value.substring(index + snippet.length).trim();
                    if (tail.length) {
                        this.initValuesCore(tail, values, snippets);
                    }

                    // Stop at first hit.
                    return true;
                }

                return false;
            });

            if (!matches) {
                // Add whitespace separated tokens.
                value.match(/\S+/g).forEach(x => values.push(x));
            }
        }

        private addValueList(values: string[]): void {
            this.getModel().deselectAllValues();
            values.forEach((value) => {
                this.getModel().values.push(this.createValue(value, false));
            });
        }

        private insertSnippetValue(value: string): void {
            const selected = this.getModel().values.find((x) => x.selected);
            const newItem = this.createValue(value, false);
            if (selected) {
                this.getModel().values.splice(selected.index, 0, newItem);
                // Set new indexes
                for (let i = selected.index; i < this.getModel().values.length; i++) {
                    this.getModel().values[i].set("index", i);
                }
            }
            else {
                this.getModel().values.push(newItem);
            }
        }

        private createValue(value: string, selected: boolean): any /* SnippetItem */ {
            const item = new SnippetItem({
                id: kendo.guid(),
                index: this.getModel().values.length,
                selected: selected,
                value: value
            }) as any;
            item.bind("change", item.onChanged);

            return item;
        }

        private getAllSnippetStrings(): string[] {
            const list: string[] = [];
            // NOTE: kendo.data.ObservableArray is *not* iterable. We can't use a for..of loop.
            this.getModel().containers.forEach(container =>
                container.items.forEach(item =>
                    list.push(item.value))
            );

            return list;
        }

        start(): Promise<void> {
            return this.refresh();
        }

        refresh(): Promise<void> {
            let url = "/odata/TextItems/Query()?$select=Id,Value1,IsContainer,ContainerId&$orderby=Index";
            url += "&$filter=TypeId+eq+" + this.args.params.typeId;

            let ds = new kendo.data.DataSource({
                type: "odata-v4",
                transport: {
                    read: {
                        url: url,
                        dataType: "json"
                    },
                }
            });

            return new Promise((resolve, reject) => {

                ds.query().then(() => {

                    const items: TextItemEntity[] = ds.data().map(x => x as TextItemEntity);

                    // Find containers
                    const containers: TextItemContainerLiteral[] = items
                        .filter((x) => x.IsContainer)
                        .map((x) => {
                            const container: TextItemContainerLiteral = {
                                id: x.Id,
                                value: x.Value1.trim(),
                                selected: false,
                                items: []
                            };
                            return container;
                        });

                    // Fill containers
                    let item;
                    let container;
                    for (let i = 0; i < items.length; i++) {
                        item = items[i];
                        for (let k = 0; k < containers.length; k++) {
                            container = containers[k];
                            if (item.ContainerId === container.id) {
                                container.items.push({
                                    id: item.Id,
                                    value: item.Value1.trim()
                                });
                            }
                        }
                    }

                    const viewModel = this.getModel();

                    for (let x of containers)
                        viewModel.containers.push(x as any);

                    if (viewModel.containers.length) {
                        const first = viewModel.containers[0];
                        first.set("selected", true);
                        viewModel.set("container", first);
                    }

                    this.initValue(this.args.value);

                    resolve();
                })
                    .fail((err) => reject(err));
            });
        }

        // Create component ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        createView(): void {
            if (this._isViewInitialized)
                return;
            this._isViewInitialized = true;

            this.$view = $("#view-" + this._options.id);

            if (this.args.mode === "List") {
                this.$view.find("#snippets-values-mode-default").remove();
                this.$view.find("#snippets-value-mode-default").remove();
            }
            else {
                this.$view.find("#snippets-values-mode-list").remove();
                this.$view.find("#snippets-value-mode-list").remove();
            }

            kendo.bind(this.$view, this.getModel());

            this._initViewAsDialog();
        }

        private _initViewAsDialog(): void {
            this._dialogWindow = kmodo.findKendoWindow(this.$view);

            this._initDialogWindowTitle();

            // KABU TODO: IMPORTANT: There was no time yet to develop a
            //   decorator for dialog functionality. That's why the view model
            //   itself has to take care of the dialog commands which are located
            //   *outside* the widget.
            const $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", () => {
                this.args.buildResult();
                this.args.isCancelled = false;
                this.args.isOk = true;

                this._dialogWindow.close();
            });

            $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", () => {
                this.args.isCancelled = true;
                this.args.isOk = false;

                this._dialogWindow.close();
            });
        }

        private _initDialogWindowTitle(): void {
            let title = "";

            if (this.args.title) {
                title = this.args.title;
            }
            else {
                title = this._options.title || "";

                if (this._options.isLookup)
                    title += " wählen";
            }

            this._dialogWindow.title(title);
        }
    }
}