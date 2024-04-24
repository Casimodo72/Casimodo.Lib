import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, OnInit, ViewChild, inject } from "@angular/core"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"
import { MatSort, MatSortModule, Sort } from "@angular/material/sort"
import { MatTableModule } from "@angular/material/table"

import { ODataFilterBuilder, ODataQueryBuilder } from "@lib/data-utils"
import { DataSourceWebService, ProjectWebService } from "@lib/data/web"
import { DialogService, configureFullScreenDialog } from "@lib/dialogs"
import {
    IContract, ICountryState, IParty, IProject,
    expandFromProject2Contract, expandFromProject2Customer
} from "@lib/data"

import { TableColumnModel, TableFilterType, TableModel } from "./tableModels"
import { TableFilterODataDataSource, TableODataDataSource } from "./tableODataDataSource"
import { TableCellRendererComponent, TableFilterRendererComponent } from "./tableComponents"
import { ButtonComponent, GlobalProgressBarComponent, IconComponent } from "@lib/components"
import { MatIconModule } from "@angular/material/icon"

// TODO: Check out for examples: https://github.com/twittwer/components

@Component({
    selector: "app-table",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatTableModule, MatSortModule, MatFormFieldModule, MatInputModule, MatIconModule,
        GlobalProgressBarComponent,
        TableFilterRendererComponent,
        TableCellRendererComponent,
        ButtonComponent, IconComponent
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

    readonly #dataSourceWebService = inject(DataSourceWebService)
    readonly #projectWebSerice = inject(ProjectWebService)

    #buildProjectQuery(q: ODataQueryBuilder<IProject>) {
        return this.#projectWebSerice.buildQuery({
            isForManagement: true,
            builder: q
        })
    }

    #buildProjectFilter(f: ODataFilterBuilder<IProject>): ODataFilterBuilder {
        return this.#projectWebSerice.buildFilter({
            excludeClosed: true,
            filter: f
        })
    }

    readonly #dataSource = new TableODataDataSource<IProject>({
        webService: this.#dataSourceWebService,
        query: q => this.#buildProjectQuery(q)
            .url("api/projects/query")
            // TODO: IMPL table paging
            .top(5)
            .select(["Id", "Number", "ModifiedOn"])
            .expand<IContract>("Contract", q => q
                .select(["Street", "ZipCode", "City"])
                .expand<ICountryState>("CountryState", q => q
                    .select("Code")
                )
                .expand<IParty>("Customer", q => q
                    .select("NameShortest")
                )
            ),
        filter: f => this.#buildProjectFilter(f),
        orderby: o => o.select("ModifiedOn", "desc")
    })

    readonly tableModel = new TableModel<IProject>({
        dataSource: this.#dataSource,
        columns: [
            new TableColumnModel({
                title: "Nummer",
                select: "Number",
                isSortable: true,
                filter: {
                    type: TableFilterType.STRING_PICKER_WITH_TYPEAHEAD,
                    source: new TableFilterODataDataSource<IProject>({
                        webService: this.#dataSourceWebService,
                        query: q => this.#buildProjectQuery(q)
                            .select("Number")
                            .filter(f => this.#buildProjectFilter(f))
                            .orderby("Number")
                    })
                }
            }),
            new TableColumnModel({
                title: "Straße",
                select: p => expandFromProject2Contract(p).select("Street"),
                filter: {
                    type: TableFilterType.STRING
                }
            }),
            new TableColumnModel({
                title: "PLZ",
                select: p => expandFromProject2Contract(p).select("ZipCode"),
                filter: {
                    type: TableFilterType.STRING
                }
            }),
            new TableColumnModel({
                title: "Stadt",
                select: p => expandFromProject2Contract(p).select("City"),
                filter: {
                    type: TableFilterType.STRING
                }
            }),
            new TableColumnModel({
                title: "Land",
                select: p => expandFromProject2Contract(p)
                    .expand<ICountryState>("CountryState").select("Code")
            }),
            new TableColumnModel({
                title: "Kunde",
                select: p => expandFromProject2Customer(p).select("NameShortest"),
                isSortable: true,
                filter: {
                    type: TableFilterType.STRING_PICKER_WITH_TYPEAHEAD,
                    target: p => expandFromProject2Contract(p).select("CustomerId"),
                    source: new TableFilterODataDataSource<IProject>({
                        webService: this.#dataSourceWebService,
                        query: q => this.#buildProjectQuery(q)
                            .apply(a => a
                                .filter(f => this.#buildProjectFilter(f))
                                .groupby([
                                    p => expandFromProject2Customer(p).select("Id"),
                                    p => expandFromProject2Customer(p).select("NameShortest")
                                ])
                            )
                            .orderby(p => expandFromProject2Customer(p).select("NameShortest")),
                        value: p => expandFromProject2Customer(p).select("Id"),
                        text: p => expandFromProject2Customer(p).select("NameShortest")
                    })
                }
            }),
            // new TableColumnModel({
            //     title: "BA",
            //     path: "ModifiedOn",
            //     cellComponent: TableCellExampleComponent,
            // })
        ]
    })

    displayedColumns: string[] = ["created", "state", "number", "title"]

    @ViewChild(MatSort) sort!: MatSort

    ngOnInit() {
        this.#dataSource.load()
    }

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
