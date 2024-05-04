using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Thry
{
    public class ExpressionParser
    {
        public static Expression<Func<bool>> Parse(string expression, object variableValue = null)
        {
            expression = SanitizeExpressionAddVariable(expression, variableValue);

            // Parse the expression string into an expression tree
            Expression body = ParseExpression(expression);

            // Create a lambda expression with no parameters and parsed body
            return Expression.Lambda<Func<bool>>(body);
        }

        static string SanitizeExpressionAddVariable(string expression, object variableValue = null)
        {
            StringBuilder sb = new StringBuilder(expression.ToLowerInvariant());
            sb.Replace(" ", "");
            sb.Replace(',', '.');
            sb.Replace("\t", "");

            if(variableValue != null)
                sb.Replace("x", variableValue.ToString());

            return sb.ToString();
        }

        private static Expression ParseExpression(string expression)
        {
            // Split the expression into tokens
            string[] tokens = Tokenize(expression);

            // Initialize the expression stack and operator stack
            var stack = new Stack<Expression>();
            var operatorStack = new Stack<string>();

            foreach(string token in tokens)
            {
                if(IsBooleanOperator(token) || IsMathOperator(token))
                {
                    while(operatorStack.Count > 0 && GetPrecedence(token) <= GetPrecedence(operatorStack.Peek()))
                    {
                        ApplyOperator(stack, operatorStack.Pop());
                    }
                    operatorStack.Push(token);
                }
                else if(IsNumeric(token))
                {
                    stack.Push(Expression.Constant(Convert.ToDouble(token)));
                }
                else if(IsBoolean(token))
                {
                    stack.Push(Expression.Constant(Convert.ToBoolean(token)));
                }
                else if(token == "(")
                {
                    operatorStack.Push(token);
                }
                else if(token == ")")
                {
                    while(operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    {
                        ApplyOperator(stack, operatorStack.Pop());
                    }
                    if(operatorStack.Count == 0 || operatorStack.Pop() != "(")
                    {
                        throw new ArgumentException("Invalid expression");
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid token: {token}");
                }
            }

            while(operatorStack.Count > 0)
            {
                ApplyOperator(stack, operatorStack.Pop());
            }

            if(stack.Count != 1)
            {
                throw new ArgumentException("Invalid expression");
            }

            return stack.Pop();
        }

        private static string[] Tokenize(string expression)
        {
            // Split the expression into tokens
            List<string> tokens = new List<string>();
            string token = "";

            for(int i = 0; i < expression.Length; i++)
            {
                char ch = expression[i];
                if(char.IsWhiteSpace(ch))
                {
                    if(!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                        token = "";
                    }
                }
                else if(IsOperator(ch))
                {
                    if(!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                        token = "";
                    }
                    tokens.Add(ch.ToString());
                }
                else if(IsParenthesis(ch))
                {
                    if(!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                        token = "";
                    }
                    tokens.Add(ch.ToString());
                }
                else
                {
                    token += ch;
                    // Check for positive/negative numbers
                    if(i < expression.Length - 1 && (expression[i + 1] == '+' || expression[i + 1] == '-'))
                    {
                        token += expression[i + 1];
                        i++;
                    }
                }
            }

            if(!string.IsNullOrEmpty(token))
            {
                tokens.Add(token);
            }

            return tokens.ToArray();
        }

        private static bool IsOperator(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '^';
        }

        private static bool IsParenthesis(char c)
        {
            return c == '(' || c == ')';
        }

        private static void ApplyOperator(Stack<Expression> stack, string op)
        {
            if(stack.Count < 2)
            {
                throw new ArgumentException("Invalid expression");
            }

            Expression right = stack.Pop();
            Expression left = stack.Pop();
            stack.Push(CreateExpression(left, right, op));
        }

        private static Expression CreateExpression(Expression left, Expression right, string op)
        {
            switch(op)
            {
                case "+":
                    return Expression.Add(left, right);
                case "-":
                    return Expression.Subtract(left, right);
                case "*":
                    return Expression.Multiply(left, right);
                case "/":
                    return Expression.Divide(left, right);
                case "%":
                    return Expression.Modulo(left, right);
                case "^":
                    return Expression.Power(left, right);
                case "==":
                    return Expression.Equal(left, right);
                case "!=":
                    return Expression.NotEqual(left, right);
                case "<":
                    return Expression.LessThan(left, right);
                case "<=":
                    return Expression.LessThanOrEqual(left, right);
                case ">":
                    return Expression.GreaterThan(left, right);
                case ">=":
                    return Expression.GreaterThanOrEqual(left, right);
                case "&&":
                    return Expression.AndAlso(left, right);
                case "||":
                    return Expression.OrElse(left, right);
                default:
                    throw new ArgumentException($"Invalid operator: {op}");
            }
        }

        private static bool IsBooleanOperator(string token)
        {
            return token == "==" || token == "!=" || token == "<" || token == "<=" || token == ">" || token == ">=" ||
                   token == "&&" || token == "||";
        }

        private static bool IsMathOperator(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/" || token == "%" || token == "^";
        }

        private static bool IsNumeric(string token)
        {
            return double.TryParse(token, out _);
        }

        private static bool IsBoolean(string token)
        {
            return token == "true" || token == "false";
        }

        private static int GetPrecedence(string op)
        {
            switch(op)
            {
                case "||":
                    return 1;
                case "&&":
                    return 2;
                case "==":
                case "!=":
                    return 3;
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return 4;
                case "+":
                case "-":
                    return 5;
                case "*":
                case "/":
                case "%":
                case "^":
                    return 6;
                default:
                    return 0;
            }
        }
    }
}