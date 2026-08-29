using EbiosRM.Api.Modules.Conformite;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static EbiosRM.Api.Modules.Reporting.RapportPdfStyle;

namespace EbiosRM.Api.Modules.Reporting;

/// <summary>
/// Annexe de conformité : pour chaque référentiel (ISO 27001, NIS2), le
/// tableau de couverture des exigences par le contenu de l'étude (socle A1 +
/// plan de traitement A5). Reflète l'état courant, pas un atelier figé.
/// </summary>
public sealed class RapportConformitePdfGenerator
{
    public byte[] Generer(string nomEtude, IReadOnlyList<ServiceConformite.RapportConformite> rapports)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily(Sans).FontColor(Encre));

                page.Header().Column(col =>
                {
                    col.Item().Text("EBIOS RISK MANAGER").FontFamily(MonoMedium).FontSize(8).FontColor(BleuFrance).LetterSpacing(0.05f);
                    col.Item().PaddingTop(2).Text("Annexe -- Mapping de conformité").FontFamily(SerifTitreSemiBold).FontSize(19).FontColor(Encre);
                    col.Item().Text(nomEtude).FontFamily(Mono).FontSize(8).FontColor(GrisTexte);
                    col.Item().PaddingTop(10).LineHorizontal(1.4f).LineColor(BleuFrance);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(18);

                    col.Item().Text("Croisement du socle de sécurité (Atelier 1) et du plan de traitement du risque (Atelier 5) avec les exigences des référentiels. La correspondance ISO/IEC 27001 -> NIS2 est indicative et doit être validée par l'analyste pour le contexte de l'entité.")
                        .FontSize(8.5f).Italic().FontColor(GrisTexte);

                    foreach (var rapport in rapports)
                        col.Item().Column(c => Section(c, rapport));
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(4).LineHorizontal(0.6f).LineColor(GrisLigne);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("EBIOS Risk Manager -- Annexe de conformité").FontFamily(Mono).FontSize(7).FontColor(GrisTexte);
                        row.RelativeItem().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontFamily(MonoMedium).FontSize(7).FontColor(GrisTexte);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string Titre(string referentiel) => referentiel switch
    {
        "Nis2" => "Directive NIS2 -- article 21 (mesures de gestion des risques)",
        _ => "ISO/IEC 27001:2022 -- Annexe A",
    };

    private static string LibelleCouverture(ServiceConformite.Couverture c) => c switch
    {
        ServiceConformite.Couverture.Conforme => "Conforme",
        ServiceConformite.Couverture.Partielle => "Partielle",
        ServiceConformite.Couverture.NonApplicable => "Non applicable",
        _ => "Non couverte",
    };

    private static string CouleurCouverture(ServiceConformite.Couverture c) => c switch
    {
        ServiceConformite.Couverture.Conforme => VertConforme,
        ServiceConformite.Couverture.Partielle => OrangeAlerte,
        ServiceConformite.Couverture.NonApplicable => GrisTexte,
        _ => RougeAlerte,
    };

    private static void Section(ColumnDescriptor col, ServiceConformite.RapportConformite rapport)
    {
        var s = rapport.Synthese;
        col.Item().Text(Titre(rapport.Referentiel)).FontFamily(SansSemiBold).FontSize(11).FontColor(BleuFrance);
        col.Item().PaddingTop(2).Text(text =>
        {
            text.Span($"{s.Conforme} conforme(s)").FontFamily(MonoMedium).FontSize(8).FontColor(VertConforme);
            text.Span($"  --  {s.Partielle} partielle(s)").FontFamily(MonoMedium).FontSize(8).FontColor(OrangeAlerte);
            text.Span($"  --  {s.NonCouverte} non couverte(s)").FontFamily(MonoMedium).FontSize(8).FontColor(RougeAlerte);
            if (s.NonApplicable > 0)
                text.Span($"  --  {s.NonApplicable} non applicable(s)").FontFamily(MonoMedium).FontSize(8).FontColor(GrisTexte);
        });

        col.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.ConstantColumn(52); cd.RelativeColumn(3); cd.ConstantColumn(72); cd.RelativeColumn(3); });

            foreach (var entete in new[] { "Code", "Exigence", "Couverture", "Mesures / socle" })
                table.Cell().Background(GrisFond).PaddingVertical(3).PaddingHorizontal(4).Text(entete).FontFamily(MonoMedium).FontSize(6.5f).FontColor(GrisTexte);

            var i = 0;
            foreach (var ligne in rapport.Lignes)
            {
                var fond = i++ % 2 == 1 ? GrisFond : "#FFFFFF";
                table.Cell().Background(fond).PaddingVertical(2.5f).PaddingHorizontal(4).Text(ligne.Code).FontFamily(Mono).FontSize(7.5f);
                table.Cell().Background(fond).PaddingVertical(2.5f).PaddingHorizontal(4).Text(ligne.Titre).FontSize(7.5f);
                table.Cell().Background(fond).PaddingVertical(2.5f).PaddingHorizontal(4).Text(LibelleCouverture(ligne.Couverture)).FontFamily(MonoMedium).FontSize(7).FontColor(CouleurCouverture(ligne.Couverture));

                var detail = ligne.EtatSocle is not null ? $"Socle : {ligne.EtatSocle}. " : "";
                detail += string.Join(" ; ", ligne.Mesures.Select(m => m.Description));
                table.Cell().Background(fond).PaddingVertical(2.5f).PaddingHorizontal(4).Text(detail.Length > 0 ? detail : "--").FontSize(7).FontColor(GrisTexte);
            }
        });
    }
}
