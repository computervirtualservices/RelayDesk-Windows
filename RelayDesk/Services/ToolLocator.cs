namespace RelayDesk.Services;

public static class ToolLocator
{
    public static string? Find(string filename)
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "tools", filename),
            Path.Combine(ConfigStore.DirectoryPath, "tools", filename),
            Path.Combine(AppContext.BaseDirectory, filename)
        };

        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? "";
        candidates.AddRange(pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => Path.Combine(folder.Trim('"'), filename)));
        return candidates.FirstOrDefault(File.Exists);
    }
}
