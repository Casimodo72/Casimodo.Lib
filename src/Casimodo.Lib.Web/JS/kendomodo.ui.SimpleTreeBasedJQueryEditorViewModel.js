"use strict";
var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var SimpleJQueryEditorViewModelBase = (function () {

            function SimpleJQueryEditorViewModelBase($area) {
                this.$area = $area;
                this.$commands = $area.find(".editor-commands");
                this.$editor = $area.find(".content-editor");
                this.commands = ["add", "edit", "save", "cancel", "delete"];

                this.state = "";
                this.currentItem = null;
                this.isEditing = false;
                this.commandStateDefinitions = {
                    "": ["add"],
                    "selected": ["add", "edit", "delete"],
                    "add": ["save", "cancel"],
                    "edit": ["save", "cancel"]
                };
            }

            var fn = SimpleJQueryEditorViewModelBase.prototype;

            fn.init = function () {
                var self = this;
                this.commands.forEach(this._bindCmd.bind(self));

                return this;
            };

            fn.updateCommandStates = function () {
                var self = this;
                var states = this.commandStateDefinitions[this.state] || [];
                this.commands.forEach(function (cmd) {
                    if (!self.currentItem)
                        self._setCmd(cmd, false);
                    else if (cmd === "add" && !self.canAdd(self.currentItem))
                        self._setCmd(cmd, false);
                    else
                        self._setCmd(cmd, states.indexOf(cmd) !== -1);
                });

                this.isEditing = this.state === "edit" || self.state === "add";

                var editorColor = this.isEditing ? "orange" : "gray";
                self.$editor.attr('readonly', !this.isEditing);
                self.$editor.css('border-color', editorColor);
            };

            fn.canAdd = function (contextItem) {
                return true;
            };

            fn.createItem = function () {
                return null;
            };

            fn._add = function () {
                this._transition("add");
            };

            fn._edit = function () {
                this._transition("edit");
            };

            fn._save = function () {
                // NOTE: This is just an example stub which does not save anything.
                var self = this;

                if (!self.currentItem) return;

                var text = self.$editor.val().trim();

                //var data;

                if (self.state === "edit") {

                    var id = self.currentItem.Id;

                    //data = {
                    //    Id: id
                    //};
                }
                else if (self.state === "add") {

                    var cur = self.currentItem;
                    if (!cur) {
                        self._failedToAdd();
                        return;
                    }

                    //data = {
                    //    Id: cur.Id
                    //};
                }
            };

            fn._cancel = function () {

                if (this.state === "add") {
                    this._transition("");
                }

                if (this.state === "edit") {
                    this._transition("selected");
                }
            };

            // Just an example template of the delete function.
            fn._delete = function () {
                var self = this;
                var id = this.currentItem.Id;

                // Confirm deletion and delete.
                kendomodo.ui.openDeletionConfirmationDialog(this.options.messages.confirmationOnDeleting)
                    .then(function (result) {
                        if (result !== true)
                            return;

                        casimodo.oDataCRUD("DELETE", "/odata/XYZ", id)
                            .then(function () {

                                // IMPL: Delete from view model.

                            })
                            .catch(function (ex) {
                                self._failedToDelete();
                            });
                    });

                this._transition("");
            };

            fn._failedToUpdate = function () {
                alert(this.options.messages.failedToUpdate || "Änderung fehlgeschlagen");
            };

            fn._failedToAdd = function () {
                alert(this.options.messages.failedToAdd || "Hinzufügen fehlgeschlagen");
            };

            fn._failedToDelete = function () {
                alert(this.options.messages.failedToDelete || "Löschen fehlgeschlagen");
            };

            fn._transition = function (state) {
                this.state = state;
                this.updateCommandStates();
            };

            fn._forEachCommand = function (callback) {
                var self = this;
                this.commands.forEach(function (name) {
                    callback(this[this._getCmdName(name)]);
                });
            };

            fn._bindCmd = function (name) {
                var cmdClass = name + "-command";
                var cmd = this.$commands.find("." + cmdClass);
                if (!cmd.length) {
                    alert("Command not found: " + cmdClass);
                    return;
                }

                this[this._getCmdName(name)] = cmd;
                cmd.prop("disabled", true);
                cmd.on("click", $.proxy(this["_" + name], this));
            };

            fn._setCmd = function (name, enabled) {
                this[this._getCmdName(name)].prop("disabled", !enabled);
            };

            fn._getCmdName = function (name) {
                return "$" + name + "Command";
            };

            return SimpleJQueryEditorViewModelBase;

        })();
        ui.SimpleJQueryEditorViewModelBase = SimpleJQueryEditorViewModelBase;

        var SimpleTreeBasedJQueryEditorViewModel = (function (_super) {
            casimodo.__extends(SimpleTreeBasedJQueryEditorViewModel, _super);

            function SimpleTreeBasedJQueryEditorViewModel($area, tree) {

                _super.call(this, $area);

                this.tree = tree;

                this.options = {
                    model: {
                        id: "id",
                        text: "text",
                        fields: {}
                    },
                    url: "/odata/TextItems",
                    messages: {
                        failedToUpdate: "Das Element konnte nicht geändert werden.",
                        failedToAdd: "Das Element konnte nicht hinzugefügt werden.",
                        failedToDelete: "Das Element konnte nicht gelöscht werden.",
                        confirmationOnDeleting: "Wollen Sie dieses Element wirklich unwiderruflich löschen?"
                    }
                };
            }

            var fn = SimpleTreeBasedJQueryEditorViewModel.prototype;

            fn.canSelect = function (item) {
                return !!item;
            };

            fn.onItemSelected = function (node) {

                var item = this.tree.dataItem(node);

                this.currentItem = item;

                if (!this.canSelect(item)) {

                    this.$editor.val(null);
                    this._transition("");

                    return;
                }

                this.$editor.val(item[this.options.model.text]);
                this._transition("selected");
            };

            fn._getCurrentInfo = function () {
                var cur = this.currentItem;

                if (!cur)
                    return null;

                var item = {
                    IsSection: cur.IsSection,
                    ContainerId: null
                };

                if (cur.IsSection) {
                    item.Id = cur.Id;
                    item.TypeId = cur.TypeId;
                    item.IsContainer = true;
                }
                else {
                    item.Id = cur.Id;
                    item.TypeId = cur.TypeId;
                    item.ContainerId = cur.ContainerId;
                    item.IsContainer = cur.IsContainer;
                }

                return item;
            };

            // Adds a child node
            fn._add = function () {

                var parent = this.currentItem;
                if (!parent)
                    return;

                var item = this.createItem();

                var parentNode = this.tree.findByUid(parent.uid);

                // Find the sibling node to insert-before the new item.
                var siblingNode = null;
                if (parent.items && parent.items.length) {
                    siblingNode = this.tree.findByUid(parent.items[0].uid);
                }

                if (siblingNode && siblingNode.length) {
                    // Sibling exists. Insert before.
                    this.tree.insertBefore(item, siblingNode);
                }
                else {
                    // No siblings exist. Add to parent as a child.
                    this.tree.append(item, parentNode);
                }

                // Get the processed item from the data-source.
                item = this.tree.dataSource.get(item[this.options.model.id]);

                // Get the added node and select it.
                var newNode = this.tree.findByUid(item.uid);
                this.tree.select(newNode);

                this.onItemSelected(newNode);

                this._transition("add");
            };

            fn.getEditorValue = function () {
                return this.$editor.val().trim();
            };

            fn.getDataForCreateOrUpdate = function () {
                var item = this.currentItem;
                var model = this.options.model;
                var result = {};
                result[model.id] = item[model.id];
                result[model.text] = this.getEditorValue();

                for (var prop in model.fields) {
                    result[prop] = item[prop];
                }

                //ParentId: item.ParentId,
                //IsThreadContainer: item.IsThreadContainer

                return result;
            };

            fn.getDataForUpdate = function () {
                return null;
            };

            fn.getDataForCreate = function () {
                return null;
            };

            fn.onSaved = function (mode) {
                this.currentItem.set(this.options.model.text, this.getEditorValue());
            };

            fn._save = function () {
                var self = this;

                if (!self.currentItem) return;

                var data;

                if (self.state === "edit") {

                    data = this.getDataForUpdate() || this.getDataForCreateOrUpdate();
                    var id = data[this.options.model.id];

                    casimodo.oDataCRUD("PUT", this.options.url, id, data)
                        .then(function () {
                            self._transition("selected");
                            self.onSaved("edit");
                        })
                        .catch(function (ex) {
                            self._failedToUpdate();
                        });
                }
                else if (self.state === "add") {

                    var cur = self.currentItem;
                    if (!cur) {
                        self._failedToAdd();
                        return;
                    }

                    data = this.getDataForCreate() || this.getDataForCreateOrUpdate();

                    casimodo.oDataCRUD("POST", this.options.url, null, data)
                        .then(function (result) {
                            self._transition("selected");
                            self.onSaved("add");
                        })
                        .catch(function (ex) {
                            self._failedToAdd();
                        });
                }
            };

            fn._cancel = function () {

                if (this.state === "add") {
                    var node = this.tree.findByUid(this.currentItem.uid);
                    this.tree.remove(node);
                    this.onItemSelected(null);
                }
                else if (this.state === "edit") {
                    this.$editor.val(this.currentItem[this.options.model.text]);
                    this._transition("selected");
                }
            };

            fn._delete = function () {
                var self = this;
                var id = this.currentItem.Id;

                // Confirm deletion and delete.
                kendomodo.ui.openDeletionConfirmationDialog(this.options.messages.confirmationOnDeleting)
                    .then(function (result) {
                        if (result !== true)
                            return;

                        casimodo.oDataCRUD("DELETE", self.options.url, id)
                            .then(function () {
                                var node = self.tree.findByUid(self.currentItem.uid);
                                self.tree.remove(node);
                                self.onItemSelected(null);

                            })
                            .catch(function (ex) {
                                self._failedToDelete();
                            });
                    });

                this._transition("");
            };

            return SimpleTreeBasedJQueryEditorViewModel;

        })(SimpleJQueryEditorViewModelBase);
        ui.SimpleTreeBasedJQueryEditorViewModel = SimpleTreeBasedJQueryEditorViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));