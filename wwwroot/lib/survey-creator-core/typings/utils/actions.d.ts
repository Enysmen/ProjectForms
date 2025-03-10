import { QuestionMatrixDynamicModel, IAction, MatrixDynamicRowModel, MatrixDropdownRowModelBase, Action } from "survey-core";
export declare function findAction(actions: Array<IAction>, id: string): IAction;
export declare function updateMatrixLogicExpandAction(question: QuestionMatrixDynamicModel, actions: Array<IAction>, row: MatrixDropdownRowModelBase): void;
export declare function updateMatrixLogicRemoveAction(question: QuestionMatrixDynamicModel, actions: Array<IAction>, row: MatrixDynamicRowModel): void;
export declare function updateMatrixRemoveAction(question: QuestionMatrixDynamicModel, actions: Array<IAction>, row: MatrixDynamicRowModel): void;
export declare function updateMatixActionsClasses(actions: Array<IAction>): void;
export declare class MenuButton extends Action {
    contentType: "icon" | "text-description-vertical";
    get buttonClassName(): string;
}
