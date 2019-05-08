namespace cmodo {
    export interface GlobalAuthInfo {
        userId: string;
        userName: string;
    }
}

namespace cmodo.run {
    export let authInfo: GlobalAuthInfo = {
        userId: null,
        userName: null
    };

    export let environment = {
        companyId: null
    }
}

namespace cmodo {
    export function getGlobalAuthInfo(): GlobalAuthInfo {
        return run.authInfo;
    }

    export function getGlobalInitialCompanyId(): string | null {
        return null;
    }
}

