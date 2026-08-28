using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EbiosRM.Api.Infrastructure.Hebergement;

/// <summary>Ouvre l'URL de l'application dans le navigateur par défaut (mode bureau).</summary>
public static class LanceurNavigateur
{
    public static void Ouvrir(string url, string? dossierJournal = null)
    {
        foreach (var tentative in Commandes(url))
        {
            try
            {
                var p = Process.Start(tentative);
                if (p is not null)
                    return;
            }
            catch (Exception ex)
            {
                Journaliser(dossierJournal, $"Echec {tentative.FileName} {tentative.Arguments} : {ex.Message}");
            }
        }
        Journaliser(dossierJournal, $"Impossible d'ouvrir un navigateur. Ouvrez manuellement : {url}");
    }

    private static IEnumerable<ProcessStartInfo> Commandes(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return new ProcessStartInfo(url) { UseShellExecute = true };
            yield return new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"") { CreateNoWindow = true };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return new ProcessStartInfo("open", url);
        }
        else
        {
            yield return new ProcessStartInfo("xdg-open", url);
            foreach (var nav in new[] { "gio", "gnome-open", "firefox", "google-chrome", "chromium", "chromium-browser" })
            {
                var args = nav == "gio" ? $"open {url}" : url;
                yield return new ProcessStartInfo(nav, args);
            }
        }
    }

    private static void Journaliser(string? dossier, string message)
    {
        if (string.IsNullOrEmpty(dossier))
            return;
        try
        {
            File.AppendAllText(Path.Combine(dossier, "demarrage.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch { /* pas de trace possible, tant pis */ }
    }
}
