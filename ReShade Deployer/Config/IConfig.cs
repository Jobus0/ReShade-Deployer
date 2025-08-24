using System;

namespace ReShadeDeployer;

public interface IConfig
{
    /// <summary>
    /// Represents the latest online ReShade version number.
    /// </summary>
    string LatestReShadeVersionNumber { get; set; }

    /// <summary>
    /// Represents the date of the last check for a new ReShade version.
    /// </summary>
    DateTime LatestReShadeVersionNumberCheckDate { get; set; }

    /// <summary>
    /// Represents the latest online ReShade Deployer version number.
    /// </summary>
    string LatestDeployerVersionNumber { get; set; }

    /// <summary>
    /// Represents the date of the last check for a new ReShade Deployer version.
    /// </summary>
    DateTime LatestDeployerVersionNumberCheckDate { get; set; }

    /// <summary>
    /// Whether the application should automatically exit after deployment.
    /// </summary>
    bool AlwaysExitOnDeploy { get; set; }
}