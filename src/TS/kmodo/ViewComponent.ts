﻿namespace kmodo {

    export type KendoTemplate = (data: any) => string;

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

    export interface ViewComponentModel extends kendo.data.ObservableObject {
        item: any
    }

    export interface IViewComponent extends cmodo.EventListenable {
        refresh(): Promise<void>;
        clear(): void;
        getModel(): any;
        setFilter(filter: kendo.data.DataSourceFilter | kendo.data.DataSourceFilter[]): void;
    }

    export abstract class ViewComponent extends cmodo.ViewComponent implements IViewComponent {
        protected _options: ViewComponentOptions;
        $view: JQuery = null;
        scope: any = null;
        protected filters: kendo.data.DataSourceFilter[] = [];
        protected args: ViewComponentArgs = null;
        protected _isComponentInitialized = false;
        protected _isDebugLogEnabled = false;

        constructor(options: ViewComponentOptions) {
            super(options);

            // Extend with extra options.
            if (this._options.extra) {
                for (const prop in this._options.extra)
                    this._options[prop] = this._options.extra[prop];
            }

            this.scope = kendo.observable({ item: null });
            this.scope.bind("change", (e) => this._onScopeChanged(e));
        }

        refresh(): Promise<void> {
            return super.refresh();
        }

        setFilter(filter: kendo.data.DataSourceFilter | kendo.data.DataSourceFilter[]): void {
            this.filters = filter
                ? (Array.isArray(filter)
                    ? filter
                    : [filter])
                : [];
        }

        getModel(): ViewComponentModel {
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
                this.args.isCancelled = true;
                this.args.isOk = false;
                this.args.buildResult = () => {
                    // KABU TODO: REMOVE selection
                    // this.args.value = this.selection[this.keyName];
                    this.args.value = this.scope.get("item") ? this.scope.get("item")[this.keyName] : null;
                    this.args.item = this.scope.get("item");
                };
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