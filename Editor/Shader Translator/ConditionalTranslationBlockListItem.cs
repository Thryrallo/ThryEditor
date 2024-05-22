using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Thry.ThryEditor.ShaderTranslations
{

    public class ConditionalTranslationBlockListItem : BindableElement
    {
#if UNITY_2022_1_OR_NEWER
        EnumField statementField;
        TextField conditionField;

        public ConditionalTranslationBlockListItem()
        {
            var uxml = Resources.Load<VisualTreeAsset>("Thry/TranslatorConditionalListItem");
            uxml.CloneTree(this);

            statementField = this.Q<EnumField>("statementEnum");
            conditionField = this.Q<TextField>("conditionText");

            statementField.RegisterValueChangedCallback(evt => HandleConditionFieldReadonly(statementField, conditionField));
            EditorApplication.delayCall += () =>
            {
                HandleConditionFieldReadonly(statementField, conditionField); 
            };
        }

        void HandleConditionFieldReadonly(EnumField statementField, TextField conditionField)
        {
            if(statementField.value == null)
                return;

            bool isReadOnly = (ConditionalTranslationBlock.ConditionalBlockType)statementField.value == ConditionalTranslationBlock.ConditionalBlockType.Else;
            UIElementsHelpers.SetTextFieldReadonly(conditionField, isReadOnly);
        }
#endif
    }
}