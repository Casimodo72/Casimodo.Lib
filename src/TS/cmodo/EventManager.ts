
// Basic types: https://www.typescriptlang.org/docs/handbook/basic-types.html

// https://github.com/KeesCBakker/Strongly-Typed-Events-for-TypeScript
// https://stackoverflow.com/questions/12881212/does-typescript-support-events-on-classes

namespace cmodo {

    enum EventListenerMode {
        Always,
        Once
    }

    // TODO: Do we need that?
    //interface Event {
    //    preventDefault: Function;
    //    isDefaultPrevented(): boolean;
    //}

    class EventDefinition {
        public name: string;
        public bindings: EventListenerBinding[] = [];

        constructor(name: string) {
            this.name = name;
        }
    }

    class EventListenerBinding {
        public mode: EventListenerMode;
        public handler: Function;

        constructor(mode: EventListenerMode, handler: Function) {
            this.mode = mode;
            this.handler = handler;
        }
    }

    export interface EventListenable {
        on(eventName: string, handler: Function): void;
    }

    export class EventManager {
        private source: any;
        private events: any[] = [];

        constructor(source: any) {
            this.source = source;
        }

        public trigger(eventName: string, e?: any, source?: any | undefined): void {

            const eve = this._get(eventName, false);
            if (!eve)
                return;

            if (e) {
                if (typeof e._defaultPrevented === "undefined" && typeof e.defaultPrevented === "undefined")
                    e.defaultPrevented = false;

                if (typeof e.preventDefault === "undefined") {
                    e.preventDefault = function () {
                        e.defaultPrevented = true;
                    };
                }
            }

            for (let i = 0; i < eve.bindings.length; i++) {
                const binding = eve.bindings[i];

                if (binding.mode === EventListenerMode.Once) {
                    eve.bindings.splice(i, 1);
                    i--;
                }

                try {
                    binding.handler.call(source || this.source, e);
                }
                catch (ex) {
                    console.error(ex);
                    // TODO: How to handle exceptions here? Suppress?
                }
            }
        }

        public on(eventName: string, handler: Function): void {
            this._add(eventName, handler, EventListenerMode.Always);
        }

        public one(eventName: string, handler: Function): void {
            this._add(eventName, handler, EventListenerMode.Once);
        }

        private _add(eventName: string, handler: Function, mode: EventListenerMode): void {
            this._get(eventName, true).bindings.push(new EventListenerBinding(mode, handler));
        }

        private _get(eventName: string, createIfMissing: boolean): EventDefinition {
            const eves = this.events;
            for (let i = 0; i < eves.length; i++)
                if (eves[i].name === eventName)
                    return eves[i];

            if (!createIfMissing)
                return null;

            const eve = new EventDefinition(eventName);
            eves.push(eve);

            return eve;
        }
    }
}





