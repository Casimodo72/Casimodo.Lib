import { Directive, computed, input } from "@angular/core"
import { MatFormFieldAppearance, SubscriptSizing } from "@angular/material/form-field"

import type { FormInputModel } from "@lib/models"

type NgClassType = string | string[] | Set<string> | null | undefined | {
    [klass: string]: any;
}

type NgStyleType = null | undefined | {
    [klass: string]: any;
}

@Directive()
export abstract class StandardInputComponent<TInputModel extends FormInputModel> {
    readonly model = input.required<TInputModel>()
    readonly label = input("")
    readonly required = input<boolean>()
    readonly viewOnly = input(false, { transform: (value: undefined | boolean | string) => value == null || `${value}` !== "false" })
    readonly disabled = input<boolean>()
    readonly appearance = input<MatFormFieldAppearance>("outline")
    readonly groupClass = input<NgClassType>()
    readonly groupStyle = input<NgStyleType>()
    readonly errorSubscriptSizing = input<SubscriptSizing>()
    readonly stretch = input(false, { transform: (value: undefined | boolean | string) => value == null || `${value}` !== "false" })
    readonly flexStretch = input(false, { transform: (value: undefined | boolean | string) => value == null || `${value}` !== "false" })
    readonly width = input<string>()

    protected readonly effectiveLabel = computed(() => this.label() || (this.model()?.label ?? ""))
    protected readonly effectiveIsViewOnly = computed(() => this.viewOnly())
    protected readonly effectiveIsReadOnly = computed(() => this.model().isReadOnly() || this.effectiveIsViewOnly())
    protected readonly effectiveIsDisabled = computed(() => !!this.disabled() || this.model().isDisabled())
    protected readonly effectiveLabelMode = computed(() => this.effectiveIsReadOnly() || this.effectiveIsDisabled() ? "always" : "auto")
    protected readonly effectiveCanFocus = computed(() => !this.effectiveIsReadOnly())
    protected readonly effectiveIsRequired = computed(() => !!this.required() || this.model().isRequired())
    protected readonly effectiveAppearance = computed(() => {
        return this.effectiveIsViewOnly()
            ? "fill"
            : this.appearance() ?? "outline"
    })
    protected readonly effectiveGroupClass = computed(() => this.groupClass())
    protected readonly effectiveGroupStyle = computed(() => {
        const effectiveGroupStyle = this.groupStyle() ?? {}

        if (this.flexStretch()) {
            effectiveGroupStyle["width"] = "100%"
        }
        else if (this.#effectiveWidth()) {
            effectiveGroupStyle["width"] = this.#effectiveWidth()
        }

        return effectiveGroupStyle
    })
    protected readonly effectiveErrorSubscriptSizing = computed(() => this.errorSubscriptSizing() ?? "dynamic")
    readonly #effectiveWidth = computed(() => this.width() || (this.stretch() ? "100%" : undefined))
}
