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
            switch(conditionOperator)
            {
                case ConditionOperator.Equals:
                    return name.Equals(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase);
                case ConditionOperator.Contains:
                    return name.IndexOf(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase) != -1;
                case ConditionOperator.StartsWith:
                    return name.StartsWith(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase);
                case ConditionOperator.EndsWith:
                    return name.EndsWith(shaderNameMatch, StringComparison.CurrentCultureIgnoreCase);
            }
            return false;
        }
    }
}