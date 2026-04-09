using System.Text.RegularExpressions;

namespace YtDlpGui.App.Services;

public sealed partial class ProgressParser
{
    [GeneratedRegex(@"\[download\]\s+(?<percent>\d{1,3}\.?\d*)%.*?at\s+(?<speed>[^\s]+).*?ETA\s+(?<eta>.+)$")]
    private static partial Regex ProgressRegex();

    public ProgressUpdate? Parse(string line)
    {
        var match = ProgressRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var percentText = match.Groups["percent"].Value;
        double? percent = null;

        if (double.TryParse(
            percentText,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed))
        {
            percent = Math.Clamp(parsed, 0, 100);
        }

        return new ProgressUpdate
        {
            Percent = percent,
            Speed = match.Groups["speed"].Value.Trim(),
            Eta = match.Groups["eta"].Value.Trim()
        };
    }
}
