namespace EbiosRM.Api.Tests.Integration;

public class SmokeTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public SmokeTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_repond_200_et_base_connectee()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"database\":\"connected\"", body);
    }
}
