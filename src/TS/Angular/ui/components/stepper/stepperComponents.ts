import { ChangeDetectionStrategy, Component, input } from "@angular/core"

import { MatButton } from "@angular/material/button"
import { MatIcon } from "@angular/material/icon"
import { StepModel } from "./stepperModels"

@Component({
    selector: "app-next-step-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButton, MatIcon],
    styles: [" :host { display: block; } "],
    template: `
<button mat-stroked-button type="button"
    [disabled]="disabled() === true"
    aria-label="next step"
    (click)="goToNextStep()"><mat-icon></mat-icon>Weiter</button>
`
})
export class NextStepButton {
    readonly model = input.required<StepModel>()
    readonly disabled = input<boolean>()

    protected async goToNextStep() {
        this.model().moveToNext()
    }
}

@Component({
    selector: "app-previous-step-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButton, MatIcon],
    styles: [" :host { display: block; } "],
    template: `
<button mat-stroked-button type="button"
    [disabled]="disabled() === true"
    aria-label="previous step"
    (click)="goToPreviousStep()"><mat-icon></mat-icon>Zurück</button>
`
})
export class PreviousStepButton {
    readonly model = input.required<StepModel>()
    readonly disabled = input<boolean>()

    protected async goToPreviousStep() {
        this.model().moveToPrevious()
    }
}
