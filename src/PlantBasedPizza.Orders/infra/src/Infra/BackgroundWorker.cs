using System.Collections.Generic;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.SSM;
using Constructs;
using PlantBasedPizza.Infra.Constructs;

namespace Infra;

public record BackgroundWorkerProps(SharedInfrastructureProps SharedProps, string ApplicationRoot, IStringParameter DatabaseConnectionParameter, IQueue LoyaltyPointsUpdatedQueue, IQueue DriverCollectedOrderQueue, IQueue DriverDeliveredOrderQueue, IQueue OrderBakedQueue, IQueue OrderPrepCompleteQueue, IQueue OrderPreparingQueue, IQueue OrderQualityCheckedQueue, IQueue PaymentSuccessfulQueue);

public class BackgroundWorker : Construct
{
    public IFunction LoyaltyPointsUpdatedFunction { get; private set; }
    public IFunction OrderBakedEventFunction { get; private set; }
    public IFunction OrderPrepCompleteEventFunction { get; private set; }
    public IFunction OrderPreparingEventFunction { get; private set; }
    public IFunction OrderQualityCheckedEventFunction { get; private set; }
    public IFunction DriverCollectedEventFunction { get; private set; }
    public IFunction DriverDeliveredEventFunction { get; private set; }
    
    public IFunction PaymentSuccessfulEventFunction { get; private set; }
    
    public BackgroundWorker(Construct scope, string id, BackgroundWorkerProps props) : base(scope, id)
    {
        var queueWorkerEnvVars = new Dictionary<string, string>()
        {
            { "Messaging__BusName", props.SharedProps.Bus.EventBusName },
            { "RedisConnectionString", "" },
            { "Services__PaymentInternal", "http://localhost:1234" },
            { "Services__Recipes", $"https://api.{props.SharedProps.Environment}.plantbasedpizza.net"},
            { "Auth__PaymentApiKey", "12345" },
            { "DATABASE_CONNECTION_PARAM_NAME", props.DatabaseConnectionParameter.ParameterName }
        };
        var serviceName = "OrderWorker";
        
        this.LoyaltyPointsUpdatedFunction = new QueueWorkerFunction(this, "LoyaltyPointsFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "LoyaltyPointsUpdated",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandleLoyaltyPointsUpdate_Generated::HandleLoyaltyPointsUpdate",
                props.LoyaltyPointsUpdatedQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        this.OrderBakedEventFunction = new QueueWorkerFunction(this, "OrderBakedFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "OrderBakedFunction",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandleOrderBakedEvent_Generated::HandleOrderBakedEvent",
                props.OrderBakedQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        this.DriverCollectedEventFunction = new QueueWorkerFunction(this, "DriverCollectedOrderFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "DriverCollectedOrderFunction",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandleDriverCollectedOrder_Generated::HandleDriverCollectedOrder",
                props.DriverCollectedOrderQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        this.DriverDeliveredEventFunction = new QueueWorkerFunction(this, "DriverDeliveredOrderFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "DriverDeliveredOrderFunction",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandleDriverDeliveredOrder_Generated::HandleDriverDeliveredOrder",
                props.DriverDeliveredOrderQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        this.OrderPreparingEventFunction = new QueueWorkerFunction(this, "OrderPreparingFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "OrderPreparingFunction",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandleOrderPreparingEvent_Generated::HandleOrderPreparingEvent",
                props.OrderPreparingQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        this.OrderPrepCompleteEventFunction = new QueueWorkerFunction(this, "OrderPrepCompleteFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "OrderPrepCompleteFunction",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandleOrderPrepCompleteEvent_Generated::HandleOrderPrepCompleteEvent",
                props.OrderPrepCompleteQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        this.OrderQualityCheckedEventFunction = new QueueWorkerFunction(this, "OrderQualityCheckedFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "OrderQualityCheckedFunction",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandleOrderQualityCheckedEvent_Generated::HandleOrderQualityCheckedEvent",
                props.OrderQualityCheckedQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        this.PaymentSuccessfulEventFunction = new QueueWorkerFunction(this, "PaymentSuccessfulFunction",
            new QueueWorkerFunctionProps(
                serviceName,
                "PaymentSuccessfulFunction",
                props.SharedProps.Environment,
                $"{props.ApplicationRoot}/BackgroundWorkers",
                "BackgroundWorkers::BackgroundWorkers.Functions_HandlePaymentSuccessfulEvent_Generated::HandlePaymentSuccessfulEvent",
                props.PaymentSuccessfulQueue,
                props.SharedProps.Vpc,
                props.SharedProps.CommitHash,
                queueWorkerEnvVars)).Function;
        
        props.DatabaseConnectionParameter.GrantRead(this.LoyaltyPointsUpdatedFunction);
        props.DatabaseConnectionParameter.GrantRead(this.DriverCollectedEventFunction);
        props.DatabaseConnectionParameter.GrantRead(this.DriverDeliveredEventFunction);
        props.DatabaseConnectionParameter.GrantRead(this.OrderBakedEventFunction);
        props.DatabaseConnectionParameter.GrantRead(this.OrderPreparingEventFunction);
        props.DatabaseConnectionParameter.GrantRead(this.OrderPrepCompleteEventFunction);
        props.DatabaseConnectionParameter.GrantRead(this.OrderQualityCheckedEventFunction);
        props.DatabaseConnectionParameter.GrantRead(this.PaymentSuccessfulEventFunction);
        
        props.SharedProps.Bus.GrantPutEventsTo(this.LoyaltyPointsUpdatedFunction);
        props.SharedProps.Bus.GrantPutEventsTo(this.DriverCollectedEventFunction);
        props.SharedProps.Bus.GrantPutEventsTo(this.DriverDeliveredEventFunction);
        props.SharedProps.Bus.GrantPutEventsTo(this.OrderBakedEventFunction);
        props.SharedProps.Bus.GrantPutEventsTo(this.OrderPreparingEventFunction);
        props.SharedProps.Bus.GrantPutEventsTo(this.OrderPrepCompleteEventFunction);
        props.SharedProps.Bus.GrantPutEventsTo(this.OrderQualityCheckedEventFunction);
        props.SharedProps.Bus.GrantPutEventsTo(this.PaymentSuccessfulEventFunction);
    }
}