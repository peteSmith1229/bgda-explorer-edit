namespace WorldExplorer.Infrastructure;

/// <summary>Formats counts with the right noun form: "1 item", "12 items".</summary>
public static class Plural
{
    public static string Of(int count, string singular, string? plural = null)
        => $"{count:N0} {(count == 1 ? singular : plural ?? singular + "s")}";
}
