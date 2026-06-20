using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;
return PlugHubPackageValidator.Run(args);

internal static class PlugHubPackageValidator
{
    private static readonly Regex VersionPattern = new("^V\\d+\\.\\d+\\.\\d+$", RegexOptions.Compiled);
    private static readonly string[] DisallowedRootKeys = ["packageDirectories", "moduleSources", "repositories", "conflictPolicy"];
    private static readonly string[] DisallowedModuleKeys = ["enabled", "visible", "order"];
    private static readonly string[] DisallowedFeatureKeys = ["defaultState", "buttonSize", "order"];

    public static int Run(string[] args)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var options = ParseOptions(args, errors);
        if (errors.Count > 0)
        {
            return Finish(errors, warnings);
        }

        var packageRoot = Path.GetFullPath(options.PackageRoot);
        if (!Directory.Exists(packageRoot))
        {
            errors.Add($"Package root does not exist: {packageRoot}");
        }

        if (!IsRelativePathInsideRoot(options.Manifest))
        {
            errors.Add($"Manifest path must be relative to the package root: {options.Manifest}");
        }

        var manifestPath = Path.GetFullPath(Path.Combine(packageRoot, options.Manifest));
        if (!File.Exists(manifestPath))
        {
            errors.Add($"Manifest does not exist: {manifestPath}");
        }

        if (errors.Count > 0)
        {
            return Finish(errors, warnings);
        }

        using var document = ReadManifest(manifestPath, errors);
        if (document is null)
        {
            return Finish(errors, warnings);
        }

        var manifest = document.RootElement;
        if (manifest.ValueKind != JsonValueKind.Object)
        {
            errors.Add("Manifest root must be a JSON object.");
            return Finish(errors, warnings);
        }

        ValidateManifest(manifest, Path.GetDirectoryName(manifestPath)!, errors, warnings);
        return Finish(errors, warnings);
    }

    private static ValidatorOptions ParseOptions(string[] args, List<string> errors)
    {
        var packageRoot = ".";
        var manifest = "packages.json";
        var packageRootSet = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg == "--manifest")
            {
                if (index + 1 >= args.Length)
                {
                    errors.Add("--manifest requires a relative manifest path.");
                    break;
                }

                manifest = args[++index];
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                errors.Add($"Unknown argument: {arg}");
                continue;
            }

            if (packageRootSet)
            {
                errors.Add($"Only one package root can be supplied. Extra argument: {arg}");
                continue;
            }

            packageRoot = arg;
            packageRootSet = true;
        }

        return new ValidatorOptions(packageRoot, manifest);
    }

    private static JsonDocument? ReadManifest(string manifestPath, List<string> errors)
    {
        try
        {
            var json = File.ReadAllText(manifestPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
            return JsonDocument.Parse(json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or DecoderFallbackException)
        {
            errors.Add($"Manifest JSON could not be parsed: {ex.Message}");
            return null;
        }
    }

    private static void ValidateManifest(JsonElement manifest, string manifestDirectory, List<string> errors, List<string> warnings)
    {
        if (!HasNonEmptyProperty(manifest, "schemaVersion"))
        {
            errors.Add("Missing required root field: schemaVersion");
        }

        if (!TryGetArray(manifest, "modules", out var modules) || modules.GetArrayLength() == 0)
        {
            errors.Add("Missing or empty required root field: modules");
        }

        var indexVersion = GetPropertyText(manifest, "indexVersion");
        if (indexVersion.Length > 0 && !VersionPattern.IsMatch(indexVersion))
        {
            errors.Add("Root indexVersion must match V<major>.<minor>.<patch>, or be omitted.");
        }

        var rootRevitVersions = GetStringArray(manifest, "revitVersions");
        if (!rootRevitVersions.Contains("2020", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Root revitVersions must include \"2020\".");
        }

        if (GetPropertyText(manifest, "frameworkVersionRange") != ">=1.3.0")
        {
            errors.Add("Root frameworkVersionRange should be \">=1.3.0\".");
        }

        foreach (var key in DisallowedRootKeys.Where(key => manifest.TryGetProperty(key, out _)).Order(StringComparer.Ordinal))
        {
            errors.Add($"External package manifest should not define root config key: {key}");
        }

        if (!TryGetArray(manifest, "modules", out modules))
        {
            return;
        }

        var moduleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var featureIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var moduleIndex = 0;

        foreach (var module in modules.EnumerateArray())
        {
            ValidateModule(module, moduleIndex, manifestDirectory, moduleIds, featureIds, errors, warnings);
            moduleIndex++;
        }
    }

    private static void ValidateModule(
        JsonElement module,
        int moduleIndex,
        string manifestDirectory,
        HashSet<string> moduleIds,
        HashSet<string> featureIds,
        List<string> errors,
        List<string> warnings)
    {
        var location = $"modules[{moduleIndex}]";
        if (module.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{location} must be an object.");
            return;
        }

        var moduleId = GetPropertyText(module, "id").Trim();
        if (moduleId.Length == 0)
        {
            errors.Add($"{location}.id is required.");
        }
        else if (!moduleIds.Add(moduleId))
        {
            errors.Add($"Duplicate module id: {moduleId}");
        }

        var moduleVersion = GetPropertyText(module, "version").Trim();
        if (!VersionPattern.IsMatch(moduleVersion))
        {
            errors.Add($"{location}.version must match V<major>.<minor>.<patch>.");
        }

        foreach (var key in DisallowedModuleKeys.Where(key => module.TryGetProperty(key, out _)).Order(StringComparer.Ordinal))
        {
            errors.Add($"External package manifests should not actively define runtime state or layout field: {location}.{key}");
        }

        var moduleRevitVersions = GetStringArray(module, "revitVersions");
        if (moduleRevitVersions.Count > 0 && !moduleRevitVersions.Contains("2020", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"{location}.revitVersions declares versions but does not include 2020.");
        }

        var assembly = GetPropertyText(module, "assembly").Trim();
        if (assembly.Length > 0)
        {
            ValidatePayloadPath(manifestDirectory, assembly, $"{location}.assembly", errors);
        }
        else
        {
            errors.Add($"{location}.assembly is required; features inherit it as their command assembly by default.");
        }

        if (!TryGetArray(module, "features", out var features) || features.GetArrayLength() == 0)
        {
            errors.Add($"{location}.features must be a non-empty array.");
            return;
        }

        var featureIndex = 0;
        foreach (var feature in features.EnumerateArray())
        {
            ValidateFeature(feature, featureIndex, location, manifestDirectory, assembly, featureIds, errors, warnings);
            featureIndex++;
        }
    }

    private static void ValidateFeature(
        JsonElement feature,
        int featureIndex,
        string moduleLocation,
        string manifestDirectory,
        string moduleAssembly,
        HashSet<string> featureIds,
        List<string> errors,
        List<string> warnings)
    {
        var location = $"{moduleLocation}.features[{featureIndex}]";
        if (feature.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{location} must be an object.");
            return;
        }

        var featureId = GetPropertyText(feature, "id").Trim();
        if (featureId.Length == 0)
        {
            errors.Add($"{location}.id is required.");
        }
        else if (!featureIds.Add(featureId))
        {
            errors.Add($"Duplicate feature id: {featureId}");
        }

        foreach (var key in DisallowedFeatureKeys.Where(key => feature.TryGetProperty(key, out _)).Order(StringComparer.Ordinal))
        {
            errors.Add($"External package manifests should not actively define runtime state or layout field: {location}.{key}");
        }

        var commandAssembly = GetPropertyText(feature, "commandAssembly").Trim();
        if (commandAssembly.Length == 0)
        {
            commandAssembly = moduleAssembly;
        }

        if (commandAssembly.Length == 0)
        {
            errors.Add($"{location}.commandAssembly is required when module.assembly is empty.");
        }
        else
        {
            ValidatePayloadPath(manifestDirectory, commandAssembly, $"{location}.commandAssembly", errors);
        }

        var commandType = GetPropertyText(feature, "commandType").Trim();
        if (commandType.Length == 0)
        {
            errors.Add($"{location}.commandType is required.");
        }
        else if (!commandType.Contains('.', StringComparison.Ordinal) || commandType.Any(char.IsWhiteSpace))
        {
            errors.Add($"{location}.commandType must be a full CLR type name.");
        }

        var iconPath = GetPropertyText(feature, "iconPath").Trim();
        if (iconPath.Length == 0)
        {
            warnings.Add($"{location}.iconPath is empty; PlugHub will use its default icon.");
        }
        else
        {
            ValidatePayloadPath(manifestDirectory, iconPath, $"{location}.iconPath", errors);
            if (!string.Equals(Path.GetExtension(iconPath), ".png", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{location}.iconPath should point to a PNG file.");
            }
        }
    }

    private static void ValidatePayloadPath(string manifestDirectory, string relativePath, string label, List<string> errors)
    {
        if (!IsRelativePathInsideRoot(relativePath))
        {
            errors.Add($"{label} must be a package-relative path inside the manifest directory: {relativePath}");
            return;
        }

        var resolved = Path.GetFullPath(Path.Combine(manifestDirectory, relativePath));
        if (!File.Exists(resolved))
        {
            errors.Add($"{label} points to a missing package file: {relativePath}");
        }
    }

    private static bool IsRelativePathInsideRoot(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
        {
            return false;
        }

        var parts = value.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        return parts.All(part => part != "." && part != "..");
    }

    private static bool HasNonEmptyProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && ElementToText(value).Trim().Length > 0;
    }

    private static bool TryGetArray(JsonElement element, string propertyName, out JsonElement array)
    {
        if (element.TryGetProperty(propertyName, out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        array = default;
        return false;
    }

    private static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (!TryGetArray(element, propertyName, out var array))
        {
            return [];
        }

        return array.EnumerateArray().Select(ElementToText).Where(item => item.Length > 0).ToList();
    }

    private static string GetPropertyText(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? ElementToText(value) : string.Empty;
    }

    private static string ElementToText(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }

    private static int Finish(List<string> errors, List<string> warnings)
    {
        foreach (var warning in warnings)
        {
            Console.WriteLine($"WARNING: {warning}");
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Console.WriteLine($"ERROR: {error}");
            }

            Console.WriteLine($"PlugHub package validation failed with {errors.Count} error(s).");
            return 1;
        }

        Console.WriteLine("PlugHub package validation passed.");
        return 0;
    }

    private sealed record ValidatorOptions(string PackageRoot, string Manifest);
}
