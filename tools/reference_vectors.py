"""Reproduce original Python controls without GUI imports; only stdlib needed.

Run from the repository root: python tools/reference_vectors.py --original legacy/python
The expected migration values use Python integer arithmetic independently of C#.
"""
import argparse
import ast
import hashlib
import json
import math
from pathlib import Path
import random


def original_methods(path, names):
    source = path.read_text(encoding="utf-8-sig")
    module = ast.parse(source)
    cls = next(n for n in module.body if isinstance(n, ast.ClassDef))
    methods = [n for n in cls.body if isinstance(n, ast.FunctionDef) and n.name in names]
    original = ast.Module(body=[ast.ClassDef(name=cls.name, bases=[], keywords=[], body=methods, decorator_list=[])], type_ignores=[])
    ast.fix_missing_locations(original)
    namespace = {"random": random}
    exec(compile(original, str(path), "exec"), namespace)
    return namespace[cls.name](), hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--original", default="legacy/python")
    args = parser.parse_args()
    random.seed(20261019)
    source_dir = Path(args.original)
    hashes = {}
    loaded = {}
    for tab in range(6, 11):
        methods = {"calculate_extended_gcd", "perform_extended_gcd", "check_is_prime", "perform_primality_check", "calculate_jacobi_algorithm", "perform_solovay_strassen", "calculate_modular_inverse"}
        loaded[tab], hashes[f"tab{tab}.py"] = original_methods(source_dir / f"tab{tab}.py", methods)
    gcd_cases = [(240, 46), (-240, 46), (240, -46), (0, 0), (1, 0)]
    jacobi_cases = [(1001, 9907), (-1, 7), (5, 21), (0, 1), (6, 9)]
    p, q, e, message = 61, 53, 17, 65
    n, phi = p * q, (p - 1) * (q - 1)
    d = pow(e, -1, phi)
    representative = int.from_bytes(hashlib.sha256(b"abc").digest(), "big") % n
    shares = [{"x": x, "y": (1234 + 166 * x + 94 * x * x) % 2089} for x in range(1, 6)]
    output = {
        "seed": 20261019,
        "source_sha256": hashes,
        "original": {
            "extended_gcd": [{"a": a, "b": b, "result": loaded[6].calculate_extended_gcd(a, b)} for a, b in gcd_cases],
            "jacobi": [{"a": a, "n": n_, "result": loaded[7].calculate_jacobi_algorithm(a, n_)} for a, n_ in jacobi_cases],
            "ss_zero_rounds_composite_9": loaded[7].perform_solovay_strassen(9, 0),
            "mr_561": {str(tab): (loaded[tab].check_is_prime(561) if tab == 6 else loaded[tab].perform_primality_check(561)) for tab in [6, 8, 9, 10]},
            "modpow_zero_exponent_mod_one": 1,
        },
        "migration": {
            "gcd": [{"a": a, "b": b, "gcd": math.gcd(a, b)} for a, b in gcd_cases],
            "modpow": [{"base": a, "exponent": b, "modulus": m, "result": pow(a, b, m)} for a, b, m in [(4, 13, 497), (-2, 5, 13), (7, 0, 1), (0, 0, 7)]],
            "rsa": {"p": p, "q": q, "e": e, "n": n, "phi": phi, "d": d, "message": message, "cipher": pow(message, e, n)},
            "educational_signature": {"data_utf8": "abc", "representative": representative, "signature": pow(representative, d, n)},
            "shamir": {"prime": 2089, "threshold": 3, "secret": 1234, "coefficients": [1234, 166, 94], "shares": shares},
        },
    }
    output_path = Path(__file__).resolve().parents[1] / "tests" / "vectors" / "mathematics.json"
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    print(output_path)


if __name__ == "__main__":
    main()
