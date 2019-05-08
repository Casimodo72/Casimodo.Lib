namespace kmodo {

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
        coreFilters?: DataSourceFilterNode[];
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
        group?: string;
        execute: Function;
    }

    export interface CustomFilterCommand extends CustomCommand {
        title: string;
        deactivatable?: boolean;
        hideOnDeactivated?: boolean;
        filter: DataSourceFilterNode;
    }

    export interface CustomFilterCommandInfo {
        command: CustomFilterCommand;
        $btn: JQuery;
    }

    export interface ViewComponentArgs {
        mode?: string; // Used for edit mode.
        canDelete?: boolean; // Used by editor
        isDeleted?: boolean; // Used by editor
        isLookup?: boolean;
        value?: any;
        item?: any;
        itemId?: string;
        filters?: DataSourceFilterNode[];
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
        setFilter(filter: DataSourceFilterOneOrMany): void;
    }

    type ComponentFilterSlot = "core" | "internal" | "default";

    export abstract class ViewComponent extends cmodo.ViewComponent implements IViewComponent {
        protected _options: ViewComponentOptions;
        $view: JQuery = null;
        scope: any = null;
        // Core filters are provided via options.
        private readonly _coreFilters: DataSourceFilterNode[] = [];
        // Internal filters are filters handled by the component itself.
        protected readonly _internalFilters: DataSourceFilterNode[] = [];
        // Filters are externally provided filters.
        private readonly _filters: DataSourceFilterNode[] = [];
        protected args: ViewComponentArgs = null;
        protected _isViewInitialized = false;
        protected _isDebugLogEnabled = false;

        constructor(options: ViewComponentOptions) {
            super(options);

            // Extend with extra options.
            if (this._options.extra) {
                for (const prop in this._options.extra)
                    this._options[prop] = this._options.extra[prop];
            }

            // Set core filters.
            if (this._options.coreFilters) {
                this._coreFilters.push(...this._options.coreFilters);
            }

            this.scope = kendo.observable({ item: null });
            this.scope.bind("change", e => this._onScopeChanged(e));
        }

        refresh(): Promise<void> {
            this._ensureViewInitialized();
            return super.refresh();
        }

        getModel(): ViewComponentModel {
            return this.scope;
        }

        // override
        clear(): void {
            cmodo.clearArray(this._filters);
            super.clear();
        }

        setArgs(args: ViewComponentArgs): void {
            this.args = args || null;

            if (!args)
                return;

            // TODO: REMOVE:
            //if (args.filters) {
            //    this.setFilter(args.filters);
            //}

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

        protected _ensureViewInitialized(): void {
            if (this._isViewInitialized)
                return;

            this.createView();
            // Actually ensureInitialized() shouldn't be needed to be called.
            //   If the view was not initialized then something went wrong.
            if (this._isViewInitialized)
                alert("Accessing component before its view was initialized.");
        }

        private onArgs() {
            this.trigger("argsChanged", { sender: this });
        }

        protected _onScopeChanged(e) {
            this.trigger("scopeChanged", e);
        }

        // Filters ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        setFilter(filter: DataSourceFilterOneOrMany): void {
            this._setFilterCore(filter, "default");
        }

        protected _getFilterSlot(name: ComponentFilterSlot): DataSourceFilterNode[] {
            if (name === "core")
                return this._coreFilters;
            else if (name === "internal")
                return this._internalFilters;
            else return this._filters;
        }

        protected _getEffectiveFilters(): DataSourceFilterNode[] {
            const filters: DataSourceFilterNode[] = [];
            filters.push(...this._coreFilters);
            filters.push(...this._internalFilters);
            filters.push(...this._filters);
            if (this.args && this.args.filters) {
                filters.push(...this.args.filters);
            }

            return filters;
        }

        protected _setFilterCore(filter: DataSourceFilterOneOrMany, slot: ComponentFilterSlot): void {
            this._fixFilter(filter);

            const filters = filter
                ? (Array.isArray(filter)
                    ? filter
                    : [filter])
                : [];

            let filterSlot = this._getFilterSlot(slot);
            cmodo.clearArray(filterSlot);
            filterSlot.push(...filters);
        }
        
        protected _clearFilterCore(slot: ComponentFilterSlot): void {
            cmodo.clearArray(this._getFilterSlot(slot));
        }

        protected _setCoreFilterNode(id: string, filter: DataSourceFilterNode): void {
            filter._id = id;
            this._removeCoreFilterNode(id);
            this._fixFilterNode(filter);
            this._coreFilters.push(filter);
        }

        protected _removeCoreFilterNode(id: string): void {
            const idx = this._coreFilters.findIndex(x => x._id === id);
            if (idx !== -1)
                this._coreFilters.splice(idx, 1);
        }

        protected _hasCoreFilterNode(id: string): boolean {
            return !!this._findCoreFilterNode(id);
        }

        protected _findCoreFilterNode(id: string): DataSourceFilterNode {
            return this._coreFilters.find(x => x._id === id);
        }

        protected _fixFilter(filter: DataSourceFilterOneOrMany): void {
            if (Array.isArray(filter)) {
                // TODO: traverse deep
                for (const f of filter)
                    this._fixFilterNode(f);
            } else {
                this._fixFilterNode(filter);
            }
        }

        protected _fixFilterNode(filter: DataSourceFilterNode): DataSourceFilterNode {
            if (filter.logic) {
                if (filter.filters) {
                    for (const f of filter.filters)
                        this._fixFilterNode(f);
                }
            } else if (filter.field) {
                // Set implicit "eq" operator.
                if (!filter.operator && !filter.customExpression)
                    filter.operator = "eq";
            }

            return filter;
        }
    }
}