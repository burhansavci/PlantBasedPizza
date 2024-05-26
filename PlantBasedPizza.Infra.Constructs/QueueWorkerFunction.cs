using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.DotNet;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace PlantBasedPizza.Infra.Constructs;

public record QueueWorkerFunctionProps(string ServiceName, string FunctionName, string Env, string ProjectPath, string Handler, IQueue Queue, IVpc? Vpc, string CommitHash, Dictionary<string, string> EnvironmentVariables);

public class QueueWorkerFunction : Construct
{
    public IFunction Function { get; private set; }
    
    public QueueWorkerFunction(Construct scope, string id, QueueWorkerFunctionProps props) : base(scope, id)
    {
        var functionName = $"{props.ServiceName}-{props.FunctionName}-{props.Env}";
        
        var defaultEnvironmentVariables = new Dictionary<string, string>()
        {
            { "SERVICE_NAME", functionName },
            { "BUILD_VERSION", "dev" },
            { "OtlpEndpoint", "http://localhost:4318/v1/traces" },
            { "OtlpUseHttp", "Y" },
            { "Environment", "dev" },
            { "ServiceDiscovery__MyUrl", "" },
            { "ServiceDiscovery__ServiceName", "" },
            { "ServiceDiscovery__ConsulServiceEndpoint", "" },
            { "Auth__Issuer", "https://plantbasedpizza.com" },
            { "Auth__Audience", "https://plantbasedpizza.com" },
            { "ENV", System.Environment.GetEnvironmentVariable("DEPLOYMENT_ENV") ?? "dev" },
            { "DD_ENV", System.Environment.GetEnvironmentVariable("DEPLOYMENT_ENV") ?? "dev" },
            { "DD_SERVICE", functionName },
            { "DD_VERSION", props.CommitHash },
            { "DD_API_KEY", System.Environment.GetEnvironmentVariable("DATADOG_API_KEY") ?? "" },
            { "DD_OTLP_CONFIG_RECEIVER_PROTOCOLS_HTTP_ENDPOINT", "localhost:4318" },
            { "AWS_LAMBDA_EXEC_WRAPPER", "/opt/datadog_wrapper" },
            { "DD_SITE", "datadoghq.eu" },
        };
        
        Function = new DotNetFunction(this, "LoyaltyPointsUpdatedFunction",
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
                Environment = defaultEnvironmentVariables.Union(props.EnvironmentVariables).ToDictionary(x => x.Key, x => x.Value),
                FunctionName = functionName,
                Layers =
                [
                    LayerVersion.FromLayerVersionArn(this, "DDExtension", "arn:aws:lambda:eu-west-1:464622532012:layer:Datadog-Extension-ARM:57")
                ],
                Vpc = props.Vpc,
                VpcSubnets = props.Vpc == null ? null : new SubnetSelection()
                {
                    Subnets = props.Vpc.PublicSubnets
                },
            });

        this.Function.AddEventSource(new SqsEventSource(props.Queue, new SqsEventSourceProps()
        {
            ReportBatchItemFailures = true
        }));
    }
}