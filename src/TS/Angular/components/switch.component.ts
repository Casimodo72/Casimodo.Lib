
import { ChangeDetectionStrategy, Component, Input } from "@angular/core"
import { FormsModule } from "@angular/forms"
import { MatSlideToggleModule } from "@angular/material/slide-toggle"
import { BooleanFormProp } from "@lib/models/props"

@Component({
    selector: "app-switch",
    standalone: true,
    imports: [FormsModule, MatSlideToggleModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
@if (model) {
    <mat-slide-toggle [ngModel]="model.value()" (ngModelChange)="model.setValue($event)">
        {{model.label}}
    </mat-slide-toggle>
}
`
})
export class AppSwitchComponent {
    @Input({ required: true }) model?: BooleanFormProp
}
