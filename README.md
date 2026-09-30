# XmlCompareApp

A Windows desktop (WinForms, .NET 8) application that compares two XML files and lists every element and attribute, showing whether it is **Present** or **Missing** in each file.

## Comparison rules

- Checks all nesting levels (inner child elements included).
- **Element order is ignored**: children are matched by name within the same parent, so shuffled elements still match.
- Attributes are matched by name.
- Text values and attribute values are **not** compared.

## How to use

1. Open `XmlCompareApp.sln` in Visual Studio 2022 (or run `dotnet run --project XmlCompareApp`).
2. Click **Browse...** to select **File 1** and **File 2**.
3. Choose where to save the **HTML report**.
4. Optionally tick **Show only mismatches**.
5. Click **Compare**.

The results appear in the on-screen table and are saved as an HTML report.

## HTML report

| Field (hierarchy) | Type | Full Path | File 1 | File 2 |
|---|---|---|---|---|
| `<root>` | Element | /root | Present | Present |
| &nbsp;&nbsp;`<customers>` | Element | /root/customers | Present | Present |
| &nbsp;&nbsp;&nbsp;&nbsp;`<email>` | Element | /root/customers/customer/email | Present | Missing |
| &nbsp;&nbsp;`@mode` | Attribute | /root/settings/@mode | Present | Missing |

The report also includes summary counts for total fields, fields present in both files, and fields missing from each file.

## Requirements

- Windows
- .NET 8 SDK
