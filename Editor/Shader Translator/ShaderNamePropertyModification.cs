using System;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.ShaderTranslations
{
    [Serializable]
    public class ShaderNamePropertyModification
    {
        public enum ConditionOperator
        {
            Equals,
            Contains,
            StartsWith,
            EndsWith,
        }

        public enum ActionType
        {
            ChangeTargetShader,
            SetTargetPropertyValue,
        }

        [SerializeField] string shaderNameMatch;
        public ConditionOperator conditionOperator;

        public ActionType actionType;
        public string propertyName;
        public string targetValue;

        public bool IsShaderNameMatch(string name)
        {
            return conditionOperator switch
            {
                ConditionOperator.Equals => name.Equals(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase),
                ConditionOperator.Contains => name.Contains(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase),
                ConditionOperator.StartsWith => name.StartsWith(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase),
                ConditionOperator.EndsWith => name.EndsWith(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase),
                _ => false
            };
        }
    }
}