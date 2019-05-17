
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
        create(init: boolean, options: any): any;
    }

    export function createComponentFactory(): ViewComponentFactory {
        return {
            createCore: function (options: any): any {
                throw new Error("createCore() is not implemented.");
            },
            create: function (init: boolean, options: any): any {
                const component = this.createCore(options) as any;

                if (init && typeof component.createView === "function")
                    component.createView();

                return component;
            },
        };
    }
}





