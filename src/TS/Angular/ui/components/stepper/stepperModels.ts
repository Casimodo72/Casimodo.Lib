import { WritableSignal, signal } from "@angular/core"

import { MatStep, MatStepper } from "@angular/material/stepper"

import { AsyncVoidFunction, SignalHelper } from "@lib/utils"
import { FormPartModel } from "@lib/models"

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
    }

    async moveToNext() {
        await this.stepper.moveToNext()
    }

    async moveToPrevious() {
        await this.stepper.moveToPrevious()
    }

    async completeAndNext(nextStep: StepModel) {
        if (nextStep === this) return

        await this.stepper.complete(this)
        this.stepper.select(nextStep)
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

export interface ISelectedMatStepChangedEvent {
    readonly selectedStep: MatStep
    readonly selectedIndex: number
}

export class StepperModel extends FormPartModel {
    _matStepper: MatStepper | null = null
    readonly #steps = signal<StepModel[]>([])
    readonly current = signal<StepModel | null>(null)
    //readonly selectedMapStep = signal<MatStep | null>(null)

    initialize() {

    }

    async moveToNext() {
        const current = this.current()
        if (!current || !this._matStepper) return

        const currentStepIndex = this._matStepper.selectedIndex
        const matSteps = this._matStepper.steps
        const nextMapStep = matSteps.get(currentStepIndex + 1)
        if (!nextMapStep) return

        const nextStep = this.#findModelForMatStep(nextMapStep)
        if (!nextStep) return

        await this.complete(current)

        this.select(nextStep)
    }

    async moveToPrevious() {
        const current = this.current()
        if (!current || !this._matStepper) return

        const currentStepIndex = this._matStepper.selectedIndex
        const matSteps = this._matStepper.steps
        const prevMapStep = matSteps.get(currentStepIndex - 1)
        if (!prevMapStep) return

        const prevStep = this.#findModelForMatStep(prevMapStep)
        if (!prevStep) return

        await this.complete(current)

        this.select(prevStep)
    }

    #findModelForMatStep(matStep: MatStep) {
        return this.#steps().find(x => x._matStep === matStep)
    }

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
    _onMatStepperSelectionChanged(event: ISelectedMatStepChangedEvent): void {
        const selectedStep = this.#steps().find(step => step._matStep === event.selectedStep)
        if (!selectedStep) return

        selectedStep.isCurrent.set(true)
        this.current.set(selectedStep)

        for (const step of this.#steps()) {
            if (step !== selectedStep) {
                step.isCurrent.set(false)
            }
        }

        // Not awaiting on purpose.
        selectedStep._onSelected()
    }
}
