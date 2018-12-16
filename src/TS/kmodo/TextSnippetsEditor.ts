namespace kmodo {

    // KABU TODO: VERY IMPORTANT: DO NOT CHANGE any names here,
    // because we are using this also in the Angular mobile client project,
    // which is not TypeScript and thus will break silently otherwise.

    // See issue: https://www.telerik.com/forums/defining-a-kendo-model-in-typescript
    var SnippetItem = kendo.data.Model.define({
        fields: {
            index: { type: "number" },
            selected: { type: "boolean" },
            value: { type: "string" },
        },
        onChanged: function (e) {
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

    interface ViewModel extends ViewComponentModel {
        values: TextItem[];
        containers: TextItemContainer[];
        deleteAllValues: Function;
        deselectAllValues: Function;
    }

    interface ViewArgsParams {
        typeId: string;
    }

    interface ViewArgs extends ViewComponentArgs {
        params: ViewArgsParams;
    }

    export class TextSnippetsEditor extends ViewComponent {
        private _dialogWindow: kendo.ui.Window;
        protected args: ViewArgs;

        constructor(options: ViewComponentOptions) {
            super(options);

            var self = this;

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
                    var wasSelected = e.data.selected;
                    this.deselectAllValues();

                    if (!wasSelected)
                        e.data.set("selected", true);
                },
                deleteSelectedValue: function (e) {
                    var selected = this.values.find(x => x.selected);
                    if (selected) {
                        this.values.splice(selected.index, 1);
                        if (this.values.length) {

                            // Set new indexes
                            for (let i = selected.index; i < this.values.length; i++) {
                                this.values[i].set("index", i);
                            }

                            // Select the next or previous sibling.
                            var index = selected.index;
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
                    this.containers.forEach(function (x) {
                        x.set("selected", false);
                    });
                },
                onUseSnippet: function (e) {
                    self.insertSnippetValue(e.data.value);
                },
                onDeleteSnippet: function (e) {
                    alert("onDeleteSnippet");
                },
            });
        }

        getModel(): ViewModel {
            return super.getModel() as ViewModel;
        }

        setArgs(args: ViewArgs): void {

            this.args = args;

            this.args.isCancelled = false;
            this.args.isOk = false;

            this.args.buildResult = this.buildResult.bind(this);
        }

        private buildResult(): void {
            var self = this;
            var value: string = "";
            var values: string[] = this.getModel().values.map(function (x) { return x.value });

            values.forEach(function (val) {
                value += val;
                if (self.args.mode === "List") {
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
                this.addValueList(value.match(/[^\r\n]+/g).map(function (val) { return val.trim() }));

            }
            else {
                // Try to split into snippet items.
                var snippets = this.getAllSnippetStrings()
                    // Longer snippets first
                    .sort(function (a, b) { return -1 * (a.length - b.length) });

                var values: string[] = [];

                this.initValuesCore(value, values, snippets);

                this.addValueList(values);
            }
        }

        private initValuesCore(value: string, values: string[], snippets: string[]): void {
            var self = this;
            var matches = snippets.some((snippet) => {

                var index = value.indexOf(snippet);
                if (index !== -1) {

                    var pre = value.substring(0, index).trim();
                    if (pre.length) {
                        self.initValuesCore(pre, values, snippets);
                    }

                    values.push(snippet);

                    var tail = value.substring(index + snippet.length).trim();
                    if (tail.length) {
                        self.initValuesCore(tail, values, snippets);
                    }

                    // Stop at first hit.
                    return true;
                }

                return false;
            });

            if (!matches) {
                // Add whitespace separated tokens.
                value.match(/\S+/g).forEach(function (x) {
                    values.push(x);
                });
            }
        }

        private addValueList(values: string[]): void {
            var self = this;
            this.getModel().deselectAllValues();
            values.forEach(function (value) {
                self.getModel().values.push(self.createValue(value, false));
            });
        }

        private insertSnippetValue(value: string): void {
            var selected = this.getModel().values.find((x) => x.selected);
            var newItem = this.createValue(value, false);
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
            var item = new SnippetItem({
                id: kendo.guid(),
                index: this.getModel().values.length,
                selected: selected,
                value: value
            }) as any;
            item.bind("change", item.onChanged);

            return item;
        }

        private getAllSnippetStrings(): string[] {
            var list: string[] = [];
            for (let container of this.getModel().containers)
                for (let item of container.items)
                    list.push(item.value);

            return list;
        }

        start(): Promise<void> {
            return this.refresh();
        }

        refresh(): Promise<void> {
            let self = this;

            let url = "/odata/TextItems/Ga.Query()?$select=Id,Value1,IsContainer,ContainerId&$orderby=Index";
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

                ds.query().then(function () {

                    let items: TextItemEntity[] = ds.data().map(x => x as TextItemEntity);

                    // Find containers
                    let containers: TextItemContainerLiteral[] = items
                        .filter(function (x) { return x.IsContainer })
                        .map(function (x) {
                            let container: TextItemContainerLiteral = {
                                id: x.Id,
                                value: x.Value1.trim(),
                                selected: false,
                                items: []
                            };
                            return container;
                        });

                    // Fill containers
                    let item, container;
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

                    let viewModel = self.getModel();

                    for (let x of containers)
                        viewModel.containers.push(x as any);

                    if (viewModel.containers.length) {
                        let first = viewModel.containers[0];
                        first.set("selected", true);
                        viewModel.set("container", first);
                    }

                    self.initValue(self.args.value);

                    resolve();
                })
                    .fail((err) => reject(err));
            });
        }

        // Create component ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        createView(): void {
            if (this._isComponentInitialized)
                return;
            this._isComponentInitialized = true;

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

            this._initComponentAsDialog();
        }

        private _initComponentAsDialog(): void {
            var self = this;

            this._dialogWindow = kmodo.findKendoWindow(this.$view);

            this._initDialogWindowTitle();

            // KABU TODO: IMPORTANT: There was no time yet to develop a
            //   decorator for dialog functionality. That's why the view model
            //   itself has to take care of the dialog commands which are located
            //   *outside* the widget.
            var $dialogCommands = $('#dialog-commands-' + this._options.id);
            // Init OK/Cancel buttons.
            $dialogCommands.find('button.ok-button').first().off("click.dialog-ok").on("click.dialog-ok", function () {

                self.args.buildResult();
                self.args.isCancelled = false;
                self.args.isOk = true;

                self._dialogWindow.close();
            });

            $dialogCommands.find('button.cancel-button').first().off("click.dialog-cancel").on("click.dialog-cancel", function () {
                self.args.isCancelled = true;
                self.args.isOk = false;

                self._dialogWindow.close();
            });
        }

        private _initDialogWindowTitle(): void {
            var title = "";

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