using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EbiosRM.Api.Modules.Reporting;

public sealed class RapportAtelier1PdfGenerator
{
    public byte[] Generer(RapportAtelier1Data data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Column(col =>
                {
                    col.Item().Text("EBIOS Risk Manager").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text("Atelier 1 — Cadrage et socle de sécurité").FontSize(16).Bold();
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(15);

                    col.Item().Column(c =>
                    {
                        c.Item().Text("Cadrage de l'étude").FontSize(13).Bold();
                        c.Item().PaddingTop(4).Text($"Étude : {data.NomEtude}");
                        c.Item().Text($"Périmètre : {data.Perimetre}");
                        c.Item().Text($"Statut : {data.Statut}");
                        c.Item().Text($"Validé le : {data.DateValidationUtc:dd/MM/yyyy HH:mm} UTC");
                    });

                    col.Item().Column(c =>
                    {
                        c.Item().Text("Valeurs métier et biens supports").FontSize(13).Bold();

                        foreach (var vm in data.ValeursMetier)
                        {
                            c.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Column(vmCol =>
                            {
                                vmCol.Item().Text(vm.Description).Bold();
                                vmCol.Item().Text($"Entité responsable : {vm.EntiteResponsable}").FontSize(9).FontColor(Colors.Grey.Darken1);

                                if (vm.BiensSupport.Count > 0)
                                {
                                    vmCol.Item().PaddingTop(4).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(3);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(2);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Text("Bien support").Bold().FontSize(9);
                                            header.Cell().Text("Type").Bold().FontSize(9);
                                            header.Cell().Text("Entité responsable").Bold().FontSize(9);
                                        });

                                        foreach (var bien in vm.BiensSupport)
                                        {
                                            table.Cell().Text(bien.Description).FontSize(9);
                                            table.Cell().Text(bien.Type).FontSize(9);
                                            table.Cell().Text(bien.EntiteResponsable).FontSize(9);
                                        }
                                    });
                                }
                            });
                        }
                    });

                    col.Item().Column(c =>
                    {
                        c.Item().Text("Événements redoutés").FontSize(13).Bold();

                        c.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Valeur métier").Bold().FontSize(9);
                                header.Cell().Text("Événement redouté").Bold().FontSize(9);
                                header.Cell().Text("Gravité").Bold().FontSize(9);
                            });

                            foreach (var er in data.EvenementsRedoutes)
                            {
                                table.Cell().Text(er.ValeurMetierDescription).FontSize(9);
                                table.Cell().Text(er.Description).FontSize(9);
                                table.Cell().Text(er.Gravite.ToString()).FontSize(9).Bold();
                            }
                        });
                    });

                    col.Item().Column(c =>
                    {
                        c.Item().Text("Socle de sécurité").FontSize(13).Bold();

                        if (data.ReferentielsApplicables.Count == 0)
                        {
                            c.Item().PaddingTop(4).Text("Aucun référentiel renseigné.").FontColor(Colors.Grey.Medium);
                        }
                        else
                        {
                            c.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Référentiel").Bold().FontSize(9);
                                    header.Cell().Text("État").Bold().FontSize(9);
                                });

                                foreach (var r in data.ReferentielsApplicables)
                                {
                                    table.Cell().Text(r.Nom).FontSize(9);
                                    table.Cell().Text(r.Etat).FontSize(9);
                                }
                            });
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Généré par EbiosRM — ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}
