import { DateTime } from "luxon"
import { Directive, Injector, inject } from "@angular/core"

import { DialogService } from "@lib/ui/dialogs/dialog.service"
import { EntityPickerModel } from "@lib/models/inputs/entityPickerModel"
import { EntityLookupModel } from "@lib/models/inputs/entityLookupModel"
import { AnyLuxonDateTimeInputModel } from "@lib/models/inputs/dateTimeInputModels"

import { FormPartVisitFn, visitInputModels } from "./entityFormModels"
import { InputOutputForm } from "./inputOutputFormComponent"

export function toJsDate(date?: DateTime | null): Date | null {
    return date
        ? date.toJSDate()
        : null
}

@Directive()
export abstract class EntityForm<TEntity = any> extends InputOutputForm<TEntity, TEntity> {
    protected readonly _injector = inject(Injector)
    protected readonly _dialogService = inject(DialogService)

    override async initializeForm() {
        await super.initializeForm()

        await this._visitInputModelsAsync(async ctx => {
            if (!ctx.targetProp) {
                throw new Error("Form: target property of input model is not assigned.")
            }

            ctx.input._setInjector(this._injector)

            if (ctx.input.isInactiveOnModify && this.mode === "modify") {
                ctx.input._setIsValidationDisabled()
                ctx.input.setIsReadOnly()
                if (ctx.input.isRequired()) {
                    ctx.input.setRules(r => r.notRequired())
                }
            }
        })
    }

    async assignFrom(sourceObject: object) {
        await this._visitInputModelsAsync(async ctx => {
            const value = ctx.targetProp?.getValue(sourceObject) ?? null

            if (value != null) {
                // Date-times
                if (ctx.input instanceof AnyLuxonDateTimeInputModel) {
                    if (value instanceof Date) {
                        const luxonDateTime = DateTime.fromJSDate(value)
                        ctx.input.setValue(luxonDateTime)
                    }
                    else if (DateTime.isDateTime(value)) {
                        ctx.input.setValue(value)
                    }
                    else {
                        throw new Error("Form error: Date-time value is neigher a Luxon DateTime nor a JS Date.")
                    }
                }
                // Entity references
                else if (
                    ctx.input instanceof EntityPickerModel ||
                    ctx.input instanceof EntityLookupModel
                ) {
                    if (!ctx.input.valueIdProp) {
                        throw new Error("Form error: Value property not assigned on input model.")
                    }

                    await ctx.input.setValueById(value)
                }
                else {
                    ctx.input.setValue(value)
                }
            }
        })
    }

    async assignTo(targetObject: object) {
        await this._visitInputModelsAsync(async ctx => {
            if (!ctx.targetProp) {
                throw new Error("Form: target property of input model is not assigned.")
            }
            let value: any = null

            // Date-times
            if (ctx.input instanceof AnyLuxonDateTimeInputModel) {
                const luxonDateTime = ctx.input.value()
                if (luxonDateTime) {
                    value = luxonDateTime.toJSDate()
                }
            }
            // Entity references
            else if (ctx.input instanceof EntityPickerModel ||
                ctx.input instanceof EntityLookupModel
            ) {
                if (!ctx.input.valueIdProp) {
                    throw new Error("Form error: Value property not assigned on input model.")
                }

                const referencedEntity = ctx.input.value()
                if (referencedEntity) {
                    // Assign the foreign ID.
                    value = ctx.input.valueIdProp.getValue(ctx.input.value()) ?? null
                    if (!value) {
                        throw new Error("Form error: Referenced entity is assigned but its ID is not assigned.")
                    }
                }
            }
            else {
                value = ctx.input.value()
            }

            const segments = ctx.targetProp.segments
            if (segments.length === 1) {
                (targetObject as any)[segments[0]] = value
            }
            else if (segments.length > 1) {
                // We have descendant data objects if there are multiple prop-path segments.
                // Ensure descendant objects exist.
                let contextObject: any = targetObject as any
                for (let i = 0; i < segments.length - 1; i++) {
                    let childObject = contextObject[segments[i]]
                    if (childObject == null) {
                        childObject = {}
                        contextObject[segments[i]] = childObject
                    }

                    contextObject = childObject
                }

                contextObject[segments[segments.length - 1]] = value
            }
        })
    }

    async _visitInputModelsAsync(visit: FormPartVisitFn) {
        await visitInputModels(undefined, this, visit)
    }
}
