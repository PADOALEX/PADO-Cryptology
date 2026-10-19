using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cryptology.Core.Asymmetric;
using Cryptology.Core.ClassicalCiphers;
using Cryptology.Core.Cryptanalysis;
using Cryptology.Core.Models;
using Cryptology.Core.NumberTheory;
using Cryptology.Core.SecretSharing;
using Cryptology.Security;

return await CryptologyCli.RunAsync(args);

internal static class CryptologyCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            return await ExecuteAsync(args, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Cancelled.");
            return 130;
        }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException
            or IOException or UnauthorizedAccessException or CryptographicException or JsonException
            or InvalidOperationException or NotSupportedException)
        {
            Console.Error.WriteLine($"Error: {error.Message}");
            return 2;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Unexpected failure ({error.GetType().Name}).");
            return 1;
        }
        finally { Console.CancelKeyPress -= cancelHandler; }
    }

    private static async Task<int> ExecuteAsync(string[] args, CancellationToken token)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h") { Help(); return 0; }
        token.ThrowIfCancellationRequested();
        switch (args[0].ToLowerInvariant())
        {
            case "demo":
                Exact(args, 1);
                Demo(token);
                return 0;
            case "caesar":
                Minimum(args, 5);
                bool caesarEncrypt = Operation(args[1]);
                var caesarAlphabet = Alphabet.FromId(args[2]);
                if (!long.TryParse(args[3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long shift))
                    throw new ArgumentException("The shift must be a signed 64-bit integer.");
                Console.WriteLine(caesarEncrypt
                    ? CaesarCipher.Encrypt(Text(args, 4), shift, caesarAlphabet, token)
                    : CaesarCipher.Decrypt(Text(args, 4), shift, caesarAlphabet, token));
                return 0;
            case "vigenere":
                Minimum(args, 5);
                bool vigenereEncrypt = Operation(args[1]);
                var vigenereAlphabet = Alphabet.FromId(args[2]);
                Console.WriteLine(vigenereEncrypt
                    ? VigenereCipher.Encrypt(Text(args, 4), args[3], vigenereAlphabet, token)
                    : VigenereCipher.Decrypt(Text(args, 4), args[3], vigenereAlphabet, token));
                return 0;
            case "frequency":
                Minimum(args, 3);
                var frequencies = FrequencyAnalyzer.Analyze(Text(args, 2), Alphabet.FromId(args[1]), cancellationToken: token);
                Console.WriteLine("symbol,count,percentage,reference_percentage,reference_rank");
                foreach (var row in frequencies.Rows)
                    Console.WriteLine($"\"{row.Symbol.Replace("\"", "\"\"")}\",{row.Count.ToString(CultureInfo.InvariantCulture)},{row.Percentage.ToString("F6", CultureInfo.InvariantCulture)},{row.ReferencePercentage.ToString("F6", CultureInfo.InvariantCulture)},{row.ReferenceRank.ToString(CultureInfo.InvariantCulture)}");
                return 0;
            case "gcd":
                Exact(args, 3);
                Console.WriteLine(IntegerMath.Gcd(Integer(args[1]), Integer(args[2]), token).ToString(CultureInfo.InvariantCulture));
                return 0;
            case "modpow":
                Exact(args, 4);
                Console.WriteLine(IntegerMath.ModPow(Integer(args[1]), Integer(args[2]), Integer(args[3]), cancellationToken: token).Value.ToString(CultureInfo.InvariantCulture));
                return 0;
            case "prime":
                Exact(args, 3);
                BigInteger number = Integer(args[1]);
                int rounds = SmallInteger(args[2], "rounds");
                bool millerRabin = PrimalityTest.MillerRabin(number, rounds, token);
                bool solovay = PrimalityTest.SolovayStrassen(number, rounds, token);
                Console.WriteLine($"Miller-Rabin: {(millerRabin ? "Probably prime" : "Composite")}");
                Console.WriteLine($"Solovay-Strassen: {(solovay ? "Probably prime" : "Composite")}");
                Console.WriteLine($"Rounds: {rounds.ToString(CultureInfo.InvariantCulture)}; composite false-positive bounds: 4^-rounds (MR), 2^-rounds (SS).");
                return 0;
            case "shamir":
                Minimum(args, 2);
                if (args[1] == "split")
                {
                    Exact(args, 6);
                    var set = ShamirSecretSharing.Split(Integer(args[2]), SmallInteger(args[3], "threshold"), SmallInteger(args[4], "share count"), Integer(args[5]), cancellationToken: token);
                    Console.WriteLine(ShamirSecretSharing.Export(set, cancellationToken: token));
                }
                else if (args[1] == "recover")
                {
                    Exact(args, 3);
                    string json = await ReadBoundedTextAsync(args[2], 16 * 1024 * 1024, token);
                    var set = ShamirSecretSharing.Import(json, cancellationToken: token);
                    Console.WriteLine(ShamirSecretSharing.Recover(set, cancellationToken: token).ToString(CultureInfo.InvariantCulture));
                }
                else throw new ArgumentException("Choose 'shamir split' or 'shamir recover'.");
                return 0;
            case "otp":
                Minimum(args, 4);
                bool otpEncrypt = Operation(args[1]);
                string encodedKey = args[2].StartsWith("env:", StringComparison.Ordinal)
                    ? Environment.GetEnvironmentVariable(args[2][4..]) ?? throw new ArgumentException("The named key environment variable is missing.")
                    : args[2];
                Console.WriteLine(otpEncrypt ? ByteOneTimePad.EncryptToBase64(Text(args, 3), encodedKey)
                    : ByteOneTimePad.DecryptFromBase64(Text(args, 3), encodedKey));
                return 0;
            case "modern-rsa-demo":
                RsaDemo(args.Length > 1 ? Text(args, 1) : "Hello, ҐЄІЇ!", token);
                return 0;
            case "rsa":
                Exact(args, 2);
                if (args[1] != "demo") throw new ArgumentException("Use 'rsa demo' or 'modern-rsa-demo [text]'.");
                RsaDemo("Hello, ҐЄІЇ!", token);
                return 0;
            case "sign-demo":
                SignatureDemo(args.Length > 1 ? Text(args, 1) : "Public signature demonstration", token);
                return 0;
            case "signature":
                Exact(args, 5);
                return await SignatureFileAsync(args, token);
            default:
                throw new ArgumentException("Unknown command. Run 'help' for syntax.");
        }
    }

    private static void Demo(CancellationToken token)
    {
        Console.WriteLine("PADO Cryptology — public educational demonstrations");
        var alphabet = Alphabet.English;
        var frequency = FrequencyAnalyzer.Analyze("Aaa B!", alphabet, cancellationToken: token);
        Console.WriteLine($"1. Frequency: {frequency.TotalSymbols} letters; A={frequency.Rows[0].Percentage.ToString("F0", CultureInfo.InvariantCulture)}%.");

        string encrypted = CaesarCipher.Encrypt("Abc XyZ!", 3, alphabet, token);
        var key = SubstitutionKey.Create("ZYXWVUTSRQPONMLKJIHGFEDCBA", alphabet);
        Console.WriteLine($"2. Classical: Caesar={encrypted}; substitution={SubstitutionCipher.Encrypt("Abc XyZ!", key, token)}.");

        var guess = CryptanalysisService.BaselineFrequencyGuess("ZZZ XX Y", alphabet, token);
        var grams = NGramAnalyzer.Analyze("ABABA AB", alphabet, 2, cancellationToken: token);
        Console.WriteLine($"3. Cryptanalysis: baseline Z->{guess["Z"]}; bigram AB={grams[0].Count}.");
        Console.WriteLine($"4. Vigenere: {VigenereCipher.Encrypt("Attack at dawn!", "LEMON", alphabet, token)}");

        const string otpText = "UTF-8: ҐЄІЇ 😀";
        byte[] pad = ByteOneTimePad.GenerateKeyForText(otpText);
        try
        {
            string encoded = ByteOneTimePad.EncodeKey(pad);
            string cipher = ByteOneTimePad.EncryptToBase64(otpText, encoded);
            bool recovered = ByteOneTimePad.DecryptFromBase64(cipher, encoded) == otpText;
            Console.WriteLine($"5. OTP: {pad.Length} bytes; UTF-8 round trip={recovered}. Secret pad must be used once.");
        }
        finally { CryptographicOperations.ZeroMemory(pad); }

        var euclid = IntegerMath.ExtendedGcd(240, 46, includeSteps: true, cancellationToken: token);
        var power = IntegerMath.ModPow(4, 13, 497, cancellationToken: token).Value;
        Console.WriteLine($"6. Mathematics: gcd(240,46)={euclid.Gcd}; x={euclid.X}; y={euclid.Y}; 4^13 mod 497={power}.");
        Console.WriteLine($"7. Number theory: Jacobi(1001/9907)={JacobiSymbol.Calculate(1001, 9907, token)}; 104729 MR={PrimalityTest.MillerRabin(104729, 16, token)}, SS={PrimalityTest.SolovayStrassen(104729, 16, token)}.");

        var educational = EducationalRsa.CreateKey(61, 53, 17, cancellationToken: token);
        BigInteger rsaCipher = EducationalRsa.Encrypt(65, educational, token);
        Console.WriteLine($"8. Educational RSA: n={educational.N}; phi={educational.Phi}; e={educational.E}; 65->{rsaCipher}->{EducationalRsa.Decrypt(rsaCipher, educational, token)}. Textbook RSA is insecure.");
        RsaDemo("Hello, ҐЄІЇ!", token);
        Console.Write("9. ");
        SignatureDemo("Public signature demonstration", token);

        var shares = ShamirSecretSharing.Split(1234, 3, 5, 2089, cancellationToken: token);
        var subset = shares with { Shares = shares.Shares.Take(3).ToArray() };
        Console.WriteLine($"10. Shamir: 3-of-5, p=2089; recovered={ShamirSecretSharing.Recover(subset, cancellationToken: token)}. Shares are unauthenticated.");
    }

    private static void RsaDemo(string text, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var privateKey = ModernRsaService.Generate(2048);
        token.ThrowIfCancellationRequested();
        using var publicKey = ModernRsaService.ImportPublicPem(privateKey.ExportPublicPem());
        string cipher = publicKey.EncryptUtf8ToBase64(text);
        bool valid = privateKey.DecryptUtf8FromBase64(cipher) == text;
        Console.WriteLine($"RSA-OAEP SHA-256: {privateKey.KeySize} bits; maximum={privateKey.MaxPlaintextBytes} bytes; round trip={valid}.");
    }

    private static void SignatureDemo(string text, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var privateKey = ModernRsaService.Generate(2048);
        token.ThrowIfCancellationRequested();
        using var publicKey = ModernRsaService.ImportPublicPem(privateKey.ExportPublicPem());
        string signature = privateKey.SignUtf8ToBase64(text);
        Console.WriteLine($"RSA-PSS SHA-256: valid={publicKey.VerifyUtf8FromBase64(text, signature)}; modified text accepted={publicKey.VerifyUtf8FromBase64(text + "!", signature)}.");
    }

    private static async Task<int> SignatureFileAsync(string[] args, CancellationToken token)
    {
        if (args[1] == "sign")
        {
            string password = Environment.GetEnvironmentVariable("PADO_KEY_PASSWORD")
                ?? throw new ArgumentException("Set PADO_KEY_PASSWORD to the encrypted private-key password before signing.");
            using var key = await ModernRsaService.ImportEncryptedPrivatePemFileAsync(args[2], password, token);
            string signature = await key.SignFileAsync(args[3], token);
            await using var output = new FileStream(args[4], FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await output.WriteAsync(Encoding.UTF8.GetBytes(signature + "\n"), token);
            Console.WriteLine("Signature saved.");
            return 0;
        }
        if (args[1] == "verify")
        {
            using var key = await ModernRsaService.ImportPublicPemFileAsync(args[2], token);
            string signature = await ReadBoundedTextAsync(args[4], 16_384, token);
            bool valid = await key.VerifyFileAsync(args[3], signature.Trim(), token);
            Console.WriteLine(valid ? "VALID" : "INVALID");
            return valid ? 0 : 3;
        }
        throw new ArgumentException("Choose 'signature sign' or 'signature verify'.");
    }

    private static async Task<string> ReadBoundedTextAsync(string path, int maximumBytes, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > maximumBytes) throw new ArgumentException($"The input file exceeds the {maximumBytes.ToString(CultureInfo.InvariantCulture)}-byte limit.");
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(token);
    }

    private static BigInteger Integer(string text) => BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
        ? value : throw new ArgumentException("Expected a decimal integer with an optional leading sign.");
    private static int SmallInteger(string text, string name) => int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value)
        ? value : throw new ArgumentException($"The {name} must be a 32-bit decimal integer.");
    private static bool Operation(string text) => text.ToLowerInvariant() switch
    {
        "enc" or "encrypt" => true,
        "dec" or "decrypt" => false,
        _ => throw new ArgumentException("Choose enc/encrypt or dec/decrypt.")
    };
    private static string Text(string[] args, int start) => string.Join(' ', args.Skip(start));
    private static void Exact(string[] args, int count)
    {
        if (args.Length != count) throw new ArgumentException("Incorrect number of arguments. Run 'help' for syntax.");
    }
    private static void Minimum(string[] args, int count)
    {
        if (args.Length < count) throw new ArgumentException("Missing arguments. Run 'help' for syntax.");
    }

    private static void Help() => Console.WriteLine("""
        PADO Cryptology CLI — educational cryptography and standard .NET RSA demos

        dotnet run --project src/Cryptology.Cli -- <command> [arguments]

        help
        demo                                      Exercise all ten original modules
        caesar enc|dec en|uk shift "text"
        vigenere enc|dec en|uk keyword "text"
        frequency en|uk "text"                    Case-folded letters; invariant CSV
        gcd a b
        modpow base exponent modulus
        prime number rounds                       Miller-Rabin and Solovay-Strassen
        shamir split secret threshold count prime JSON document to standard output
        shamir recover shares.json                Recover threshold-bearing JSON
        otp encrypt|decrypt base64-key "text"      Decrypt expects Base64 ciphertext
        otp encrypt|decrypt env:VARIABLE "text"    Read Base64 pad from environment
        modern-rsa-demo ["short text"]             Ephemeral RSA-OAEP SHA-256 round trip
        rsa demo                                  Alias for modern-rsa-demo
        sign-demo ["text"]                        Ephemeral RSA-PSS SHA-256 verification
        signature sign encrypted-private.pem file signature.txt
        signature verify public.pem file signature.txt

        Quote text and paths containing spaces. UI language does not select the alphabet.
        File signing reads the password from PADO_KEY_PASSWORD; it never exports keys.
        Signature output must be a new file. Verification prints VALID or INVALID.
        Classical ciphers, textbook RSA and byte OTP are educational. OTP keys must be
        secret, exactly the UTF-8 byte length, and used once; XOR does not authenticate.
        Avoid real secrets in command arguments or shell history. Prefer env:VARIABLE
        for a demonstration pad; environment variables are not a secure key vault.
        Exit codes: 0 success, 1 unexpected failure, 2 input/I/O/crypto error,
        3 invalid file signature, 130 cancelled. Ctrl+C cancels managed computation;
        platform RSA key generation finishes its current call before observing cancel.
        """);
}
