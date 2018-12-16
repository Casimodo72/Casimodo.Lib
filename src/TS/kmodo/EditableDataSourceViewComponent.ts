/// <reference path="Dialog.ts" />
/// <reference path="DataSourceViewComponent.ts" />
/// <reference path="Data.ts" />

namespace kmodo {

    interface EditStates {
        mode: string;
        item: any;
        itemId: string;
        isCancelled: boolean;
        isPersisted: boolean;
        canDelete: boolean;
        isDeleted: boolean;
        isError: boolean;
        isSaving: boolean;
        isSaved: boolean;
        errorMessage: string;
    }

    interface PropAssignment {
        s: string;
        t: string;
    }

    export interface EditableViewEvent {
        $view: JQuery;
        sender: EditorForm;
    }

    export interface EditableViewOnEditingEvent extends EditableViewEvent {
        mode: string;
        item: any;
        isNew: boolean;
    }

    export interface EditableDataSourceViewOptions extends DataSourceViewOptions {
        dataTemplate?: string;        
        editing?: (e: EditableViewOnEditingEvent) => void;
    }

    export abstract class EditableDataSourceViewComponent extends DataSourceViewComponent {
        protected _options: EditableDataSourceViewOptions;
        protected _iedit: EditStates;
        protected dataModel: any;
        protected readQuery: string;

        constructor(options) {
            super(options);

            // TODO: REMOVE: this._options = super._options as EditableDataSourceViewOptions;

            var self = this;

            this._iedit = {
                mode: null,
                item: null,
                itemId: null,
                isCancelled: false,
                isPersisted: false,
                canDelete: false,
                isDeleted: false,
                isError: false,
                isSaving: false,
                isSaved: false,
                errorMessage: null
            };

            this.on("argsChanged", function (e) {
                self._iedit.mode = self.args.mode;

                if (self._iedit.mode === "modify")
                    self._iedit.canDelete = true;

                // KABU TODO: Eval if "canDelete" is ever specified on args.
                if (self._iedit.mode === "modify" && typeof self.args.canDelete !== "undefined")
                    self._iedit.canDelete = !!self.args.canDelete;
            });
        }

        // override
        protected createDataSourceOptions(): kendo.data.DataSourceOptions {
            var dataSourceOptions: kendo.data.DataSourceOptions = {
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

            return dataSourceOptions;
        }

        // override
        protected extendDataSourceOptions(options: kendo.data.DataSourceOptions) {
            // Attach event handlers.
            options.change = this._eve(this.onDataSourceChanged);
            options.sync = this._eve(this.onDataSourceSync);
            options.requestStart = this._eve(this.onDataSourceRequestStart);
            options.requestEnd = this._eve(this.onDataSourceRequestEnd);
            options.error = this._eve(this.onDataSourceError);
        }

        protected _delete() {
            var self = this;

            if (this._iedit.mode !== "modify" || !this.auth.canDelete || !this._iedit.canDelete)
                return;

            var item = this.getCurrentItem();
            if (!item)
                return;

            kmodo.openDeletionConfirmationDialog(
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

                            kmodo.openInfoDialog("Der Eintrag wurde gelöscht.", {
                                kind: "info"
                            })
                                .then(function () {
                                    // KABU TODO: Should we do something else other than cancel?
                                    self._cancel();
                                });
                        });
                });
        }

        protected _cancel() {
            if (this._isDebugLogEnabled)
                console.debug("- EDITOR: cancelling");

            this.args.isDeleted = this._iedit.isDeleted;
            this.args.isOk = false;
            this.args.isCancelled = true;
        }

        // Can be overriden by derived class.
        protected onEditingGenerated(context) {
            // NOP           
        }

        private getDataSourceModelFiels(): any {
            var ds: any = this.dataSource;
            return ds.reader.model.fields;
        }

        protected onEditing(e: EditableViewOnEditingEvent): void {
            var self = this;

            if (this._isDebugLogEnabled)
                console.debug("- EDITOR: '%s'", e.isNew ? "create" : "modify");

            // Call generated code.
            if (this._options.editing) {
                this._options.editing(e);
            }

            //if (typeof this.onEditingGenerated === "function")
            //    this.onEditingGenerated(context);

            if (e.mode === "create") {
                kmodo.initDataItemOnCreating(e.item, this.getDataSourceModelFiels());

                // Prepare domain object for editing.
                // E.g. this will create nested complex properties if missing.
                kmodo.initDomainDataItem(e.item, this._options.dataTypeName, "create");
            }

            // If authorized and modifying: add delete button.
            if (e.mode === "modify" &&
                this.auth.canDelete &&
                this._iedit.canDelete &&
                e.item.IsReadOnly === false &&
                e.item.IsDeletable === true) {

                // Add delete button at bottom-left position.
                $('<a class="k-button k-button-icontext" style="float:left" href="#"><span class="k-icon k-delete"></span>Löschen</a>')
                    .appendTo(this.$view.find(".k-edit-buttons"))
                    .on("click", (e) => {
                        self._delete();
                    });
            }

            // KABU TODO: IMPORTANT: Eval is this used.
            this.trigger("editing", { sender: this, model: e.item, item: e.item, isNew: e.isNew });
        }

        // kendo.data.DataSource events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected onDataSourceChanged(e) {
            this.setCurrentItem(this.dataSource.data()[0] || null);
        }

        private onDataSourceSync(e) {
            if (this._isDebugLogEnabled)
                console.debug("- EDITOR: DS.sync");
        }

        protected onDataSourceRequestStart(e) {
            if (this._isDebugLogEnabled)
                console.debug("- EDITOR: DS.requestStart: type: '%s'", e.type);
        }

        /**
            Called by the kendo.DataSource on event "requestEnd".
        */
        // override
        protected onDataSourceRequestEnd(e) {

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
        }

        private _setNewDataItemId(id: string) {
            this._iedit.itemId = id;
            this.args.itemId = id;
        }

        protected onDataSourceError(e): string {
            this._iedit.isError = true;
            var message = super.onDataSourceError(e);
            this._iedit.errorMessage = message;

            return message;
        }

        _setNestedItem(prop: string, foreignKey: string, referencedKey: string, odataQuery: string, assignments?: PropAssignment[]) {

            var self = this;

            var item: kendo.data.Model = this.getCurrentItem();
            if (!item)
                return;

            // Clear previously assigned referenced object.
            item.set(prop, null);
            var foreignKeyValue = item.get(foreignKey);
            if (!foreignKeyValue)
                return;

            // KABU TODO: MAGIC: assumes there's an "Id" property on the referenced object.
            kmodo.odataQueryFirstOrDefault(odataQuery + "&$filter=" + referencedKey + " eq " + foreignKeyValue, null)
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
                        self._setNestedItemProps(result, item, assignments);
                    }
                });
        }

        /**
            Either sets all @target properties to values of properties of source
            or, if @source is null, sets all target properties to null.
        */
        private _setNestedItemProps(
            source: kendo.data.ObservableObject,
            target: kendo.data.ObservableObject,
            assignments?: PropAssignment[]) {

            if (!assignments || !assignments.length)
                return;

            var dataTemplate = this._options.dataTemplate;

            var item: PropAssignment;
            for (let i = 0; i < assignments.length; i++) {
                item = assignments[i];

                // If a data template specifies a value for a direct
                // property then use that value instead.
                if (dataTemplate &&
                    item.t.indexOf(".") === -1 &&
                    typeof dataTemplate[item.t] !== "undefined") {

                    target.set(item.t, dataTemplate[item.t]);
                }
                else {
                    target.set(item.t, source ? cmodo.getValueAtPropPath(source, item.s) : null);
                }
            }
        }
    }
}   