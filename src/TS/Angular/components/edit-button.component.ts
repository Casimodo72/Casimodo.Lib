import { ChangeDetectionStrategy, Component, Input } from "@angular/core"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"

// TODO: How to properly extend MatButton (or any other material component)?
//   E.g. we would need to provide and pass down every attribute we want to use (e.g. "disabled"),
//   which is super annoying.
@Component({
    selector: "app-edit-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule, MatIconModule],
    styles: [`
        // TODO: REMOVE? :host { display: contents; }
    `],
    template: `
        <button mat-icon-button type="button" aria-label="edit" [disabled]="disabled">
            <mat-icon>edit</mat-icon>
            <ng-content />
        </button>
    `
})
export class EditButtonComponent {
    @Input() disabled: boolean = false
}
