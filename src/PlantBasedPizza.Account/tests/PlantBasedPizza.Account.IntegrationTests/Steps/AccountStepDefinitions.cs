using System.Diagnostics;
using FluentAssertions;
using PlantBasedPizza.Account.IntegrationTests.Drivers;
using TechTalk.SpecFlow;

namespace PlantBasedPizza.Account.IntegrationTests.Steps;

[Binding]
public class AccountStepDefinitions
{
    private readonly AccountDriver _driver;
    private readonly ScenarioContext _scenarioContext;

    public AccountStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        this._driver = new AccountDriver();
    }
    [Given("an un-registered email address")]
    public async Task AnUnregisteredEmail()
    {
        Activity.Current = _scenarioContext.Get<Activity>("Activity");
        _scenarioContext.Add("emailAddress", "arandomemail@test.com");
        _scenarioContext.Add("password", "RandomPassword!23");
    }

    [Given(@"a user registers")]
    public async Task AUserRegisters()
    {
        Activity.Current = _scenarioContext.Get<Activity>("Activity");

        var emailAddress = $"{Guid.NewGuid().ToString()}@test.com";
        _scenarioContext.Add("emailAddress", emailAddress);
        
        var password = Guid.NewGuid().ToString();
        _scenarioContext.Add("password", password);

        var res = await this._driver.RegisterUser(emailAddress, password);

        res.Should().NotBeNull();
    }

    [Then(@"they should be able to successfully login")]
    public async Task TheyShouldBeAbleToLogin()
    {
        Activity.Current = _scenarioContext.Get<Activity>("Activity");
        var emailAddress = _scenarioContext.Get<string>("emailAddress");
        var password = _scenarioContext.Get<string>("password");
            
        var loginResponse = await this._driver.Login(emailAddress, password);

        loginResponse.Should().NotBeNull();
        loginResponse!.AuthToken.Should().NotBeEmpty();
    }
    
    [Then("they should not be able to login with an invalid password")]
    public async Task ThenTheyShouldNotLoginWithAnInvalidPassword()
    {
        Activity.Current = _scenarioContext.Get<Activity>("Activity");
        var emailAddress = _scenarioContext.Get<string>("emailAddress");
            
        var loginResponse = await this._driver.Login(emailAddress, "some random stuff");

        loginResponse.Should().BeNull();
    }
    
    [Then("they should not be able to login")]
    public async Task ThenTheyCantLogin()
    {
        Activity.Current = _scenarioContext.Get<Activity>("Activity");
        var emailAddress = _scenarioContext.Get<string>("emailAddress");
        var password = _scenarioContext.Get<string>("password");
            
        var loginResponse = await this._driver.Login(emailAddress, "some random stuff");

        loginResponse.Should().BeNull();
    }
}