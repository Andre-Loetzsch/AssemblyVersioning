using Oleander.Assembly.Comparers.Core;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Oleander.Assembly.Comparers;

namespace Oleander.Assembly.Versioning.Tool.OutputFormats
{
    internal class AsciiOutputFormat : IOutputFormat
    {
        public string Format(AssemblyComparison assemblyComparison)
        {
            var xml = assemblyComparison.ToXml();

            if (string.IsNullOrEmpty(xml)) return xml;

            var stringBuilder = new StringBuilder();
            var doc = XDocument.Parse(xml);
            var modulElement = doc.Descendants("Module").FirstOrDefault();
            var moduleName = modulElement?.Attribute("Name")?.Value ?? "?";

            foreach (var typeElement in doc.Descendants("DeclarationDiffs"))
            {
                if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                WriteAssemblyReferenceDeclarationDiffElement(stringBuilder, moduleName, typeElement);
            }

            foreach (var typeElement in doc.Descendants("Type"))
            {
                if (stringBuilder.Length > 0) stringBuilder.AppendLine();
                WriteTypeElement(stringBuilder, typeElement);
            }

            return stringBuilder.ToString();
        }

        private static void WriteAssemblyReferenceDeclarationDiffElement(StringBuilder stringBuilder, string typeName, XElement typeElement)
        {
            var assemblyReferenceElements = typeElement.Elements("AssemblyReference")
                .Where(m =>
                {
                    var diffType = m.Attribute("DiffType")?.Value;
                    return diffType == "New" || diffType == "Deleted" || m.Descendants("DiffItem").Any();
                }).ToList();

            if (assemblyReferenceElements.Any())
            {
                stringBuilder.AppendLine("[discrete]");
                stringBuilder.AppendLine($"=== `{typeName}`");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("[horizontal]");

                foreach (var memberElement in assemblyReferenceElements)
                {
                    var memberName = memberElement.Attribute("Name")?.Value;
                    if (!string.IsNullOrEmpty(memberName) && Enum.TryParse(memberElement.Attribute("DiffType")?.Value,
                            out DiffType diffType))
                    {
                        var memberType = memberElement.Name.LocalName.ToLowerInvariant();

                        switch (diffType)
                        {
                            case DiffType.Deleted:
                                stringBuilder.AppendLine($"`{memberName}` {memberType}:: deleted");
                                break;
                            case DiffType.Modified:
                                stringBuilder.AppendLine($"`{memberName}` {memberType}:: modified");
                                break;
                            case DiffType.New:
                                stringBuilder.AppendLine($"`{memberName}` {memberType}:: added");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        private static void WriteTypeDeclarationDiffElement(StringBuilder stringBuilder, XElement typeElement)
        {
            var assemblyReferenceElements = typeElement.Elements("DiffItem")
                .Where(m =>
                {
                    var diffType = m.Attribute("DiffType")?.Value;
                    return diffType == "New" || diffType == "Deleted" || diffType == "Modified" || m.Descendants("DiffItem").Any();
                }).ToList();

            if (assemblyReferenceElements.Any())
            {
                foreach (var memberElement in assemblyReferenceElements)
                {

                    if (Enum.TryParse(memberElement.Attribute("DiffType")?.Value,
                            out DiffType diffType))
                    {
                        var memberType = memberElement.Name.LocalName.ToLowerInvariant();

                        switch (diffType)
                        {
                            case DiffType.Deleted:
                                stringBuilder.AppendLine($"`{memberElement.Value}` {memberType}:: deleted");
                                break;
                            case DiffType.Modified:
                                stringBuilder.AppendLine($"`{memberElement.Value}` {memberType}:: modified");
                                break;
                            case DiffType.New:
                                stringBuilder.AppendLine($"`{memberElement.Value}` {memberType}:: added");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        private static void WriteTypeElement(StringBuilder stringBuilder, XElement typeElement)
        {
            var typeName = typeElement.Attribute("Name")?.Value;
            if (typeName == null) return;

           

            var diffTypeValue = typeElement.Attribute("DiffType")?.Value;
            if (diffTypeValue == null) return;

            var diffType = (DiffType)Enum.Parse(typeof(DiffType), diffTypeValue);

            switch (diffType)
            {
                case DiffType.Deleted:
                    stringBuilder.AppendLine("[discrete]");
                    stringBuilder.AppendLine($"=== `{typeName}`");
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("[horizontal]");
                    stringBuilder.AppendLine("type:: deleted");
                    break;
                case DiffType.Modified:
                    WriteMemberElements(stringBuilder, typeName, typeElement);

                    foreach (var declarationDiffsElement in typeElement.Descendants("DeclarationDiffs"))
                    {
                        WriteTypeDeclarationDiffElement(stringBuilder, declarationDiffsElement);
                    }
                    break;
                case DiffType.New:
                    stringBuilder.AppendLine("[discrete]");
                    stringBuilder.AppendLine($"=== `{typeName}`");
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("[horizontal]");
                    stringBuilder.AppendLine("type:: added");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void WriteMemberElements(StringBuilder stringBuilder, string typeName, XElement typeElement)
        {
            var memberElements = typeElement.Elements("Method")
                .Concat(typeElement.Elements("Property"))
                .Where(m =>
                {
                    var diffType = m.Attribute("DiffType")?.Value;
                    return diffType == "New" || diffType == "Deleted" || m.Descendants("DiffItem").Any();
                }).ToList();

            if (memberElements.Any())
            {
                stringBuilder.AppendLine("[discrete]");
                stringBuilder.AppendLine($"=== `{typeName}`");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("[horizontal]");

                foreach (var memberElement in memberElements)
                {
                    var memberName = memberElement.Attribute("Name")?.Value;
                    if (!string.IsNullOrEmpty(memberName) && Enum.TryParse(memberElement.Attribute("DiffType")?.Value,
                            out DiffType diffType))
                    {
                        var memberType = memberElement.Name.LocalName.ToLowerInvariant();

                        switch (diffType)
                        {
                            case DiffType.Deleted:
                                stringBuilder.AppendLine($"`{memberName}` {memberType}:: deleted");
                                break;
                            case DiffType.Modified:
                                if (memberType == "method")
                                {
                                    var diffItem = memberElement.Descendants("DiffItem").FirstOrDefault();
                                    if (diffItem != null)
                                    {
                                        stringBuilder.AppendLine($"`{memberName}` {memberType}::");
                                        stringBuilder.AppendLine(
                                            Regex.Replace(diffItem.Value, "changed from (.*?) to (.*).",
                                                "changed from `$1` to `$2`."));
                                    }
                                }
                                else if (memberType == "property")
                                {
                                    var methods = memberElement.Elements("Method").ToList();
                                    if (methods.Any())
                                    {
                                        foreach (var propertyMethod in methods)
                                        {
                                            var propertyMethodNameAttribute = propertyMethod.Attribute("Name")?.Value;
                                            if (propertyMethodNameAttribute == null) continue;

                                            stringBuilder.AppendLine($"`{memberName}` {memberType} {propertyMethodNameAttribute}ter::");
                                            var diffItem = propertyMethod.Descendants("DiffItem").FirstOrDefault();
                                            if (diffItem != null)
                                            {
                                                if (Regex.IsMatch(diffItem.Value, "changed from (.*?) to (.*)."))
                                                {
                                                    stringBuilder.AppendLine(
                                                        Regex.Replace(diffItem.Value, "changed from (.*?) to (.*).",
                                                            "changed from `$1` to `$2`."));
                                                }
                                                else if (Regex.IsMatch(diffItem.Value, "Method changed"))
                                                {
                                                    stringBuilder.AppendLine(
                                                        Regex.Replace(diffItem.Value, "Method changed (.*)",
                                                            "changed $1"));
                                                }
                                                else
                                                    stringBuilder.AppendLine(diffItem.Value);
                                            }
                                        }
                                    }

                                    var propertyDiffItem = memberElement.Elements("DiffItem").FirstOrDefault();
                                    if (propertyDiffItem != null)
                                    {
                                        stringBuilder.AppendLine($"`{memberName}` {memberType}::");
                                        if (Regex.IsMatch(propertyDiffItem.Value, "changed from (.*?) to (.*)."))
                                        {
                                            stringBuilder.AppendLine(
                                                Regex.Replace(propertyDiffItem.Value, "changed from (.*?) to (.*).",
                                                    "changed from `$1` to `$2`."));
                                        }
                                        else if (Regex.IsMatch(propertyDiffItem.Value, "Method changed"))
                                        {
                                            stringBuilder.AppendLine(
                                                Regex.Replace(propertyDiffItem.Value, "Method changed (.*)",
                                                    "changed $1"));
                                        }
                                        else
                                            stringBuilder.AppendLine(propertyDiffItem.Value);
                                    }

                                }

                                break;
                            case DiffType.New:
                                stringBuilder.AppendLine($"`{memberName}` {memberType}:: added");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }
    }
}
