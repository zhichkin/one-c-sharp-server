﻿using System.Collections.Generic;

namespace OneCSharp.AST.Model
{
    public sealed class FunctionConcept : SyntaxNode, IIdentifiable
    {
        private const string PLACEHOLDER = "<function name>";
        public string Identifier { get; set; } = PLACEHOLDER;
        public Optional<SimpleDataType> ReturnType { get; } = new Optional<SimpleDataType>();
        public Optional<List<ParameterConcept>> Parameters { get; } = new Optional<List<ParameterConcept>>();
        public Optional<List<VariableConcept>> Variables { get; } = new Optional<List<VariableConcept>>();
        public Optional<List<SelectConcept>> Statements { get; } = new Optional<List<SelectConcept>>();
    }
    public sealed class ParameterConcept : SyntaxNode, IIdentifiable
    {
        private const string PLACEHOLDER = "<parameter name>";
        public string Identifier { get; set; } = PLACEHOLDER;
        public SimpleDataType ParameterType { get; set; }
        public Optional<bool> IsOutput { get; } = new Optional<bool>();
    }
    public sealed class VariableConcept : SyntaxNode, IIdentifiable
    {
        private const string PLACEHOLDER = "<variable name>";
        public string Identifier { get; set; } = PLACEHOLDER;
        public SimpleDataType VariableType { get; set; }
        public override string ToString()
        {
            return $"{Identifier} ({VariableType})";
        }
    }
}