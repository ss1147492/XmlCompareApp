# XmlCompareApp

A Windows desktop (WinForms, .NET 8) application that compares XML structure and writes an HTML table report.

## Skip level options

The application lets you ignore variable wrapper/user-name elements:

- **Skip 0**: compare from the root element.
- **Skip 1**: skip the root (`<Xelp>`) and compare the root's children.
- **Skip 2**: skip the root and the first child of each root. Compare the contents inside those first children. The first child names can differ between files, for example `<USER1>` in File 1 and `<USER2>` in File 2.

For example:

```xml
<Xelp>
  <USER1>
    <Customer><Name /><Address /></Customer>
  </USER1>
</Xelp>
```

and:

```xml
<Xelp>
  <USER2>
    <Customer><Name /></Customer>
  </USER2>
</Xelp>
```

Use **Skip 2**. The comparison starts at `Customer`; `Xelp`, `USER1`, and `USER2` are not reported as mismatches.

## Comparison rules

- Checks all nested elements and attributes.
- Element order is ignored.
- The skipped wrapper names do not need to match.
- Text values and attribute values are not compared.
- The HTML report shows hierarchy, full path, and Present/Missing status for each file.

## How to use

1. Open `XmlCompareApp.sln` in Visual Studio 2022.
2. Browse for File 1 and File 2.
3. Select Skip level `1` or `2` as required.
4. Select the HTML output path.
5. Click **Compare**.

The comparison table appears in the application and the HTML report is saved to the selected location.
