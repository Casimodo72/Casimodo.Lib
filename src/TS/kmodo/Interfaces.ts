
namespace kmodo {
    export interface ObservableObject {
        uid?: string;
        get?(name: string): any;
        set?(name: string, value: any): void;
        bind?(eventName: string, handler: Function): kendo.Observable;
        trigger?(eventName: string, e?: any): boolean;
        toJSON?();
    }

    export class ObservableHelper {
        public static observable<TModel>(item: TModel): TModel {
            return (kendo.observable(item) as any) as TModel;
        }
    }
}