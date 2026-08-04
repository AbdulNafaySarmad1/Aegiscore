using System.Text.Json;

var command = args.FirstOrDefault()?.ToLowerInvariant();
if (command is null or "help" or "--help" or "-h")
{
    Console.WriteLine("aegiscore-plugin init <name> <developer-email>");
    return 0;
}

if (command == "init" && args.Length >= 3)
{
    var name = args[1];
    var email = args[2];
    Directory.CreateDirectory(name);
    var manifest = new
    {
        name,
        version = "1.0.0",
        description = $"{name} plugin for AegisCore",
        category = "Finance",
        pricingModel = "Free",
        price = 0,
        developerName = email.Split('@')[0],
        developerEmail = email,
        requiredPermissions = Array.Empty<string>()
    };
    await File.WriteAllTextAsync(Path.Combine(name, "aegiscore-plugin.json"),
        JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    await File.WriteAllTextAsync(Path.Combine(name, "README.md"), $"# {name}{Environment.NewLine}{Environment.NewLine}AegisCore plugin package.");
    Console.WriteLine($"Created plugin package in {Path.GetFullPath(name)}");
    return 0;
}

Console.Error.WriteLine("Invalid command. Run: aegiscore-plugin help");
return 2;
