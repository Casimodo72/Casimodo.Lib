"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

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

        var TextSnippetsEditorViewModel = (function (_super) {
            casimodo.__extends(TextSnippetsEditorViewModel, _super);

            function TextSnippetsEditorViewModel(options) {
                _super.call(this, options);

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
                                for (var i = selected.index; i < this.values.length; i++) {
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

            var fn = TextSnippetsEditorViewModel.prototype;

            fn.setArgs = function (args) {
                var self = this;
                this.args = args;

                this.args.isCancelled = false;
                this.args.isOk = false;

                this.args.buildResult = this.buildResult.bind(this);
            };

            fn.buildResult = function () {
                var self = this;
                var value = "";
                var values = this.scope.values.map(function (x) { return x.value });

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
            };

            fn.initValue = function (value) {
                var self = this;
                this.scope.deleteAllValues();

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

                    var values = [];

                    this.initValuesCore(value, values, snippets);

                    this.addValueList(values);
                }
            };

            fn.initValuesCore = function (value, values, snippets) {
                var self = this;
                var matches = snippets.some(function (snippet) {
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
                });

                if (!matches) {
                    // Add whitespace separated tokens.
                    value.match(/\S+/g).forEach(function (x) {
                        values.push(x);
                    });
                }
            };

            fn.addValueList = function (values) {
                var self = this;
                this.scope.deselectAllValues();
                values.forEach(function (value) {
                    self.scope.values.push(self.createValue(value, false));
                });
            };

            fn.insertSnippetValue = function (value) {
                var selected = this.scope.values.find((x) => x.selected);
                var newItem = this.createValue(value, false);
                if (selected) {
                    this.scope.values.splice(selected.index, 0, newItem);
                    // Set new indexes
                    for (var i = selected.index; i < this.scope.values.length; i++) {
                        this.scope.values[i].set("index", i);
                    }
                }
                else {
                    this.scope.values.push(newItem);
                }
            };

            fn.createValue = function (value, selected) {
                var item = new SnippetItem({
                    id: kendo.guid(),
                    index: this.scope.values.length,
                    selected: selected,
                    value: value
                });
                item.bind("change", item.onChanged);

                return item;
            };

            fn.getAllSnippetStrings = function () {
                var list = [];
                this.scope.containers.forEach(function (container) {
                    container.items.forEach(function (item) {
                        list.push(item.value);
                    });
                });

                return list;
            };

            fn.start = function () {
                return this.refresh();
            };

            fn.refresh = function () {
                var self = this;

                var url = "/odata/TextItems/Ga.Query()?$select=Id,Value1,IsContainer,ContainerId&$orderby=Index";
                url += "&$filter=TypeId+eq+" + this.args.params.typeId;

                var ds = new kendo.data.DataSource({
                    type: "odata-v4",
                    transport: {
                        read: {
                            url: url,
                            dataType: "json"
                        },
                    }
                });

                return ds.query().then(function () {

                    var items = ds.data();

                    // Find containers
                    var containers = items
                        .filter(function (x) { return x.IsContainer })
                        .map(function (x) {
                            return {
                                id: x.Id,
                                value: x.Value1.trim(),
                                selected: false,
                                items: []
                            }
                        });

                    // Fill containers
                    var item, container;
                    for (var i = 0; i < items.length; i++) {
                        item = items[i];
                        for (var k = 0; k < containers.length; k++) {
                            container = containers[k];
                            if (item.ContainerId === container.id) {
                                container.items.push({
                                    id: item.Id,
                                    value: item.Value1.trim()
                                });
                            }
                        }
                    }

                    containers.forEach(function (x) {
                        self.scope.containers.push(x);
                    });

                    if (self.scope.containers.length) {
                        var first = self.scope.containers.at(0);
                        first.set("selected", true);
                        self.scope.set("container", first);
                    }

                    self.initValue(self.args.value);
                });
            };

            // Create component ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            fn.createComponent = function () {
                if (this._isComponentInitialized)
                    return;
                this._isComponentInitialized = true;

                var self = this;

                this.$view = $("#view-" + this._options.id);

                if (this.args.mode === "List") {
                    this.$view.find("#snippets-values-mode-default").remove();
                    this.$view.find("#snippets-value-mode-default").remove();
                }
                else {
                    this.$view.find("#snippets-values-mode-list").remove();
                    this.$view.find("#snippets-value-mode-list").remove();
                }

                kendo.bind(this.$view, this.scope);

                this._initComponentAsDialog();
            };

            fn._initComponentAsDialog = function () {
                var self = this;

                this._dialogWindow = kendomodo.findKendoWindow(this.$view);

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
            };

            fn._initDialogWindowTitle = function () {
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
            };

            return TextSnippetsEditorViewModel;

        })(kendomodo.ui.ComponentViewModel);
        ui.TextSnippetsEditorViewModel = TextSnippetsEditorViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));