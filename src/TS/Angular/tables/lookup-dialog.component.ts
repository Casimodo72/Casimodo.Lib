import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Inject, signal } from "@angular/core"

import { ButtonComponent, GlobalProgressBarComponent, IconComponent } from "@lib/components"
import { MatIconModule } from "@angular/material/icon"
import { DialogService, configureFullScreenDialog } from "@lib/dialogs"
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog"
import { TableModel } from "./tableModels"
import { StandardTableComponent } from "./standard-table.component"

// TODO: IMPL lookup dialogs holding a lookup table. Single row selection.
// TODO: Move to dedicated "lookup" folder.
@Component({
    selector: "app-lookup-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatDialogModule, MatIconModule,
        StandardTableComponent,
        GlobalProgressBarComponent, ButtonComponent, IconComponent
    ],
    styles: [`

`],
    template: `
<div class="flex">
    @if (title(); as title) {
        <h1 mat-dialog-title>{{title}}</h1>
    }
    <button mat-button class="app-dialog-title-close-button" (click)="cancel()">
        <mat-icon>close</mat-icon>
    </button>

</div>

<!-- Setting max-height because Material sets it to 65vh. -->
<div mat-dialog-content class="full-flex-col-item" style="max-height: 100%">
    <app-standard-table [model]="table"/>
</div>

<div mat-dialog-actions align="end">
<button mat-raised-button (click)="cancel()">Abbrechen</button>

<!-- TODO: We can't set cdkFocusInitial if the button is disabled. This will throw an exception. -->
<button mat-raised-button color="primary" (click)="submit()" [disabled]="!table.selectedRows().length">
    Ok
</button>
</div>
`
})
export class LookupTableDialog {
    static async open(dialogService: DialogService, table: TableModel) {

        return await dialogService.open<LookupTableDialog>(
            LookupTableDialog,
            configureFullScreenDialog({
                data: table,
                autoFocus: false
            }))
    }

    // TODO: Title
    readonly title = signal("Wählen")

    constructor(
        private readonly dialogRef: MatDialogRef<LookupTableDialog>,
        @Inject(MAT_DIALOG_DATA)
        public readonly table: TableModel
    ) {
        table.setOnRowClicked((event) => {
            this.table.selectRow(event.row)

            if (event.clickType === "double") {
                this.submit()
            }
        })
    }

    cancel() {

    }

    submit() {

    }
}
