using System.Diagnostics;
using FluentAssertions;
using PlantBasedPizza.LoyaltyPoints.IntegrationTest.Drivers;
using TechTalk.SpecFlow;

namespace PlantBasedPizza.LoyaltyPoints.IntegrationTest.Steps;

[Binding]
public sealed class LoyaltyPointSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly LoyaltyPointsDriver _driver;

    public LoyaltyPointSteps(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;

        this._driver = new LoyaltyPointsDriver();
    }
    
    [Given(@"the loyalty points are added for order (.*) with a value of (.*)")]
    public async Task LoyaltyPointsAreAdded(string customerId, string orderIdentifier, decimal orderValue)
    {
        Activity.Current = _scenarioContext.Get<Activity>("Activity");
        await this._driver.AddLoyaltyPoints(orderIdentifier, orderValue);
    }

    [Then(@"the total points should be greater than (.*)")]
    public async Task ThenTheTotalPointsShouldBe(int totalPoints)
    {
        Activity.Current = _scenarioContext.Get<Activity>("Activity");
        
        var points = await this._driver.GetLoyaltyPoints();
        var internalPoints = await this._driver.GetLoyaltyPointsInternal();

        points.TotalPoints.Should().BeGreaterThan(totalPoints);
    }
}