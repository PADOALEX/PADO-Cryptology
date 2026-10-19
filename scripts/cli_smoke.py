"""CLI process integration checks. Requires only Python 3.10+ and .NET 10 SDK.

Run from any directory: python scripts/cli_smoke.py
Builds Release, creates ephemeral test RSA keys in a temporary directory, and
removes every fixture on completion or failure. No real keys or fixed passwords.
"""
import base64
import csv
import html
import io
import json
import os
import pathlib
import secrets
import subprocess
import tempfile

REPO = pathlib.Path(__file__).resolve().parents[1]
CLI = ["dotnet", str(REPO / "src/Cryptology.Cli/bin/Release/net10.0/Cryptology.Cli.dll")]


def execute(command, **kwargs):
    return subprocess.run(command, cwd=REPO, capture_output=True, text=True, encoding="utf-8", timeout=120, **kwargs)


def main():
    build = execute(["dotnet", "build", "src/Cryptology.Cli", "-c", "Release"])
    if build.returncode:
        raise RuntimeError(build.stdout + build.stderr)
    checked = 0

    def run(args, code=0, expected=None, env=None):
        nonlocal checked
        result = execute(CLI + args, env=env)
        assert result.returncode == code, (args[0], result.returncode, result.stderr)
        if expected is not None:
            assert result.stdout.strip() == expected, (args[0], result.stdout)
        if code == 2:
            assert "Error:" in result.stderr and " at " not in result.stderr
        checked += 1
        return result.stdout

    with tempfile.TemporaryDirectory(prefix="pado-cli-smoke-") as temporary:
        fixture = pathlib.Path(temporary)
        env = os.environ.copy()
        env["PADO_KEY_PASSWORD"] = secrets.token_urlsafe(32)
        # Generate encrypted PEM fixtures using the actual public service, never a mock.
        project = fixture / "fixture.csproj"
        security_project = html.escape(str(REPO / "src/Cryptology.Security/Cryptology.Security.csproj"), quote=True)
        project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
<ItemGroup><ProjectReference Include="{security_project}" /></ItemGroup></Project>''', encoding="utf-8")
        (fixture / "Program.cs").write_text('''using Cryptology.Security;
using var key = ModernRsaService.Generate(2048);
await File.WriteAllTextAsync(Path.Combine(args[0], "private.pem"), key.ExportEncryptedPrivatePem(Environment.GetEnvironmentVariable("PADO_KEY_PASSWORD")!, true));
await File.WriteAllTextAsync(Path.Combine(args[0], "public.pem"), key.ExportPublicPem());
await File.WriteAllTextAsync(Path.Combine(args[0], "document.txt"), "Public CLI fixture: ҐЄІЇ 😀");
await File.WriteAllTextAsync(Path.Combine(args[0], "empty.txt"), "");''', encoding="utf-8")
        setup = execute(["dotnet", "run", "--project", str(project), "-c", "Release", "--", str(fixture)], env=env)
        if setup.returncode:
            raise RuntimeError(setup.stdout + setup.stderr)

        run(["help"])
        run(["caesar", "enc", "en", "3", "Abc XyZ!"], expected="Def AbC!")
        run(["caesar", "dec", "en", "3", "Def AbC!"], expected="Abc XyZ!")
        run(["caesar", "enc", "uk", "5", "ҐЄІЇ, ґєії! Яя"], expected="ЗЇМН, зїмн! Ґґ")
        run(["vigenere", "enc", "en", "LEMON", "Attack at dawn!"], expected="Lxfopv ef rnhr!")
        run(["vigenere", "dec", "en", "LEMON", "Lxfopv ef rnhr!"], expected="Attack at dawn!")
        run(["vigenere", "enc", "en", "L!EMON", "abc"], 2)
        rows = list(csv.DictReader(io.StringIO(run(["frequency", "uk", "ҐґЄєІіЇї ЫЭЪ"]))))
        assert len(rows) == 4 and all(r["count"] == "2" and r["percentage"] == "25.000000" for r in rows)
        run(["gcd", "-240", "46"], expected="2")
        run(["modpow", "-2", "10", "17"], expected="4")
        assert "Probably prime" in run(["prime", "104729", "8"])
        assert "Composite" in run(["prime", "561", "8"])
        run(["prime", "104729", "0"], 2)
        text = "Привіт 😀"
        key = bytes(range(len(text.encode("utf-8"))))  # Public independent XOR control.
        b64key = base64.b64encode(key).decode()
        env["CLI_TEST_OTP"] = b64key
        cipher = base64.b64encode(bytes(a ^ b for a, b in zip(text.encode("utf-8"), key))).decode()
        run(["otp", "encrypt", "env:CLI_TEST_OTP", text], expected=cipher, env=env)
        run(["otp", "decrypt", b64key, cipher], expected=text)
        run(["otp", "encrypt", "AA==", "too long"], 2)
        shares = json.loads(run(["shamir", "split", "1234", "3", "5", "2089"]))
        shares["shares"] = shares["shares"][:3]
        sharespath = fixture / "shares.json"
        sharespath.write_text(json.dumps(shares), encoding="utf-8")
        run(["shamir", "recover", str(sharespath)], expected="1234")
        shares["shares"] = shares["shares"][:2]
        sharespath.write_text(json.dumps(shares), encoding="utf-8")
        run(["shamir", "recover", str(sharespath)], 2)
        assert "round trip=True" in run(["modern-rsa-demo"])
        assert "modified text accepted=False" in run(["sign-demo"])
        signature = fixture / "signature.txt"
        run(["signature", "sign", str(fixture / "private.pem"), str(fixture / "document.txt"), str(signature)], expected="Signature saved.", env=env)
        run(["signature", "verify", str(fixture / "public.pem"), str(fixture / "document.txt"), str(signature)], expected="VALID")
        run(["signature", "sign", str(fixture / "private.pem"), str(fixture / "document.txt"), str(signature)], 2, env=env)
        changed = fixture / "changed.txt"
        changed.write_text("Modified document", encoding="utf-8")
        run(["signature", "verify", str(fixture / "public.pem"), str(changed), str(signature)], 3, expected="INVALID")
        bad = fixture / "bad-signature.txt"
        bad.write_text("malformed base64", encoding="utf-8")
        run(["signature", "verify", str(fixture / "public.pem"), str(fixture / "document.txt"), str(bad)], 3, expected="INVALID")
        good_password = env["PADO_KEY_PASSWORD"]
        env["PADO_KEY_PASSWORD"] = secrets.token_urlsafe(32)
        run(["signature", "sign", str(fixture / "private.pem"), str(fixture / "document.txt"), str(fixture / "wrong.txt")], 2, env=env)
        env["PADO_KEY_PASSWORD"] = good_password
        empty_signature = fixture / "empty-signature.txt"
        run(["signature", "sign", str(fixture / "private.pem"), str(fixture / "empty.txt"), str(empty_signature)], env=env)
        run(["signature", "verify", str(fixture / "public.pem"), str(fixture / "empty.txt"), str(empty_signature)], expected="VALID")
        run(["unknown-command"], 2)
        run(["gcd", "invalid", "5"], 2)
    print(f"CLI process integration: {checked}/{checked} checks passed.")


if __name__ == "__main__":
    main()
