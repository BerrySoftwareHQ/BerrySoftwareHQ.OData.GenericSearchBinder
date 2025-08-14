# BerrySoftwareHQ.OData.GenericSearchBinder

A lightweight and robust ISearchBinder implementation for ASP.NET Core OData ($search). It builds a boolean predicate that performs a safe, case‑insensitive Contains search across an entity’s readable scalar properties.

- Strings: case‑insensitive substring
- Booleans: matches when the term is exactly "true" or "false"
- Numerics: substring via ToString() (server‑translatable)
- Temporal types: DateTime, DateOnly, TimeOnly, DateTimeOffset, TimeSpan supported with helpful extras (see below)

Works with .NET 8 and Microsoft.AspNetCore.OData v8+.

## Quick start

1) Install packages (example)
- Microsoft.AspNetCore.OData
- OData.GenericSearchBinder (this package)

2) Register the binder

```csharp
using BerrySoftwareHQ.OData.GenericSearchBinder;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Expressions;

builder.Services.AddControllers().AddOData(opt =>
{
    opt.AddRouteComponents("odata", GetEdmModel())
       .Count()
       .Filter()
       .OrderBy()
       .Select()
       .Expand()
       .SetMaxTop(100)
       .EnableQueryFeatures();
});

// Register the custom $search binder
builder.Services.AddSingleton<ISearchBinder, GenericSearchBinder.GenericSearchBinder>();
```

3) Use $search in your queries

```
GET /odata/Products?$search=laptop
GET /odata/Orders?$search=2024 AND shipped
GET /odata/People?$search=true // will match boolean properties equal to true
```

## What it does

For a given entity type, the binder builds an Expression<Func<TEntity,bool>> that evaluates to true if any readable scalar property matches the search term(s). Logical operators AND/OR/NOT are supported as provided by OData $search.

- Strings: e.StringProp != null && e.StringProp.ToLower().Contains(termLower)
- Bool: only matches if the term is exactly "true" or "false" (nullable supported)
- Numeric (int, long, double, float, decimal): e.Prop.ToString().ToLower().Contains(termLower)
- Temporal extras:
  - DateTime/DateTimeOffset: substring on ToString + matches Year (e.g., "2023") + exact date "yyyy-MM-dd"
  - DateOnly: substring on ToString + Year + exact "yyyy-MM-dd"
  - TimeOnly: substring on ToString + Hour + exact times like "HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"
  - TimeSpan: substring on ToString + Hours + exact spans like "hh:mm", "hh:mm:ss", "hh:mm:ss.FFFFFFF"

Notes on translation:
- The binder uses ToLower() and Contains(string) for compatibility with EF Core SQL translation. StringComparison overloads are not used because they are not translatable by EF Core.
- Whether you need ToLower depends on your DB collation; the current default is predictable and provider‑friendly.

## Usage tips and limitations

- Navigation properties are ignored by default; only readable scalar properties are considered.
- Empty terms are handled defensively (e.g., $search= OR NOT("") produces predictable tautologies/contradictions).
- If you need domain‑specific behavior, you can wrap or fork the binder; the internal structure is organized for easy extension.

## Testing status

- All unit and integration tests pass (120/120) on .NET 8.

## License

Licensed under the MIT License. See LICENSE for details.