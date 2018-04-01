"use strict";

var kendomodo;
(function (kendomodo) {
    (function (ui) {

        var FormReadOnlyViewModel = (function (_super) {
            casimodo.__extends(FormReadOnlyViewModel, _super);

            function FormReadOnlyViewModel(options) {
                _super.call(this, options);

                this.$view = null;
                this.$toolbar = null;
                this.createDataSource();
            }

            var fn = FormReadOnlyViewModel.prototype;

            fn.setTitle = function (title) {
                this.$toolbar.find(".details-view-title").text(title || null);
            };

            fn.createDataSourceOptions = function () {
                if (this.dataSourceOptions) return this.dataSourceOptions;
                this.dataSourceOptions = {
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

                return this.dataSourceOptions;
            };

            fn.extendDataSourceOptions = function (options) {
                // Attach event handlers.
                options.change = this._eve(this.onDataSourceChanged);
                options.error = this._eve(this.onDataSourceError);
                options.requestStart = this._eve(this.onDataSourceRequestStart);
                options.requestEnd = this._eve(this.onDataSourceRequestEnd);
            };

            fn.onDataSourceChanged = function (e) {
                this.setCurrentItem(this.dataSource.data()[0] || null);
            };

            // overwrite
            fn._applyAuth = function () {
                var self = this;

                if (!this.auth.canView) {
                    this.$view.children().remove();
                    this.$view.prepend("<div style='color:red'>This view is not permitted.</div>");
                    // this.component.wrapper.hide();
                }

                // Init edit button.
                var $editBtn = this.$toolbar.find(".edit-command");
                if (this.auth.canModify && this._options.editor) {

                    $editBtn.on("click", function (e) {
                        self._edit();
                    });
                    $editBtn.show();
                }
                else {
                    // Editing not permitted. Remove edit button.
                    $editBtn.remove();
                }
            };

            fn._edit = function () {
                var self = this;

                if (!this.auth.canModify)
                    throw new Error("Modification is not permitted.");

                if (!this._options.editor || !this._options.editor.url)
                    return;

                var item = self.getCurrentItem();
                if (item === null)
                    return;

                kendomodo.ui.openById(this._options.editor.id,
                    {
                        mode: "modify",
                        itemId: item[this.keyName],
                        // editor: this._options.editor,
                        finished: function (result) {
                            self.refresh();
                        }
                    });
            };

            fn._prepareView = function () {
                this.$view.find('.remove-on-Read').remove();
            };

            fn.createComponent = function () {

                if (this.component) return;

                var self = this;

                // Create dummy component object.
                this.space.component = {};
                this.setComponent(this.space.component);

                this.$view = $("#details-view-" + this._options.id);

                this._prepareView();

                // Toolbar commands.
                this.$toolbar = this.$view.find(".details-view-toolbar");
                // Refresh command.
                this.$toolbar.find(".refresh-command").on("click", function (e) {
                    kendomodo.ui.progress(true, self.$view);
                    self.refresh()
                        .finally(function () {
                            kendomodo.ui.progress(false, self.$view);
                        });
                });

                this.setTitle(this._options.title);
            };

            return FormReadOnlyViewModel;

        })(kendomodo.ui.DataComponentViewModel);
        ui.FormReadOnlyViewModel = FormReadOnlyViewModel;

    })(kendomodo.ui || (kendomodo.ui = {}));
})(kendomodo || (kendomodo = {}));