"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var TextItemsTreeViewModel = (function (_super) {
            casimodo.__extends(TextItemsTreeViewModel, _super);

            function TextItemsTreeViewModel(options) {
                _super.call(this, options);

                this.$view = null;
                this.component = null;

                this.selectedNodeId = null;
                this.expandedNodeIds = [];

                this.createComponent();
            }

            var fn = TextItemsTreeViewModel.prototype;

            fn.refresh = function () {
                var self = this;

                var tree = this.component;

                var node = tree.select();
                if (node.length) {
                    this.selectedNodeId = tree.dataItem(node).Id;
                }

                this.saveExpandedNodes();

                tree.dataSource.read().then(function () {

                    tree.setOptions({ animation: false });

                    tree.expandPath(self.expandedNodeIds, function () {
                        tree.setOptions({ animation: kendo.ui.TreeView.fn.options.animation });
                        if (self.selectedNodeId) {
                            var item = tree.dataSource.get(self.selectedNodeId);
                            if (item)
                                tree.select(tree.findByUid(item.uid));
                        }
                    });
                });
            };

            fn.saveExpandedNodes = function () {
                var self = this;

                var tree = this.component;
                this.expandedNodeIds = [];

                tree.element.find(".k-item").each(function () {
                    var item = tree.dataItem(this);
                    if (item.expanded) {
                        self.expandedNodeIds.push(item.Id);
                    }
                });
            };

            // Create component ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            fn.createComponent = function () {
                if (this._isComponentInitialized)
                    return;
                this._isComponentInitialized = true;

                var self = this;

                this.$view = $("#view-" + this._options.id);

                this.$refreshCommand = self.$view.find("button.refresh-command");
                this.$refreshCommand.on("click", function () {
                    if (self.editor.isEditing)
                        return;

                    self.refresh();
                });

                this.component = this.$view.find(".text-items-treeview").first().kendoTreeView({
                    select: function (e) {

                        if (self.editor.isEditing) {
                            e.preventDefault();
                            return;
                        }

                        // Notify editor
                        self.editor.onItemSelected(e.node);
                    },
                    dataTextField: ["DisplayName", "Value1", "Value1"],
                    dataSource: {
                        type: "odata-v4",
                        transport: {
                            read: {
                                url: function (options) {
                                    return "/odata/TextItems/Ga.GetSections()";
                                }
                            }
                        },
                        schema: {
                            model: {
                                id: "Id",
                                hasChildren: true,
                                fields: {
                                    Id: { type: "number" },
                                    TypeId: { type: "number" }
                                },
                                children: {
                                    type: "odata-v4",
                                    transport: {
                                        read: {
                                            url: function (options) {
                                                return '/odata/TextItems/Ga.Query()?$select=Id,Value1,IsContainer,Index,TypeId&$orderby=Index&$filter=ContainerId+eq+null+and+TypeId+eq+' + options.Id;
                                            }
                                        }
                                    },
                                    schema: {
                                        model: {
                                            id: "Id",
                                            hasChildren: "IsContainer",
                                            fields: {
                                                Id: { type: "string" },
                                                Value1: { type: "string" },
                                                IsContainer: { type: 'boolean' },
                                                Index: { type: 'number' }
                                            },
                                            children: {
                                                type: "odata-v4",
                                                transport: {
                                                    read: {
                                                        url: function (options) {
                                                            return '/odata/TextItems/Ga.Query()?$select=Id,Value1,IsContainer,Index&$orderby=Index&$filter=ContainerId+eq+' + options.Id;
                                                        }
                                                    }
                                                },
                                                schema: {
                                                    model: {
                                                        id: "Id",
                                                        hasChildren: false,
                                                        fields: {
                                                            Id: { type: "string" },
                                                            Value1: { type: "string" },
                                                            IsContainer: { type: 'boolean' },
                                                            Index: { type: 'number' }
                                                        }
                                                    }

                                                },
                                                serverFiltering: true,
                                                serverPaging: true,
                                                serverSorting: true
                                            }
                                        }
                                    },
                                    serverFiltering: true,
                                    serverPaging: true,
                                    serverSorting: true
                                }
                            }
                        },
                        serverFiltering: true,
                        serverPaging: true,
                        serverSorting: true
                    }
                }).data("kendoTreeView");

                this.editor = new TextItemEditorViewModel(this.$view.find(".text-items-editor-area"), this.component);
                this.editor.init();
            };

            return TextItemsTreeViewModel;

        })(kendomodo.ui.ComponentViewModel);
        ui.TextItemsTreeViewModel = TextItemsTreeViewModel;

        // Editor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  

        var TextItemEditorViewModel = (function (_super) {
            casimodo.__extends(TextItemEditorViewModel, _super);

            function TextItemEditorViewModel($area, tree) {

                _super.call(this, $area, tree);

                this.options.model = {
                    id: "Id",
                    text: "Value1",
                    fields: {
                        TypeId: {},
                        ContainerId: {},
                        IsContainer: {}
                    }
                };
                this.options.url = "/odata/TextItems";
                this.options.messages = {
                    failedToUpdate: "Der Schnipsel konnte nicht geändert werden.",
                    failedToAdd: "Der Schnipsel konnte nicht hinzugefügt werden.",
                    failedToDelete: "Der Schnipsel konnte nicht gelöscht werden.",
                    confirmationOnDeleting: "Wollen Sie diesen Text-Schnipsel wirklich un­wi­der­ruf­lich löschen?"
                };
            }

            var fn = TextItemEditorViewModel.prototype;

            fn.canAdd = function (contextItem) {
                return contextItem.IsContainer;
            };

            fn.canSelect = function (item) {
                return !!item && !item.IsSection;
            };

            fn.createItem = function () {
                var parentInfo = this._getCurrentInfo();

                var item = {
                    Id: kendomodo.guid(),
                    TypeId: parentInfo.TypeId,
                    Value1: "Neu",
                    IsContainer: parentInfo.IsSection,
                    ContainerId: parentInfo.IsSection ? null : parentInfo.Id
                };

                return item;
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

            return TextItemEditorViewModel;

        })(kendomodo.ui.SimpleTreeBasedJQueryEditorViewModel);

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));