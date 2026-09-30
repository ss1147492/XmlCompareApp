# XmlCompareApp

This project compares two XML files and reports missing elements and attributes.

## Run

```bash
dotnet run --project XmlCompareApp -- first.xml second.xml
```

## Example

`first.xml`
```xml
<root>
  <customers>
    <customer id="1">
      <name>John</name>
      <email>john@example.com</email>
    </customer>
    <customer id="2">
      <name>Mary</name>
    </customer>
  </customers>
  <settings mode="on" />
</root>
```

`second.xml`
```xml
<root>
  <customers>
    <customer id="1">
      <name>John</name>
    </customer>
    <customer id="3">
      <name>Sam</name>
    </customer>
  </customers>
  <settings />
</root>
```

## Output

```text
Missing attribute: /root/settings/@mode in second.xml (exists in first.xml)
Missing element: /root/customers/customer[2]/email in second.xml (exists in first.xml)
```