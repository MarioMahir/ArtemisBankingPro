using System.Security.Cryptography;
using System.Text;
using ArtemisBankingPro.Core.Application.Interfaces.Services;

namespace ArtemisBankingPro.Infrastructure.Shared.Services;

public class HashingService : IHashingService
{
    public string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
