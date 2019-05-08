
// Performance: https://www.contentful.com/blog/2017/04/04/es6-modules-support-lands-in-browsers-is-it-time-to-rethink-bundling/
// Multi files per module: https://github.com/Microsoft/TypeScript/issues/17
namespace cmodo {
    
    export function showError(message: string): void {
        // NOP. Consumer has to provide its own implementation.
    }

    export function showWarning(message: string): void {
        // NOP. Consumer has to provide its own implementation.
    }

    export function showInfo(message: string): void {
        // NOP. Consumer has to provide its own implementation.
    }

    export abstract class ComponentBase {
        private _events: EventManager;

        constructor() {
            this._events = new EventManager(this);
        }

        on(eventName: string, func: Function): void {
            this._events.on(eventName, func);
        }

        one(eventName: string, func: Function): void {
            this._events.one(eventName, func);
        }

        trigger(eventName: string, e?: any): void {
            this._events.trigger(eventName, e);
        }
    }

    export abstract class ObservableObject extends ComponentBase {
        constructor() {
            super();
        }
    }

    export interface ViewComponentFactory {
        createCore(options: any): any;
        create(options: any): any;
        createViewModel(options: any);
    }

    export function createComponentViewModelFactory(): ViewComponentFactory {
        return {
            createCore: function (options: any): any {
                throw new Error("Not implemented.");
            },
            create: function (options: any): any {
                const vm = this.createCore(options) as any;
                if (typeof vm.createView === "function")
                    vm.createView();

                return vm;
            },
            createViewModel: function (options: any): any {
                return this.createCore(options);
            }
        };
    }
}





