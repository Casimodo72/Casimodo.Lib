"use strict";
var casimodo;
(function (casimodo) {

    (function (ui) {

        ui.createComponentViewModelFactory = function () {
            return {
                createCore: function (options) {
                    throw new Error("Not implemented.");
                },
                create: function (options) {
                    var vm = this.createCore(options);
                    if (typeof vm.createComponent === "function")
                        vm.createComponent();

                    return vm;
                },
                createViewModel: function (options) {
                    return this.createCore(options);
                }
            };
        };

        var ComponentArgsContainer = (function () {
            var ComponentArgsContainer = function () {
                this.items = [];
            };

            var fn = ComponentArgsContainer.prototype;

            fn.add = function (id, paramName, value) {
                var item = this.get(id);
                if (!item) {
                    item = { id: id };
                    this.items.push(item);
                }

                item[paramName] = value;
            };

            fn.get = function (id) {
                return this.items.find(x => x.id === id);
            };

            fn.consume = function (id) {
                var item = this.get(id);
                if (!item)
                    return null;

                var idx = this.items.indexOf(item);
                if (idx !== -1)
                    this.items.splice(idx, 1);

                return item;
            };

            return ComponentArgsContainer;

        })();
        ui.componentArgs = new ComponentArgsContainer();

        var NavigationArgsContainer = (function () {
            var NavigationArgsContainer = function () {
                this.items = [];

                // Init with items of window.opener.
                var prev = casimodo.getValueAtPropPath(window, "opener.casimodo.ui.navigationArgs");
                if (prev) {
                    for (var i = 0; i < prev.items.length; i++) {
                        this.items.push(prev.items[i]);
                    }
                    prev.items = [];
                    prev = null;
                }
            };

            var fn = NavigationArgsContainer.prototype;

            fn.add = function (item) {
                this.items.push(item);
            };

            fn.get = function (id) {
                return this.items.find(x => x.id === id);
            };

            fn.consume = function (id) {
                var item = this.get(id);
                if (!item)
                    return null;

                var idx = this.items.indexOf(item);
                if (idx !== -1)
                    this.items.splice(idx, 1);

                return item;
            };

            return NavigationArgsContainer;

        })();
        ui.navigationArgs = new NavigationArgsContainer();

        var ComponentRegistry = (function () {
            var ComponentRegistry = function () {
                this.namespace = "";
                this.items = [];
            };

            var fn = ComponentRegistry.prototype;

            fn.add = function (item) {

                var reg = new ui.ComponentRegItem();
                reg.registry = this;
                reg.id = item.id;
                reg.part = item.part;
                reg.group = item.group || null;
                reg.role = item.role;
                reg.custom = !!item.custom;
                reg.isCached = !!item.isCached;
                reg.vmType = item.vmType || null;
                reg.isDialog = !!item.isDialog;
                reg.url = item.url;
                reg.maxWidth = item.maxWidth || null;
                reg.maxHeight = item.maxHeight || null;
                reg.editorId = item.editorId || null;

                this.items.push(reg);
            };

            fn._buidTypeName = function (reg) {
                return this.namespace + "." + reg.part + (reg.group ? "_" + reg.group + "_" : "") + reg.role;
            };

            fn.getById = function (id) {
                return this.items.find(x => x.id === id);
            };

            fn.getByAlias = function (alias) {
                return this.items.find(x => x.alias === alias);
            };

            fn.getAuthQueries = function (item) {
                var result = [];

                result.push({
                    Part: item.part,
                    Group: item.group,
                    VRole: item.role
                });

                if (item.editorId) {
                    var editor = this.getById(item.editorId);
                    result.push({
                        Part: editor.part,
                        Group: editor.group,
                        VRole: editor.role
                    });
                }

                return result;
            };

            fn.createViewModel = function (item, options) {
                var typeName = this._buidTypeName(item);
                return casimodo.getValueAtPropPath(window, typeName + "Factory").create(options);
            };

            fn.createViewModelOnly = function (item, options) {
                if (item.vmType) {
                    return new item.vmType({ id: item.id, isDialog: item.isDialog, isLookup: item.isLookup });
                }
                else {
                    var typeName = this._buidTypeName(item);
                    return casimodo.getValueAtPropPath(window, typeName + "Factory").createViewModel(options);
                }
            };

            return ComponentRegistry;

        })();
        ui.ComponentRegistry = ComponentRegistry;

        ui.componentRegistry = new ComponentRegistry();

        ui.ComponentRegItem = (function () {
            var ComponentRegItem = function () {
                this.registry = null;
                this.id = null;
            };

            var fn = ComponentRegItem.prototype;

            fn.getAuthQueries = function () {
                return this.registry.getAuthQueries(this);
            };

            fn.createViewModelOnly = function (options) {
                return this.registry.createViewModelOnly(this, options);
            };

            fn.vmOnly = function (options) {
                return this.registry.createViewModelOnly(this, options);
            };

            fn.vm = function (options) {
                return this.registry.createViewModel(this, options);
            };

            return ComponentRegItem;
        })();

        var ComponentViewModel = (function (_super) {
            casimodo.__extends(ComponentViewModel, _super);

            function ComponentViewModel(options) {
                _super.call(this, options);

                this._options = options || {};

                this.keyName = "Id";
                if (typeof this._options.dataKeyName !== "undefined")
                    this.keyName = this._options.dataKeyName;

                this.auth = {
                    canView: true,
                    canCreate: false,
                    canModify: false,
                    canDelete: false
                };
                this.scope = {
                    item: null
                };

                this.args = null;
                // KABU TODO: REMOVE selection
                //this.selection = {};

                this.filters = [];

                this.component = null;
            }

            var fn = ComponentViewModel.prototype;

            fn.init = function () {
                return this;
            };

            fn.refresh = function () {
                return Promise.resolve();
            };

            fn.createComponent = function () {
                // NOP
            };

            fn.setComponent = function (value) {
                this.component = value;
            };

            fn.hasChanges = function () {
                // NOP
                return false;
            };

            fn.clear = function () {
                this.args = null;
                this.filters = [];
                this.trigger("clear", { sender: this });
            };

            fn.setArgs = function (args) {
                this.args = args || null;

                if (!args)
                    return;

                this.onArgValues(args.values);

                // KABU TODO: Move filterable data component view model.
                if (args.filters && typeof this.setFilter === "function") {
                    this.setFilter(args.filters);
                }

                // Dialog result builder function.
                if (this._options.isDialog) {
                    this.args.isCancelled = true;
                    this.args.isOk = false;
                    this.args.buildResult = function () {
                        // KABU TODO: REMOVE selection
                        // this.args.value = this.selection[this.keyName];
                        this.args.value = this.scope.item ? this.scope.item[this.keyName] : null;
                        this.args.item = this.scope.item;
                    }.bind(this);
                }

                this.onArgs();
            };

            fn.onArgValues = function () {
                // NOP
            };

            fn.onArgs = function () {
                this.trigger("argsChanged", { sender: this });
            };

            fn.executeCustomCommand = function (cmd) {
                if (!this.extension)
                    return;

                if (!this.extension.actions[cmd.name])
                    return;

                this.extension.actions[cmd.name](cmd);
            };

            fn._throwAbstractFunc = function (name) {
                throw new Error("Not implemented. The function '" + name + "' is abstract.");
            };

            return ComponentViewModel;
        })(casimodo.ObservableObject);

        ui.ComponentViewModel = ComponentViewModel;

        ui.ComponentViewModelExtensionBase = (function () {
            var ComponentViewModelExtensionBase = function (options) {
                this.actions = {};
                if (options) {
                    this.vm = options.vm || null;
                }
            };

            var fn = ComponentViewModelExtensionBase.prototype;

            return ComponentViewModelExtensionBase;
        })();

    })(casimodo.ui || (casimodo.ui = {}));

})(casimodo || (casimodo = {}));
