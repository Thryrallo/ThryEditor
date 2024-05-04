using UnityEngine;
using UnityEngine.UIElements;

namespace Thry.ThryEditor.ShaderTranslations
{
    public class TranslatorListItem : BindableElement
    {
        public TextField sourceField;
        public TextField targetField;
        public TextField expressionField;
        public Toggle advancedToggle;
        public VisualElement conditionalContainer;
        public ListView conditionalList;

        public TranslatorListItem()
        {
            var uxml = Resources.Load<VisualTreeAsset>("Thry/TranslatorListItem");
            uxml.CloneTree(this);

            sourceField = this.Q<TextField>("sourceProperty");
            targetField = this.Q<TextField>("targetProperty");
            expressionField = this.Q<TextField>("mathExpression");
            advancedToggle = this.Q<Toggle>("advancedToggle");
            conditionalContainer = this.Q<VisualElement>("conditionalContainer");

            advancedToggle.RegisterValueChangedCallback((evt) =>
                SetContainerVisibleAndTextFieldDisabled(conditionalContainer, expressionField, evt.newValue));

            SetContainerVisibleAndTextFieldDisabled(conditionalContainer, expressionField, advancedToggle.value);

            conditionalList = this.Q<ListView>("conditionalList");
            conditionalList.makeItem = () => new ConditionalTranslationBlockListItem();
        }

        void SetContainerVisibleAndTextFieldDisabled(VisualElement container, TextField textField, bool isVisible)
        {
            container.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            UIElementsHelpers.SetTextFieldReadonly(textField, isVisible);
        }
    }
}