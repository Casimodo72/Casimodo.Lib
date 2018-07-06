"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var FormEditorViewModel = (function (_super) {
            casimodo.__extends(FormEditorViewModel, _super);

            function FormEditorViewModel(options) {
                _super.call(this, options);

                this._editorWindow = null;
                this.scope.set("item", {});
                this._editors = [];
                this.$view = null;
                this.$toolbar = null;
                this.createDataSource();
            }

            var fn = FormEditorViewModel.prototype;

            fn.start = function () {
                if (!this.args)
                    throw new Error("Editor arguments were not assigned.");

                this.args.isCancelled = true;

                this._iedit.mode = this.args.mode;
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
            };

            fn._edit = function () {
                var self = this;

                if (!this._options.isLocalData) {
                    // Load data from server.
                    this.setFilter([{ field: this.keyName, operator: 'eq', value: this._iedit.itemId }]);
                    this.refresh()
                        .then(() => self._startEditing());
                }
                else {
                    // Edit local data.
                    var item = this._options.localData[0];
                    this.dataSource.insert(0, item);
                    this._startEditing();
                }
            };

            fn._add = function () {
                this.setCurrentItem(this.dataSource.insert(0, {}));
                this._startEditing();
            };

            fn._startEditing = function () {
                var item = this.getCurrentItem();

                if (!item) {
                    if (this._iedit.mode === "modify")
                        casimodo.ui.showError("Der Datensatz wurde nicht gefunden. Möglicherweise wurde er bereits gelöscht.");
                    else
                        casimodo.ui.showError("Fehler: Der Datensatz konnte nicht erstellt werden.");

                    this._cancel();

                    return;
                }

                var context = {
                    mode: this._iedit.mode,
                    item: item,
                    isNew: item.isNew(),
                    $view: this.$view
                };

                this._editors.forEach(function (ed) {

                    // Perform setValue() on code-mirror editor in order to
                    //   populate it with the current textarea's value.
                    //   This is needed because code-mirror does not do that automatically.
                    if (ed.type === "CodeMirror") {
                        ed.component.setValue(ed.$el.val());
                    }
                });

                this.onEditing(context);
            };

            fn._finish = function () {

                if (!this._iedit.isSaved)
                    return;

                var item = this.getCurrentItem();
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
                this._editorWindow.close();
            };

            fn._cancel = function () {
                if (this._isDebugLogEnabled)
                    console.debug("- EDITOR: cancelling");

                this.args.isDeleted = this._iedit.isDeleted;
                this.args.isOk = false;
                this.args.isCancelled = true;
                this._editorWindow.close();
            };

            // overwrite
            fn._applyAuth = function () {
                if (!this.auth.canView) {
                    this.$view.children().remove();
                    this.$view.prepend("<div style='color:red'>This view is not permitted.</div>");
                    // this.component.wrapper.hide();
                }
            };

            fn._initTitle = function () {
                var title = "";

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

                this._editorWindow.title(title);
            };

            fn._save = function () {
                var self = this;

                // KABU TODO: IMPORTANT: When will isSaved be true exactly? Even if the server
                //   returned an error while saving?
                if (this._iedit.isSaving || (this._iedit.isSaved && this._iedit.mode === "create"))
                    return;

                var item = this.getCurrentItem();
                // Prepare domain object for saving.
                //   E.g. this will set non-nested object references (aka navigation properties)
                //   to NULL before the object is sent to the server.
                // KABU TODO: IMPORTANT: This also means that
                //   we don't support editing of non-nested collections yet.
                casimodo.data.initDomainDataItem(item, this._options.dataTypeName, "saving");

                if (this._iedit.isSaving || (this._iedit.isSaved && this._iedit.mode === "create"))
                    return;

                this._iedit.isSaving = true;

                if (this._options.isCustomSave) {
                    this._iedit.isSaving = false;
                    this._iedit.isSaved = true;
                    this._finish();

                    return;
                }

                // KABU TODO: VERY IMPORTANT: If the saving fails then we are left
                //   with nulled navigation properties.
                this.dataSource.sync().then(function () {

                    // NOTE: This will not be executed if the server returns an error.

                    self._iedit.isSaving = false;

                    if (!self._iedit.isSaved)
                        return;

                    self._finish();
                });
            };

            fn._prepareView = function () {

                var mode = this._iedit.mode;

                if (mode === "create")
                    this.$view.find('.remove-on-Create').remove();

                if (mode === "modify")
                    this.$view.find('.remove-on-Update').remove();
            };

            fn.createComponent = function () {
                if (this.component) return;

                var self = this;

                this.$view = $("#editor-view-" + this._options.id);

                this._prepareView();

                this._initTextRenderers();

                // KABU TODO: Move to "initAsDialog" section.
                this._editorWindow = kendomodo.findKendoWindow(self.$view);
                this._editorWindow.element.css("overflow", "visible");

                // KABU TODO: ELIMINATE need for a dummy component.
                // Create dummy component.
                this.setComponent({});

                this.$toolbar = this.$view.find(".k-edit-buttons");

                this.dataSource.one("change", function (e) {
                    self._initCommitBehavior();
                });
            };

            fn._getErrorTemplate = function () {
                return `<a href="\\#" class="km-form-field-error-tooltip" data-tooltip="#:message#"><span class='icon-prop-validation-error'></a>`;
            };

            fn._initCommitBehavior = function () {
                var self = this;

                if (!self.dataSource.data().length) return;

                var model = self.dataSource.data()[0];
                var editable = self.$view.kendoEditable({
                    model: model,
                    clearContainer: false,
                    errorTemplate: this._getErrorTemplate()
                }).data("kendoEditable");

                var saveAttempted = false;

                var $saveCmd = self._$saveCmd = self.$toolbar.find(".k-update").on("click", function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    if (self._iedit.isSaving)
                        return false;

                    saveAttempted = true;

                    self._editors.forEach(function (ed) {

                        // Perform save() on code-mirror editor in order to
                        //   populate the textarea with the current value.
                        //   This is needed because code-mirror does not update
                        //   the underlying textarea automatically.
                        if (ed.type === "CodeMirror") {
                            ed.component.save();
                            ed.$el.change();
                        }
                    });

                    // Save only if modified and valid.
                    if (model.dirty && editable.validatable.validate()) {
                        self._save();
                    }
                    return false;
                });

                var $cancelCmd = self.$toolbar.find(".k-cancel").on("click", function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    self._cancel();
                    return false;
                });

                editable.bind("change", function (e) {

                    if (saveAttempted) {
                        // Validate all if a save was already attempted.
                        editable.validatable.validate();
                    }
                    else {
                        
                        var field = Object.keys(e.values)[0];
                        var value = e.values[field];
                        var input = self._findInputElement(field, value);

                        // NOTE: if we don't find the specific input element
                        //   then this might be a hidden field or a complex type field.
                        //   We will ignore such fields if a save was not attempted yet.

                        if (input && input.length === 1) {
                            var valid = editable.validatable.validateInput(input);

                            // If a save was not attempted yet then enable the
                            //   save button when there are changes.
                            self._updateSaveable(model.dirty && valid);
                        }
                    }
                });

                editable.validatable.bind("validate", function (e) {

                    self._updateSaveable(e.valid && model.dirty);

                    // Just for debug purposes.
                    //var errors = editable.validatable.errors();
                    //if (errors.length) {
                    //    alert("Got errors");
                    //}
                });
            };

            fn._updateSaveable = function (saveable) {
                if (saveable)
                    this._$saveCmd.removeClass("k-state-disabled");
                else
                    this._$saveCmd.addClass("k-state-disabled");
            };

            fn._findInputElement = function (fieldName, fieldValue) {
                // KABU TODO: Unfortunately Kendo's editable change event does
                //   not give us the input element. Thus we need find it
                //   in the same way Kendo does.

                var self = this;
                var bindAttribute = kendo.attr('bind');
                var bindingRegex = new RegExp('(value|checked)\\s*:\\s*' + fieldName + '\\s*(,|$)');

                var input = $(':input[' + bindAttribute + '*="' + fieldName + '"]', this.$view)
                    .filter('[' + kendo.attr('validate') + '!=\'false\']').filter(function () {
                        return bindingRegex.test($(this).attr(bindAttribute));
                    });


                return input;
            };

            fn._initTextRenderers = function () {
                var self = this;

                this.$view.find("textarea[data-use-renderer]").each(function () {
                    var el = this;
                    var $el = $(el);
                    var type = $el.data("use-renderer");

                    if (type === "scss" || type === "html") {

                        // https://codemirror.net/doc/manual.html
                        var editor = CodeMirror.fromTextArea(el, {
                            mode: self._getCodeMirrorMode(type),
                            lineNumbers: true,
                            indentUnit: 4,
                            indentWithTabs: true,
                            onChange: function (cm) {

                            }
                        });

                        // Register editor.
                        self._editors.push({ type: "CodeMirror", component: editor, $el: $el });
                    }
                });
            };

            fn._getCodeMirrorMode = function (type) {
                if (type === "scss")
                    return "text/x-scss";
                if (type === "html")
                    return "htmlmixed";

                throw new Error("Unexpected text content type '" + type + "'.");
            };

            return FormEditorViewModel;

        })(kendomodo.ui.EditableViewModel);
        ui.FormEditorViewModel = FormEditorViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));