namespace AoE4WorldReplaysParser;

[AttributeUsage(AttributeTargets.Field)]
public class ExpectedValueAttribute : Attribute
{
    public int ExpectedValue { get; set; }
    public string Message { get; set; }
    public bool Fatal { get; set; }

    public ExpectedValueAttribute(int value, string message, bool fatal = false)
    {
        ExpectedValue = value;
        Message = message;
        Fatal = fatal;
    }
}
