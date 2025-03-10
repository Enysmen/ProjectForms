import { QuestionDropdownModel, SurveyElement, SurveyTemplateRendererTemplateData, ItemValue } from "survey-core";
import { SurveyCreatorModel } from "../creator-base";
import { QuestionAdornerViewModel } from "./question";
export declare class QuestionDropdownAdornerViewModel extends QuestionAdornerViewModel {
    private isCollapsed;
    private visibleCount;
    constructor(creator: SurveyCreatorModel, surveyElement: SurveyElement, templateData: SurveyTemplateRendererTemplateData);
    get question(): QuestionDropdownModel;
    get itemComponent(): string;
    get needToCollapse(): boolean;
    get isCollapseView(): boolean;
    leftFocus(): void;
    getChoiceCss(): string;
    getRenderedItems(): ItemValue[];
    getButtonText(): string;
    switchCollapse: () => void;
    attachElement(surveyElement: SurveyElement): void;
    detachElement(surveyElement: SurveyElement): void;
}
