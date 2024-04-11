import { Pipe, PipeTransform } from "@angular/core"
import { DateTime, DateTimeFormatOptions } from "luxon"

@Pipe({
    name: "luxonDate",
    standalone: true,
})
export class LuxonDatePipe implements PipeTransform {
    transform(
        value: DateTime | null | undefined,
        format: DateTimeFormatOptions = DateTime.DATE_SHORT,
    ): string {
        if (!value) return ""

        return value.toLocaleString(format)
    }
}

@Pipe({
    name: "luxonTime",
    standalone: true,
})
export class LuxonTimePipe implements PipeTransform {
    transform(
        value: DateTime | null | undefined,
        format: DateTimeFormatOptions = DateTime["TIME_24_SIMPLE"],
    ): string {
        if (!value) return ""

        return value.toLocaleString(format)
    }
}
