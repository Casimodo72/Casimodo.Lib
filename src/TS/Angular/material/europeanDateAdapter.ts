import { NativeDateAdapter } from "@angular/material/core"

export class EuropeanDateAdapter extends NativeDateAdapter {
    override getFirstDayOfWeek(): number {
        return 1
    }
}
