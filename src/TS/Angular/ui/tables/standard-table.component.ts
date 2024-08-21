import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, OnInit, input } from "@angular/core"

import { MatTableModule } from "@angular/material/table"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"
import { MatIcon } from "@angular/material/icon"

import { GlobalProgressBarComponent } from "@lib/ui/components/global-progress-bar.component"
import { ButtonComponent } from "@lib/ui/components/button.component"
import { IconComponent } from "@lib/ui/components/icons/icon.component"
import { ClickHandlerDirective } from "@lib/ui/components/directives"

import type { TableModel } from "./tableModels"
import { TableCellRendererComponent, TableFilterRendererComponent } from "./tableComponents"
import { PaginatorComponent } from "./paginator.component"
import { TableColumnRole } from "./tablePrimitives"

// TODO: Allow resizing of table columns.
//   Maybe see: https://stackblitz.com/edit/mat-table-resize-column?file=src%2Fapp%2Fapp.component.ts

@Component({
    selector: "app-standard-table",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatTableModule,
        MatFormFieldModule, MatInputModule, MatIcon,
        GlobalProgressBarComponent,
        TableFilterRendererComponent,
        TableCellRendererComponent,
        PaginatorComponent,
        ButtonComponent, IconComponent, ClickHandlerDirective
    ],
    templateUrl: "./standard-table.component.html"
})
export class StandardTableComponent implements OnInit {
    readonly TableColumnRole = TableColumnRole
    readonly model = input.required<TableModel>()

    ngOnInit() {
        this.model().source.load()
    }
}
