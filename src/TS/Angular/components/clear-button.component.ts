import { ChangeDetectionStrategy, Component } from "@angular/core"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"

@Component({
    selector: "app-clear-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule, MatIconModule],
    template: `
        <button mat-icon-button type="button" aria-label="delete">
            <mat-icon>close</mat-icon>
        </button>
    `
})
export class ClearButtonComponent {
}
