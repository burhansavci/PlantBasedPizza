namespace PlantBasedPizza.Account.Core;

public interface IUserAccountRepository
{
    Task<UserAccount> CreateAccount(UserAccount userAccount);

    Task<UserAccount> ValidateCredentials(string emailAddress, string password);
    
    Task SeedInitialUser();
}