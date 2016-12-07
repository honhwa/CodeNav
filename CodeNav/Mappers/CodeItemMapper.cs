﻿using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CodeNav.Models;
using EnvDTE;
using TextSelection = EnvDTE.TextSelection;

namespace CodeNav.Mappers
{
    public static class CodeItemMapper
    {
        public static List<CodeItem> MapDocument(CodeElements elements, dynamic selection)
        {
            var document = new List<CodeItem>();

            foreach (CodeElement codeElement in elements)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement namespaceMember in (codeElement as CodeNamespace).Members)
                    {
                        switch (namespaceMember.Kind)
                        {
                            case vsCMElement.vsCMElementClass:
                                document.Add(MapClass(namespaceMember, selection));
                                break;
                            case vsCMElement.vsCMElementEnum:
                                document.Add(MapEnum(namespaceMember));
                                break;
                            case vsCMElement.vsCMElementInterface:
                                document.Add(MapInterface(namespaceMember));
                                break;
                        }
                    }
                }
            }

            return document;
        }

        public static CodeFunctionItem MapFunction(CodeElement element)
        {
            var function = element as CodeFunction;

            return new CodeFunctionItem
            {
                Name = function.Name,
                StartPoint = function.StartPoint,
                Type = function.Type.AsString,
                Parameters = MapParameters(function),
                IconPath = function.FunctionKind == vsCMFunction.vsCMFunctionConstructor ? "Icons/Method/MethodAdded_16x.xaml" : MapIcon<CodeFunction>(element),
                Id = element.Name,
                Foreground = new SolidColorBrush(Colors.Black)
            };
        }

        public static CodeClassItem MapEnum(CodeElement element)
        {
            var enumItem = new CodeClassItem
            {
                Name = element.Name,
                StartPoint = element.StartPoint,
                IconPath = MapIcon<CodeEnum>(element),
                Parameters = MapMembers(element as CodeEnum),
                Id = element.Name,
                Foreground = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.DarkGray)
            };

            foreach (CodeElement enumMember in (element as CodeEnum).Members)
            {
                enumItem.Members.Add(new CodeItem
                {
                    Name = enumMember.Name,
                    StartPoint = enumMember.StartPoint,
                    IconPath = MapIcon<CodeVariable>(enumMember, true),
                    Id = element.Name,
                    Foreground = new SolidColorBrush(Colors.Black)
                });
            }

            return enumItem;
        }

        private static string MapInheritance(CodeElement element)
        {
            var codeClass = element as CodeClass;

            var basesList = (from CodeElement bases in codeClass.Bases select bases.Name).ToList();
            var interfaceList = (from CodeElement interfaces in codeClass.ImplementedInterfaces select interfaces.Name).ToList();
            basesList.AddRange(interfaceList);

            return $" : {string.Join(", ", basesList)}";
        }

        public static CodeClassItem MapInterface(CodeElement element)
        {
            var item = new CodeClassItem
            {
                Name = element.Name,
                StartPoint = element.StartPoint,
                IconPath = MapIcon<CodeInterface>(element),
                Id = element.Name,
                Foreground = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.DarkGray)
            };

            foreach (CodeElement member in (element as CodeInterface).Members)
            {
                item.Members.Add(MapFunction(member));
            }

            return item;
        }

        public static CodeClassItem MapClass(CodeElement element, dynamic selection)
        {
            var classItem = new CodeClassItem
            {
                Name = element.Name,
                StartPoint = element.StartPoint,
                IconPath = MapIcon<CodeClass>(element),
                Parameters = MapInheritance(element),
                Id = element.Name,
                Foreground = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.DarkGray)
            };

            List<CodeRegionItem> classRegions = MapRegions(selection, element.StartPoint, element.EndPoint);

            foreach (CodeElement classMember in (element as CodeClass).Members)
            {
                if (classMember.Kind == vsCMElement.vsCMElementFunction)
                {
                    var function = MapFunction(classMember);
                    if (!AddToRegion(classRegions, function))
                    {
                        classItem.Members.Add(function);
                    }                 
                }
                else if (classMember.Kind == vsCMElement.vsCMElementVariable)
                {
                    var variable = classMember as CodeVariable;
                    var constant = MapConstant(classMember);
                    if (variable.IsConstant && variable.Access != vsCMAccess.vsCMAccessPrivate)
                    {
                        if (!AddToRegion(classRegions, constant))
                        {
                            classItem.Members.Add(constant);
                        }
                    }
                }
                else if (classMember.Kind == vsCMElement.vsCMElementProperty)
                {
                    var property = MapProperty(classMember);
                    if (property != null)
                    {
                        if (!AddToRegion(classRegions, property))
                        {
                            classItem.Members.Add(property);
                        }
                    }
                }
                else
                {
                    var item = new CodeItem {Name = classMember.Name, StartPoint = classMember.StartPoint};
                    if (!AddToRegion(classRegions, item))
                    {
                        classItem.Members.Add(item);
                    }
                }
            }

            if (classRegions.Any())
            {
                classItem.Members.AddRange(classRegions);
            }

            return classItem;
        }

        public static List<CodeRegionItem> MapRegions(TextSelection selection, TextPoint lowerBound, TextPoint upperBound)
        {
            var regionList = new List<CodeRegionItem>();
            if (selection == null) return regionList;
            var activepoint = selection.ActivePoint.CreateEditPoint();

            selection.MoveToPoint(lowerBound);

            while (selection.FindText("#region"))
            {             
                var start = selection.TopPoint.CreateEditPoint();

                selection.SelectLine();
                var name = selection.Text.Replace("#region", string.Empty).Trim();

                selection.FindText("#endregion");
                var end = selection.TopPoint.CreateEditPoint();

                if (end.LessThan(upperBound))
                {
                    regionList.Add(new CodeRegionItem { Name = name, StartPoint = start, EndPoint = end, Id = name,
                        Foreground = new SolidColorBrush(Colors.Black),
                        BorderBrush = new SolidColorBrush(Colors.DarkGray)
                    });
                }               
            }

            selection.MoveToPoint(activepoint);

            return regionList;
        }

        public static bool AddToRegion(List<CodeRegionItem> regions, CodeItem item)
        {
            foreach (var region in regions)
            {
                if (item.StartPoint.GreaterThan(region.StartPoint) && item.StartPoint.LessThan(region.EndPoint))
                {
                    region.Members.Add(item);
                    return true;
                }
            }
            return false;
        }

        private static CodeItem MapConstant(CodeElement element)
        {
            return new CodeItem
            {
                Name = element.Name,
                StartPoint = element.StartPoint,
                IconPath = MapIcon<CodeVariable>(element),
                Id = element.Name,
                Foreground = new SolidColorBrush(Colors.Black)
            };
        }

        private static CodeItem MapProperty(CodeElement element)
        {
            var property = element as CodeProperty;
            if (property.Access == vsCMAccess.vsCMAccessPrivate) return null;
            return new CodeItem
            {
                Name = element.Name,
                StartPoint = element.StartPoint,
                IconPath = MapIcon<CodeProperty>(element),
                Id = element.Name,
                Foreground = new SolidColorBrush(Colors.Black)
            };
        }

        private static string MapMembers(CodeEnum element)
        {
            var memberList = (from CodeElement member in element.Members select member.Name).ToList();
            return $"{string.Join(", ", memberList)}";
        }

        private static string MapParameters(CodeFunction function)
        {
            var paramList = (from CodeElement parameter in function.Parameters select (parameter as CodeParameter).Type.AsString).ToList();
            return $"({string.Join(", ", paramList)})";
        }

        public static string MapIcon<T>(CodeElement element, bool isEnumMember = false)
        {
            if (typeof(T) == typeof(CodeFunction))
            {
                switch ((element as CodeFunction).Access)
                {
                    case vsCMAccess.vsCMAccessPrivate:
                        return "Icons/Method/MethodPrivate_16x.xaml";
                    case vsCMAccess.vsCMAccessProjectOrProtected:
                    case vsCMAccess.vsCMAccessProtected:
                        return "Icons/Method/MethodProtect_16x.xaml";
                    default:
                        return "Icons/Method/Method_purple_16x.xaml";
                }
            }

            if (typeof(T) == typeof(CodeClass))
            {
                switch ((element as CodeClass).Access)
                {
                    case vsCMAccess.vsCMAccessPrivate:
                        return "Icons/Class/ClassPrivate_16x.xaml";
                    case vsCMAccess.vsCMAccessProtected:
                    case vsCMAccess.vsCMAccessProjectOrProtected:
                        return "Icons/Class/ClassProtected_16x.xaml";
                    default:
                        return "Icons/Class/Class_yellow_16x.xaml";
                }
            }

            if (typeof(T) == typeof(CodeProperty))
            {
                switch ((element as CodeProperty).Access)
                {
                    case vsCMAccess.vsCMAccessPrivate:
                        return "Icons/Property/PropertyPrivate_16x.xaml";
                    case vsCMAccess.vsCMAccessProtected:
                    case vsCMAccess.vsCMAccessProjectOrProtected:
                        return "Icons/Property/PropertyProtected_16x.xaml";
                    default:
                        return "Icons/Property/Property_16x.xaml";
                }
            }

            if (typeof(T) == typeof(CodeEnum))
            {
                switch ((element as CodeEnum).Access)
                {
                    case vsCMAccess.vsCMAccessPrivate:
                        return "Icons/Enum/EnumPrivate_16x.xaml";
                    case vsCMAccess.vsCMAccessProtected:
                    case vsCMAccess.vsCMAccessProjectOrProtected:
                        return "Icons/Enum/EnumProtect_16x";
                    default:
                        return "Icons/Enum/Enumerator_orange_16x.xaml";
                }
            }

            if (typeof(T) == typeof(CodeVariable))
            {
                switch ((element as CodeVariable).Access)
                {
                    case vsCMAccess.vsCMAccessPrivate:
                        return isEnumMember ? "Icons/Enum/EnumItemPrivate_16x.xaml" : "Icons/Constant/ConstantPrivate_16x.xaml";
                    case vsCMAccess.vsCMAccessProtected:
                    case vsCMAccess.vsCMAccessProjectOrProtected:
                        return isEnumMember ? "Icons/Enum/EnumItemProtect_16x.xaml" : "Icons/Constant/ConstantProtect_16x.xaml";
                    default:
                        return isEnumMember ? "Icons/Enum/EnumItem_16x.xaml" : "Icons/Constant/Constant_16x.xaml";
                }
            }

            if (typeof(T) == typeof(CodeInterface))
            {
                switch ((element as CodeInterface).Access)
                {
                    case vsCMAccess.vsCMAccessPrivate:
                        return "Icons/Interface/InterfacePrivate_16x.xaml";
                    case vsCMAccess.vsCMAccessProtected:
                    case vsCMAccess.vsCMAccessProjectOrProtected:
                        return "Icons/Interface/InterfaceProtect_16x";
                    default:
                        return "Icons/Interface/Interface_blue_16x.xaml";
                }
            }

            return string.Empty;
        }
    }
}