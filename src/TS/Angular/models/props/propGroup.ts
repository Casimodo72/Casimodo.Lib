// TODO: REMOVE file

// import { signal } from "@angular/core"

// import { PropItem } from "./prop"
// import { ListModel } from "../lists"
// import { IItemModel } from "../items"
// import { IPropItem } from "./core"

// export class PropGroupListItem extends PropItem implements IItemModel {
//     readonly id: string
//     protected readonly _isSelected = signal(false)
//     readonly isSelected = this._isSelected.asReadonly()
//     readonly canChangeSelection = signal(true)
//     readonly isCurrent = signal(false)

//     constructor(id?: string) {
//         super()

//         this.id = id ?? crypto.randomUUID()
//     }

//     /**
//      * @inheritdoc
//      */
//     setIsSelected(isSelected: boolean): boolean {
//         if (!this.canChangeSelection() || this._isSelected() === isSelected) {
//             return false
//         }

//         this._isSelected.set(isSelected)

//         return true
//     }
// }

// export class PropGroupList<TItem extends PropGroupListItem> extends ListModel<TItem> {
//     readonly parent: IPropItem

//     constructor(parent: IPropItem) {
//         super()

//         this.parent = parent
//     }
// }
