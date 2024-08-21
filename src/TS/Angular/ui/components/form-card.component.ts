import { CommonModule } from "@angular/common"
import { Component, ChangeDetectionStrategy, input } from "@angular/core"

import { MatCard, MatCardContent } from "@angular/material/card"

@Component({
    selector: "app-form-card",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatCard, MatCardContent],
    styles: [" :host { display: block } "],
    template: `
<mat-card class="flex-column gap-2">
    @if (title(); as title) {
        <div class="app-form-card-header">
            {{title}}
        </div>
    }
    <div class="app-form-card-content">
        <ng-content />
    </div>
</mat-card>
`
})
export class FormCardComponent {
    readonly title = input<string | undefined>()
}
