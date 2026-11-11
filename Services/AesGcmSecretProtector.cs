using System.Security.Cryptography;
using System.Text;
using LoboEsteparioAPI.Options;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Services;

/// <summary>AES-256-GCM. Formato: Base64(nonce[12] | tag[16] | texto cifrado).</summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[]? _key;

    public AesGcmSecretProtector(IOptions<SecurityOptions> options)
    {
        var raw = options.Value.SecretsKey;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        try
        {
            var key = Convert.FromBase64String(raw.Trim());
            if (key.Length == 32)
            {
                _key = key;
            }
        }
        catch (FormatException)
        {
            // Clave mal formada: IsConfigured queda en false.
        }
    }

    public bool IsConfigured => _key is not null;

    public string Protect(string plainText, string context)
    {
        var key = RequireKey();
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag, Encoding.UTF8.GetBytes(context));

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipher.CopyTo(payload, NonceSize + TagSize);
        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue, string context)
    {
        var key = RequireKey();
        var payload = Convert.FromBase64String(protectedValue);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Valor cifrado inválido.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain, Encoding.UTF8.GetBytes(context));
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] RequireKey() =>
        _key ?? throw new InvalidOperationException("Falta Security:SecretsKey (32 bytes en Base64) para cifrar secretos.");
}
