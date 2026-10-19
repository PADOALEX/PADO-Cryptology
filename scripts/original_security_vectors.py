"""Run original UI-bound Python methods with inert widgets; no GUI dependencies.

Usage: python scripts/original_security_vectors.py [path-to-original-python]
This captures historical behavior, including bugs. It is not a crypto implementation.
"""
import ast
import hashlib
import json
from pathlib import Path
import random
import sys
import tempfile
from types import SimpleNamespace


class Widget:
    def __init__(self, value=""):
        self.value = value
        self.options = {}

    def get(self, *args):
        return self.value

    def delete(self, *args):
        self.value = ""

    def insert(self, position, value):
        self.value = str(value)

    def configure(self, **options):
        self.options.update(options)


class Translations(dict):
    def __missing__(self, key):
        return key


class Messages:
    def __init__(self):
        self.messages = []

    def showwarning(self, *args):
        self.messages.append(args[-1])

    showerror = showwarning


def original_class(source, filename, class_name, methods, messages):
    tree = ast.parse((source / filename).read_text(encoding="utf-8-sig"))
    original = next(n for n in tree.body if isinstance(n, ast.ClassDef) and n.name == class_name)
    selected = [n for n in original.body if isinstance(n, ast.FunctionDef) and n.name in methods]
    assert len(selected) == len(methods)
    harness = ast.Module(body=[ast.ClassDef(name=class_name, bases=[], keywords=[], body=selected, decorator_list=[])], type_ignores=[])
    namespace = {"messagebox": messages, "random": random, "hashlib": hashlib}
    exec(compile(ast.fix_missing_locations(harness), filename, "exec"), namespace)
    return namespace[class_name]


def main():
    source = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[1] / "legacy" / "python"
    random.seed(20261019)
    messages = Messages()
    app = SimpleNamespace(current_lang_str="Eng", translations={"Eng": Translations()})
    otp_class = original_class(source, "tab5.py", "Tab5", {"process_otp"}, messages)
    otp_vectors = []
    for text, key in [("ABC", "XYZ"), ("ABC", "XYZABC"), ("Їжак 🧪", "АБВГҐД"), ("", ""), ("ABC", "XY")]:
        otp = otp_class()
        otp.app = app
        otp.input_text, otp.key_text, otp.output_text = Widget(text), Widget(key), Widget()
        messages.messages.clear()
        otp.process_otp(True)
        otp_vectors.append({"text": text, "key": key, "ciphertext_code_points": [ord(c) for c in otp.output_text.value], "warnings": list(messages.messages)})

    methods = {"perform_primality_check", "perform_extended_gcd", "execute_calculate_keys", "execute_encryption", "execute_decryption"}
    rsa_class = original_class(source, "tab8.py", "Tab8", methods, messages)
    rsa = rsa_class()
    rsa.application_instance = app
    rsa.entry_prime_p, rsa.entry_prime_q = Widget("61"), Widget("53")
    rsa.textbox_public_key, rsa.textbox_private_key = Widget(), Widget()
    rsa.execute_calculate_keys()
    rsa_vectors = []
    for text in ["A", "AA", "🧪"]:
        rsa.textbox_message, rsa.textbox_result = Widget(text), Widget()
        rsa.execute_encryption()
        ciphertext = rsa.textbox_result.value
        rsa.textbox_message.value = ciphertext
        rsa.execute_decryption()
        rsa_vectors.append({"text": text, "ciphertext": ciphertext, "decrypted": rsa.textbox_result.value, "decrypted_code_points": [ord(c) for c in rsa.textbox_result.value]})

    signature_class = original_class(source, "tab9.py", "Tab9", {"execute_sign_document", "execute_verify_signature"}, messages)
    signature = signature_class()
    signature.application_instance = app
    signature.signature_public_exponent = rsa.rsa_public_exponent
    signature.signature_private_exponent = rsa.rsa_private_exponent
    signature.signature_modulo_n = rsa.rsa_modulo_n
    signature.textbox_signature, signature.label_verification_status = Widget(), Widget()
    signature_vectors = []
    with tempfile.TemporaryDirectory() as directory:
        for data in [b"abc", b""]:
            path = Path(directory) / "input.bin"
            path.write_bytes(data)
            signature.target_file_path = str(path)
            signature.execute_sign_document()
            value = int(signature.textbox_signature.value)
            signature.execute_verify_signature()
            valid = signature.label_verification_status.options["text"]
            signature.textbox_signature.value = str(value + rsa.rsa_modulo_n)
            signature.execute_verify_signature()
            signature_vectors.append({"file_hex": data.hex(), "sha256": hashlib.sha256(data).hexdigest(), "signature": value, "verification": valid, "signature_plus_n_verification": signature.label_verification_status.options["text"]})
    print(json.dumps({"source": "original unmodified tab5.py/tab8.py/tab9.py methods extracted with ast", "otp": otp_vectors, "rsa_key": {"p": 61, "q": 53, "e": rsa.rsa_public_exponent, "n": rsa.rsa_modulo_n, "d": rsa.rsa_private_exponent}, "rsa": rsa_vectors, "signature": signature_vectors}, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
