/// <reference path="../casimodo/ViewComponent.ts" />

namespace kmodo {

    export interface EditorInfo {
        id?: string;
        url?: string
    }

    export interface ViewComponentEvent {
        sender: ViewComponent;
    }

    export interface ViewComponentOptions {
        id?: string;
        viewId?: string;
        title?: string;
        isLookup?: boolean;
        isDialog?: boolean;
        isAuthRequired?: boolean;
        part?: string;
        group?: string;
        role?: string;
        computedFields?: any[];
        isGlobalCompanyFilterEnabled?: boolean;
        isCompanyFilterEnabled?: boolean;
        extra?: any; // Extra options.
    }

    export interface CustomPropViewComponentInfo {
        type: string;
        component: any;
        $el: JQuery
    }

    export interface CustomCommand {
        name: string;
        execute: Function;
    }

    export interface CustomFilterCommand extends CustomCommand {
        field: string;
        title: string; // KABU TODO: Rename to "displayName".
        deactivatable?: boolean;
    }

    export type ViewComponentFilter = kendo.data.DataSourceFilters | kendo.data.DataSourceFilterItem;

    export interface ViewComponentArgs {
        mode?: string; // Used for edit mode.
        canDelete?: boolean; // Used by editor
        isDeleted?: boolean; // Used by editor
        isLookup?: boolean;
        value?: any;
        item?: any;
        itemId?: string;
        filters?: ViewComponentFilter[];
        filterCommands?: CustomFilterCommand[];
        isCancelled?: boolean;
        isOk?: boolean;
        buildResult?: Function;
        items?: any; // NOTE: Can be a kendo.data.ObservableArray.
        title?: string;
        message?: string;

    }

    export abstract class ViewComponent extends cmodo.ViewComponent {
        protected _options: ViewComponentOptions;
        $view: JQuery;
        scope: kendo.data.ObservableObject;
        protected filters: ExtKendoDataSourceFilterItem[];
        protected args: ViewComponentArgs;
        protected _isComponentInitialized: boolean;
        protected _isDebugLogEnabled: boolean;

        constructor(options: ViewComponentOptions) {
            super(options);

            // TODO: REMOVE: this._options = super._options as ViewComponentOptions;

            // Extend with extra options.
            if (this._options.extra) {
                for (var prop in this._options.extra)
                    this._options[prop] = this._options.extra[prop];
            }

            this.$view = null;
            this.args = null;
            this.filters = [];
            this.scope = kendo.observable({ item: null });
            this.scope.bind("change", $.proxy(this._onScopeChanged, this));

            this._isComponentInitialized = false;
            this._isDebugLogEnabled = false;
        }

        refresh(): Promise<void> {
            return super.refresh();
        }

        setFilter(filters: ExtKendoDataSourceFilterItem[]) {
            this.filters = filters;
        }

        getModel(): any {
            return this.scope;
        }

        // override
        clear(): void {
            this.filters = [];
            super.clear();
        }

        public setArgs(args) {
            this.args = args || null;

            if (!args)
                return;

            this.onArgValues(args.values);

            // KABU TODO: Move to filterable data component view model?
            if (args.filters && typeof this.setFilter === "function") {
                this.setFilter(args.filters);
            }

            // Dialog result builder function.
            if (this._options.isDialog) {
                var self = this;
                this.args.isCancelled = true;
                this.args.isOk = false;
                this.args.buildResult = function () {
                    // KABU TODO: REMOVE selection
                    // this.args.value = this.selection[this.keyName];
                    self.args.value = self.scope.get("item") ? self.scope.get("item")[self.keyName] : null;
                    self.args.item = self.scope.get("item");
                }.bind(this);
            }

            this.onArgs();
        }

        private onArgValues(values: any) {
            // NOP
        }

        private onArgs() {
            this.trigger("argsChanged", { sender: this });
        }

        protected _onScopeChanged(e) {
            this.trigger("scopeChanged", e);
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected _eve(handler: any) {
            return $.proxy(handler, this);
        }
    }
}