import { DestroyRef, Directive, Injector, OnInit, inject, input } from "@angular/core"
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop"

import { MatStep, MatStepper } from "@angular/material/stepper"

import { StepModel, StepperModel } from "./models"

@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: "mat-stepper[cmatModel]",
    standalone: true,
})
export class CMatStepperModelDirective implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #matStepper: MatStepper

    constructor(matStepper: MatStepper) {
        this.#matStepper = matStepper
    }

    readonly cmatModel = input.required<StepperModel>()

    ngOnInit(): void {
        this.cmatModel()._matStepper = this.#matStepper

        // TODO: Do directives need to unsubscribe when subscribing to
        // the components they sit on?
        this.#matStepper.selectionChange
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(event =>
                this.cmatModel()._onMatStepperSelectionChanged(event)
            )
    }
}

@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: "mat-step[cmatModel]",
    standalone: true,
})
export class CMatStepModelDirective implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #injector = inject(Injector)
    readonly #matStep: MatStep

    constructor(matStep: MatStep) {
        this.#matStep = matStep
    }

    readonly cmatModel = input.required<StepModel>()

    ngOnInit(): void {
        this.cmatModel()._matStep = this.#matStep

        this.#matStep.label = this.cmatModel().label()

        toObservable(this.cmatModel().isCompleted, { injector: this.#injector })
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(isCompleted =>
                this.#matStep.completed = isCompleted
            )
    }
}
