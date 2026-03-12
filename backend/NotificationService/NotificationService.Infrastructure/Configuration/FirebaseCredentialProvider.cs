using CarDealer.Shared.Secrets;
using Microsoft.Extensions.Logging;
using NotificationService.Shared;

namespace NotificationService.Infrastructure.Configuration;

/// <summary>
/// Proveedor de credenciales de Firebase desde secretos.
/// Soporta tanto archivo JSON como variables de entorno individuales.
/// </summary>
public class FirebaseCredentialProvider
{
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<FirebaseCredentialProvider>? _logger;

    public FirebaseCredentialProvider(
        ISecretProvider? secretProvider = null,
        ILogger<FirebaseCredentialProvider>? logger = null)
    {
        _secretProvider = secretProvider ?? CompositeSecretProvider.CreateDefault();
        _logger = logger;
    }

    /// <summary>
    /// Verifica si Firebase está configurado.
    /// </summary>
    public bool IsConfigured()
    {
        // Verificar si hay service account JSON completo
        var serviceAccountJson = _secretProvider.GetSecret(SecretKeys.FirebaseServiceAccountJson);
        if (!string.IsNullOrEmpty(serviceAccountJson))
        {
            return true;
        }

        // O verificar credenciales individuales
        var projectId = _secretProvider.GetSecret(SecretKeys.FirebaseProjectId);
        var privateKey = _secretProvider.GetSecret(SecretKeys.FirebasePrivateKey);
        var clientEmail = _secretProvider.GetSecret(SecretKeys.FirebaseClientEmail);

        return !string.IsNullOrEmpty(projectId) &&
               !string.IsNullOrEmpty(privateKey) &&
               !string.IsNullOrEmpty(clientEmail);
    }

    /// <summary>
    /// Obtiene el Project ID de Firebase.
    /// </summary>
    public string? GetProjectId()
    {
        return _secretProvider.GetSecret(SecretKeys.FirebaseProjectId);
    }

    /// <summary>
    /// Obtiene el JSON del service account.
    /// Si está codificado en base64, lo decodifica.
    /// </summary>
    public string? GetServiceAccountJson()
    {
        // Primero intentar obtener JSON completo
        var json = _secretProvider.GetSecret(SecretKeys.FirebaseServiceAccountJson);

        if (!string.IsNullOrEmpty(json))
        {
            // Si parece base64, decodificar
            if (IsBase64String(json))
            {
                try
                {
                    var bytes = Convert.FromBase64String(json);
                    return System.Text.Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    _logger?.LogWarning("Failed to decode Firebase service account from base64");
                    return json;
                }
            }
            return json;
        }

        // Construir JSON desde credenciales individuales
        var projectId = _secretProvider.GetSecret(SecretKeys.FirebaseProjectId);
        var privateKey = _secretProvider.GetSecret(SecretKeys.FirebasePrivateKey);
        var clientEmail = _secretProvider.GetSecret(SecretKeys.FirebaseClientEmail);

        if (string.IsNullOrEmpty(projectId) ||
            string.IsNullOrEmpty(privateKey) ||
            string.IsNullOrEmpty(clientEmail))
        {
            _logger?.LogDebug("Firebase credentials not fully configured");
            return null;
        }

        // Decodificar private key si está en base64
        if (IsBase64String(privateKey))
        {
            try
            {
                var bytes = Convert.FromBase64String(privateKey);
                privateKey = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                _logger?.LogWarning("Failed to decode Firebase private key from base64");
            }
        }

        // Construir JSON del service account
        return $$"""
        {
            "type": "service_account",
            "project_id": "{{projectId}}",
            "private_key": "{{EscapeJsonString(privateKey)}}",
            "client_email": "{{clientEmail}}",
            "token_uri": "https://oauth2.googleapis.com/token"
        }
        """;
    }

    /// <summary>
    /// Guarda las credenciales en un archivo temporal y retorna la ruta.
    /// Útil para SDKs que requieren un archivo.
    /// </summary>
    public string? GetOrCreateCredentialFile()
    {
        var json = GetServiceAccountJson();
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"firebase-{Guid.NewGuid()}.json");
            File.WriteAllText(tempPath, json);

            _logger?.LogDebug("Firebase credentials written to temporary file: {Path}", tempPath);
            return tempPath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write Firebase credentials to file");
            return null;
        }
    }

    private static bool IsBase64String(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length % 4 != 0)
            return false;

        try
        {
            Convert.FromBase64String(s);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EscapeJsonString(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }
}
