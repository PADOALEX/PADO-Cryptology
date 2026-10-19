using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Cryptology.Core.NumberTheory;

namespace Cryptology.Core.SecretSharing;

public sealed record Share(BigInteger X, BigInteger Y);
public sealed record ShamirShareSet(BigInteger Prime, int Threshold, int TotalShares, IReadOnlyList<Share> Shares);

/// <summary>Educational prime-field Shamir sharing. Shares are unauthenticated; threshold-sized tampering is not generally detectable.</summary>
public static class ShamirSecretSharing
{
    public static ShamirShareSet Split(BigInteger secret, int threshold, int shareCount, BigInteger prime, int rounds = 32, CancellationToken cancellationToken = default)
    {
        ValidateParameters(prime, threshold, shareCount, rounds, cancellationToken);
        if (secret < 0 || secret >= prime) throw new ArgumentOutOfRangeException(nameof(secret), "Secret must satisfy 0 <= secret < prime.");
        var coefficients = new BigInteger[threshold];
        try
        {
            coefficients[0] = secret;
            for (var i = 1; i < threshold - 1; i++) coefficients[i] = CryptoRandom.Below(prime, cancellationToken);
            if (threshold > 1) coefficients[^1] = CryptoRandom.InRange(1, prime - 1, cancellationToken);
            var shares = new List<Share>(shareCount);
            for (var index = 0; index < shareCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BigInteger x = index + 1;
                BigInteger y = 0;
                for (var i = coefficients.Length - 1; i >= 0; i--)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    y = (y * x + coefficients[i]) % prime;
                }
                shares.Add(new(x, y));
            }
            return new(prime, threshold, shareCount, shares.AsReadOnly());
        }
        finally { Array.Clear(coefficients); }
    }

    public static BigInteger Recover(ShamirShareSet set, int rounds = 32, CancellationToken cancellationToken = default)
    {
        ValidateSet(set, rounds, cancellationToken);
        // The first k points define the candidate polynomial. Extra points must lie on it.
        var basis = set.Shares.Take(set.Threshold).ToArray();
        var weights = new BigInteger[basis.Length];
        for (var i = 0; i < basis.Length; i++)
        {
            BigInteger denominator = 1;
            for (var j = 0; j < basis.Length; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (i != j) denominator = IntegerMath.Normalize(denominator * (basis[i].X - basis[j].X), set.Prime);
            }
            weights[i] = IntegerMath.ModInverse(denominator, set.Prime, cancellationToken);
        }
        var recovered = EvaluateAt(0, basis, weights, set.Prime, cancellationToken);
        for (var i = set.Threshold; i < set.Shares.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (EvaluateAt(set.Shares[i].X, basis, weights, set.Prime, cancellationToken) != set.Shares[i].Y)
                throw new ArgumentException("Additional shares are inconsistent with the threshold polynomial.", nameof(set));
        }
        return recovered;
    }

    public static string Export(ShamirShareSet set, int rounds = 32, CancellationToken cancellationToken = default)
    {
        ValidateSet(set, rounds, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            version = 1,
            prime = Decimal(set.Prime),
            threshold = set.Threshold,
            totalShares = set.TotalShares,
            shares = set.Shares.Select(s =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new { x = Decimal(s.X), y = Decimal(s.Y) };
            })
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    public static ShamirShareSet Import(string json, int rounds = 32, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            var root = ReadProperties(document.RootElement, ["version", "prime", "threshold", "totalShares", "shares"]);
            if (root["version"].GetInt32() != 1) throw new FormatException("Unsupported share format version.");
            var prime = ReadInteger(root["prime"]);
            var threshold = root["threshold"].GetInt32();
            var count = root["totalShares"].GetInt32();
            if (root["shares"].ValueKind != JsonValueKind.Array) throw new FormatException("Shares must be an array.");
            var points = new List<Share>();
            foreach (var element in root["shares"].EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var point = ReadProperties(element, ["x", "y"]);
                points.Add(new(ReadInteger(point["x"]), ReadInteger(point["y"])));
            }
            var result = new ShamirShareSet(prime, threshold, count, points.AsReadOnly());
            ValidateSet(result, rounds, cancellationToken);
            return result;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or OverflowException)
        {
            throw new FormatException("Invalid Shamir share document.", exception);
        }
    }

    private static BigInteger EvaluateAt(BigInteger x, IReadOnlyList<Share> basis, IReadOnlyList<BigInteger> weights, BigInteger prime, CancellationToken cancellationToken)
    {
        BigInteger result = 0;
        for (var i = 0; i < basis.Count; i++)
        {
            BigInteger numerator = 1;
            for (var j = 0; j < basis.Count; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (i != j) numerator = IntegerMath.Normalize(numerator * (x - basis[j].X), prime);
            }
            result = IntegerMath.Normalize(result + basis[i].Y * numerator * weights[i], prime);
        }
        return result;
    }

    private static void ValidateParameters(BigInteger prime, int threshold, int totalShares, int rounds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (threshold < 1 || totalShares < threshold) throw new ArgumentException("Require 1 <= threshold <= total shares.");
        if (prime <= totalShares) throw new ArgumentException("Field prime must exceed total shares.", nameof(prime));
        if (!PrimalityTest.MillerRabin(prime, rounds, cancellationToken)) throw new ArgumentException("Field modulus must be prime.", nameof(prime));
    }

    private static void ValidateSet(ShamirShareSet set, int rounds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(set);
        ValidateParameters(set.Prime, set.Threshold, set.TotalShares, rounds, cancellationToken);
        ArgumentNullException.ThrowIfNull(set.Shares);
        if (set.Shares.Count < set.Threshold) throw new ArgumentException("Insufficient shares for the declared threshold.", nameof(set));
        if (set.Shares.Count > set.TotalShares) throw new ArgumentException("More shares than the declared total.", nameof(set));
        var coordinates = new HashSet<BigInteger>();
        foreach (var share in set.Shares)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (share is null || share.X <= 0 || share.X >= set.Prime || share.Y < 0 || share.Y >= set.Prime)
                throw new ArgumentException("Shares must have 0 < x < prime and 0 <= y < prime.", nameof(set));
            if (!coordinates.Add(share.X)) throw new ArgumentException("Share coordinates must be distinct.", nameof(set));
        }
    }

    private static string Decimal(BigInteger value) => value.ToString(CultureInfo.InvariantCulture);

    private static BigInteger ReadInteger(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String) throw new FormatException("Field integers must be decimal strings.");
        var value = element.GetString()!;
        if (value.Length == 0 || (value.Length > 1 && value[0] == '0') || value.Any(c => c < '0' || c > '9'))
            throw new FormatException("Field integers must be canonical nonnegative decimal strings.");
        return BigInteger.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, JsonElement> ReadProperties(JsonElement element, string[] required)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new FormatException("Expected a JSON object.");
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            if (!required.Contains(property.Name, StringComparer.Ordinal) || !result.TryAdd(property.Name, property.Value))
                throw new FormatException("Unknown or duplicate share-document property.");
        if (result.Count != required.Length) throw new FormatException("Missing share-document property.");
        return result;
    }
}
