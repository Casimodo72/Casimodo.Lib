import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Inject, Signal, computed, signal } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog"
import { MatIcon } from "@angular/material/icon"
import { MatButton } from "@angular/material/button"
import { MatSlideToggle } from "@angular/material/slide-toggle"

import { UICommand, UISwitchCommand } from "@lib/models"
import {
    DialogService, DialogTitleComponent, configureBlackBackgroundDialog,
    configureFullScreenDialog
} from "@lib/ui/dialogs"
import { TableModel } from "@lib/ui/tables/tableModels"
import { StandardTableComponent } from "@lib/ui/tables/standard-table.component"
import { GlobalProgressBarComponent } from "@lib/ui/components/global-progress-bar.component"
import { ButtonComponent } from "@lib/ui/components/button.component"
import { IconComponent } from "@lib/ui/components/icons/icon.component"
import { TableRowSelectionMode } from "@lib/ui/tables/tableTypes"

export interface ILookupTableDialogConfig {
    title: string
    table: TableModel
    selectionMode?: TableRowSelectionMode
    commands?: UICommand[]
}

@Component({
    selector: "app-lookup-table-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule, FormsModule,
        MatDialogModule, MatButton, MatIcon, MatSlideToggle,
        DialogTitleComponent, StandardTableComponent, GlobalProgressBarComponent,
        ButtonComponent, IconComponent
    ],
    styles: [`

`],
    template: `
<app-dialog-title [title]="title()" [isCancelable]="true" (cancelled)="cancel()"/>

<!-- Setting max-height because Material sets it to 65vh. -->
<div mat-dialog-content class="full-flex-col-item full-flex-col app-lookup-table-dialog-content" style="max-height: 100%">
    @if (commands().length) {
        <div class="flex gap-2">
            @for (command of commands(); track command) {
                @if (command.type === 'switch') {
                    <mat-slide-toggle class="m-2"
                        [ngModel]="asUISwitchCommand(command).isOn()"
                        (ngModelChange)="asUISwitchCommand(command).toggle()">
                        {{command.text}}
                    </mat-slide-toggle>
                }
            }
        </div>
    }

    <app-standard-table class="full-flex-col-item" [model]="table"/>
</div>

<div mat-dialog-actions align="end">

    <button mat-raised-button (click)="cancel()">Abbrechen</button>

    <button mat-raised-button color="primary" (click)="submit()" [disabled]="!hasSelection()">
        Ok
    </button>
</div>
`
})
export class LookupTableDialog {
    static async select<TOutput>(dialogService: DialogService, config: ILookupTableDialogConfig) {
        return await dialogService.open<LookupTableDialog>(
            LookupTableDialog,
            configureBlackBackgroundDialog(configureFullScreenDialog({
                data: config,
                autoFocus: "dialog"
            }))) as TOutput[] | undefined
    }

    readonly table: TableModel
    readonly title = signal("Lookup")
    readonly hasSelection: Signal<boolean>
    readonly commands = signal<UICommand[]>([])

    constructor(
        private readonly dialogRef: MatDialogRef<LookupTableDialog>,
        @Inject(MAT_DIALOG_DATA)
        config: ILookupTableDialogConfig
    ) {
        this.table = config.table
        this.table.reset()

        if (config.title) {
            this.title.set(config.title)
        }

        if (this.table.selectionMode === "none") {
            this.table.setSelectionMode("single")
        }
        this.hasSelection = computed(() => this.table.selectedRows().length > 0)
        this.table.setOnRowClicked((event) => {
            this.table.toggleRowIsSelected(event.row)

            if (event.clickType === "double" && event.row.isSelected()) {
                this.submit()
            }
        })

        if (config.commands?.length) {
            this.commands.set(config.commands)
        }
    }

    cancel() {
        this.closeDialog(undefined)
    }

    submit() {
        const selectedRows = this.table.selectedRows()
        if (!selectedRows?.length) return

        const selectedDataItems = selectedRows.map(x => x.data)

        this.closeDialog(selectedDataItems)
    }

    closeDialog(dialogResult: any) {
        this.dialogRef.close(dialogResult)
        this.table.reset()
    }

    asUISwitchCommand(command: UICommand): UISwitchCommand {
        return command as UISwitchCommand
    }
}
