﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;

namespace Rubberduck.Refactorings.ExtractInterface
{
    public class Parameter
    {
        public string ParamAccessibility { get; set; }
        public string ParamName { get; set; }
        public string ParamType { get; set; }

        public override string ToString()
        {
            return $"{ParamAccessibility} {ParamName} As {ParamType}";
        }
    }

    public class InterfaceMember : INotifyPropertyChanged
    {
        public Declaration Member { get; }
        public IEnumerable<Parameter> MemberParams { get; }
        private string Type { get; }
        private string MemberType { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Identifier { get; }

        public string FullMemberSignature
        {
            get
            {
                var signature = $"{MemberType} {Member.IdentifierName}({string.Join(", ", MemberParams)})";

                return Type == null ? signature : $"{signature} As {Type}";
            }
        }

        public InterfaceMember(Declaration member)
        {
            Member = member;
            Identifier = member.IdentifierName;
            Type = member.AsTypeName;
            
            MemberType = GetMethodType(Member.Context);

            if (member is IParameterizedDeclaration memberWithParams)
            {
                MemberParams = memberWithParams.Parameters
                    .OrderBy(o => o.Selection.StartLine)
                    .ThenBy(t => t.Selection.StartColumn)
                    .Select(p => new Parameter
                    {
                        ParamAccessibility =
                            ((VBAParser.ArgContext) p.Context).BYVAL() != null ? Tokens.ByVal : Tokens.ByRef,
                        ParamName = p.IdentifierName,
                        ParamType = p.AsTypeName
                    })
                    .ToList();
            }
            else
            {
                MemberParams = new List<Parameter>();
            }

            if (MemberType == "Property Get")
            {
                MemberParams = MemberParams.Take(MemberParams.Count() - 1);
            }
        }

        private string GetMethodType(Antlr4.Runtime.ParserRuleContext context)
        {
            if (context is VBAParser.SubStmtContext)
            {
                return Tokens.Sub;
            }

            if (context is VBAParser.FunctionStmtContext)
            {
                return Tokens.Function;
            }

            if (context is VBAParser.PropertyGetStmtContext)
            {
                return $"{Tokens.Property} {Tokens.Get}";
            }

            if (context is VBAParser.PropertyLetStmtContext)
            {
                return $"{Tokens.Property} {Tokens.Let}";
            }

            if (context is VBAParser.PropertySetStmtContext)
            {
                return $"{Tokens.Property} {Tokens.Set}";
            }

            return null;
        }

        public string Body => string.Format("Public {0}{1}End {2}{1}", FullMemberSignature, Environment.NewLine, MemberType.Split(' ').First());
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
