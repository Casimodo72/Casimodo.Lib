import { DestroyRef, Directive, Injector, Input, OnInit, inject } from "@angular/core"
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop"

import { MatStep, MatStepper } from "@angular/material/stepper"

import { StepModel, StepperModel } from "./models"

@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: "mat-stepper[ccStepper]",
    standalone: true,
})
export class CCStepperDirective implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #matStepper: MatStepper

    constructor(matStepper: MatStepper) {
        this.#matStepper = matStepper
    }

    @Input({ required: true }) ccStepper!: StepperModel

    ngOnInit(): void {
        this.ccStepper._matStepper = this.#matStepper

        // TODO: Do directives need to unsubscribe when subscribing to
        // the components they sit on?
        this.#matStepper.selectionChange
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(event =>
                this.ccStepper._onMatStepperSelectionChanged(event)
            )
    }
}

@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: "mat-step[ccStep]",
    standalone: true,
})
export class CCStepDirective implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #injector = inject(Injector)
    readonly #matStep: MatStep

    constructor(matStep: MatStep) {
        this.#matStep = matStep
    }

    @Input({ required: true }) ccStep!: StepModel

    ngOnInit(): void {
        this.ccStep._matStep = this.#matStep

        this.#matStep.label = this.ccStep.label()

        toObservable(this.ccStep.isCompleted, { injector: this.#injector })
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(isCompleted =>
                this.#matStep.completed = isCompleted
            )
    }
}
