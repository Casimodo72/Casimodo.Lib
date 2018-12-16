/// <reference path="Utils.ts" />

namespace cmodo {

    export class ComponentRegItem {
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
        public vmType: any;
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

        createViewModelOnly(options?: any): any {
            return this.registry.createViewModelOnly(this, options);
        }

        vmOnly(options?: any): any {
            return this.registry.createViewModelOnly(this, options);
        }

        vm(options?: any): any {
            return this.registry.createViewModel(this, options);
        }
    }

    export interface ComponentRegItemOptions {
        id: string;
        part: string;
        group?: string;
        role: string;
        custom?: boolean;
        isCached?: boolean;
        vmType?: any;
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
        public items: ComponentRegItem[];

        constructor() {
            this.ns = "";
            this.items = [];
        }

        add(options: ComponentRegItemOptions): void {

            var item = new ComponentRegItem();
            item.registry = this;
            item.id = options.id;
            item.part = options.part;
            item.group = options.group || null;
            item.role = options.role;
            item.custom = !!options.custom;
            item.isCached = !!options.isCached;
            item.vmType = options.vmType || null;
            item.isDialog = !!options.isDialog;
            item.url = options.url;
            item.width = options.width || null;
            item.minWidth = options.minWidth || null;
            item.maxWidth = options.maxWidth || null;
            item.height = options.height || null;
            item.minHeight = options.minHeight || null;
            item.maxHeight = options.maxHeight || null;
            item.maximize = !!options.maximize;
            item.editorId = options.editorId || null;

            this.items.push(item);
        }

        _buidTypeName(reg: ComponentRegItem): string {
            return this.ns + "." + reg.part + (reg.group ? "_" + reg.group + "_" : "") + reg.role;
        }

        getById(id: string, options?: any): ComponentRegItem {
            var item = this.items.find(x => x.id === id);

            if (item && options) {
                // KABU TODO: IMPORTANT: Will this work with ES6 class instances?
                item = Object.assign(Object.create(Object.getPrototypeOf(item)), item);
                item._options = options;
            }

            return item;
        }

        getByAlias(alias: string): ComponentRegItem {
            return this.items.find(x => x.alias === alias);
        }

        getAuthQueries(item: ComponentRegItem): AuthQuery[] {
            var result: AuthQuery[] = [];

            result.push({
                Part: item.part,
                Group: item.group,
                VRole: item.role
            });

            if (item.editorId) {
                var editor = this.getById(item.editorId);
                result.push({
                    Part: editor.part,
                    Group: editor.group,
                    VRole: editor.role
                });
            }

            return result;
        }

        createViewModel(item: ComponentRegItem, options?: any): any {

            if (item._options && options)
                throw new Error("Component options were already provided.");

            if (item._options)
                options = item._options;

            if (item.vmType) {
                return new item.vmType({ id: item.id, isDialog: item.isDialog, isLookup: item.isLookup });
            }
            else {
                var typeName = this._buidTypeName(item);

                // TypeScript:
                //var myClassInstance = Object.create(window["MyClass"].prototype);
                //myClassInstance.constructor.apply(greeter, new Array(myContructorArg));

                return this.getComponentFactory(typeName).create(options);
            }
        }

        createViewModelOnly(item: ComponentRegItem, options?: any): any {

            if (item._options && options)
                throw new Error("Component options were already provided.");

            if (item._options)
                options = item._options;

            if (item.vmType) {
                return new item.vmType({ id: item.id, isDialog: item.isDialog, isLookup: item.isLookup });
            }
            else {
                var typeName = this._buidTypeName(item);
                return this.getComponentFactory(typeName).createViewModel(options);
            }
        }

        private getComponentFactory(typeName: string): any {
            return cmodo.getValueAtPropPath(window, typeName + "Factory");
        }
    }

    export var componentRegistry = new ComponentRegistry();
}