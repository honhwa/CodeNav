﻿using CodeNav.Helpers;
using CodeNav.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Windows.Media;
using VisualBasicSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CodeNav.Mappers
{
    public static class EnumMapper
    {
        public static CodeItem MapEnumMember(EnumMemberDeclarationSyntax member,
            CodeViewUserControl control, SemanticModel semanticModel)
        {
            if (member == null) return null;

            var item = BaseMapper.MapBase<CodeItem>(member, member.Identifier, control, semanticModel);
            item.Kind = CodeItemKindEnum.EnumMember;
            item.Moniker = IconMapper.MapMoniker(item.Kind, item.Access);

            return item;
        }

        public static CodeItem MapEnumMember(VisualBasicSyntax.EnumMemberDeclarationSyntax member,
            CodeViewUserControl control, SemanticModel semanticModel)
        {
            if (member == null) return null;

            var item = BaseMapper.MapBase<CodeItem>(member, member.Identifier, control, semanticModel);
            item.Kind = CodeItemKindEnum.EnumMember;
            item.Moniker = IconMapper.MapMoniker(item.Kind, item.Access);

            return item;
        }

        public static CodeClassItem MapEnum(EnumDeclarationSyntax member,
            CodeViewUserControl control, SemanticModel semanticModel)
        {
            if (member == null) return null;

            var item = BaseMapper.MapBase<CodeClassItem>(member, member.Identifier, member.Modifiers, control, semanticModel);
            item.Kind = CodeItemKindEnum.Enum;
            item.Moniker = IconMapper.MapMoniker(item.Kind, item.Access);
            item.Parameters = MapMembersToString(member.Members);
            item.BorderBrush = ColorHelper.ToBrush(Colors.DarkGray);

            foreach (var enumMember in member.Members)
            {
                item.Members.Add(SyntaxMapper.MapMember(enumMember));
            }

            return item;
        }

        public static CodeClassItem MapEnum(VisualBasicSyntax.EnumBlockSyntax member,
            CodeViewUserControl control, SemanticModel semanticModel)
        {
            if (member == null) return null;

            var item = BaseMapper.MapBase<CodeClassItem>(member, member.EnumStatement.Identifier, 
                member.EnumStatement.Modifiers, control, semanticModel);
            item.Kind = CodeItemKindEnum.Enum;
            item.Moniker = IconMapper.MapMoniker(item.Kind, item.Access);
            item.Parameters = MapMembersToString(member.Members);
            item.BorderBrush = ColorHelper.ToBrush(Colors.DarkGray);

            foreach (var enumMember in member.Members)
            {
                item.Members.Add(SyntaxMapper.MapMember(enumMember));
            }

            return item;
        }

        private static string MapMembersToString(SeparatedSyntaxList<EnumMemberDeclarationSyntax> members)
        {
            var memberList = (from EnumMemberDeclarationSyntax member in members select member.Identifier.Text).ToList();
            return $"{string.Join(", ", memberList)}";
        }

        private static string MapMembersToString(SyntaxList<VisualBasicSyntax.StatementSyntax> members)
        {
            var memberList = (from VisualBasicSyntax.EnumMemberDeclarationSyntax member in members select member.Identifier.Text).ToList();
            return $"{string.Join(", ", memberList)}";
        }
    }
}
