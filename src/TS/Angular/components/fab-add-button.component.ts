import { ChangeDetectionStrategy, Component, Input } from "@angular/core"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"

@Component({
    selector: "app-fab-add-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule, MatIconModule],
    styles: [`

    `],
    template: `
         <button mat-mini-fab type="button" color="primary" aria-label="add" [disabled]="disabled">
            <mat-icon>add</mat-icon>
        </button>
    `
})
export class FabAddButtonComponent {
    @Input() disabled: boolean = false
}
