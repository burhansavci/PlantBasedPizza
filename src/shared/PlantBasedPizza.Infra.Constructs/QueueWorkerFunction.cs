using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.DotNet;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SQS;
using Constructs;
using Environment = System.Environment;

namespace PlantBasedPizza.Infra.Constructs;

public record QueueWorkerFunctionProps(
    string ServiceName,
    string FunctionName,
    string Env,
    string ProjectPath,
    string Handler,
    IQueue Queue,
    IVpc? Vpc,
    string CommitHash,
    Dictionary<string, string> EnvironmentVariables);

public class QueueWorkerFunction : Construct
{
    public IFunction Function { get; }

    public QueueWorkerFunction(Construct scope, string id, QueueWorkerFunctionProps props) : base(scope, id)
    {
        var functionName = $"{props.ServiceName}-{props.FunctionName}-{props.Env}";

        var defaultEnvironmentVariables = new Dictionary<string, string>
        {
            { "SERVICE_NAME", props.ServiceName },
            { "BUILD_VERSION", props.CommitHash },
            { "OtlpEndpoint", "http://localhost:4318/v1/traces" },
            { "OtlpUseHttp", "Y" },
            { "Environment", props.Env },
            { "ServiceDiscovery__MyUrl", "" },
            { "ServiceDiscovery__ServiceName", "" },
            { "ServiceDiscovery__ConsulServiceEndpoint", "" },
            { "Auth__Issuer", "https://plantbasedpizza.com" },
            { "Auth__Audience", "https://plantbasedpizza.com" },
            { "ENV", props.Env },
            { "DD_ENV", props.Env },
            { "DD_SERVICE", props.ServiceName },
            { "service", props.ServiceName },
            { "DD_VERSION", props.CommitHash },
            { "DD_API_KEY", Environment.GetEnvironmentVariable("DD_API_KEY") ?? "" },
            { "AWS_LAMBDA_EXEC_WRAPPER", "/opt/datadog_wrapper" },
            { "DD_SITE", "datadoghq.eu" },
            { "DD_GIT_COMMIT_SHA", props.CommitHash },
            { "DD_GIT_REPOSITORY_URL", "https://github.com/jeastham1993/PlantBasedPizza" },
            { "DD_IAST_ENABLED", "true" }
        };

        Function = new DotNetFunction(this, id,
            new DotNetFunctionProps
            {
                ProjectDir = props.ProjectPath,
                Handler = props.Handler,
                MemorySize = 1024,
                Timeout = Duration.Seconds(29),
                Runtime = Runtime.DOTNET_8,
                AllowAllOutbound = props.Vpc == null ? null : true,
                AllowPublicSubnet = props.Vpc == null ? null : true,
                Architecture = Architecture.ARM_64,
                Environment = defaultEnvironmentVariables.Union(props.EnvironmentVariables)
                    .ToDictionary(x => x.Key, x => x.Value),
                FunctionName = functionName,
                Layers =
                [
                    LayerVersion.FromLayerVersionArn(this, "DDExtension",
                        "arn:aws:lambda:eu-west-1:464622532012:layer:Datadog-Extension-ARM:57"),
                    LayerVersion.FromLayerVersionArn(this, "DDTrace",
                        "arn:aws:lambda:eu-west-1:464622532012:layer:dd-trace-dotnet-ARM:15")
                ],
                Vpc = props.Vpc,
                VpcSubnets = props.Vpc == null
                    ? null
                    : new SubnetSelection
                    {
                        Subnets = props.Vpc.PublicSubnets
                    },
                LogRetention = RetentionDays.ONE_DAY
            });

        Tags.Of(Function).Add("service", props.ServiceName);
        Tags.Of(Function).Add("version", props.CommitHash);

        Function.AddEventSource(new SqsEventSource(props.Queue, new SqsEventSourceProps
        {
            ReportBatchItemFailures = true
        }));
    }
}