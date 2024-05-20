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
                Debug.Log($"<b>{Origin}</b> -> <b>{Target}</b> uses conditional expressions but has one or more empty conditional block. Returning math expression <b>{Math}</b>");
                return Math;
            }

            foreach(ConditionalTranslationBlock block in ConditionalProperties)
            {
                if(block.ConditionType == ConditionalTranslationBlock.ConditionalBlockType.If)
                {
                    Delegate parsedExpression = ExpressionParser.Parse(block.ConditionalExpression);
                    bool? result = null;

                    // Check if the delegate is a Func<double, bool>
                    if(parsedExpression is Func<double, bool> expressionWithParameter)
                    {
                        result = expressionWithParameter(value);
                    }
                    else if(parsedExpression is Func<bool> expressionWithoutParameter)
                    {
                        result = expressionWithoutParameter();
                    }

                    if((bool)result)
                    {
                        Debug.Log($"<b>{Origin}</b> -> <b>{Target}</b>: <b>if</b> conditional <b>{block.ConditionalExpression}</b> returned math expression <b>{block.MathExpression}</b>");
                        return block.MathExpression;
                    }
                }
                else
                {
                    Debug.Log($"<b>{Origin}</b> -> <b>{Target}</b>: <b>else</b> returned math expression <b>{block.MathExpression}</b>");
                    return block.MathExpression;
                }
            }
            return Math;
        }
    }
}