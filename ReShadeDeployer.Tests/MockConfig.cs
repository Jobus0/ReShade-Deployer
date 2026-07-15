namespace ReShadeDeployer.Tests;

public class MockConfig : IConfig
{
    public string LatestReShadeVersionNumber { get; set; } = "1.0.0";
    public DateTime LatestReShadeVersionNumberCheckDate { get; set; } = DateTime.MinValue;
    public string LatestDeployerVersionNumber { get; set; } = "1.0.0";
    public DateTime LatestDeployerVersionNumberCheckDate { get; set; } = DateTime.MinValue;
    public bool AlwaysExitOnDeploy { get; set; } = false;
    public bool UseLocalPresets { get; set; } = false;
    public string[] LastSelectedAddons { get; set; } = Array.Empty<string>();
}