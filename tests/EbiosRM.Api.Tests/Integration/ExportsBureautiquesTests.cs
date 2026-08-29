using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static EbiosRM.Api.Tests.Integration.OutilsEtudeDeTest;

namespace EbiosRM.Api.Tests.Integration;

/// <summary>
/// Exports bureautiques (feuille de route point 8) : registre des risques en
/// Excel, synthèse en Word, portefeuille en Excel. On vérifie que ce sont des
/// fichiers OOXML valides et qu'ils contiennent les données de l'étude.
/// </summary>
public class ExportsBureautiquesTests : IClassFixture<EbiosApiFactory>
{
    private readonly EbiosApiFactory _factory;

    public ExportsBureautiquesTests(EbiosApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> LireTexteZip(byte[] fichier, params string[] entreesSouhaitees)
    {
        using var zip = new ZipArchive(new MemoryStream(fichier), ZipArchiveMode.Read);
        var sb = new StringBuilder();
        foreach (var entree in zip.Entries)
        {
            if (entreesSouhaitees.Length > 0 && !entreesSouhaitees.Any(s => entree.FullName.Contains(s)))
                continue;
            using var r = new StreamReader(entree.Open());
            sb.Append(await r.ReadToEndAsync());
        }
        return sb.ToString();
    }

    [Fact]
    public async Task Le_registre_excel_contient_les_feuilles_et_les_donnees_de_l_etude()
    {
        var c = await NouveauCompteAsync(_factory, "export-xlsx");
        var etudeId = await ConstruireEtudeCompleteAsync(c);

        var reponse = await c.GetAsync($"/api/v1/etudes/{etudeId}/exports/registre.xlsx");
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            reponse.Content.Headers.ContentType?.MediaType);

        var octets = await reponse.Content.ReadAsByteArrayAsync();
        Assert.Equal((byte)'P', octets[0]);
        Assert.Equal((byte)'K', octets[1]);

        var workbook = await LireTexteZip(octets, "workbook.xml");
        Assert.Contains("Scénarios de risque", workbook);
        Assert.Contains("Plan de traitement", workbook);
        Assert.Contains("Conformité", workbook);

        var contenu = await LireTexteZip(octets);  // tout le classeur (feuilles + chaînes)
        Assert.Contains("Infogéreur", contenu);   // partie prenante de l'étude type (feuille Écosystème)
    }

    [Fact]
    public async Task La_synthese_word_reprend_le_registre_des_risques()
    {
        var c = await NouveauCompteAsync(_factory, "export-docx");
        var etudeId = await ConstruireEtudeCompleteAsync(c);

        var reponse = await c.GetAsync($"/api/v1/etudes/{etudeId}/exports/synthese.docx");
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            reponse.Content.Headers.ContentType?.MediaType);

        var texte = await LireTexteZip(await reponse.Content.ReadAsByteArrayAsync(), "word/document.xml");
        Assert.Contains("Synthèse de l'étude", texte);
        Assert.Contains("Registre des risques", texte);
        Assert.Contains("Plan de traitement", texte);
        Assert.Contains("Rebond depuis le SI", texte); // chemin d'attaque de l'étude type (dans le registre)
        Assert.Contains("Sauvegardes hors-ligne", texte); // mesure de traitement de l'étude type
    }

    [Fact]
    public async Task Le_portefeuille_excel_liste_les_etudes_de_l_utilisateur()
    {
        var c = await NouveauCompteAsync(_factory, "export-portef");
        var etudeId = await ConstruireEtudeCompleteAsync(c);
        var nom = (await (await c.GetAsync($"/api/v1/etudes/{etudeId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("nom").GetString();

        var reponse = await c.GetAsync("/api/v1/portefeuille/export.xlsx");
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var contenu = await LireTexteZip(await reponse.Content.ReadAsByteArrayAsync(), "sheet", "sharedStrings");
        Assert.Contains(nom!, contenu);
    }

    [Fact]
    public async Task Etude_introuvable_renvoie_404()
    {
        var c = await NouveauCompteAsync(_factory, "export-404");
        Assert.Equal(HttpStatusCode.NotFound,
            (await c.GetAsync($"/api/v1/etudes/{Guid.NewGuid()}/exports/registre.xlsx")).StatusCode);
    }
}
