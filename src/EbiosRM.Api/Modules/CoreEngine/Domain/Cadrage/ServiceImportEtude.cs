using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using EbiosRM.Api.Infrastructure.Persistence;
using EbiosRM.Api.Modules.CoreEngine.Domain.ScenariosDeRisque;
using EbiosRM.Api.Modules.CoreEngine.Domain.SourcesRisque;
using Microsoft.EntityFrameworkCore;

namespace EbiosRM.Api.Modules.CoreEngine.Domain.Cadrage;

/// <summary>
/// Reconstruit une étude à partir d'un fichier JSON produit par
/// <c>GET /etudes/{id}/export</c> (installation différente, sauvegarde,
/// transfert entre comptes non partagés...).
///
/// Le fichier est désérialisé en entités du domaine (setters et champs privés
/// ouverts pour la seule désérialisation, cf. <see cref="OptionsEntitesInternes"/>),
/// puis <see cref="RecableurClesEtude"/> ré-attribue toutes les clés (mode
/// strict : une référence interne cassée = fichier rejeté). Entrée non fiable :
/// tout échec renvoie un message, jamais une 500, et rien n'est écrit
/// (transaction).
///
/// Comme la duplication : snapshots, journal et membres non importés ; les 5
/// ateliers repartent en brouillon.
/// </summary>
public sealed class ServiceImportEtude
{
    private readonly EbiosDbContext _db;

    public ServiceImportEtude(EbiosDbContext db)
    {
        _db = db;
    }

    public sealed record Resultat(Guid? EtudeId, string? Erreur);

    public async Task<Resultat> ImporterAsync(Stream contenu, Guid proprietaireId, CancellationToken ct)
    {
        Enveloppe? enveloppe;
        try
        {
            enveloppe = await JsonSerializer.DeserializeAsync<Enveloppe>(contenu, OptionsEntitesInternes, ct);
        }
        catch (JsonException ex)
        {
            return new Resultat(null, $"Fichier JSON illisible : {ex.Message}");
        }

        if (enveloppe is null || enveloppe.Etude is null)
            return new Resultat(null, "Le fichier ne contient pas d'étude exploitable.");
        if (enveloppe.FormatVersion != 1)
            return new Resultat(null, $"Version de format non reconnue (attendu : 1, reçu : {enveloppe.FormatVersion}). Ce fichier vient probablement d'une version plus récente de l'application.");
        if (string.IsNullOrWhiteSpace(enveloppe.Etude.Nom)
            || string.IsNullOrWhiteSpace(enveloppe.Etude.Perimetre)
            || string.IsNullOrWhiteSpace(enveloppe.Etude.Mission))
            return new Resultat(null, "L'étude du fichier est incomplète (nom, périmètre ou mission manquant).");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var nouvelleEtude = Etude.Creer(
            enveloppe.Etude.Nom.Trim() + " (importée)",
            enveloppe.Etude.Perimetre.Trim(),
            enveloppe.Etude.Mission.Trim(),
            proprietaireId);
        _db.Etudes.Add(nouvelleEtude);

        var recableur = new RecableurClesEtude(_db, strict: true);
        recableur.EnregistrerEtude(enveloppe.EtudeSourceId, nouvelleEtude.Id);

        // Ordre = ordre de la chaîne de traçabilité, mais sans importance ici
        // (aucune FK inter-agrégats en base). ReserverId d'abord sur tout, pour
        // que les références "en avant" (un chemin -> une partie prenante) soient
        // déjà dans la table.
        var sections = new IReadOnlyList<object>?[]
        {
            enveloppe.ValeursMetier, enveloppe.BiensSupport, enveloppe.EvenementsRedoutes,
            Liste(enveloppe.SocleSecurite), enveloppe.CouplesSourceRisqueObjectifVise, enveloppe.PartiesPrenantes,
            enveloppe.ScenariosStrategiques, enveloppe.CheminsAttaque, enveloppe.ScenariosOperationnels,
            enveloppe.ScenariosDeRisque, Liste(enveloppe.PlanTraitementRisque),
        };

        foreach (var entite in sections.Where(s => s is not null).SelectMany(s => s!))
            recableur.ReserverId(entite);

        try
        {
            Rattacher(recableur, enveloppe.ValeursMetier);
            Rattacher(recableur, enveloppe.BiensSupport, "ValeurMetierId");
            Rattacher(recableur, enveloppe.EvenementsRedoutes, "ValeurMetierId");
            Rattacher(recableur, Liste(enveloppe.SocleSecurite));
            Rattacher(recableur, enveloppe.CouplesSourceRisqueObjectifVise);
            Rattacher(recableur, enveloppe.PartiesPrenantes);
            Rattacher(recableur, enveloppe.ScenariosStrategiques, "CoupleSourceRisqueObjectifViseId", "EvenementRedouteId");
            Rattacher(recableur, enveloppe.CheminsAttaque, "ScenarioStrategiqueId");
            Rattacher(recableur, enveloppe.ScenariosOperationnels, "CheminAttaqueId");
            Rattacher(recableur, enveloppe.ScenariosDeRisque, "CheminAttaqueId");
            Rattacher(recableur, Liste(enveloppe.PlanTraitementRisque));

            await _db.SaveChangesAsync(ct);
        }
        catch (ReferenceIntrouvableException ex)
        {
            return new Resultat(null, ex.Message);
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException or ArgumentException)
        {
            return new Resultat(null, $"Le contenu du fichier est invalide : {ex.Message}");
        }

        await transaction.CommitAsync(ct);
        return new Resultat(nouvelleEtude.Id, null);
    }

    private static void Rattacher(RecableurClesEtude recableur, IEnumerable<object>? entites, params string[] clesEtrangeres)
    {
        if (entites is null) return;
        foreach (var entite in entites)
            recableur.Rattacher(entite, clesEtrangeres);
    }

    private static IReadOnlyList<object>? Liste(object? entiteUnique)
        => entiteUnique is null ? null : new[] { entiteUnique };

    /// <summary>
    /// Enveloppe de l'export. Les collections sont désérialisées directement en
    /// entités du domaine grâce à <see cref="OptionsEntitesInternes"/>.
    /// <c>EtudeSourceId</c> vient du bloc <c>etude</c> du fichier (son ancien Id)
    /// et sert de clé "étude" dans le recableur.
    /// </summary>
    private sealed record Enveloppe(
        int FormatVersion,
        EnteteEtude? Etude,
        List<ValeurMetier>? ValeursMetier,
        List<BienSupport>? BiensSupport,
        List<EvenementRedoute>? EvenementsRedoutes,
        SocleSecurite? SocleSecurite,
        List<CoupleSourceRisqueObjectifVise>? CouplesSourceRisqueObjectifVise,
        List<PartiePrenante>? PartiesPrenantes,
        List<ScenarioStrategique>? ScenariosStrategiques,
        List<CheminAttaque>? CheminsAttaque,
        List<ScenarioOperationnel>? ScenariosOperationnels,
        List<ScenarioDeRisque>? ScenariosDeRisque,
        PlanTraitementRisque? PlanTraitementRisque)
    {
        public Guid EtudeSourceId => Etude?.Id ?? Guid.Empty;
    }

    private sealed record EnteteEtude(Guid Id, string? Nom, string? Perimetre, string? Mission);

    /// <summary>
    /// Options de désérialisation qui ouvrent les setters privés et les champs
    /// de collection privés (<c>_referentiels</c>, <c>_mesures</c>,
    /// <c>_scenariosDeRisqueIds</c>...) des entités du domaine -- uniquement pour
    /// l'import. Ne s'applique qu'aux types sous
    /// <c>EbiosRM.Api.Modules.CoreEngine.Domain</c>.
    /// </summary>
    private static readonly JsonSerializerOptions OptionsEntitesInternes = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { OuvrirMembresPrivesDuDomaine },
        },
    };

    private const string NamespaceDomaine = "EbiosRM.Api.Modules.CoreEngine.Domain";

    private static void OuvrirMembresPrivesDuDomaine(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
        if (typeInfo.Type.Namespace?.StartsWith(NamespaceDomaine, StringComparison.Ordinal) != true) return;

        // Instanciation via le constructeur privé sans paramètre des entités.
        var ctor = typeInfo.Type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, binder: null, Type.EmptyTypes, modifiers: null);
        if (typeInfo.CreateObject is null && ctor is not null)
            typeInfo.CreateObject = () => ctor.Invoke(null);

        foreach (var propriete in typeInfo.Properties)
        {
            if (propriete.Set is not null) continue;

            var clr = typeInfo.Type.GetProperty(propriete.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (clr?.GetSetMethod(nonPublic: true) is { } setter)
            {
                propriete.Set = (obj, valeur) => setter.Invoke(obj, new[] { valeur });
                continue;
            }

            // Champ de backing : auto-propriété (<Nom>k__BackingField) ou champ
            // écrit à la main "_nom" (collections owned + _scenariosDeRisqueIds).
            var champ = typeInfo.Type.GetField($"<{propriete.Name}>k__BackingField",
                            BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? typeInfo.Type.GetField(
                            "_" + char.ToLowerInvariant(propriete.Name[0]) + propriete.Name[1..],
                            BindingFlags.NonPublic | BindingFlags.Instance);
            if (champ is not null)
                propriete.Set = champ.SetValue;
        }
    }
}
