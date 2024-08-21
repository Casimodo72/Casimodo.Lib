import type { FormInputModel } from "@lib/models"

// This tries to mimic Angular's Signal stuff a bit because I don't know yet
// if I need this to be a "reactivity" thingy in the future.

// See computed signal implementation:
//     https://github.com/angular/angular/blob/main/packages/core/src/render3/reactivity/computed.ts
// and https://github.com/angular/angular/blob/main/packages/core/primitives/signals/src/computed.ts

export const DEFERRED_FORM_INPUT = Symbol("DEFERRED_FORM_INPUT")

interface DeferredFormInputNode<T extends FormInputModel> {
    getterFn: () => T
    value?: T
}

type DeferredFormInputGetter<T extends FormInputModel> = (() => T) & {
    [DEFERRED_FORM_INPUT]: DeferredFormInputNode<T>;
}

function createDeferredFormInput<T extends FormInputModel>(getterFn: () => T): DeferredFormInputGetter<T> {
    const node: DeferredFormInputNode<T> = Object.create({}) //COMPUTED_NODE);
    node.getterFn = getterFn

    const effectiveGetterFn = () => {
        if (node.value === undefined) {
            node.value = node.getterFn()
        }

        return node.value
    };
    (effectiveGetterFn as DeferredFormInputGetter<T>)[DEFERRED_FORM_INPUT] = node

    return effectiveGetterFn as unknown as DeferredFormInputGetter<T>
}

export type DeferredFormInput<T extends FormInputModel> = (() => T) & {
    [DEFERRED_FORM_INPUT]: unknown;
};

export function deferInput<T extends FormInputModel>(getterFn: () => T): DeferredFormInput<T> {
    const getter = createDeferredFormInput(getterFn)

    return getter
}
