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
            errors.Add($"插件包根目录不存在：{packageRoot}");
        }

        if (!IsRelativePathInsideRoot(options.Manifest))
        {
            errors.Add($"清单路径必须是插件包根目录内的相对路径：{options.Manifest}");
        }

        var manifestPath = Path.GetFullPath(Path.Combine(packageRoot, options.Manifest));
        if (!File.Exists(manifestPath))
        {
            errors.Add($"清单文件不存在：{manifestPath}");
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
            errors.Add("清单根节点必须是 JSON 对象。");
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
                    errors.Add("--manifest 需要一个相对清单路径。");
                    break;
                }

                manifest = args[++index];
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                errors.Add($"未知参数：{arg}");
                continue;
            }

            if (packageRootSet)
            {
                errors.Add($"只能提供一个插件包根目录，额外参数为：{arg}");
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
            errors.Add($"清单 JSON 无法解析：{ex.Message}");
            return null;
        }
    }

    private static void ValidateManifest(JsonElement manifest, string manifestDirectory, List<string> errors, List<string> warnings)
    {
        if (!HasNonEmptyProperty(manifest, "schemaVersion"))
        {
            errors.Add("缺少必需根字段：schemaVersion");
        }

        if (!TryGetArray(manifest, "modules", out var modules) || modules.GetArrayLength() == 0)
        {
            errors.Add("缺少必需根字段 modules，或 modules 为空。");
        }

        var indexVersion = GetPropertyText(manifest, "indexVersion");
        if (indexVersion.Length > 0 && !VersionPattern.IsMatch(indexVersion))
        {
            errors.Add("根字段 indexVersion 必须匹配 V<major>.<minor>.<patch>，或省略。");
        }

        var rootRevitVersions = GetStringArray(manifest, "revitVersions");
        if (!rootRevitVersions.Contains("2020", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("根字段 revitVersions 必须包含 \"2020\"。");
        }

        if (GetPropertyText(manifest, "frameworkVersionRange") != ">=1.3.0")
        {
            errors.Add("根字段 frameworkVersionRange 应为 \">=1.3.0\"。");
        }

        foreach (var key in DisallowedRootKeys.Where(key => manifest.TryGetProperty(key, out _)).Order(StringComparer.Ordinal))
        {
            errors.Add($"外部插件包清单不应定义根配置键：{key}");
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
            errors.Add($"{location} 必须是对象。");
            return;
        }

        var moduleId = GetPropertyText(module, "id").Trim();
        if (moduleId.Length == 0)
        {
            errors.Add($"{location}.id 为必填。");
        }
        else if (!moduleIds.Add(moduleId))
        {
            errors.Add($"重复的 module id：{moduleId}");
        }

        var moduleVersion = GetPropertyText(module, "version").Trim();
        if (!VersionPattern.IsMatch(moduleVersion))
        {
            errors.Add($"{location}.version 必须匹配 V<major>.<minor>.<patch>。");
        }

        foreach (var key in DisallowedModuleKeys.Where(key => module.TryGetProperty(key, out _)).Order(StringComparer.Ordinal))
        {
            errors.Add($"外部插件包清单不应主动定义运行时状态或布局字段：{location}.{key}");
        }

        var moduleRevitVersions = GetStringArray(module, "revitVersions");
        if (moduleRevitVersions.Count > 0 && !moduleRevitVersions.Contains("2020", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"{location}.revitVersions 声明了版本，但未包含 2020。");
        }

        var assembly = GetPropertyText(module, "assembly").Trim();
        if (assembly.Length > 0)
        {
            ValidatePayloadPath(manifestDirectory, assembly, $"{location}.assembly", errors);
        }
        else
        {
            errors.Add($"{location}.assembly 为必填，feature 默认继承它作为命令程序集。");
        }

        if (!TryGetArray(module, "features", out var features) || features.GetArrayLength() == 0)
        {
            errors.Add($"{location}.features 必须是非空数组。");
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
            errors.Add($"{location} 必须是对象。");
            return;
        }

        var featureId = GetPropertyText(feature, "id").Trim();
        if (featureId.Length == 0)
        {
            errors.Add($"{location}.id 为必填。");
        }
        else if (!featureIds.Add(featureId))
        {
            errors.Add($"重复的 feature id：{featureId}");
        }

        foreach (var key in DisallowedFeatureKeys.Where(key => feature.TryGetProperty(key, out _)).Order(StringComparer.Ordinal))
        {
            errors.Add($"外部插件包清单不应主动定义运行时状态或布局字段：{location}.{key}");
        }

        var commandAssembly = GetPropertyText(feature, "commandAssembly").Trim();
        if (commandAssembly.Length == 0)
        {
            commandAssembly = moduleAssembly;
        }

        if (commandAssembly.Length == 0)
        {
            errors.Add($"{location}.commandAssembly 在 module.assembly 为空时为必填。");
        }
        else
        {
            ValidatePayloadPath(manifestDirectory, commandAssembly, $"{location}.commandAssembly", errors);
        }

        var commandType = GetPropertyText(feature, "commandType").Trim();
        if (commandType.Length == 0)
        {
            errors.Add($"{location}.commandType 为必填。");
        }
        else if (!commandType.Contains('.', StringComparison.Ordinal) || commandType.Any(char.IsWhiteSpace))
        {
            errors.Add($"{location}.commandType 必须是完整 CLR 类型名。");
        }

        var iconPath = GetPropertyText(feature, "iconPath").Trim();
        if (iconPath.Length == 0)
        {
            warnings.Add($"{location}.iconPath 为空；PlugHub 将使用默认图标。");
        }
        else
        {
            ValidatePayloadPath(manifestDirectory, iconPath, $"{location}.iconPath", errors);
            if (!string.Equals(Path.GetExtension(iconPath), ".png", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{location}.iconPath 应指向 PNG 文件。");
            }
        }
    }

    private static void ValidatePayloadPath(string manifestDirectory, string relativePath, string label, List<string> errors)
    {
        if (!IsRelativePathInsideRoot(relativePath))
        {
            errors.Add($"{label} 必须是清单目录内的包相对路径：{relativePath}");
            return;
        }

        var resolved = Path.GetFullPath(Path.Combine(manifestDirectory, relativePath));
        if (!File.Exists(resolved))
        {
            errors.Add($"{label} 指向缺失的包文件：{relativePath}");
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
            Console.WriteLine($"警告：{warning}");
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Console.WriteLine($"错误：{error}");
            }

            Console.WriteLine($"PlugHub 插件包验证失败，共 {errors.Count} 个错误。");
            return 1;
        }

        Console.WriteLine("PlugHub 插件包验证通过。");
        return 0;
    }

    private sealed record ValidatorOptions(string PackageRoot, string Manifest);
}
