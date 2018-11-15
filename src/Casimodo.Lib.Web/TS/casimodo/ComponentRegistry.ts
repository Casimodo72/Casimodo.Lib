/// <reference path="Utils.ts" />

namespace cmodo {

    export class ComponentRegItem {
        public id: string;
        public registry: ComponentRegistry;
        public _options: any;
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
        public minWidth: number;
        public maxWidth: number;
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

    export class ComponentRegistry {
        protected ns: string;
        public items: ComponentRegItem[];

        constructor() {
            this.ns = "";
            this.items = [];
        }

        add(item): void {

            var reg = new ComponentRegItem();
            reg.registry = this;
            reg.id = item.id;
            reg.part = item.part;
            reg.group = item.group || null;
            reg.role = item.role;
            reg.custom = !!item.custom;
            reg.isCached = !!item.isCached;
            reg.vmType = item.vmType || null;
            reg.isDialog = !!item.isDialog;
            reg.url = item.url;
            reg.minWidth = item.minWidth || null;
            reg.maxWidth = item.maxWidth || null;
            reg.minHeight = item.minHeight || null;
            reg.maxHeight = item.maxHeight || null;
            reg.maximize = !!item.maximize;
            reg.editorId = item.editorId || null;

            this.items.push(reg);
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