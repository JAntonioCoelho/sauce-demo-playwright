using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Pages;

public sealed class CheckoutInformationPage(IPage page) : BasePage(page)
{
    private ILocator FirstNameInput => Page.ByTestId("firstName");
    private ILocator LastNameInput => Page.ByTestId("lastName");
    private ILocator PostalCodeInput => Page.ByTestId("postalCode");
    private ILocator ContinueButton => Page.ByTestId("continue");

    public ILocator ErrorMessage => Page.ByTestId("error");

    public async Task FillAsync(CustomerInfo customer)
    {
        await FirstNameInput.FillAsync(customer.FirstName);
        await LastNameInput.FillAsync(customer.LastName);
        await PostalCodeInput.FillAsync(customer.PostalCode);
    }

    public Task SubmitAsync() => ContinueButton.ClickAsync();

    public async Task<CheckoutOverviewPage> ContinueWithAsync(CustomerInfo customer)
    {
        await FillAsync(customer);
        await SubmitAsync();
        return new CheckoutOverviewPage(Page);
    }
}
