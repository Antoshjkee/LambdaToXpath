﻿using LambdaToXpath.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LambdaToXpath
{
    public class XpathExpressionParser
    {
        private static string extractMethodArguments = @"(?<=\().+?(?=\))";

        public static void ParseExpression(BinaryExpression chain, Element element)
        {
            if (chain.Left is BinaryExpression)
            {
                Parse(element, ParseSubExpressions(chain.Right).ToString());

                ParseExpression((BinaryExpression)chain.Left, element);
            }
            else
            {
                Parse(element, string.Format("{0} == {1}", chain.Left.ToString(), ParseSubExpressions(chain.Right).ToString()));
            }
        }

        private static object ParseSubExpressions(Expression operation)
        {
            if(operation.NodeType == ExpressionType.Constant)
            {
                return ((ConstantExpression)operation).Value;
            }

            else if (operation.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression me = (MemberExpression)operation;

                if (me.Expression.Type == typeof(Model.Attribute))
                {
                    return ElementParser.ParseAttribute(me);
                }

                //Don't execute functions defined on internal objects 
                if (me.Member.Module.Assembly.FullName == typeof(XpathExpressionParser).Assembly.FullName)
                {
                    return operation;
                }

                //Evaluate values of variables/properties
                object instance = ParseSubExpressions(me.Expression);
                if (instance != null)
                {
                    return ElementParser.ResolveFieldOrProperty(me, instance);
                }

                return operation.ToString();
            }
            else if (operation.NodeType == ExpressionType.Call)
            {
                MethodCallExpression me = (MethodCallExpression)operation;

                if (me.Object != null && me.Object.Type == typeof(Model.Attribute))
                {
                    return ElementParser.ParseAttribute(me);
                }

                return Expression.Lambda(me).Compile().DynamicInvoke();
            }

            else if (operation.NodeType == ExpressionType.Equal)
            {
                BinaryExpression be = (BinaryExpression)operation;

                var e = string.Format("{0} == {1}", ParseSubExpressions(be.Left).ToString(), ParseSubExpressions(be.Right).ToString());
                return e;
            }

            else if (operation.NodeType == ExpressionType.Convert)
            {
                var value = Expression.Lambda(operation).Compile().DynamicInvoke();
                return Expression.Convert(Expression.Constant(value),value.GetType());
            }

            return null;
        }

        public static void Parse(Element element, string expressionPart)
        {
            bool done = ParseParent(element,expressionPart);

            if (done == false)
            {
                done = ParseContextElement(element, expressionPart);
            }
            if (done == false)
            {
                done = ParseSiblings(element, expressionPart);
            }
            if (done == false)
            {
                done = ParsePosition(element, expressionPart);
            }
            if (done == false)
            {
                done = ParseRelatives(element,expressionPart);
            }
        }

        private static bool ParseSiblings(Element element, string expressionPart)
        {
            if (expressionPart.Contains(".FollowingSibling.Name") == true)
            {
                element.Siblings.Add(new Sibling() { Name = GetValue(expressionPart) });
                return true;
            }
            else if (expressionPart.Contains(".PrecedingSibling.Name") == true)
            {
                element.Siblings.Add(new Sibling() { Name = GetValue(expressionPart),Preceding = true });
                return true;
            }

            return false;
        }

        private static bool ParseContextElement(Element element, string expressionPart)
        {
            if (expressionPart.Contains(".TargetElementName") == true)
            {
                element.TargetElementName = GetValue(expressionPart);
                return true;
            }
            else if (Regex.IsMatch(expressionPart,"Attribute(.*)\\.Text") == true)
            {
                var keyVal = GetAttributeNameAndValue(expressionPart);
                element.Attributes.Add(new Model.Attribute(keyVal.Key) { Text = CleanUp(keyVal.Value),ExactMatch = true });
                return true;
            }
            else if (Regex.IsMatch(expressionPart, "Attribute(.*)\\.Contains") == true)
            {
                var keyVal = GetAttributeNameAndContainedText(expressionPart);
                element.Attributes.Add(new Model.Attribute(keyVal.Key) { Text = CleanUp(keyVal.Value) });
                return true;
            }

            return false;
        }

        private static bool ParseRelatives(Element element, string expressionPart)
        {
            if (expressionPart.Contains(".Descendant") == true)
            {
                element.Relatives.Add(new Relative() { Descendant = true,Name = GetValue(expressionPart)});
                return true;
            }

            if (expressionPart.Contains(".Ancestor") == true)
            {
                element.Relatives.Add(new Relative() { Descendant = false, Name = GetValue(expressionPart) });
                return true;
            }

            return false;
        }

        private static bool ParsePosition(Element element, string expressionPart)
        {
            if (expressionPart.Contains(".Position") == true)
            {
                var matches = Regex.Matches(expressionPart.Trim('('), extractMethodArguments);
                element.Position = Int32.Parse(CleanUp(matches[0].Value));
                return true;
            }

            return false;
        }

        private static bool ParseParent(Element element, string expressionPart)
        {
            if (expressionPart.Contains("Parent.Name") == true)
            {
                EnsureParent(element);
                element.Parent.Name = GetValue(expressionPart);
                return true;
            }
            else if (Regex.IsMatch(expressionPart, "Parent.Attribute(.*)\\.Contains") == true)
            {
                EnsureParent(element);
                var keyVal = GetAttributeNameAndContainedText(expressionPart);
                element.Parent.Attributes.Add(new Model.Attribute(keyVal.Key) { Text = CleanUp(keyVal.Value) });
                return true;
            }
            else if (Regex.IsMatch(expressionPart, "Parent.Attribute(.*)\\.Text") == true)
            {
                EnsureParent(element);
                var keyVal = GetAttributeNameAndValue(expressionPart);
                element.Parent.Attributes.Add(new Model.Attribute(keyVal.Key) { Text = CleanUp(keyVal.Value), ExactMatch = true });
                return true;
            }
            else if (expressionPart.Contains("Parent.Position") == true)
            {
                var matches = Regex.Matches(expressionPart.Trim('('), extractMethodArguments);
                element.Parent.Position = Int32.Parse(CleanUp(matches[0].Value));
                return true;
            }

            return false;
        }

        private static void EnsureParent(Element element)
        {
            if (element.Parent == null)
            {
                element.Parent = new Parent();
            }
        }

        private static KeyValuePair<string, string> GetAttributeNameAndContainedText(string expressionPart)
        {
            var matches = Regex.Matches(expressionPart.Trim('('), extractMethodArguments);

            return new KeyValuePair<string, string>(CleanUp(matches[0].Value), CleanUp(matches[1].Value));
        }

        private static KeyValuePair<string,string> GetAttributeNameAndValue(string expressionPart)
        {
            var temp = expressionPart.Split("==".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var attributeName = Regex.Match(expressionPart.Trim(')', '('), extractMethodArguments).Value;

            return new KeyValuePair<string, string>(CleanUp(attributeName), CleanUp(temp[1]));
        }

        private static string GetValue(string expressionPart)
        {
            var temp = expressionPart.Split("==".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);

            string name = temp[0];

            if (temp.Length == 2)
            {
                name = temp[1];
            }

            return CleanUp(name);
        }

        private static string CleanUp(string raw)
        {
            return raw.Trim().Trim(')').Trim('\\').Trim('"');
        }
    }
}