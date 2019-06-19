namespace kmodo {

    export type KendoTemplate = (data: any) => string;

    export interface EditorInfo {
        id?: string;
        url?: string
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

    export interface ComponentEvent {
        sender: any;
    }

    export interface GenericComponentEvent<TComponent extends ViewComponent> extends ComponentEvent {
        sender: TComponent;
    }

    export interface ComponentCommand {
        name: string;
        title: string;
        group?: string;

        deactivatable?: boolean;
        hideOnDeactivated?: boolean;
        filter?: DataSourceFilterNode;

        execute: (e: any) => void;
    }

    export interface GenericComponentCommand<
        TComponent extends ViewComponent,
        TEvent extends GenericComponentEvent<TComponent>
        > extends ComponentCommand {
        execute: (e: TEvent) => void;
    }

    export interface CustomFilterCommandInfo {
        command: ComponentCommand;
        $btn: JQuery;
    }

    export interface ViewComponentArgs {
        mode?: string; // Used for edit mode.
        canDelete?: boolean; // Used by editor
        isDeleted?: boolean; // Used by editor
        isLookup?: boolean;
        params?: any;
        value?: any;
        item?: any;
        itemId?: string;
        filters?: DataSourceFilterNode[];
        filterCommands?: ComponentCommand[];
        isCancelled?: boolean;
        isOk?: boolean;
        buildResult?: Function;
        items?: any; // NOTE: Can be a kendo.data.ObservableArray.
        title?: string;
        message?: string;
    }

    export interface ComponentViewModel extends kendo.data.ObservableObject {
        item: any
    }

    export interface IViewComponent extends cmodo.EventListenable {
        refresh(): Promise<void>;
        clear(): void;
        getModel(): any;
    }

    export abstract class ViewComponent extends cmodo.ViewComponent
        implements IViewComponent {

        protected _options: ViewComponentOptions;
        $view: JQuery = null;
        scope: any = null;

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

            this.scope = kendo.observable({ item: null });
            this.scope.bind("change", e => this._onScopeChanged(e));
        }

        refresh(): Promise<void> {
            this._ensureViewInitialized();
            return super.refresh();
        }

        getModel(): ComponentViewModel {
            return this.scope;
        }

        setArgs(args: ViewComponentArgs): void {
            this.args = args || null;

            if (!args)
                return;

            // Dialog result build function.
            if (this._options.isDialog) {
                this.args.isCancelled = true;
                this.args.isOk = false;
                this.args.buildResult = () => {
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
    }

    export interface IFilterableViewComponent extends IViewComponent {
        setFilter(filter: DataSourceFilterOneOrMany): void;
    }

    export abstract class FilterableViewComponent extends ViewComponent
        implements IFilterableViewComponent {

        protected readonly filter: ComponentFilterManager = new ComponentFilterManager();

        constructor(options: ViewComponentOptions) {
            super(options);

            // Set base filters.
            if (this._options.coreFilters) {
                this.filter.setBase(this._options.coreFilters);
            }
        }

        // override
        clear(): void {
            this.filter.clear();
            super.clear();
        }

        setFilter(filter: DataSourceFilterOneOrMany): void {
            this.filter.set(filter);
        }

        protected _getEffectiveFilters(): DataSourceFilterNode[] {
            const filters = this.filter.getEffective();
            if (this.args && this.args.filters) {
                filters.push(...this.args.filters);
            }

            return filters;
        }
    }

    type ComponentFilterSlot = "core" | "internal" | "default";

    export class ComponentFilterManager {
        // Base filters are provided via options.
        private readonly _baseFilters: DataSourceFilterNode[] = [];
        // Internal filters are filters handled by the component itself.
        private readonly _internalFilters: DataSourceFilterNode[] = [];
        // Filters are externally provided filters.
        private readonly _filters: DataSourceFilterNode[] = [];

        set(filter: DataSourceFilterOneOrMany): void {
            this._set(filter, "default");
        }

        setInternal(filter: DataSourceFilterOneOrMany): void {
            this._set(filter, "internal");
        }

        setBase(filter: DataSourceFilterOneOrMany): void {
            this._set(filter, "core");
        }

        private _getSlot(name: ComponentFilterSlot): DataSourceFilterNode[] {
            if (name === "core")
                return this._baseFilters;
            else if (name === "internal")
                return this._internalFilters;
            else return this._filters;
        }

        getEffective(): DataSourceFilterNode[] {
            const filters: DataSourceFilterNode[] = [];
            filters.push(...this._baseFilters);
            filters.push(...this._internalFilters);
            filters.push(...this._filters);

            return filters;
        }

        private _set(filter: DataSourceFilterOneOrMany, slot: ComponentFilterSlot): void {
            this._fixFilter(filter);

            const filters = filter
                ? (Array.isArray(filter)
                    ? filter
                    : [filter])
                : [];

            let filterSlot = this._getSlot(slot);
            cmodo.clearArray(filterSlot);
            filterSlot.push(...filters);
        }

        clear(): void {
            this._clear("default");
        }

        private _clear(slot: ComponentFilterSlot): void {
            cmodo.clearArray(this._getSlot(slot));
        }

        setCoreNode(id: string, filter: DataSourceFilterNode): void {
            filter._id = id;
            this.removeCoreNode(id);
            this._fixFilterNode(filter);
            this._baseFilters.push(filter);
        }

        removeCoreNode(id: string): void {
            const idx = this._baseFilters.findIndex(x => x._id === id);
            if (idx !== -1)
                this._baseFilters.splice(idx, 1);
        }

        hasCoreNode(id: string): boolean {
            return !!this.findCoreNode(id);
        }

        findCoreNode(id: string): DataSourceFilterNode {
            return this._baseFilters.find(x => x._id === id);
        }

        private _fixFilter(filter: DataSourceFilterOneOrMany): void {
            if (!filter)
                return;
            if (Array.isArray(filter)) {
                // TODO: traverse deep
                for (const f of filter)
                    this._fixFilterNode(f);
            } else {
                this._fixFilterNode(filter);
            }
        }

        private _fixFilterNode(filter: DataSourceFilterNode): DataSourceFilterNode {
            if (!filter)
                return null;
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