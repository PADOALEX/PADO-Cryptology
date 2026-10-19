"""Execute the supplied legacy methods with GUI adapters, before migration."""
import json
import pathlib
import sys
import types

repo = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(repo / "legacy/python"))
sys.modules["customtkinter"] = types.SimpleNamespace(CTkFrame=object)
dialogs = types.SimpleNamespace(showwarning=lambda *a: None, showerror=lambda *a: None)
sys.modules["tkinter"] = types.SimpleNamespace(ttk=None, filedialog=None, messagebox=dialogs)
import config
import tab1, tab2, tab3, tab4

class Box:
    def __init__(self, value=""): self.value = value
    def get(self, *a): return self.value
    def set(self, value): self.value = value
    def delete(self, *a): self.value = ""
    def insert(self, *args, **kw): self.value += str(args[1])
    def configure(self, **kw): pass
    def tag_config(self, *a, **kw): pass

class Tree:
    def __init__(self): self.rows = []
    def get_children(self): return []
    def insert(self, *a, values): self.rows.append(values)

def app(lang): return types.SimpleNamespace(current_lang_str=lang, translations=config.TRANSLATIONS)

def caesar(text, shift, lang):
    obj = tab2.Tab2.__new__(tab2.Tab2)
    obj.app, obj.input_text, obj.output_text = app(lang), Box(text), Box()
    obj.cipher_mode = Box(config.TRANSLATIONS[lang]["cipher_caesar"])
    obj.caesar_slider = Box(shift)
    obj.process_cipher(True)
    return obj.output_text.value

def vigenere(text, key, lang):
    obj = tab4.Tab4.__new__(tab4.Tab4)
    obj.app, obj.input_text, obj.output_text, obj.key_entry = app(lang), Box(text), Box(), Box(key)
    obj.process_vigenere(True)
    return obj.output_text.value

def frequencies(text, lang):
    obj = tab1.Tab1.__new__(tab1.Tab1)
    obj.app, obj.input_text, obj.tree, obj.total_label = app(lang), Box(text), Tree(), Box()
    obj.run_analysis()
    return obj.tree.rows

def substitution(text, key, lang):
    obj = tab2.Tab2.__new__(tab2.Tab2)
    obj.app, obj.input_text, obj.output_text = app(lang), Box(text), Box()
    obj.cipher_mode = Box(config.TRANSLATIONS[lang]["cipher_subst"])
    obj.get_substitution_key_string = lambda: key
    obj.process_cipher(True)
    return obj.output_text.value

def ngrams(text, lang):
    obj = tab3.Tab3.__new__(tab3.Tab3)
    obj.ngrams_text = Box()
    obj.extract_ngrams(text.upper(), config.ALPHABET_ENG if lang == "Eng" else config.ALPHABET_UKR)
    return obj.ngrams_text.value

vectors = {
    "provenance": "Executed original supplied Python methods without modifying algorithms; GUI only replaced by adapters.",
    "caesar": [
        {"text": "Abc XyZ! 123 😀", "shift": 3, "alphabet": "en", "expected": caesar("Abc XyZ! 123 😀", 3, "Eng")},
        {"text": "ҐЄІЇ, ґєії! Яя", "shift": 5, "alphabet": "uk", "expected": caesar("ҐЄІЇ, ґєії! Яя", 5, "Ukr")},
    ],
    "vigenere": [
        {"text": "Attack at dawn!", "key": "LEMON", "alphabet": "en", "expected": vigenere("Attack at dawn!", "LEMON", "Eng")},
        {"text": "Привіт, ҐЄІЇ!", "key": "КЛЮЧ", "alphabet": "uk", "expected": vigenere("Привіт, ҐЄІЇ!", "КЛЮЧ", "Ukr")},
    ],
    "substitution": {"text": "Abc XyZ!", "key": config.ALPHABET_ENG[::-1], "expected": substitution("Abc XyZ!", config.ALPHABET_ENG[::-1], "Eng")},
    "frequencies": {"text": "Aaa B!", "alphabet": "en", "rows": frequencies("Aaa B!", "Eng")},
    "ngrams": {"text": "ABABA AB", "result": ngrams("ABABA AB", "Eng")},
    "defects_not_csharp_expectations": [
        {"description": "UA regex accepts Russian-only characters", "text": "ҐґЄєІіЇї ЫЭЪ", "legacy_rows": frequencies("ҐґЄєІіЇї ЫЭЪ", "Ukr")},
        {"description": "Vigenere silently removes invalid key characters", "key": "L!EMON", "legacy_output": vigenere("Attack at dawn!", "L!EMON", "Eng")},
        {"description": "Foreign unique substitution characters accepted", "key": config.ALPHABET_ENG[1:] + "0", "legacy_output": substitution("Abc XyZ!", config.ALPHABET_ENG[1:] + "0", "Eng")},
    ],
}
output = repo / "docs/audit/classical-control-vectors.json"
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(vectors, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(output)
