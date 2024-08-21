import { ChangeDetectionStrategy, Component, input } from "@angular/core"

import { MatHint } from "@angular/material/form-field"

import { FormInputModel } from "@lib/models"

@Component({
    selector: "app-mat-read-only-input-hint",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatHint],
    styles: [`
        :host { display: block; }
    `],
    template: `
 @if (model().isReadOnly()) {
    <mat-hint class="app-mat-read-only-input-hint">Nicht bearbeitbar</mat-hint>
}
`
})
export class AppMatReadOnlyInputHintComponent {
    readonly model = input.required<FormInputModel>()
}
