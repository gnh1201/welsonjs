# WelsonJS.Esent

WelsonJS.Esent is a library that enables the use of the [ESENT](https://learn.microsoft.com/en-us/windows/win32/extensible-storage-engine/extensible-storage-engine) database (also known as the Extensible Storage Engine or JET Blue).

Although it was developed to support the WelsonJS framework, it can be used in any .NET-based project.

For more details, refer to the [WelsonJS Documentation](https://catswords-oss.rdbl.io/5719744820/5330609327).

## Example code

```csharp
using WelsonJS.Esent;

// connect the database to manage instances
Schema schema = new Schema("Instances", new List<Column>
{
    new Column("InstanceId", typeof(string), 255),
    new Column("FirstDeployTime", typeof(DateTime), 1)
});
schema.SetPrimaryKey("InstanceId");
_db = new EsentDatabase(schema, Path.GetTempPath());

// Insert row
try
{
    _db.Insert(new Dictionary<string, object>
    {
        ["InstanceId"] = instanceId,
        ["FirstDeployTime"] = now
    }, out _);
}
catch (Exception ex)
{
    // Handle exception
}

// find all
var instances = _db.FindAll();
foreach (var instance in instances)
{
    try
    {
        string instanceId = instance["InstanceId"].ToString();
        string firstDeployTime = instance.ContainsKey("FirstDeployTime")
            ? ((DateTime)instance["FirstDeployTime"]).ToString(_dateTimeFormat)
            : "Unknown";

        Console.WriteLine($"{firstDeployTime}, {instanceId}");
    }
    catch (Exception ex)
    {
        // Handle exception
    }
}

```

Source code available: https://github.com/gnh1201/welsonjs
