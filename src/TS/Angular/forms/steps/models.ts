import { WritableSignal, signal } from "@angular/core"

import { MatStep, MatStepper } from "@angular/material/stepper"
import { StepperSelectionEvent } from "@angular/cdk/stepper"

import { AsyncVoidFunction, SignalHelper } from "@lib/utils"

export class StepModel {
    _matStep: MatStep | null = null
    #onSelected?: AsyncVoidFunction
    readonly stepper: StepperModel
    readonly label: WritableSignal<string>
    readonly isCurrent = signal(false)
    readonly isCompleted = signal(false)
    readonly isEnabled = signal(true)

    constructor(stepper: StepperModel, label?: string) {
        this.stepper = stepper
        this.label = signal(label ?? "")

        this.stepper._addStep(this)
        this.stepper._onMatStepperSelectionChanged
    }

    setEnabled(enabled: boolean): this {
        this.isEnabled.set(enabled)

        return this
    }

    setOnSelected(onSelected: AsyncVoidFunction | undefined): this {
        this.#onSelected = onSelected

        return this
    }

    /** Called by the StepperModel only. */
    async _onSelected() {
        await this.#onSelected?.()
    }
}

export class StepperModel {
    _matStepper: MatStepper | null = null
    readonly #steps = signal<StepModel[]>([])
    readonly current = signal<StepModel | null>(null)
    //readonly selectedMapStep = signal<MatStep | null>(null)

    /**
     * This method needs to be async because Material's stepper will not react
     * when selecting a step immediately after completing a step.
     * One needs to use a timeout between completion and selection.
     */
    async complete(step: StepModel) {
        await new Promise(resolve => setTimeout(
            () => {
                step.isCompleted.set(true)
                resolve(undefined)
            },
            1)
        )
    }

    select(step: StepModel): void {
        if (this._matStepper && step._matStep) {
            this._matStepper!.selected = step._matStep
        }
    }

    _addStep(step: StepModel): void {
        SignalHelper.push(this.#steps, step)
    }

    /** To be called by the directives only. */
    _onMatStepperSelectionChanged(event: StepperSelectionEvent): void {
        const selectedMatStep = event.selectedStep as unknown as MatStep

        const foundStep = this.#steps().find(step => step._matStep === selectedMatStep)
        if (foundStep) {
            //this.selectedMapStep.set(selectedMatStep)
            foundStep.isCurrent.set(true)
            this.current.set(foundStep)

            for (const step of this.#steps()) {
                if (step !== foundStep) {
                    step.isCurrent.set(false)
                }
            }

            // Not awaiting on purpose.
            foundStep._onSelected()
        }
    }
}
