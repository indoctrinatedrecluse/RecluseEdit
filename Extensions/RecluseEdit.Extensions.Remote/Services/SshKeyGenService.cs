using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RecluseEdit.Extensions.Remote.Services;

public record GeneratedKeyPair(string PublicKeyOpenSsh, string PrivateKeyPem, string KeyType, int KeySize);

/// <summary>
/// Generates RSA SSH key pairs and exports public keys formatted for ~/.ssh/authorized_keys.
/// </summary>
public static class SshKeyGenService
{
    public static GeneratedKeyPair GenerateRsaKey(int keySize = 2048, string comment = "user@recluseedit")
    {
        if (keySize < 2048) keySize = 2048;

        using var rsa = RSA.Create(keySize);

        // 1. Export Private Key in PKCS#8 or PKCS#1 PEM format
        var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();

        // 2. Export Public Key in OpenSSH format: "ssh-rsa <base64> <comment>"
        var parameters = rsa.ExportParameters(false);
        var openSshPublicKey = FormatRsaPublicKeyOpenSsh(parameters.Exponent!, parameters.Modulus!, comment);

        return new GeneratedKeyPair(openSshPublicKey, privateKeyPem, "RSA", keySize);
    }

    private static string FormatRsaPublicKeyOpenSsh(byte[] exponent, byte[] modulus, string comment)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Prefix: "ssh-rsa"
        WriteSshString(writer, Encoding.ASCII.GetBytes("ssh-rsa"));

        // Exponent (e)
        WriteSshBigInt(writer, exponent);

        // Modulus (n)
        WriteSshBigInt(writer, modulus);

        var base64 = Convert.ToBase64String(ms.ToArray());
        return $"ssh-rsa {base64} {comment}".Trim();
    }

    private static void WriteSshString(BinaryWriter writer, byte[] data)
    {
        var len = (uint)data.Length;
        // Big endian length
        writer.Write((byte)(len >> 24));
        writer.Write((byte)(len >> 16));
        writer.Write((byte)(len >> 8));
        writer.Write((byte)len);
        writer.Write(data);
    }

    private static void WriteSshBigInt(BinaryWriter writer, byte[] bytes)
    {
        // If highest bit is 1, prepend 0x00 so it's treated as positive in OpenSSH
        if (bytes.Length > 0 && (bytes[0] & 0x80) != 0)
        {
            var padded = new byte[bytes.Length + 1];
            padded[0] = 0x00;
            Buffer.BlockCopy(bytes, 0, padded, 1, bytes.Length);
            WriteSshString(writer, padded);
        }
        else
        {
            WriteSshString(writer, bytes);
        }
    }
}

