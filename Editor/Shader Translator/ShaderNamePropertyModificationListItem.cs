using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_2019_1
using UnityEditor.UIElements;
#endif
namespace Thry.ThryEditor.ShaderTranslations
{
    public class ShaderNamePropertyModificationListItem : BindableElement
    {
        public ShaderNamePropertyModificationListItem()
        {
            var treeAsset = Resources.Load<VisualTreeAsset>("Shader Translator/PropertyModificationListItem");
            treeAsset.CloneTree(this);

            var actionTypeField = this.Q<EnumField>("actionType");
            var propertyNameField = this.Q<TextField>("propertyName");

            actionTypeField.RegisterValueChangedCallback(evt =>
            {
                if(evt.newValue == null)
                    return;

                var value = (ShaderNamePropertyModification.ActionType)evt.newValue;
                SetPropertyFieldVisible(propertyNameField, value == ShaderNamePropertyModification.ActionType.SetTargetPropertyValue);
            });

            EditorApplication.delayCall += () =>
            {
                var actionFieldValue = (ShaderNamePropertyModification.ActionType)actionTypeField.value;
                SetPropertyFieldVisible(propertyNameField, actionFieldValue == ShaderNamePropertyModification.ActionType.SetTargetPropertyValue);
            };
        }

        void SetPropertyFieldVisible(VisualElement field, bool isVisible)
        {
            field.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
