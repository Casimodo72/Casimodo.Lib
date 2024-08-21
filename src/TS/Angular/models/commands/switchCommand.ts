import { signal } from "@angular/core"

import { IUICommandConfig, UICommand } from "../core"

export interface IUISwitchCommandConfig extends IUICommandConfig {
    isOn?: boolean
}
export class UISwitchCommand extends UICommand {
    // TODO: Can we use a symbol for the type?
    override readonly type = "switch"

    readonly isOn = signal(false)

    constructor(config: IUISwitchCommandConfig) {
        super(config)

        if (config.isOn) {
            this.isOn.set(true)
        }
    }

    toggle() {
        this.isOn.set(!this.isOn())

        this.trigger()
    }
}
