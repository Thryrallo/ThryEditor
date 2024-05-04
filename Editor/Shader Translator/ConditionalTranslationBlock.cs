using System;

namespace Thry.ThryEditor.ShaderTranslations
{
    [Serializable]
    public class ConditionalTranslationBlock
    {
        public enum ConditionalBlockType
        {
            If,
            Else
        }

        public ConditionalBlockType ConditionType;
        public string ConditionalExpression;
        public string MathExpression;

        public bool IsValid => ConditionType == ConditionalBlockType.Else || !string.IsNullOrWhiteSpace(ConditionalExpression);
    }
}