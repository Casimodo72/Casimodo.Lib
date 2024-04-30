import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, OnInit, input } from "@angular/core"

import { MatTableModule } from "@angular/material/table"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"
import { MatIconModule } from "@angular/material/icon"

import { ButtonComponent, ClickHandlerDirective, GlobalProgressBarComponent, IconComponent } from "@lib/components"
import { DialogService, configureFullScreenDialog } from "@lib/dialogs"
import { IProject } from "@lib/data"

import { TableModel, TableRowModel } from "./tableModels"
import { TableCellRendererComponent, TableFilterRendererComponent } from "./tableComponents"
import { PaginatorComponent } from "./paginator.component"

@Component({
    selector: "app-standard-table",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatTableModule,
        MatFormFieldModule, MatInputModule, MatIconModule,
        GlobalProgressBarComponent,
        TableFilterRendererComponent,
        TableCellRendererComponent,
        PaginatorComponent,
        ButtonComponent, IconComponent, ClickHandlerDirective
    ],
    styles: [`

`],
    templateUrl: "./standard-table.component.html"
})
export class StandardTableComponent implements OnInit {
    static async openAsLookupDialog(dialogService: DialogService) {
        return await dialogService.open<StandardTableComponent, undefined, Partial<IProject> | undefined>(
            StandardTableComponent,
            configureFullScreenDialog({ autoFocus: false }))
    }

    readonly model = input.required<TableModel>()

    handleRowClicked(row: TableRowModel, isDoubleClick: boolean) {
        this.model().onRowClicked(row, isDoubleClick ? "double" : "single")
    }

    // readonly #dataSourceWebService = inject(DataSourceWebService)
    // readonly #projectWebSerice = inject(ProjectWebService)

    // #buildProjectQuery(q: ODataQueryBuilder<IProject>) {
    //     return this.#projectWebSerice.buildQuery({
    //         isForManagement: true,
    //         builder: q
    //     })
    // }

    // #buildProjectFilter(f: ODataFilterBuilder<IProject>): ODataFilterBuilder {
    //     return this.#projectWebSerice.buildFilter({
    //         //excludeClosed: true,
    //         filter: f
    //     })
    // }

    // readonly #dataSource = new TableODataDataSource<IProject>({
    //     webService: this.#dataSourceWebService,
    //     query: q => this.#buildProjectQuery(q)
    //         .url("api/projects/query")
    //         // TODO: IMPL table paging
    //         //.top(5)
    //         .select(["Id", "Number", "ModifiedOn"])
    //         .expand<IContract>("Contract", q => q
    //             .select(["Street", "ZipCode", "City"])
    //             .expand<ICountryState>("CountryState", q => q
    //                 .select("Code")
    //             )
    //             .expand<IParty>("Customer", q => q
    //                 .select("NameShortest")
    //             )
    //         ),
    //     filter: f => this.#buildProjectFilter(f),
    //     orderby: o => o.select("ModifiedOn", "desc")
    // })

    // readonly tableModel = new TableModel<IProject>({
    //     pagination: {
    //         availableSizes: [3, 5, 10]
    //     },
    //     dataSource: this.#dataSource,
    //     columns: [
    //         new TableColumnModel({
    //             title: "Nummer",
    //             select: "Number",
    //             isSortable: true,
    //             filter: {
    //                 type: TableFilterType.STRING_PICKER_WITH_TYPEAHEAD,
    //                 source: new TableFilterODataDataSource<IProject>({
    //                     webService: this.#dataSourceWebService,
    //                     query: q => this.#buildProjectQuery(q)
    //                         .select("Number")
    //                         .filter(f => this.#buildProjectFilter(f))
    //                         .orderby("Number")
    //                 })
    //             }
    //         }),
    //         new TableColumnModel({
    //             title: "Straße",
    //             select: p => expandFromProject2Contract(p).select("Street"),
    //             filter: {
    //                 type: TableFilterType.STRING
    //             }
    //         }),
    //         new TableColumnModel({
    //             title: "PLZ",
    //             select: p => expandFromProject2Contract(p).select("ZipCode"),
    //             filter: {
    //                 type: TableFilterType.STRING
    //             }
    //         }),
    //         new TableColumnModel({
    //             title: "Stadt",
    //             select: p => expandFromProject2Contract(p).select("City"),
    //             filter: {
    //                 type: TableFilterType.STRING
    //             }
    //         }),
    //         new TableColumnModel({
    //             title: "Land",
    //             select: p => expandFromProject2Contract(p)
    //                 .expand<ICountryState>("CountryState").select("Code")
    //         }),
    //         new TableColumnModel({
    //             title: "Kunde",
    //             select: p => expandFromProject2Customer(p).select("NameShortest"),
    //             isSortable: true,
    //             filter: {
    //                 type: TableFilterType.STRING_PICKER_WITH_TYPEAHEAD,
    //                 target: p => expandFromProject2Contract(p).select("CustomerId"),
    //                 source: new TableFilterODataDataSource<IProject>({
    //                     webService: this.#dataSourceWebService,
    //                     query: q => this.#buildProjectQuery(q)
    //                         .apply(a => a
    //                             .filter(f => this.#buildProjectFilter(f))
    //                             .groupby([
    //                                 p => expandFromProject2Customer(p).select("Id"),
    //                                 p => expandFromProject2Customer(p).select("NameShortest")
    //                             ])
    //                         )
    //                         .orderby(p => expandFromProject2Customer(p).select("NameShortest")),
    //                     value: p => expandFromProject2Customer(p).select("Id"),
    //                     text: p => expandFromProject2Customer(p).select("NameShortest")
    //                 })
    //             }
    //         }),
    //         // new TableColumnModel({
    //         //     title: "BA",
    //         //     path: "ModifiedOn",
    //         //     cellComponent: TableCellExampleComponent,
    //         // })
    //     ]
    // })

    ngOnInit() {
        this.model().source.load()
    }
}
