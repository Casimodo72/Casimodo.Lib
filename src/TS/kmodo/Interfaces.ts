
namespace kmodo {
    export interface ObservableObject {
        uid?: string;
        get?(name: string): any;
        set?(name: string, value: any): void;
        toJSON?();
    }

    export class ObservableHelper {
        public static observable<TModel>(item: any): TModel {
            return (kendo.observable(item) as any) as TModel;
        }
    }
}