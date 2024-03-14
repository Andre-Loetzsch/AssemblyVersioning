using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Oleander.Assembly.Comparers;
using Oleander.Assembly.Comparers.Core;

namespace Oleander.Assembly.Versioning.Tool.OutputFormats;

internal class MarkdownOutputFormat : IOutputFormat
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
          
            stringBuilder.AppendLine($"## `{typeName}`");

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
                            stringBuilder.AppendLine($"### `{memberName}` {memberType}:: deleted");
                            break;
                        case DiffType.Modified:
                            stringBuilder.AppendLine($"### `{memberName}` {memberType}:: modified");
                            break;
                        case DiffType.New:
                            stringBuilder.AppendLine($"### `{memberName}` {memberType}:: added");
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
                            stringBuilder.AppendLine($"### `{memberElement.Value}` {memberType}:: deleted");
                            break;
                        case DiffType.Modified:
                            stringBuilder.AppendLine($"### `{memberElement.Value}` {memberType}:: modified");
                            break;
                        case DiffType.New:
                            stringBuilder.AppendLine($"### `{memberElement.Value}` {memberType}:: added");
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

        var typeElementDiffTypeAttribute = typeElement.Attribute("DiffType")?.Value;
        if (typeElementDiffTypeAttribute == null) return;

        var diffType = (DiffType)Enum.Parse(typeof(DiffType), typeElementDiffTypeAttribute);

        switch (diffType)
        {
            case DiffType.Deleted:
                stringBuilder.AppendLine($"## `{typeName}` is deleted");
                break;
            case DiffType.Modified:

                WriteMemberElements(stringBuilder, typeName, typeElement);

                foreach (var declarationDiffsElement in typeElement.Descendants("DeclarationDiffs"))
                {
                    WriteTypeDeclarationDiffElement(stringBuilder, declarationDiffsElement);
                }
                break;
            case DiffType.New:
                stringBuilder.AppendLine($"## `{typeName}` is new");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void WriteMemberElements(StringBuilder writer, string typeName, XElement typeElement)
    {
        var memberElements = typeElement.Elements("Method").Concat(typeElement.Elements("Property")).ToList();

        if (memberElements.Any())
            writer.AppendLine($"## `{typeName}`");

        foreach (var memberElement in memberElements)
        {
            var memberName = memberElement.Attribute("Name")?.Value;
            if (!string.IsNullOrEmpty(memberName) && Enum.TryParse(typeElement.Attribute("DiffType")?.Value, out DiffType diffType))
            {
                switch (diffType)
                {
                    case DiffType.Deleted:
                        writer.AppendLine($"### `{memberName}` is deleted");
                        break;
                    case DiffType.Modified:
                        var diffItem = memberElement.Descendants("DiffItem").FirstOrDefault();
                        if (diffItem != null)
                        {
                            writer.AppendLine($"### `{memberName}`");
                            writer.AppendLine(
                                Regex.Replace(diffItem.Value, "changed from (.*?) to (.*).", "changed from `$1` to `$2`."));
                        }
                        else
                            writer.AppendLine($"### `{memberName}` is added");
                        break;
                    case DiffType.New:
                        writer.AppendLine($"### `{memberName}` is added");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}