using System;

namespace ReShadeDeployer;

public class DeploymentException : Exception
{
    public DeploymentException(string message, Exception innerException) : base(message, innerException) { }
    public DeploymentException(Exception innerException) : base(string.Empty, innerException) { }
}