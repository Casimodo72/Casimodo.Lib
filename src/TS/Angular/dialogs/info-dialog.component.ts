import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Inject } from "@angular/core"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"
import { InfoDialogConfig } from "./dialog.service"
import { AbstractDialogComponent } from "./abstractDialogComponent"

@Component({
    selector: "app-info-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatIconModule, MatDialogModule, MatButtonModule],
    styleUrls: ["./dialog.scss"],
    template: `
        <div class="flex">
            @if (data.title) {
                <h1 mat-dialog-title>{{data.title}}</h1>
            }
            <!-- TODO: The title has a 16px margin (comes from material css).
                Dunno why; the margin in the offical examples is only 1px high (https://material.angular.io/components/dialog/overview)
            -->
            @if (data.closeStrategy === 'title-button') {
                <button mat-button class="app-dialog-title-close-button" (click)="close(false)">
                    <mat-icon>close</mat-icon>
                </button>
            }
        </div>

        <div mat-dialog-content>
            <!-- TODO: The dialog-content adds more huge margins :-/
                Together with he title margin issue -> not acceptable.
            -->
            {{data.message}}
        </div>

        <div mat-dialog-actions align="end">
            <button mat-raised-button color="primary" [mat-dialog-close]="false" cdkFocusInitial>Schließen</button>
        </div>
    `
})
export class InfoDialogComponent extends AbstractDialogComponent<InfoDialogComponent> {
    constructor(
        dialogRef: MatDialogRef<InfoDialogComponent>,
        @Inject(MAT_DIALOG_DATA)
        public readonly data: InfoDialogConfig
    ) {
        super(dialogRef)
    }
}
