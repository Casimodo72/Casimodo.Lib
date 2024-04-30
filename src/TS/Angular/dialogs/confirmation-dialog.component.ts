import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Inject } from "@angular/core"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"
import { ConfirmationDialogConfig } from "./dialog.service"
import { AbstractDialogComponent } from "./abstractDialogComponent"

@Component({
    selector: "app-confirmation-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatIconModule, MatDialogModule, MatButtonModule],
    styleUrls: ["./dialog.scss"],
    template: `
        <div class="flex">
            @if (data.title) {
                <h1 mat-dialog-title>{{data.title}}</h1>
            }
            @if (data.closeStrategy === 'title-button') {
                <button mat-button *ngIf="" class="app-dialog-title-close-button" (click)="close(false)">
                    <mat-icon>close</mat-icon>
                </button>
            }
        </div>

        <div mat-dialog-content>
            <div [class]="{ 'app-warning': data.warning }">
                @for (line of messageLines; track line) {
                    <div>{{line}}</div>
                }
            </div>
        </div>

        <div mat-dialog-actions align="end">
            <button mat-raised-button [mat-dialog-close]="false">Abbrechen</button>
            <button mat-raised-button color="primary" [mat-dialog-close]="true" cdkFocusInitial>Ok</button>
        </div>
    `
})
export class ConfirmationDialogComponent extends AbstractDialogComponent<ConfirmationDialogComponent> {
    readonly messageLines: string[] = []

    constructor(
        dialogRef: MatDialogRef<ConfirmationDialogComponent>,
        @Inject(MAT_DIALOG_DATA)
        public readonly data: ConfirmationDialogConfig
    ) {
        super(dialogRef)

        this.messageLines = (data.message ?? "").split("\n")
    }
}
