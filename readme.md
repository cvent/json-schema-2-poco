# JSON Schema to POCO
The purpose of this tool is to convert JSON schemas based on the [official JSON schema standard](http://json-schema.org/) into C# POCOs. This tool uses JSON.net as a JSON deserializer, and currently supports up to the [v3 draft](http://tools.ietf.org/html/draft-zyp-json-schema-03).

Turn this JSON schema:
```json
{
  "$schema": "http://json-schema.org/draft-03/schema#",
  "title": "Country",
  "description": "A nation with its own government, occupying a particular territory",
  "type": "object",
  "properties": {
    "flag": {
      "$ref": "flag.json"
    },
    "cities": {
      "type": "array",
      "description": "A large town",
      "items": {
        "$ref": "city.json"
      },
      "uniqueItems": true
    },
    "population": {
      "type": "integer",
      "description": "The number of people inhabiting this country",
      "minimum": 1000,
      "required": true
    }
  }
}
```
Into this! (with all references generated in separate files)
```csharp
namespace generated
{
    using System;
    using com.cvent.country.entities;
    using generated;
    using System.Collections.Generic;
    using Cvent.SchemaToPoco.Core.ValidationAttributes;
    using System.ComponentModel.DataAnnotations;

    // A nation with its own government, occupying a particular territory
    public class Country
    {
        // Used as the symbol or emblem of a country
        public Flag Flag { get; set; }

        // A large town
        public HashSet<City> Cities { get; set; }

        // The number of people inhabiting this country
        [Required()]
        [MinValue(1000)]
        public int Population { get; set; }
    }
}
```

## Usage (CLI)

### Requirements
* [.NET Framework 4.5](http://www.microsoft.com/en-us/download/details.aspx?id=30653) or later

### Instructions
1. [Download the latest executable](https://github.com/cvent/json-schema-2-poco/releases/latest), build the solution in Visual Studio (Ctrl+Shift+B), or run `msbuild \Path\to\.sln`.
2. Run the following command:
```
Cvent.SchemaToPoco.Console.exe -s \Location\To\Json\Schema
```

### Optional Flags

```
-n name.for.namespace
```
Default: `generated`

```
-o \Location\To\Generate\Files
```
Default: `<exe location>\generated`

```
-v
```
Prints out generated code without generating files

## Usage (Library)

[Download the latest DLL](https://github.com/cvent/json-schema-2-poco/releases), and add it to your project as a reference.

### Basic Usage

```csharp
// To generate files:
// The location can be a web address, an absolute path, or a relative path.
var controller = new JsonSchemaToPoco("/location/to/schema");
int status = controller.Execute();

// To get the C# code as a string:
string code = JsonSchemaToPoco.Generate("/location/to/schema");
```

## [Reference](https://github.com/cvent/json-schema-2-poco/wiki/Reference)
Current version: 1.2 (Alpha)

[View changelog](https://github.com/cvent/json-schema-2-poco/wiki/Changelog)

## [Troubleshooting](https://github.com/cvent/json-schema-2-poco/wiki/Troubleshooting)
