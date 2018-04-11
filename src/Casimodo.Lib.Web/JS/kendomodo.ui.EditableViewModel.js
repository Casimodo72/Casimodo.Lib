"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        // kendo.DataSource: https://docs.telerik.com/kendo-ui/api/javascript/data/datasource

        var EditableViewModel = (function (_super) {
            casimodo.__extends(EditableViewModel, _super);

            function EditableViewModel(options) {
                _super.call(this, options);

                var self = this;

                this._iedit = {
                    mode: null,
                    item: null,
                    itemId: null,
                    isCancelled: false,
                    isPersisted: false,
                    canDelete: false,
                    isDeleted: false,
                    isError: false
                };

                this.on("argsChanged", function (e) {
                    self._iedit.mode = self.args.mode;

                    if (self._iedit.mode === "modify")
                        self._iedit.canDelete = true;

                    if (self._iedit.mode === "modify" && typeof self.args.canDelete !== "undefined")
                        self._iedit.canDelete = !!self.args.canDelete;
                });
            };

            var fn = EditableViewModel.prototype;

            fn.createReadQuery = function () {
                this._throwAbstractFunc("createReadQuery");
            };

            fn.createDataModel = function () {
                this._throwAbstractFunc("createDataModel");
            };

            fn.createDataSourceOptions = function () {
                return this.dataSourceOptions || (this.dataSourceOptions = {
                    type: 'odata-v4',
                    schema: {
                        model: this.createDataModel()
                    },
                    transport: this.createDataSourceTransportOptions(),
                    pageSize: 1,
                    serverPaging: true,
                    serverSorting: true,
                    serverFiltering: true,
                });
            };

            fn.extendDataSourceOptions = function (options) {
                // Attach event handlers.
                options.change = this._eve(this.onDataSourceChanged);
                options.sync = this._eve(this.onDataSourceSync);
                options.requestStart = this._eve(this.onDataSourceRequestStart);
                options.requestEnd = this._eve(this.onDataSourceRequestEnd);
                options.error = this._eve(this.onDataSourceError);
            };

            fn._delete = function () {
                var self = this;

                if (this._iedit.mode !== "modify" || !this.auth.canDelete || !this._iedit.canDelete)
                    return;

                var item = this.getCurrentItem();
                if (!item)
                    return;

                kendomodo.ui.openDeletionConfirmationDialog(
                    "Wollen Sie diesen Eintrag wirklich endgültig löschen?")
                    .then(function (result) {

                        if (result !== true)
                            return;

                        self.dataSource.remove(item);
                        self.dataSource.sync()
                            .then(function () {
                                if (self._iedit.isError)
                                    return;

                                self._iedit.isDeleted = true;

                                kendomodo.ui.openInfoDialog("Der Eintrag wurde gelöscht.", {
                                    kind: "info"
                                })
                                .then(function () {
                                    // KABU TODO: Should we do something else other than cancel?
                                    self._cancel();
                                });
                            });
                    });
            };

            fn.onEditing = function (context) {
                var self = this;

                if (this._isDebugLogEnabled)
                    console.debug("- EDITOR: '%s'", context.isNew ? "create" : "modify");

                // Call generated code.
                if (typeof this.onEditingGenerated === "function")
                    this.onEditingGenerated(context);

                if (context.mode === "create") {
                    kendomodo.initDataItemOnCreating(context.item, this.dataSource.reader.model.fields);

                    // Prepare domain object for editing.
                    // E.g. this will create nested complex properties if missing.
                    casimodo.data.initDomainDataItem(context.item, this._options.dataTypeName, "create");
                }

                // If authorized and modifying: add delete button.
                if (context.mode === "modify" &&
                    this.auth.canDelete &&
                    this._iedit.canDelete &&
                    context.item.IsReadOnly === false &&
                    context.item.IsDeletable === true) {

                    // Add delete button at bottom-left position.
                    $('<a class="k-button k-button-icontext" style="float:left" href="#"><span class="k-icon k-delete"></span>Löschen</a>')
                        .appendTo(this.$view.find(".k-edit-buttons"))
                        .on("click", function (e) {
                            self._delete();
                        });
                }

                this.trigger("editing", { sender: this, model: context.item, item: context.item, isNew: context.isNew });
            };

            // kendo.data.DataSource events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            fn.onDataSourceChanged = function (e) {
                this.setCurrentItem(this.dataSource.data()[0] || null);
            };

            fn.onDataSourceSync = function (e) {
                if (this._isDebugLogEnabled)
                    console.debug("- EDITOR: DS.sync");
            };

            fn.onDataSourceRequestStart = function (e) {
                if (this._isDebugLogEnabled)
                    console.debug("- EDITOR: DS.requestStart: type: '%s'", e.type);
            };

            /**
                Called by the kendo.DataSource on event "requestEnd".
            */
            fn.onDataSourceRequestEnd = function (e) {

                if (this._isDebugLogEnabled)
                    console.debug("- EDITOR: DS.requestEnd: type: '%s'", e.type);

                // NOTE: On errors the data source will trigger "requestEnd" first and then "error".
                //   e.type will be undified in this case.

                if (typeof e.type === "undefined") {
                    // This happens when there was a request error.
                    this._iedit.isError = true;
                    this._iedit.isSaving = false;
                }
                else if (e.type === "read") {
                    // KABU TODO: IMPORTANT: Will this be hit when the item
                    // to be edited is not found on the server?                    
                }
                else if (e.type === "create") {

                    // Set the created item's ID returned from the server.
                    if (e.response)
                        this._setNewDataItemId(e.response[this.keyName]);

                    this._iedit.isSaved = true;
                }
                else if (e.type === "update") {
                    this._iedit.isSaved = true;
                }
            };

            fn._setNewDataItemId = function (id) {
                this._iedit.itemId = id;
                this.args.itemId = id;
            };

            var super_onDataSourceError = fn.onDataSourceError;

            fn.onDataSourceError = function (e) {
                this._iedit.isError = true;
                this._iedit.errorMessage = super_onDataSourceError.call(this, e);
            };

            /**
                @param {Object} e - Kendo observable "change" event of the edited item.
            */
            fn._setNestedItem = function (prop, foreignKey, referencedKey, odataQuery, assignments) {
                var item = this.getCurrentItem();
                if (!item)
                    return;

                // Clear previously assigned referenced object.
                item.set(prop, null);
                var foreignKeyValue = item.get(foreignKey);
                if (!foreignKeyValue)
                    return;

                // KABU TODO: MAGIC: assumes there's an "Id" property on the referenced object.
                kendomodo.odataQueryFirstOrDefault(odataQuery + "&$filter=" + referencedKey + " eq " + foreignKeyValue, null)
                    .then(function (result) {
                        if (!result) {
                            // Clear the foreign key property because no result was returned.
                            item.set(foreignKey, null);
                        }
                        else {
                            // Set the referenced object.
                            item.set(prop, result);
                        }

                        if (!result || item.isNew()) {
                            // Assign properties from the referenced object
                            // or set them all to null if the referenced object is null.
                            _setNestedItemProps(result, item, assignments);
                        }
                    });
            };

            /**
                Either sets all @target properties to values of properties of source
                or, if @source is null, sets all target properties to null.
            */
            function _setNestedItemProps(source, target, assignments) {
                if (!assignments || !assignments.length)
                    return;

                var item;
                for (var i = 0; i < assignments.length; i++) {
                    item = assignments[i];
                    target.set(item.t, source ? casimodo.getValueAtPropPath(source, item.s) : null);
                }
            }

            return EditableViewModel;

        })(kendomodo.ui.DataComponentViewModel);
        ui.EditableViewModel = EditableViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));
