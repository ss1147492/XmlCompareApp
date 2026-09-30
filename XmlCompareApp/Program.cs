using System.Xml.Linq;

if (args.Length != 2)
{
    Console.WriteLine("Usage: XmlCompareApp <file1.xml> <file2.xml>");
    return 1;
}

string file1 = args[0];
string file2 = args[1];

try
{
    XDocument doc1 = XDocument.Load(file1);
    XDocument doc2 = XDocument.Load(file2);

    bool foundDifference = false;

    CompareXml(doc1.Root!, doc2.Root!, file1, file2, ref foundDifference);
    CompareXml(doc2.Root!, doc1.Root!, file2, file1, ref foundDifference);

    if (!foundDifference)
    {
        Console.WriteLine("No missing elements or attributes found.");
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
    return 1;
}

static void CompareXml(
    XElement source,
    XElement target,
    string sourceFile,
    string targetFile,
    ref bool foundDifference)
{
    foreach (var attr in source.Attributes())
    {
        if (target.Attribute(attr.Name) is null)
        {
            foundDifference = true;
            Console.WriteLine($"Missing attribute: {GetElementPath(source)}/@{attr.Name} in {targetFile} (exists in {sourceFile})");
        }
    }

    foreach (var child in source.Elements())
    {
        var sameNameSiblings = target.Elements(child.Name).ToList();
        int index = child.ElementsBeforeSelf(child.Name).Count();

        if (index >= sameNameSiblings.Count)
        {
            foundDifference = true;
            Console.WriteLine($"Missing element: {GetElementPath(child)} in {targetFile} (exists in {sourceFile})");
            continue;
        }

        XElement matching = sameNameSiblings[index];
        CompareXml(child, matching, sourceFile, targetFile, ref foundDifference);
    }
}

static string GetElementPath(XElement element)
{
    var parts = new List<string>();

    XElement? current = element;
    while (current != null)
    {
        int position = current.ElementsBeforeSelf(current.Name).Count() + 1;
        parts.Insert(0, $"{current.Name}[{position}]");
        current = current.Parent;
    }

    return "/" + string.Join("/", parts);
}
