namespace AoE4WorldReplaysAPI.Services;

public static class StringExtensions
{
    public static string? ToHex(this byte[] data)
    {
        if (data == null)
            return null;

        return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
    }
}
