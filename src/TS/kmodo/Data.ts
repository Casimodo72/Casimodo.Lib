
namespace kmodo {

    export class MyStaticClass {
        public static readonly myStaticProperty: number = 42;
        public static myStaticMethod(): void { /* ... */ }
        private constructor() { /* noop */ }
    }

    // To be set by consumer app.
    export function initDomainDataItem(item: any, itemTypeName: string, scenario: string) {
        throw new Error("The function 'initDomainDataItem' is expected to be implemented by the application.");
    }

    // Sets empty string values to either default values or null.
    // This is needed because Kendo annoyingly insists in initializing string values to ""
    //   even if we explicitely specify NULL as default value.
    export function initDataItemDefaultValues(item: any, propInfos: Object) {
        const propNames = Object.getOwnPropertyNames(propInfos);
        let info;

        for (let i = 0; i < propNames.length; i++) {
            const name = propNames[i];
            if (!item.hasOwnProperty(name))
                continue;

            if (item[name] !== "")
                continue;

            info = propInfos[name];

            if (typeof info.defaultValue !== "undefined")
                item[name] = info.defaultValue;
            else
                item[name] = null;
        }
    };
}