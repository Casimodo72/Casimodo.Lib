
namespace kmodo {
    export interface ObservableObject extends Partial<kendo.data.ObservableObject> {
        // TODO: REMOVE
        //uid: string;
        //get?(name: string): any;
        //set?(name: string, value: any): void;
        //bind?(eventName: string, handler: Function): kendo.Observable;
        //trigger?(eventName: string, e?: any): boolean;
        //toJSON?();
    }

    // TODO: REMOVE
    //export interface KendoModel extends Partial<kendo.data.Model> {
    //}

    export class ObservableHelper {
        public static observable<TModel>(item: TModel): TModel {
            return (kendo.observable(item) as any) as TModel;
        }

        public static dataSource<TModel extends ObservableObject>(options?: kendo.data.DataSourceOptions)
            : IGenericDataSource<TModel> {
            return new kendo.data.DataSource(options) as unknown as IGenericDataSource<TModel>;
        }
    }

    export interface ObservableArray<
        TModel extends ObservableObject>
        extends kendo.data.ObservableArray {
        [index: number]: TModel;
        push(...items: TModel[]): number;
        remove(item: TModel): void;
        map(callback: (item: TModel, index: number, source: ObservableArray<TModel>) => any): any[];
        every(callback: (item: TModel, index: number, source: ObservableArray<TModel>) => boolean): boolean;
        filter(callback: (item: TModel, index: number, source: ObservableArray<TModel>) => boolean): TModel[];
        find(callback: (item: TModel, index: number, source: ObservableArray<TModel>) => boolean): TModel;
        forEach(callback: (item: TModel, index: number, source: ObservableArray<TModel>) => void): void;
        indexOf(item: any): number;
    }

    export interface IGenericDataSource<T> { // extends kendo.data.DataSource {              
        // add(model: Object): kendo.data.Model;
        // add(model: kendo.data.Model): kendo.data.Model;
        add(model: T): T;

        // at(index: number): kendo.data.ObservableObject;
        at(index: number): T;

        // cancelChanges(model?: T): void;
        cancelChanges(model?: T): void;

        // data(): kendo.data.ObservableArray;
        data(): ObservableArray<T>;

        // data(value: any): void;
        data(value: T[]): void;

        // get(id: any): kendo.data.Model;
        get(id: any): T;

        // getByUid(uid: string): kendo.data.Model;
        getByUid(uid: string): T;

        // indexOf(value: kendo.data.ObservableObject): number;
        indexOf(value: T): number;

        // insert(index: number, model: kendo.data.Model): kendo.data.Model;
        // insert(index: number, model: Object): kendo.data.Model;
        insert(index: number, model: T): T;

        //insert(model: Object): kendo.data.Model; 
        insert(model: T): T;

        // remove(model: kendo.data.ObservableObject): void;
        remove(model: T): void;

        // view(): kendo.data.ObservableArray;
        view(): ObservableArray<T>;

        _pristineForModel(model: T): any; // kendo.data.Model;
    }
}