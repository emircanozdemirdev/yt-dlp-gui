namespace YtDlpGui.App.Infrastructure;

/// <summary>
/// Maps the real user profile path to a stable display form using "Users\user" instead of the OS account name.
/// </summary>
public static class UserProfilePathDisplay
{
    public static string MaskForDisplay(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return absolutePath;
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(profile))
        {
            return absolutePath;
        }

        if (!absolutePath.StartsWith(profile, StringComparison.OrdinalIgnoreCase))
        {
            return absolutePath;
        }

        var rest = absolutePath[profile.Length..].TrimStart('\\', '/');
        var root = Path.GetPathRoot(profile);
        if (string.IsNullOrEmpty(root))
        {
            return absolutePath;
        }

        var fakeProfile = Path.Combine(root, "Users", "user");
        return string.IsNullOrEmpty(rest) ? fakeProfile : Path.Combine(fakeProfile, rest);
    }

    public static string UnmaskFromDisplay(string displayPath)
    {
        if (string.IsNullOrWhiteSpace(displayPath))
        {
            return displayPath;
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(profile))
        {
            return displayPath;
        }

        var root = Path.GetPathRoot(profile);
        if (string.IsNullOrEmpty(root))
        {
            return displayPath;
        }

        var fakeProfile = Path.Combine(root, "Users", "user");
        if (!displayPath.StartsWith(fakeProfile, StringComparison.OrdinalIgnoreCase))
        {
            return displayPath;
        }

        var rest = displayPath[fakeProfile.Length..].TrimStart('\\', '/');
        return string.IsNullOrEmpty(rest) ? profile : Path.Combine(profile, rest);
    }
}
