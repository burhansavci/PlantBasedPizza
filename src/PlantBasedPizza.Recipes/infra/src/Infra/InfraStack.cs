using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.SSM;
using Constructs;
using HealthCheck = Amazon.CDK.AWS.ECS.HealthCheck;
using Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol;
using PlantBasedPizza.Infra.Constructs;

namespace Infra
{
    public class RecipeApiInfraStack : Stack
    {
        internal RecipeApiInfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var bus = EventBus.FromEventBusName(this, "SharedEventBus", "PlantBasedPizzaEvents");

            var vpc = Vpc.FromLookup(this, "MainVpc", new VpcLookupOptions()
            {
                VpcId = "vpc-06c60c0d760921bc6",
            });

            var databaseConnectionParam = StringParameter.FromSecureStringParameterAttributes(this, "DatabaseParameter",
                new SecureStringParameterAttributes()
                {
                    ParameterName = "/shared/database-connection"
                });

            var cluster = new Cluster(this, "RecipeServiceCluster", new ClusterProps(){
                Vpc = vpc
            });
        
            var commitHash = "33aa663";

            var recipeService = new WebService(this, "RecipeWebService", new ConstructProps(
                vpc,
                cluster,
                "RecipeApi",
                "/shared/dd-api-key",
                "/shared/jwt-key",
                "recipe-api",
                commitHash ?? "latest",
                8080,
                new Dictionary<string, string>
                {
                    { "Messaging__BusName", bus.EventBusName },
                    { "SERVICE_NAME", "RecipeWebApi" }
                },
                new Dictionary<string, Secret>(1)
                {
                    { "DatabaseConnection", Secret.FromSsmParameter(databaseConnectionParam) }
                },
                "arn:aws:elasticloadbalancing:eu-west-1:730335273443:loadbalancer/app/plant-based-pizza-shared-ingress/1c948325c1df4e86",
                "arn:aws:elasticloadbalancing:eu-west-1:730335273443:listener/app/plant-based-pizza-ingress/d99d1b57574af81c/396097df348029f2",
                "/recipes/health",
                "/recipes/*",
                51
            ));

            databaseConnectionParam.GrantRead(recipeService.ExecutionRole);
            bus.GrantPutEventsTo(recipeService.TaskRole);
        }
    }
}
