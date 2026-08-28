using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EbiosRM.Api.Infrastructure.Hebergement;

/// <summary>Ouvre l'URL de l'application dans le navigateur par défaut (mode bureau).</summary>
public static class LanceurNavigateur
{
    public static void Ouvrir(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                Process.Start("xdg-open", url);
            }
        }
        catch
        {
            // Pas de navigateur disponible (poste sans interface, session SSH...) :
            // l'utilisateur ouvrira l'URL affichée dans la console lui-même.
        }
    }
}
