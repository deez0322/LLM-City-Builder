using System.Collections.Generic;
using AITransformer;

public class TestConfiguration
{
    public string model { get; set; }
    public LLMExecutionOptions.SelfCorrectionType selfCorrection { get; set; }
    public LLMExecutionOptions.ContextFormatType contextFormat { get; set; }

    public override string ToString()
    {
        return $"{model}_{selfCorrection}_{contextFormat}";
    }
}

public static class TestConfigurations
{
    private static readonly List<TestConfiguration> _standardConfigurations;

    // Number of iterations for each test configuration
    public const int DEFAULT_ITERATIONS = 30;

    static TestConfigurations()
    {
        _standardConfigurations = new List<TestConfiguration>
        {
            new TestConfiguration
            {
                model = "o1-mini-2024-09-12",
                selfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                contextFormat = LLMExecutionOptions.ContextFormatType.SQL
            },
            new TestConfiguration
            {
                model = "o1-mini-2024-09-12",
                selfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                contextFormat = LLMExecutionOptions.ContextFormatType.MinimapText
            },
            new TestConfiguration
            {
                model = "o1-mini-2024-09-12",
                selfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                contextFormat = LLMExecutionOptions.ContextFormatType.JSON
            },
        };
    }

    // Get all standard configurations
    public static List<TestConfiguration> GetAllConfigurations()
    {
        return new List<TestConfiguration>(_standardConfigurations);
    }
}