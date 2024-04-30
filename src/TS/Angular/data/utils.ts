export function fixupReceivedDataDeep(data: any) {
    if (!data || typeof data !== "object") return

    if (Array.isArray(data)) {
        for (const item of data) {
            fixupReceivedDataDeep(item)
        }

        return
    }

    // Parse time fields by convention: time fields end with "On/Time/Date" and do not start with "Is".
    for (const member of Object.keys(data)) {
        const value = data[member]
        if (typeof value === "string" && value.length) {
            if ((member.endsWith("On") || member.endsWith("Date") || member.endsWith("Time")) && !member.startsWith("Is")) {
                // TODO: Should we suppress errors here?
                data[member] = new Date(value)
            }
        } else if (typeof value === "object") {
            fixupReceivedDataDeep(value)
        }
    }
}
