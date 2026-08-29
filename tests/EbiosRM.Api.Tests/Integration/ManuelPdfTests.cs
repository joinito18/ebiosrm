using System.Net;
using System.Text;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Manuel d'utilisation PDF (/aide/manuel.pdf) : assemble a partir des guides
/// Markdown embarques, memes fichiers que l'aide en ligne.
/// </summary>
public class ManuelPdfTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public ManuelPdfTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Le_manuel_pdf_est_genere_a_partir_des_guides()
    {
        var c = await NouveauCompteAsync(_factory, "manuel");

        var reponse = await c.GetAsync("/api/v1/aide/manuel.pdf");
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/pdf", reponse.Content.Headers.ContentType?.MediaType);

        var octets = await reponse.Content.ReadAsByteArrayAsync();
        // En-tete PDF + taille plausible pour ~10 guides.
        Assert.Equal("%PDF", Encoding.ASCII.GetString(octets, 0, 4));
        Assert.True(octets.Length > 20_000, $"PDF trop court ({octets.Length} octets)");
    }
}
