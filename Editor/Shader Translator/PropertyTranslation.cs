using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Thry.ThryEditor.ShaderTranslations
{
    [Serializable]
    public partial class PropertyTranslation
    {
        public string Origin;
        public string Target;
        public string Math;
        public bool UseConditionals;

        public List<ConditionalTranslationBlock> ConditionalProperties;

        public bool HasValidConditionals
            => ConditionalProperties != null && ConditionalProperties.Count > 0 && ConditionalProperties.All(x => x.IsValid);


        public string GetAppropriateExpression(float value)
        {
            if(!UseConditionals)
                return Math;

            if(!HasValidConditionals)
            {
                Debug.Log($"Property Translation <b>{Origin}</b> -> <b>{Target}</b> uses conditional expressions but has one or more empty conditional block. Returning math expression <b>{Math}</b>");
                return Math;
            }

            foreach(ConditionalTranslationBlock block in ConditionalProperties)
            {
                if(block.ConditionType == ConditionalTranslationBlock.ConditionalBlockType.If)
                {
                    if(ExpressionParser.Parse(block.ConditionalExpression, value).Compile().Invoke())
                    {
                        Debug.Log("Returning <b>if</b> condition expression " + block.MathExpression);
                        return block.MathExpression;
                    }
                }
                else
                {
                    Debug.Log("Returning <b>else</b> condition expression " + block.MathExpression);
                    return block.MathExpression;
                }
            }
            return Math;
        }
    }
}