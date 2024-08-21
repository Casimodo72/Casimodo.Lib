import { ChangeDetectionStrategy, Component, Inject, OnInit, ViewContainerRef, computed, inject, signal, viewChild } from "@angular/core"
import { CommonModule } from "@angular/common"

import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog"

import { NotificationService } from "@lib/services"
import type {
    FormMode, IDialogFormArgs, IFormConfig, IFormResult, FormResultStatus,
    IInputOutputFormModel
} from "./formModels"

import type { IDialogForm } from "./dialogForms"
// NOTE: We import the dialog-service as type in order to avoid a circular dependency.
import type { DialogService } from "@lib/ui/dialogs/dialog.service"

// TODO: Check if dynamic components + passing data can be improved with Angular 16.2.
//   See https://blog.ninja-squad.com/2023/08/09/what-is-new-angular-16.2/
// NgComponentOutlet

/** Standard dialog hosting of dialog-forms. */
@Component({
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatIconModule, MatDialogModule, MatButtonModule],
    styles: [`
        :host {
            height: 100%;
        }
    `],
    templateUrl: "./dialog-form-dialog.component.html"
})
export class DialogFormDialog implements OnInit {
    /** NOTE: The dialog service field will be set by the DialogService itself
    (in order to avoid a circular dependency). */
    dialogService!: DialogService
    readonly #notificationService = inject(NotificationService)

    readonly component = signal<IDialogForm | null>(null)
    readonly vm = computed<IInputOutputFormModel | undefined>(() => this.component()?.getModel())
    readonly mode = signal<FormMode | "">("")
    readonly title = computed(() => {
        const component = this.component()
        const title = component?.dialogSettings?.title() ?? ""
        const dataDisplayName = component?.dialogSettings?.dataDisplayName() ?? ""
        const actionDisplayName = this.actionDisplayName()

        return title
            ? title
            // Produces ["My data name"] - ["add" | "edit"]
            : `${dataDisplayName} - ${actionDisplayName}`
    })
    readonly actionDisplayName = signal("")
    confirmButtonText = signal("")

    readonly dialogContentRef = viewChild("dialogContentRef", { read: ViewContainerRef })

    constructor(
        private readonly dialogRef: MatDialogRef<DialogFormDialog>,
        @Inject(MAT_DIALOG_DATA)
        private readonly args: IDialogFormArgs
    ) {
        if (args.mode) {
            this.mode.set(args.mode)
        }

        this.actionDisplayName.set(
            args.mode === "modify"
                ? "bearbeiten"
                : "hinzufügen")

        this.confirmButtonText.set(
            args.mode === "modify"
                ? "Speichern"
                : "Hinzufügen")
    }

    async ngOnInit() {
        try {
            const dialogContentRef = this.dialogContentRef()
            if (!dialogContentRef) {
                throw new Error("Dialog content reference not acquired.")
            }
            if (this.args.mode === "modify" && !this.args.data) {
                throw new Error("Form data is required in edit mode.")
            }

            const componentRef = dialogContentRef.createComponent<IDialogForm>(this.args.component)
            const component = componentRef.instance
            const vm = component.getModel()

            const formConfig: IFormConfig = {
                mode: this.args.mode,
                inputData: this.args.data
            }
            if (formConfig.mode) {
                vm.setMode(formConfig.mode)
            }
            vm.setInputData(formConfig.inputData)
            await vm.initializeForm(formConfig)

            if (component.dialogSettings.confirmButtonText) {
                this.confirmButtonText = component.dialogSettings.confirmButtonText
            }

            this.component.set(component)
        }
        catch (error) {
            this.#notificationService.showError(error)

            this.cancel()
        }
    }

    cancel() {
        const result: IFormResult = {
            hasSucceeded: false,
            status: "cancelled"
        }

        this.dialogRef.close(result)
    }

    async delete() {
        try {
            const message = "Sind Sie sicher, dass Sie diesen Eintrag löschen wollen?"
            // TODO: Let the model provide override the default deletionn confirmation message.
            const isDeletionConfirmed = await this.dialogService.confirm(
                {
                    message: message
                })
            if (!isDeletionConfirmed) {
                return
            }

            const vm = this.vm()!
            await vm.delete()
            const data = vm.outputData()

            const result: IFormResult = {
                hasSucceeded: true,
                status: "deleted",
                data: data
            }

            this.dialogRef.close(result)
        }
        catch (error) {
            this.#notificationService.showError(error)

            const result: IFormResult = {
                hasSucceeded: false,
                status: "failed"
            }

            this.dialogRef.close(result)
        }
    }

    async submit() {
        const vm = this.vm()!

        if (!await vm.validate()) {
            const errorMessage = vm.settings.validationErrorMessage
                ? vm.settings.validationErrorMessage
                : "Die Daten sind noch unvollständig/fehlerhaft. \n" +
                "Bereinigen Sie die Daten oder drücken Sie auf 'Abbrechen' " +
                "falls Sie die Eingabe verwerfen und das Formular schließen wollen."
            this.dialogService.showInfo(
                {
                    title: "Speichern noch nicht möglich",
                    message: errorMessage
                })

            return
        }

        if (!vm.isModified()) {
            this.dialogService.showInfo(
                {
                    title: "Speichern noch nicht möglich",
                    message: "Sie haben keine Änderungen vorgenommen. \n" +
                        "Nehmen Sie entweder Änderungen vor oder drücken Sie auf 'Abbrechen' " +
                        "falls Sie die Eingabe verwerfen und das Formular schließen wollen."
                })

            return
        }

        const mode = this.args.mode

        try {
            if (mode !== "add" && mode !== "modify" && mode !== "select") {
                throw new Error(`Invalid form mode for form-submit: '${mode}'.`)
            }

            let resultType: FormResultStatus = "failed"

            if (mode === "add") {
                await vm.submit()
                resultType = "added"
            } else if (mode === "modify") {
                await vm.submit()
                resultType = "modified"
            }
            else if (mode === "select") {
                await vm.submit()
                resultType = "selected"
            }

            const result: IFormResult = {
                hasSucceeded: true,
                status: resultType,
                data: vm.outputData()
            }

            this.dialogRef.close(result)
        }
        catch (error) {
            this.#notificationService.showError(error)

            const result: IFormResult = {
                hasSucceeded: false,
                status: "failed"
            }

            this.dialogRef.close(result)
        }
    }
}
