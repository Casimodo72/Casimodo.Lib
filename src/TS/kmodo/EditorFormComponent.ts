namespace kmodo {

    // KABU TODO: kendo.ui.Editable is missing.
    interface KendoUIEditableOptions {
        model: any;
        clearContainer: boolean;
        errorTemplate?: string;
    }

    interface KendoUIEditable extends kendo.ui.Widget {
        validatable: kendo.ui.Validator;
    }

    export interface EditorFormOptions extends EditableDataSourceViewOptions {
        title?: string;
    }

    export class EditorForm extends EditableDataSourceViewComponent {
        protected _options: EditorFormOptions;
        private _editorWindow: kendo.ui.Window = null;
        private _editors: CustomPropViewComponentInfo[] = [];
        private kendoEditable: KendoUIEditable = null;
        private _$dialogEditCommands: JQuery = null;
        private _$dialogSaveCmd: JQuery;

        constructor(options: EditorFormOptions) {
            super(options);

            // TODO: REMOVE: this._options = super._options as EditorFormOptions;

            this.getModel().set("item", {});
            this.createDataSource();
        }

        start() {
            if (!this.args)
                throw new Error("Editor arguments were not assigned.");

            this.args.isCancelled = true;

            this._iedit.mode = this.args.mode as kmodo.EditModeKeys;
            this._iedit.itemId = this.args.itemId;

            if (!this._iedit.mode)
                throw new Error("Edit mode not assigned.");

            if (this._iedit.mode === "modify" && (!this._iedit.itemId && !this._options.isLocalData))
                throw new Error("No item ID assigned in edit mode.");

            this._initTitle();

            if (this._iedit.mode === "create")
                this._add();
            else if (this._iedit.mode === "modify")
                this._edit();
            else
                throw new Error("Invalid edit mode '" + this._iedit.mode + "'.");
        }

        private _edit() {
            if (!this._options.isLocalData) {
                // Load data from server.
                this.filter.setCoreNode(kmodo.KEY_FILTER_ID, { field: this.keyName, operator: 'eq', value: this._iedit.itemId, _persistent: true });
                this.refresh()
                    .then(() => this._startEditing());
            }
            else {
                // Edit local data.
                const item = this._options.localData[0];
                this.dataSource.insert(0, item);
                this._startEditing();
            }
        }

        private _add() {
            this.setCurrent(this.dataSource.insert(0, {}));
            this._startEditing();
        }

        private _startEditing() {
            const item = this.getCurrent();

            if (!item) {
                if (this._iedit.mode === "modify")
                    cmodo.showError("Der Datensatz wurde nicht gefunden. Möglicherweise wurde er bereits gelöscht.");
                else
                    cmodo.showError("Fehler: Der Datensatz konnte nicht erstellt werden.");

                this._cancel();

                return;
            }

            // Set data item on HTML component-root.
            this.$view.find(".component-root").first().data("item", item);

            for (const ed of this._editors) {

                // Perform setValue() on code-mirror editor in order to
                //   populate it with the current textarea's value.
                //   This is needed because code-mirror does not do that automatically.
                if (ed.type === "CodeMirror") {
                    ed.component.setValue(ed.$el.val());
                }
            }

            this.onEditing({
                sender: this,
                $view: this.$view,
                mode: this._iedit.mode,
                item: item,
                isNew: item.isNew()
            });

            // Apply data template.
            if (this._options.dataTemplate) {
                const template = this._options.dataTemplate;

                for (const prop of Object.keys(template)) {
                    if (typeof item[prop] !== "undefined") {
                        item[prop] = template[prop];
                        item.trigger("change", { field: prop });
                    }
                }
            }
        }

        private _finish() {

            if (!this._iedit.isSaved)
                return;

            const item = this.getCurrent();
            if (!item)
                return;

            // Set the new ID sent from server for the newly created item.
            if (this._iedit.mode === "create")
                item[this.keyName] = this._iedit.itemId;

            this.args.value = this._iedit.itemId;
            this.args.item = item;
            this.args.isDeleted = this._iedit.isDeleted;
            this.args.isOk = true;
            this.args.isCancelled = false;
            this._closeWindow();

            this.trigger("saved", { sender: this, result: this.args });
        }

        protected _cancel() {
            super._cancel();
            this._closeWindow();
        }

        protected _setWindowTitle(title: string): void {
            if (this._editorWindow)
                this._editorWindow.title(title);
        }

        protected _closeWindow(): void {
            if (this._editorWindow)
                this._editorWindow.close();
        }

        // overwrite
        protected _applyAuth() {
            if (!this.auth.canView) {
                this.$view.children().remove();
                this.$view.prepend("<div style='color:red'>This view is not permitted.</div>");
            }
        }

        private _initTitle() {
            let title = "";

            if (this.args.title) {
                title = this.args.title;
            }
            else {
                title = this._options.title || "";

                if (this._iedit.mode === "create")
                    title += " hinzufügen";
                else if (this._iedit.mode === "modify")
                    title += " bearbeiten";
            }

            this._setWindowTitle(title);
        }

        save() {
            this._save();
        }

        currentAsJSON() {
            return this.getCurrent().toJSON();
        }

        // TODO: REMOVE?
        /*
        createSaveableDataItem() {
            // Transform data item from Kendo ObservableObject to simple object.
            const item = this.getCurrentItem().toJSON();

            // Prepare domain object for saving.
            //   E.g. this will set non-nested object references (aka navigation properties)
            //   to NULL before the object is sent to the server.
            // KABU TODO: IMPORTANT: This also means that
            //   we don't support editing of non-nested collections yet.
            kmodo.initDomainDataItem(item, this._options.dataTypeName, "saving");

            return item;
        }
        */

        private _save() {
            // KABU TODO: IMPORTANT: When will isSaved be true exactly? Even if the server
            //   returned an error while saving?
            if (this._iedit.isSaving || (this._iedit.isSaved && this._iedit.mode === "create"))
                return;

            // Transform data item from Kendo ObservableObject to simple object.
            const item = this.getCurrent();

            // If localdata: do not modify anything.
            if (!this._options.isLocalData) {
                // Prepare domain object for saving.
                //   E.g. this will set non-nested object references (aka navigation properties)
                //   to NULL before the object is sent to the server.
                // KABU TODO: IMPORTANT: This also means that
                //   we don't support editing of non-nested collections yet.
                kmodo.initDomainDataItem(item, this._options.dataTypeName, "saving");
            }

            if (this._iedit.isSaving || (this._iedit.isSaved && this._iedit.mode === "create"))
                return;

            this._iedit.isSaving = true;

            //if (this._options.isCustomSave) {
            //    this._iedit.isSaving = false;
            //    this._iedit.isSaved = true;
            //    this._finish();

            //    return;
            //}

            // KABU TODO: VERY IMPORTANT: If the saving fails then we are left
            //   with nulled navigation properties.
            this.dataSource.sync().then(() => {

                // NOTE: This will not be executed if the server returns an error.

                this._iedit.isSaving = false;

                if (!this._iedit.isSaved)
                    return;

                this._finish();
            });
        }

        private _initViewByEditMode() {
            const mode = this._iedit.mode;

            if (mode === "create")
                this.$view.find('.remove-on-Create').remove();

            if (mode === "modify")
                this.$view.find('.remove-on-Update').remove();
        }

        // override
        public createView() {
            if (this._isViewInitialized) return;
            this._isViewInitialized = true;

            this.$view = $("#editor-view-" + this._options.id);

            this._initViewByEditMode();

            this._initTextRenderers();

            this._$dialogEditCommands = this.$view.find(".k-edit-buttons");
            if (!this._options.isDialog) {
                this._$dialogEditCommands.remove();
            }

            this.dataSource.one("change", e => {
                this._initDataChangeBehavior();
            });

            if (this._options.isDialog)
                this._initViewAsDialog();
        }

        private _initViewAsDialog(): void {
            // KABU TODO: Move to "initAsDialog" section.
            this._editorWindow = kmodo.findKendoWindow(this.$view);
            this._editorWindow.element.css("overflow", "visible");
        }

        private _getErrorTemplate(): string {
            return `<a href="\\#" class="km-form-control-error-tooltip" data-tooltip="#:message#"><span class='icon-prop-validation-error'></a>`;
        }

        private _validateProp(field: string, input: JQuery): EditorValidationResult {
            const result: EditorValidationResult = {
                valid: true,
                errors: []
            };

            if (input && input.length === 1) {
                result.valid = this.kendoEditable.validatable.validateInput(input);
                result.errors = this.kendoEditable.validatable.errors().map(x => ({ prop: "", message: x }));
            }

            this.trigger("validating", { sender: this, field: field, validation: result, data: this.getCurrent() });

            this._processValidationResult(result);

            return result;
        }

        validate(): EditorValidationResult {
            return this._validateCore();
        }

        private _validateCore(field: string = null): EditorValidationResult {
            const result: EditorValidationResult = {
                valid: this.kendoEditable.validatable.validate(),
                errors: this.kendoEditable.validatable.errors().map(x => ({ prop: "", message: x }))
            };

            this.trigger("validating", { sender: this, field: field, validation: result, data: this.getCurrent() });

            this._processValidationResult(result);

            return result;
        }

        private _processValidationResult(validation: EditorValidationResult): void {
            const data = this.getCurrent();

            if (this._options.isDialog) {
                this._updateSaveCommand(validation.valid && data.dirty);
            }

            this._showErrors(validation.errors);
        }

        private _createKendoEditable(dataItem: any): KendoUIEditable {

            const editableOptions: KendoUIEditableOptions = {
                model: dataItem,
                clearContainer: false,
                errorTemplate: this._getErrorTemplate()
            };

            // Type definition for "kendoEditable" is missing, thus the cast.
            const editable = (this.$view as any).kendoEditable(editableOptions)
                .data("kendoEditable") as KendoUIEditable;

            return editable;
        }

        private _initDataChangeBehavior() {
            if (!this.dataSource.data().length)
                return;

            const dataItem = this.getCurrent();

            this.kendoEditable = this._createKendoEditable(dataItem);

            let saveAttempted = false;

            if (this._options.isDialog) {
                this._$dialogSaveCmd = this._$dialogEditCommands.find(".k-button.k-update").on("click", e => {
                    if (this._iedit.isSaving)
                        return false;

                    saveAttempted = true;

                    for (const ed of this._editors) {

                        // Perform save() on code-mirror editor in order to
                        //   populate the textarea with the current value.
                        //   This is needed because code-mirror does not update
                        //   the underlying textarea automatically.
                        if (ed.type === "CodeMirror") {
                            ed.component.save();
                            ed.$el.change();
                        }
                    }

                    // Save only if modified and valid.
                    if (dataItem.dirty && this.validate().valid) {
                        this._save();
                    }

                    return false;
                });

                this._$dialogEditCommands.find(".k-button.k-cancel").on("click", () => {
                    this._cancel();
                    return false;
                });
            }

            dataItem.bind("set", e => {
                const eve = {
                    sender: this,
                    field: e.field,
                    data: dataItem,
                    currentValue: e.currentValue,
                    value: e.value,
                    prevent: false
                };
                this.trigger("dataChanging", eve);

                if (eve.prevent)
                    e.preventDefault();
            });

            dataItem.bind("change", e => {
                const field = e.field;

                if (saveAttempted) {
                    // Validate all if a save was already attempted.
                    this._validateCore(field);
                }
                else {
                    // const fieldValue = e.values[fieldName];
                    const input = this._findInputElement(field);

                    // NOTE: if we don't find the specific input element
                    //   then this might be a hidden field or a complex type field.
                    //   We will ignore such fields if a save was not attempted yet.
                    const valid = this._validateProp(field, input).valid;

                    // If a save was not attempted yet then enable the
                    //   save button when there are changes.
                    this._updateSaveCommand(dataItem.dirty && valid);
                }

                this.trigger("dataChange", {
                    sender: this, field: field
                });
            });
        }

        private _updateSaveCommand(saveable: boolean) {
            if (!this._$dialogSaveCmd || !this._$dialogSaveCmd.length)
                return;

            if (saveable)
                this._$dialogSaveCmd.removeClass("k-state-disabled");
            else
                this._$dialogSaveCmd.addClass("k-state-disabled");
        }

        setFieldReadonly(fieldName: string, value: boolean): void {
            const $input = this._findInputElement(fieldName);
            if (!$input.length) {
                cmodo.showError(`Input for field '${fieldName}' not found.`);
                return;
            }

            if ($input.length > 1) {
                cmodo.showError(`More than one input elements found for field '${fieldName}'.`);
                return;
            }
            if (value)
                $input.attr("readonly", "readonly");
            else
                $input.removeAttr("readonly");

            const widget = kendo.widgetInstance($input, kendo.ui) as any;
            if (widget && typeof widget.readonly !== "undefined") {
                (widget as any).readonly(value);
            }
        }

        private _findInputElement(fieldName: string): JQuery {
            // KABU TODO: Unfortunately Kendo's editable change event does
            //   not give us the input element. Thus we need to find it
            //   in the same way Kendo does.
            const bindAttribute = kendo.attr('bind');
            const bindingRegex = new RegExp('(value|checked)\\s*:\\s*' + fieldName + '\\s*(,|$)');

            const input = $(':input[' + bindAttribute + '*="' + fieldName + '"]', this.$view)
                .filter('[' + kendo.attr('validate') + '!=\'false\']')
                .filter((index, elem) => {
                    return bindingRegex.test($(elem).attr(bindAttribute));
                });

            return input;
        }

        private _initTextRenderers() {

            this.$view.find("textarea[data-use-renderer]").each((idx, elem) => {
                const $el = $(elem);
                const type = $el.data("use-renderer");

                if (type === "scss" || type === "html") {

                    // https://codemirror.net/doc/manual.html
                    const editor = CodeMirror.fromTextArea(elem as HTMLTextAreaElement, {
                        mode: this._getCodeMirrorMode(type),
                        lineNumbers: true,
                        indentUnit: 4,
                        indentWithTabs: true
                    });

                    // Register editor.
                    this._editors.push({ type: "CodeMirror", component: editor, $el: $el });
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