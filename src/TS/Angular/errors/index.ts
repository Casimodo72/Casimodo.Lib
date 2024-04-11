
export class UserNotifiableError extends Error {
    readonly errorCode?: string

    constructor(message: string, errorCode?: string) {
        super()
        this.name = "UserNotifiableError"
        this.message = message
        this.errorCode = errorCode
    }
}
