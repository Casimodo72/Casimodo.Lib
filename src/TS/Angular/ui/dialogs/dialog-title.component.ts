import { ChangeDetectionStrategy, Component, input, output } from "@angular/core"

import { MatDialogModule } from "@angular/material/dialog"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"

@Component({
    selector: "app-dialog-title",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatIconModule, MatDialogModule, MatButtonModule],
    styles: [`
.app-dialog-title-close-button {
    margin-left: auto;
    min-width: unset !important;
    width: 50px !important;
    height: 50px !important;
    padding-left: 15px;
}
    `],
    template: `
<div class="flex">
    @if (title(); as title) {
        <div mat-dialog-title>{{title}}</div>
    }
    @if (isCancelable()) {
        <button mat-button class="app-dialog-title-close-button" (click)="canceled.emit()">
            <mat-icon>close</mat-icon>
        </button>
    }
</div>
`
})
export class DialogTitleComponent {
    readonly title = input<string | null | undefined>(undefined)
    readonly isCancelable = input(true)
    readonly canceled = output()
}
