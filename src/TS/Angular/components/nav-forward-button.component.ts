import { ChangeDetectionStrategy, Component } from "@angular/core"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"
import { ButtonComponent } from "./button.component"

@Component({
    selector: "app-nav-forward-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule, MatIconModule, ButtonComponent],
    template: `
        <app-button type="forward" />
        <!-- <button mat-icon-button type="button" aria-label="navigate forward">
            <mat-icon>arrow_forward_ios</mat-icon>
        </button> -->
    `
})
export class NavForwardButtonComponent {
}
