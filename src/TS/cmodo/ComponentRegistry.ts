namespace cmodo {

    export class ComponentInfo {
        public registry: ComponentRegistry;
        public _options: any;
        public id: string;
        public alias: string;
        public part: string;
        public group: string;
        public role: string;
        public custom: any;
        public isCached: boolean;
        public isLookup: boolean;
        public componentType: any;
        public isDialog: boolean;
        public url: string;
        public width?: number;
        public minWidth: number;
        public maxWidth: number;
        public height?: number;
        public minHeight: number;
        public maxHeight: number;
        public maximize: boolean;
        public editorId: string;

        constructor() {
            this.registry = null;
            this.id = null;
        }

        getAuthQueries(): AuthQuery[] {
            return this.registry.getAuthQueries(this);
        }

        create(init: boolean, options?: any): any {
            return this.registry.createComponent(this, init, options);
        }
    }

    export interface ComponentInfoOptions {
        id: string;
        part: string;
        group?: string;
        role: string;
        custom?: boolean;
        isCached?: boolean;
        componentType?: any;
        isDialog?: boolean;
        url: string;
        width?: number;
        minWidth?: number;
        maxWidth?: number;
        height?: number;
        minHeight?: number;
        maxHeight?: number;
        maximize?: boolean;
        editorId?: string;
    }

    export class ComponentRegistry {
        protected ns: string;
        public items: ComponentInfo[];

        constructor() {
            this.ns = "";
            this.items = [];
        }

        add(opts: ComponentInfoOptions): void {
            const item = new ComponentInfo();
            item.registry = this;
            item.id = opts.id;
            item.part = opts.part;
            item.group = opts.group || null;
            item.role = opts.role;
            item.custom = !!opts.custom;
            item.isCached = !!opts.isCached;
            item.componentType = opts.componentType || null;
            item.isDialog = !!opts.isDialog;
            item.url = opts.url;
            item.width = opts.width || null;
            item.minWidth = opts.minWidth || null;
            item.maxWidth = opts.maxWidth || null;
            item.height = opts.height || null;
            item.minHeight = opts.minHeight || null;
            item.maxHeight = opts.maxHeight || null;
            item.maximize = !!opts.maximize;
            item.editorId = opts.editorId || null;

            this.items.push(item);
        }

        _buidTypeName(info: ComponentInfo): string {
            return this.ns + "." + info.part + (info.group ? "_" + info.group + "_" : "") + info.role;
        }

        get(id: string, options?: any): ComponentInfo {
            let info = this.items.find(x => x.id === id);
            if (info && options) {
                // TODO: IMPORTANT: Will this work with ES6 class instances?
                info = Object.assign(Object.create(Object.getPrototypeOf(info)), info);
                info._options = options;
            }

            return info;
        }

        getByAlias(alias: string): ComponentInfo {
            return this.items.find(x => x.alias === alias);
        }

        getAuthQueries(info: ComponentInfo): AuthQuery[] {
            const result: AuthQuery[] = [];

            result.push({
                Part: info.part,
                Group: info.group,
                VRole: info.role
            });

            if (info.editorId) {
                const editor = this.get(info.editorId);
                result.push({
                    Part: editor.part,
                    Group: editor.group,
                    VRole: editor.role
                });
            }

            return result;
        }

        createComponent(info: ComponentInfo, init: boolean, options?: any): any {
            if (info._options && options)
                throw new Error("Component options were already provided.");

            if (info._options)
                options = info._options;

            if (info.componentType) {
                let opts = { id: info.id, isDialog: info.isDialog, isLookup: info.isLookup };
                if (options)
                    opts = Object.assign(opts, options);

                return new info.componentType(opts);
            } else {
                return this._getComponentFactory(this._buidTypeName(info)).create(init, options);
            }
        }

        private _getComponentFactory(typeName: string): ViewComponentFactory {
            return cmodo.getValueAtPropPath(window, typeName + "Factory");
        }
    }

    export let componentRegistry = new ComponentRegistry();
}