import { signal } from "@angular/core"

import { FormModel } from "@lib/forms"
import { StringFormProp } from "@lib/models"

export class LoginFormModel extends FormModel {
    readonly username = new StringFormProp(this)
        .setLabel("Benutzername")
        .setRules(r => r
            .required("Der Benutzername wird benötigt.")
            .min(6, "Der Benutzername muss mindestest 6 Zeichen lang sein."))

    readonly password = new StringFormProp(this)
        .setLabel("Passwort")
        .setRules(r => r
            .required("Das Passwort wird benötigt.")
            .min(6, "Das Passwort muss mindestest 6 Zeichen lang sein."))

    readonly isPasswordHidden = signal(true)

    togglePasswordVisibility() {
        this.isPasswordHidden.set(!this.isPasswordHidden())
    }

    // readonly test = new StringProp(this, "")
    //     .setLabel("IBAN")
    //     .setRules(r => r
    //         .rule(createIbanRule({ countryCode: "DE" }))
    //         .custom({
    //             validate: (_) => {
    //                 return this.test.value()?.startsWith("DE")
    //                     ? null
    //                     : "Die IBAN muss mit DE beginnen."
    //             }
    //         }))
}
