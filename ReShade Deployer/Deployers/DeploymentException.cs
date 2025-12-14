using System;

namespace ReShadeDeployer;

public class DeploymentException(string message, Exception innerException) : Exception(message, innerException);