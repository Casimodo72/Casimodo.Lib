import { PropPath, StringKeys } from "@lib/data/utils"
import { FormInputModel, FormPartModel, IFormPart } from "@lib/models/core"
import { DEFERRED_FORM_INPUT } from "@lib/models/inputs/deferredFormInput"

// export interface IEntityFormModel<TData = any> extends IInputOutputFormModel<TData, TData> {
// }

export interface IFormPartVisitorContext {
    readonly targetProp?: PropPath
    readonly input: FormInputModel
}

export type FormPartVisitFn = (context: IFormPartVisitorContext) => Promise<void>

export async function visitInputModels(propPath: PropPath | undefined, formPart: IFormPart, visit: FormPartVisitFn) {
    for (const key of Object.keys(formPart)) {
        let prop = (formPart as any)[key]

        if (prop[DEFERRED_FORM_INPUT]) {
            // Get deferred form input model.
            prop = prop()
        }

        if (prop instanceof FormInputModel) {
            const inputModel = prop as FormInputModel
            const targetProp = inputModel.targetProp

            // TODO: REMOVE
            // if (!targetProp) {
            //     throw new Error("Form: target property of input model is not assigned.")
            // }

            const absolutePropPath = targetProp
                ? propPath?.segments?.length
                    ? new PropPath([...propPath.segments, ...targetProp.segments])
                    : targetProp
                : undefined

            await visit({
                input: inputModel,
                targetProp: absolutePropPath,
            })
        }
        else if (prop instanceof ChildEntityFormModel) {
            const childPart = prop as ChildEntityFormModel
            if (!childPart.targetProp) {
                throw new Error("Form: target property of child part is not assigned.")
            }

            const absolutePropPath = propPath?.segments?.length
                ? new PropPath([...propPath.segments, ...childPart.targetProp.segments])
                : childPart.targetProp

            await visitInputModels(absolutePropPath, childPart, visit)
        }
    }
}

export class EntityFormPartModel extends FormPartModel {
    async _visitInputModelsAsync(visit: FormPartVisitFn) {
        await visitInputModels(undefined, this, visit)
    }
}

export class ChildEntityFormModel<TSource = any> extends FormPartModel {
    readonly targetProp: PropPath

    constructor(parent: IFormPart, target: StringKeys<TSource>) {
        super(parent)

        this.targetProp = PropPath.fromSelection(target)
    }
}
