import { Component, ChangeDetectionStrategy } from "@angular/core"

@Component({
    selector: "app-form-column",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "class": "app-form-column"
    },
    template: "<ng-content/>"
})
export class FormColummComponent {
}
