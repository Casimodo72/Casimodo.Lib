import { ChangeDetectionStrategy, Component } from "@angular/core"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"

@Component({
    selector: "app-delete-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule, MatIconModule],
    template: `
        <button mat-icon-button type="button" aria-label="delete">
            <mat-icon>delete_outline</mat-icon>
        </button>
    `
})
export class DeleteButtonComponent {
}
