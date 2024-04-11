
import { Injectable, inject } from "@angular/core"
import {
    MatSnackBar,
    MatSnackBarHorizontalPosition,
    MatSnackBarVerticalPosition,
} from "@angular/material/snack-bar"

@Injectable({ providedIn: "root" })
export class NotificationService {
    #snackBar = inject(MatSnackBar)
    #horizontalPosition: MatSnackBarHorizontalPosition = "right"
    #verticalPosition: MatSnackBarVerticalPosition = "top"

    // TODO: Colors; the CSS does not work yet.

    showError(error?: string | unknown | Error) {
        let errorMessage = ""
        if (typeof error === "string") {
            errorMessage = error
        } else if (error instanceof Error) {
            errorMessage = error.message
        } else {
            errorMessage = "Ein unbekannter Fehler ist aufgetreten."
        }

        this.#snackBar.open(errorMessage, "Schließen", {
            horizontalPosition: this.#horizontalPosition,
            verticalPosition: this.#verticalPosition,
            duration: 5000,
            panelClass: ["app-mat-snackbar-error"],
        })
    }

    showWarning(warningMessage: string) {
        this.#snackBar.open(warningMessage, "Schließen", {
            horizontalPosition: this.#horizontalPosition,
            verticalPosition: this.#verticalPosition,
            duration: 5000,
            panelClass: ["app-mat-snackbar-warning"],
        })
    }

    showSuccess(successMessage: string) {
        this.#snackBar.open(successMessage, "Schließen", {
            horizontalPosition: this.#horizontalPosition,
            verticalPosition: this.#verticalPosition,
            duration: 5000,
            panelClass: ["app-mat-snackbar-success"]
        })
    }

    showInfo(infoMessage: string) {
        this.#snackBar.open(infoMessage, "Schließen", {
            horizontalPosition: this.#horizontalPosition,
            verticalPosition: this.#verticalPosition,
            duration: 5000,
            panelClass: ["app-mat-snackbar-info"]
        })
    }
}
