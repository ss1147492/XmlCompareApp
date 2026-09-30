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
/// Compares the structure of two XML files.
/// - Elements are matched by name within the same parent (child order is ignored).
/// - Attributes are matched by name.
/// - Text values and attribute values are NOT compared.
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

    public static List<FieldResult> Compare(string file1, string file2)
    {
        XDocument doc1 = XDocument.Load(file1);
        XDocument doc2 = XDocument.Load(file2);

        var root = new Node { Name = string.Empty, Kind = FieldKind.Element };

        if (doc1.Root is not null)
            Merge(root, doc1.Root, 1);
        if (doc2.Root is not null)
            Merge(root, doc2.Root, 2);

        var results = new List<FieldResult>();
        foreach (var child in root.Children)
            Flatten(child, string.Empty, 0, results);

        return results;
    }

    // Merges an XML element (and everything inside it) into the combined tree.
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

        // Attributes first, then child elements
        foreach (var child in node.Children.Where(c => c.Kind == FieldKind.Attribute))
            Flatten(child, path, depth + 1, results);
        foreach (var child in node.Children.Where(c => c.Kind == FieldKind.Element))
            Flatten(child, path, depth + 1, results);
    }
}
