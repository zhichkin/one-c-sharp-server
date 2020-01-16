﻿using OneCSharp.Core.Model;
using System;

namespace OneCSharp.AST.Model
{
    public abstract class ConceptElement : Property
    {
        public bool IsOptional { get; set; }
    }
    public abstract class LanguageConcept : ComplexType
    {
        public void PrepareForEditing() // prepare object for editing as parse syntax tree
        {
            int index = 0;
            while (index < Properties.Count)
            {
                ConceptElement element = (ConceptElement)Properties[index];
                if (element.IsOptional)
                {
                    Properties.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }
        public LanguageConcept Literal(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            Properties.Add(new LiteralElement()
            {
                Owner = this,
                Name = name,
                IsOptional = false,
                ValueType = SimpleType.NULL
            });
            return this;
        }
        public LanguageConcept Keyword(string name)
        {
            return Keyword(name, false);
        }
        public LanguageConcept Keyword(string name, bool optional)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            Properties.Add(new KeywordElement()
            {
                Owner = this,
                Name = name,
                IsOptional = optional,
                ValueType = SimpleType.NULL
            });
            return this;
        }
        public LanguageConcept WithParameter(DataType parameterType)
        {
            return WithParameter(parameterType, null);
        }
        public LanguageConcept WithParameter(DataType parameterType, object defaultValue)
        {
            int count = Properties.Count;
            if (count == 0) throw new InvalidOperationException("Concept properties list is empty.");
            if (!(Properties[count - 1] is KeywordElement keyword)) throw new InvalidOperationException("No preceding keyword is found.");
            if (parameterType == null)
            {
                keyword.ValueType = SimpleType.NULL;
            }
            else
            {
                keyword.ValueType = parameterType;
            }
            keyword.DefaultValue = defaultValue;
            return this;
        }
        public LanguageConcept UserName()
        {
            return UserName(null);
        }
        public LanguageConcept UserName(string placeholder)
        {
            Properties.Add(new NameElement()
            {
                Owner = this,
                Name = string.IsNullOrWhiteSpace(placeholder) ? "<name>" : placeholder,
                ValueType = SimpleType.NULL
            });
            return this;
        }
        public LanguageConcept Repeat(LanguageConcept concept)
        {
            Properties.Add(new RepeatableElement()
            {
                Owner = this,
                Name = string.Empty,
                ValueType = concept
            });
            return this;
        }
        public LanguageConcept Selector(DataType dataType)
        {
            Properties.Add(new SelectorElement()
            {
                Owner = this,
                Name = string.Empty,
                ValueType = dataType
            });
            return this;
        }
    }
    public sealed class FunctionConcept : LanguageConcept
    {
        private const string FUNCTION = "FUNCTION";
        private const string RETURNS = "RETURNS";
        public FunctionConcept()
        {
            Name = FUNCTION;
            Keyword(FUNCTION)
                .UserName()
                .Keyword(RETURNS, true).WithParameter(new MultipleType())
                .Repeat(new ParameterConcept());
        }
    }
    public sealed class ParameterConcept : LanguageConcept
    {
        private const string KEYWORD = "@"; // DECLARE | VAR | PARAM ?
        private const string EQUALS_SIGN = "=";
        private const string INPUT = "INPUT";
        private const string OUTPUT = "OUTPUT";
        private const string PLACEHOLDER = "<parameter name>";
        public ParameterConcept()
        {
            Keyword(KEYWORD)
                .UserName(PLACEHOLDER)
                .Literal(EQUALS_SIGN)
                .Keyword(INPUT, true)
                .Keyword(OUTPUT, true);
        }
    }
    public sealed class NameElement : ConceptElement { }
    public sealed class LiteralElement : ConceptElement { }
    public sealed class KeywordElement : ConceptElement { }
    public sealed class RepeatableElement : ConceptElement
    {
        public RepeatableElement()
        {
            ValueType = new ListType();
        }
    }
    public sealed class SelectorElement : ConceptElement { } // !?
}