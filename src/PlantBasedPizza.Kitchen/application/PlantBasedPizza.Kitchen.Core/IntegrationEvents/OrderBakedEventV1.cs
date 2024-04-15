using PlantBasedPizza.Events;

namespace PlantBasedPizza.Kitchen.Core.IntegrationEvents;

public class OrderBakedEventV1 : IntegrationEvent
{
    public override string EventName => "kitchen.orderBaked";
    public override string EventVersion => "v1";
    public override Uri Source => new Uri("https://kitchen.plantbasedpizza");

    public string OrderIdentifier { get; init; } = "";

    public string KitchenIdentifier { get; init; } = "";
}