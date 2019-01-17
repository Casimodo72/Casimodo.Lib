
namespace cmodo {
    export class DialogArgs {

        id: string;
        value: any;
        values: any[];
        filters: any[];
        filterCommands: any;
        mode: string;
        canDelete: boolean;
        itemId: string;
        params: any;
        title: string;
        message: string;
        item: any;

        buildResult: Function;
        initSpace: Function;

        constructor(id: string, value?: any) {
            this.id = id;
            this.value = value || null;
            this.values = [];
            this.buildResult = function () { };
            this.initSpace = function () { };
        };

        getValue(name: string) {
            var vals = this.values;

            if (!vals)
                return undefined;

            for (let i = 0; i < vals.length; i++) {
                if (vals[i].name === name)
                    return vals[i].value;
            }

            return undefined;
        }

        getFilterValue(name: string) {
            var vals = this.filters;

            if (!vals)
                return undefined;

            for (let i = 0; i < vals.length; i++) {
                if (vals[i].field === name)
                    return vals[i].value;
            }

            return undefined;
        }
    }

    export class DialogArgsContainer extends ComponentBase {

        items: DialogArgs[];

        constructor() {
            super();
            this.items = [];
        }

        add(item: DialogArgs, trigger?: boolean) {
            this.items.push(item);
            if (trigger)
                this.trigger(item.id, { args: item });
        }

        get(id: string): DialogArgs {
            return this.items.find(x => x.id === id);
        }

        consume(id: string): DialogArgs {
            var item = this.get(id);
            if (item)
                this.remove(item);
            return item;
        }

        remove(item: DialogArgs) {
            if (!item) return;
            var index = this.items.indexOf(item);
            if (index !== -1) {
                this.items.splice(index, 1);
            }
        }
    }

    export var dialogArgs = new DialogArgsContainer();

    export class ComponentArgsContainer {
        private items: any[];

        constructor() {
            this.items = [];
        }

        add(id: string, paramName: string, value) {
            var item = this.get(id);
            if (!item) {
                item = { id: id };
                this.items.push(item);
            }

            item[paramName] = value;
        }

        get(id: string) {
            return this.items.find(x => x.id === id);
        }

        consume(id: string) {
            var item = this.get(id);
            if (!item)
                return null;

            var idx = this.items.indexOf(item);
            if (idx !== -1)
                this.items.splice(idx, 1);

            return item;
        }
    }
    export var componentArgs = new ComponentArgsContainer();

    export class NavigationArgsContainer {
        private items: any[];

        constructor() {
            this.items = [];

            // Init with items of window.opener.
            var prev = cmodo.getValueAtPropPath(window, "opener.cmodo.navigationArgs");
            if (prev) {
                for (let i = 0; i < prev.items.length; i++) {
                    this.items.push(prev.items[i]);
                }
                prev.items = [];
                prev = null;
            }
        };

        add(item) {
            this.items.push(item);
        };

        get(id: string) {
            return this.items.find(x => x.id === id);
        };

        consume(id: string) {
            var item = this.get(id);
            if (!item)
                return null;

            var idx = this.items.indexOf(item);
            if (idx !== -1)
                this.items.splice(idx, 1);

            return item;
        }
    }
    export var navigationArgs = new NavigationArgsContainer();
}