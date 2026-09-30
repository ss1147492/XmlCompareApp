using System.Xml.Linq;

namespace XmlCompareApp;

public enum FieldKind
{
    Element,
    Attribute
}

public sealed record FieldResult(
    string Path,
    string Name,
    FieldKind Kind,
    int Depth,
    bool InFile1,
    bool InFile2)
{
    public bool IsMatch => InFile1 && InFile2;
}

/// <summary>
/// Compares XML structure while allowing wrapper levels to be skipped.
/// Skip 0: compare from the root element.
/// Skip 1: ignore each root and compare its children.
/// Skip 2: ignore the root and the first child of each root, then compare
///         the contents inside those first children. This allows the first
///         child names to differ between files (for example USER1 and USER2).
/// Element order, text values, and attribute values are ignored.
/// </summary>
public static class XmlComparer
{
    private sealed class Node
    {
        public required string Name { get; init; }
        public FieldKind Kind { get; init; }
        public bool InFile1 { get; set; }
        public bool InFile2 { get; set; }
        public List<Node> Children { get; } = new();
    }

    public static List<FieldResult> Compare(string file1, string file2, int skipLevel)
    {
        XDocument doc1 = XDocument.Load(file1);
        XDocument doc2 = XDocument.Load(file2);

        if (doc1.Root is null || doc2.Root is null)
            throw new InvalidOperationException("Both XML files must have a root element.");

        if (skipLevel < 0 || skipLevel > 2)
            throw new ArgumentOutOfRangeException(nameof(skipLevel), "Skip level must be 0, 1, or 2.");

        var combined = new Node { Name = string.Empty, Kind = FieldKind.Element };

        foreach (var element in ElementsToCompare(doc1.Root, skipLevel))
            Merge(combined, element, 1);

        foreach (var element in ElementsToCompare(doc2.Root, skipLevel))
            Merge(combined, element, 2);

        var results = new List<FieldResult>();
        foreach (var child in combined.Children)
            Flatten(child, string.Empty, 0, results);

        return results;
    }

    private static IEnumerable<XElement> ElementsToCompare(XElement root, int skipLevel)
    {
        if (skipLevel == 0)
            return new[] { root };

        // Skip 1: ignore the root and compare all direct root children.
        if (skipLevel == 1)
            return root.Elements();

        // Skip 2: ignore the root and the first child wrapper. The wrapper
        // name may be different in each file, so only its contents are used.
        XElement? firstChild = root.Elements().FirstOrDefault();
        return firstChild?.Elements() ?? Enumerable.Empty<XElement>();
    }

    private static void Merge(Node parent, XElement element, int fileNo)
    {
        var node = GetOrAdd(parent, element.Name.LocalName, FieldKind.Element);
        Mark(node, fileNo);

        foreach (var attr in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
        {
            var attrNode = GetOrAdd(node, attr.Name.LocalName, FieldKind.Attribute);
            Mark(attrNode, fileNo);
        }

        foreach (var child in element.Elements())
            Merge(node, child, fileNo);
    }

    private static Node GetOrAdd(Node parent, string name, FieldKind kind)
    {
        var existing = parent.Children.FirstOrDefault(c => c.Kind == kind && c.Name == name);
        if (existing is not null)
            return existing;

        var node = new Node { Name = name, Kind = kind };
        parent.Children.Add(node);
        return node;
    }

    private static void Mark(Node node, int fileNo)
    {
        if (fileNo == 1) node.InFile1 = true;
        else node.InFile2 = true;
    }

    private static void Flatten(Node node, string parentPath, int depth, List<FieldResult> results)
    {
        string segment = node.Kind == FieldKind.Attribute ? "@" + node.Name : node.Name;
        string path = parentPath + "/" + segment;

        results.Add(new FieldResult(path, node.Name, node.Kind, depth, node.InFile1, node.InFile2));

        foreach (var child in node.Children.Where(c => c.Kind == FieldKind.Attribute))
            Flatten(child, path, depth + 1, results);
        foreach (var child in node.Children.Where(c => c.Kind == FieldKind.Element))
            Flatten(child, path, depth + 1, results);
    }
}
