import { Injectable, inject } from "@angular/core"

import {
    IProject, IContract, ICountryState, IParty,
    expandFromProject2Contract, expandFromProject2Customer
} from "@lib/data"
import { ODataQueryBuilder, ODataFilterBuilder } from "@lib/data-utils"
import { DataSourceWebService, ProjectWebService } from "@lib/data/web"

import { TableModel, TableColumnModel, TableFilterType } from "./tableModels"
import { TableODataDataSource, TableFilterODataDataSource } from "./tableODataDataSource"
//import { LookupFormComponent } from "./lookup-dialog.component"
import { DialogService } from "@lib/dialogs"

@Injectable({ providedIn: "root" })
export class ActiveProjectLookupService {
    readonly #dialogService = inject(DialogService)
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
            //excludeClosed: true,
            filter: f
        })
    }

    readonly #dataSource = new TableODataDataSource<IProject>({
        webService: this.#dataSourceWebService,
        query: q => this.#buildProjectQuery(q)
            .url("api/projects/query")
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
        pagination: {
            availableSizes: [3, 5, 10]
        },
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

    lookup() {
        // FormDialog (title/cancel/submit)
        // LookupFormComponent: content of the FormDialog
        //   Must have a IFormComponentModel
        //LookupFormComponent.openAsLookupDialog(this.#dialogService)
    }
}
