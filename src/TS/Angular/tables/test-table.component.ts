import { CommonModule, NgComponentOutlet } from "@angular/common"
import { AfterViewInit, ChangeDetectionStrategy, Component, ComponentRef, Directive, OnInit, Type, ViewChild, computed, inject, input, signal } from "@angular/core"
import { MatSort, MatSortModule, Sort } from "@angular/material/sort"
import { MatTableModule } from "@angular/material/table"
import { IContract, IParty, IProject } from "@lib/data"
import { ODataFilterBuilder, ODataQueryBuilder } from "@lib/data-utils"
import { AnyWebService, CustomerWebService, ProjectSegmentWebService, ProjectWebService } from "@lib/data/web"
import { DialogService, configureFullScreenDialog } from "@lib/dialogs"
import { DataItemModel, FormItem, IFormItem, IFormPropCore, ItemModel, ListModel, PickerFormProp } from "@lib/models"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"
import { StandardTypeaheadValuePickerComponent } from "@lib/components"

// TODO: Check out for examples: https://github.com/twittwer/components

class TableRowModel<TData> extends DataItemModel<Partial<TData>> {

}

type StringKeys<T> = Extract<keyof T, string>

class DataPathBuilder<TData = any> {
    #depth = 0
    path: string[] = []

    prop(name: StringKeys<TData>): this {
        if (this.#depth >= this.path.length) {
            this.path.push(name)
        }
        else {
            this.path[this.#depth] = name
        }

        return this
    }

    expand<TSubData>(name: StringKeys<TData>): DataPathBuilder<TSubData> {
        this.path.push(name)
        this.#depth++

        return this as any as DataPathBuilder<TSubData>
    }
}

type TableColumnQueryFilterDataFn = () => Promise<any[]>

export enum TableDataType {
    STRING,
    BOOLEAN,
    DATE_TIME
}

export enum TableFilterType {
    STRING,
    STRING_PICKER_WITH_TYPEAHEAD,
    BOOLEAN,
    DATE_TIME
}

export interface ITableColumnFilterOptions<TData = any> {
    type: TableFilterType
    component?: AbstractTableColumnFilterComponent
    readUrl?: string
    getPickValues?: TableColumnQueryFilterDataFn
}

export class TableColumnFilterModel<TData = any> {
    readonly type: TableFilterType
    readonly readUrl?: string
    readonly getPickValues?: TableColumnQueryFilterDataFn

    constructor(opts: ITableColumnFilterOptions<TData>) {
        this.type = opts.type
        this.readUrl = opts.readUrl
        this.getPickValues = opts.getPickValues
    }
}

export interface ITableColumnOptions<TData> {
    title?: string
    type?: TableDataType
    path: StringKeys<TData> | ((buider: DataPathBuilder<TData>) => DataPathBuilder)
    isSortable?: boolean
    filter?: ITableColumnFilterOptions<TData>
    cellComponent?: Type<any>
    cell?: ComponentRef<AbstractTableCellComponent>
}

export class TableColumnModel<TData = any> extends ItemModel {
    readonly title = signal("")
    readonly type = signal(TableDataType.STRING)
    readonly pathSegments: string[] = []
    readonly path: string
    readonly isSortable: boolean
    readonly filter?: TableColumnFilterModel<TData>
    readonly cellType?: Type<any>
    readonly isVisible = signal(true)

    constructor(opts: ITableColumnOptions<TData>) {
        super()

        if (opts.title) {
            this.title.set(opts.title)
        }

        if (opts.type) {
            this.type.set(opts.type)
        }

        let path: string[] | undefined = undefined
        if (typeof opts.path === "string") {
            path = [opts.path]
        }
        else if (typeof opts.path === "function") {
            path = opts.path(new DataPathBuilder<TData>()).path
        }

        this.pathSegments = path ?? []
        this.path = this.pathSegments.reduce((acc, next) => acc + "." + next, "")

        this.cellType = opts.cellComponent

        this.isSortable = opts.isSortable ?? false

        if (opts.filter) {
            this.filter = new TableColumnFilterModel<TData>(opts.filter)
        }
    }

    getRow(row: any): TableRowModel<TData> {
        return row as TableRowModel<TData>
    }

    getValue(row: any): any {
        const rowModel = row as TableRowModel<TData>

        let value = rowModel.data as any
        for (const propKey of this.pathSegments) {
            value = value[propKey]
        }

        return value
    }

    async queryFilterData(): Promise<any[]> {
        return this.filter?.getPickValues
            ? await this.filter.getPickValues()
            : Promise.resolve([])
    }
}

export type SortDirection = "asc" | "desc"

export interface ISortDefinition {
    path: string | string[]
    direction: SortDirection
}

export class PaginationRequest {
    readonly pageIndex: number
    readonly pageSize: number

    constructor(pageIndex: number, pageSize: number) {
        this.pageIndex = pageIndex
        this.pageSize = pageSize
    }
}

export class TablePaginationManager {
    readonly _pageIndex = signal(0)
    readonly pageIndex = this._pageIndex.asReadonly()

    readonly _pageSize = signal(20)
    readonly pageSize = this._pageIndex.asReadonly()

    readonly availablePageSizes = signal([20, 50])

    setPageSize(pageSize: number): this {
        this._pageSize.set(Math.max(pageSize, 1))

        return this
    }
}

class TableModel<TData> extends ItemModel {
    readonly queryBuilder = signal<ODataQueryBuilder<TData> | null>(null)
    readonly filterBuilder = signal<ODataFilterBuilder<TData> | null>(null)

    readonly sortPropName = signal<string>("")
    readonly sortDirection = signal<"asc" | "desc">("asc")

    readonly #columnList = new ListModel<TableColumnModel<TData>>()
    readonly columns = this.#columnList.items
    readonly visibleColumnPaths = computed(() => {
        return this.columns().filter(x => x.isVisible()).map(x => x.path)
    })

    readonly #rowList = new ListModel<TableRowModel<TData>>()
    readonly rows = this.#rowList.items

    readonly pagination = new TablePaginationManager()

    setColumns(columns: TableColumnModel<TData>[]): this {
        this.#columnList.addRange(columns)

        return this
    }

    setRows(rows: Partial<TData>[]) {
        this.#rowList.setItems(rows.map(x => new TableRowModel(x)))
    }

    setQuery(buildQuery: (q: ODataQueryBuilder<TData>) => void): this {
        const q = new ODataQueryBuilder<TData>()
        buildQuery(q)

        this.queryBuilder.set(q)

        return this
    }

    setFilter(buildFilter: (f: ODataFilterBuilder<TData>) => void): this {
        const f = new ODataFilterBuilder<TData>()
        buildFilter(f)

        this.filterBuilder.set(f)

        return this
    }

    orderBy(q: ODataQueryBuilder<any>, field: string, direction: SortDirection) {
        return q.orderby(field, direction === "desc" ? "desc" : "asc")
    }
}

export class FormComponent implements IFormItem {
    readonly #formItem = new FormItem()

    addPropGroupChild(child: IFormItem): void {
        this.#formItem.addPropGroupChild(child)
    }

    isModified(): boolean {
        return this.#formItem.isModified()
    }

    validate(): Promise<boolean> {
        return this.#formItem.validate()
    }

    onPropValueChanged(prop: IFormPropCore<any>): void {
        this.#formItem.onPropValueChanged(prop)
    }
}

@Directive()
export abstract class AbstractTableColumnFilterComponent extends FormComponent {
    readonly column = input.required<TableColumnModel>()
}

@Component({
    selector: "app-table-typeahead-picker-column-filter",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatTableModule, MatSortModule, MatFormFieldModule, MatInputModule,
        StandardTypeaheadValuePickerComponent
    ],
    template: `
    Hello
    <app-standard-typeahead-value-picker [model]="prop" />
`
})
export class TableTypeaheadPickerColumnFilterComponent
    extends AbstractTableColumnFilterComponent
    implements AfterViewInit {

    readonly #anyWebService = inject(AnyWebService)

    readonly prop = new PickerFormProp<any>(this)

    // async ngOnInit() {
    //     const column = this.column()
    //     if (!column) return

    //     const values = await column.queryFilterData()
    //     this.prop.setPickValues(values)
    // }

    async ngAfterViewInit() {
        const column = this.column()
        const filter = column.filter
        if (!filter) return

        const pickValues = await this.getPickValues(filter)

        this.prop.setPickValues(pickValues)
    }

    async getPickValues(filter: TableColumnFilterModel) {
        if (filter.readUrl) {
            await this.#anyWebService.queryByUrl(filter.readUrl)
        }
        else if (filter.getPickValues) {
            return await filter.getPickValues()
        }

        return []
    }
}

// TODO: Assess deferred loading of filter picker values:
// https://stackblitz.com/edit/angular-mat-select-deferred-loading?file=app%2Fselect-deferred-example.ts

@Component({
    selector: "app-table-column-header-renderer",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        NgComponentOutlet,
        TableTypeaheadPickerColumnFilterComponent
    ],
    template: `
@if (column(); as column) {
    @if (filterComponentType(); as filterComponentType) {
        <ng-template *ngComponentOutlet="filterComponentType; inputs: { column: column }"></ng-template>
    }
}
`
})
export class TableColumnHeaderRendererComponent implements AfterViewInit {
    readonly column = input.required<TableColumnModel>()

    readonly filterComponentType = signal<Type<AbstractTableColumnFilterComponent> | undefined>(undefined)

    ngAfterViewInit(): void {
        const column = this.column()
        if (!column) return

        if (column.filter?.type !== undefined) {
            this.filterComponentType.set(this.getFilterComponent(column.filter.type))
        }
    }

    // TODO: Move this to a service.
    getFilterComponent(type: TableFilterType): Type<AbstractTableColumnFilterComponent> | undefined {
        if (type === TableFilterType.STRING_PICKER_WITH_TYPEAHEAD) {
            return TableTypeaheadPickerColumnFilterComponent
        }

        return undefined
    }
}

@Directive()
export abstract class AbstractTableCellComponent {
    readonly column = input.required<TableColumnModel>()
    readonly row = input.required<any>()
}

@Component({
    selector: "app-table-cell-example",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule

    ],
    template: `
  `
})
export class TableCellExampleComponent extends AbstractTableCellComponent {

}

@Component({
    selector: "app-table-cell-renderer",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        NgComponentOutlet
    ],
    template: `
@if (column() && row()) {
    @if (column().cellType; as cellComponentType)     {
        <ng-template *ngComponentOutlet="cellComponentType; inputs: { column: column(), row: row()}"></ng-template>
    }
    @else {
        {{column().getValue(row())}}
    }
}
`
})
export class TableCellRendererComponent {
    readonly column = input.required<TableColumnModel>()
    readonly row = input.required<any>()
}

@Component({
    selector: "app-table",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatTableModule, MatSortModule, MatFormFieldModule, MatInputModule,
        TableColumnHeaderRendererComponent,
        TableCellRendererComponent,
    ],
    templateUrl: "./test-table.component.html",
    // styleUrls: ["./test-table.component.scss"]
})
export class TestTableComponent implements OnInit {
    static async openAsDialog(dialogService: DialogService) {
        return await dialogService.open<TestTableComponent, undefined, Partial<IProject> | undefined>(
            TestTableComponent,
            configureFullScreenDialog({}))
    }

    readonly #projectWebService = inject(ProjectWebService)
    readonly #customerWebService = inject(CustomerWebService)

    // readonly #destroyRef = inject(DestroyRef)
    readonly projectService = inject(ProjectSegmentWebService)

    readonly tableModel = new TableModel<IProject>()
        .setColumns([
            new TableColumnModel({
                title: "Nummer",
                path: "Number",
                filter: {
                    type: TableFilterType.STRING_PICKER_WITH_TYPEAHEAD,
                    readUrl: "api/projects/queryForManagement?$select=Number",
                    //getPickValues: () => this.#projectWebService.queryForTableColumnFilter()
                }
            }),
            // new TableColumnModel({
            //     title: "Straße",
            //     path: p => p.expand<IContract>("Contract").prop("Street")
            // }),
            // new TableColumnModel({
            //     title: "Kunde",
            //     path: p => p.expand<IContract>("Contract").expand<IParty>("Customer").prop("NameShortest"),
            //     filter: {
            //         type: TableFilterType.STRING_PICKER_WITH_TYPEAHEAD,
            //         getPickValues: () => this.#customerWebService.queryForTableColumnFilter()
            //     }
            // }),
            // new TableColumnModel({
            //     title: "BA",
            //     path: "ModifiedOn",
            //     cellComponent: TableCellExampleComponent,
            //     // cell: this.createCellComponent(TableCellExampleComponent)
            // })
        ])

    displayedColumns: string[] = ["created", "state", "number", "title"]

    @ViewChild(MatSort) sort!: MatSort

    async ngOnInit() {
        const projects = await this.#projectWebService.queryForManagement(q => q
            .top(5)
            .orderby("ModifiedOn", "desc")
            .select(["Id", "Number", "ModifiedOn"])
            .expand<IContract>("Contract", q => q
                .select("Street")
                .expand<IParty>("Customer", q => q
                    .select("NameShortest")
                )
            )
        )
        this.tableModel.setRows(projects)
    }

    // const componentRef = createComponent(MyComponent, {
    //     environmentInjector: this.appRef.injector,
    //   })

    onMatSort(_sort: Sort) {
        // TODO:
    }

    // Misc info:
    /*
        Issue: https://github.com/angular/components/issues/11953
            this.dataSource.paginator = this.paginator;
            this.dataSource.sort = this.sort;
            // IMPORTANT: Binding the dataset comes last
            this.dataSource.data = <YOUR DATASET>
    */
}
