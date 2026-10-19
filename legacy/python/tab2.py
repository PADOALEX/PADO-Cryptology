import customtkinter as ctk
from tkinter import filedialog, messagebox
import random
from collections import Counter
import utils
from config import ALPHABET_UKR, ALPHABET_ENG

class Tab2(ctk.CTkFrame):
    def __init__(self, master, app):
        super().__init__(master, fg_color="transparent")
        self.app = app
        self.mapping_entries = {}
        self.setup_ui()

    def setup_ui(self):
        self.grid_columnconfigure(0, weight=1)
        self.grid_columnconfigure(1, weight=0)
        self.grid_columnconfigure(2, weight=1)
        self.grid_rowconfigure(0, weight=1)
        self.grid_rowconfigure(1, weight=0)

        left_panel = ctk.CTkFrame(self, fg_color="transparent")
        left_panel.grid(row=0, column=0, sticky="nsew", padx=10, pady=10)

        self.input_label = ctk.CTkLabel(left_panel, anchor="w", font=("Arial", 14, "bold"))
        self.input_label.pack(anchor="w", pady=(0, 5))
        self.input_text = ctk.CTkTextbox(left_panel, height=150)
        self.input_text.pack(fill="both", expand=True)

        left_btns = ctk.CTkFrame(left_panel, fg_color="transparent")
        left_btns.pack(pady=5, fill="x")
        self.btn_load = ctk.CTkButton(left_btns, width=100, command=lambda: utils.load_file(self.app, self.input_text))
        self.btn_load.pack(side="left", padx=2)
        self.btn_copy_in = ctk.CTkButton(left_btns, width=80, fg_color="#555555", command=lambda: utils.copy_to_clipboard(self.app, self.input_text.get("1.0", "end-1c")))
        self.btn_copy_in.pack(side="right", padx=2)

        center_panel = ctk.CTkFrame(self, fg_color="transparent")
        center_panel.grid(row=0, column=1, sticky="ns", padx=5, pady=25)

        center_inner = ctk.CTkFrame(center_panel, fg_color="transparent")
        center_inner.pack(expand=True)

        self.btn_encrypt = ctk.CTkButton(center_inner, width=160, height=45, command=lambda: self.process_cipher(True), fg_color="#2CC985", hover_color="#209662", text_color="black", font=("Arial", 14, "bold"))
        self.btn_encrypt.pack(pady=10)

        self.btn_decrypt = ctk.CTkButton(center_inner, width=160, height=45, command=lambda: self.process_cipher(False), fg_color="#FFA500", hover_color="#D98C00", text_color="black", font=("Arial", 14, "bold"))
        self.btn_decrypt.pack(pady=10)

        ctk.CTkFrame(center_inner, height=2, fg_color="gray").pack(fill="x", pady=10)

        self.btn_swap = ctk.CTkButton(center_inner, width=160, height=35, command=self.swap_texts, fg_color="#555555", hover_color="#444444", text_color="white", font=("Arial", 13))
        self.btn_swap.pack(pady=5)

        right_panel = ctk.CTkFrame(self, fg_color="transparent")
        right_panel.grid(row=0, column=2, sticky="nsew", padx=10, pady=10)

        self.output_label = ctk.CTkLabel(right_panel, anchor="w", font=("Arial", 14, "bold"))
        self.output_label.pack(anchor="w", pady=(0, 5))
        self.output_text = ctk.CTkTextbox(right_panel, height=150)
        self.output_text.pack(fill="both", expand=True)

        right_btns = ctk.CTkFrame(right_panel, fg_color="transparent")
        right_btns.pack(pady=5, fill="x")
        self.btn_save_res = ctk.CTkButton(right_btns, width=100, command=lambda: utils.save_file(self.app, self.output_text), fg_color="transparent", border_width=2, text_color=("black", "white"))
        self.btn_save_res.pack(side="left", padx=2)
        self.btn_copy_out = ctk.CTkButton(right_btns, width=80, fg_color="#555555", command=lambda: utils.copy_to_clipboard(self.app, self.output_text.get("1.0", "end-1c")))
        self.btn_copy_out.pack(side="right", padx=2)

        bottom_panel = ctk.CTkFrame(self)
        bottom_panel.grid(row=1, column=0, columnspan=3, sticky="nsew", padx=10, pady=10)

        control_top = ctk.CTkFrame(bottom_panel, fg_color="transparent")
        control_top.pack(pady=10)

        self.cipher_label = ctk.CTkLabel(control_top, text="", font=("Arial", 14, "bold"))
        self.cipher_label.pack(side="left", padx=10)

        self.cipher_mode = ctk.CTkSegmentedButton(control_top, command=self.toggle_cipher_ui)
        self.cipher_mode.pack(side="left", padx=10)

        self.controls_frame = ctk.CTkFrame(bottom_panel, fg_color="transparent")
        self.controls_frame.pack(fill="both", expand=True, pady=5)

        self.caesar_frame = ctk.CTkFrame(self.controls_frame, fg_color="transparent")
        self.caesar_label = ctk.CTkLabel(self.caesar_frame, text="Shift: 0", font=("Arial", 16))
        self.caesar_label.pack()
        self.caesar_slider = ctk.CTkSlider(self.caesar_frame, from_=0, to=32, number_of_steps=32, command=self.update_slider_label, width=400)
        self.caesar_slider.set(0)
        self.caesar_slider.pack(pady=15)

        self.subst_frame = ctk.CTkFrame(self.controls_frame, fg_color="transparent")

        subst_header = ctk.CTkFrame(self.subst_frame, fg_color="transparent")
        subst_header.pack(fill="x", pady=5)

        self.key_label = ctk.CTkLabel(subst_header, font=("Arial", 14, "bold"))
        self.key_label.pack(side="left", padx=10)

        self.btn_gen = ctk.CTkButton(subst_header, width=90, command=self.generate_key, fg_color="#4B0082", hover_color="#3A0066")
        self.btn_gen.pack(side="left", padx=5)

        self.btn_save_key = ctk.CTkButton(subst_header, width=90, command=self.save_subst_key, fg_color="#1f538d")
        self.btn_save_key.pack(side="left", padx=5)

        self.btn_load_key = ctk.CTkButton(subst_header, width=90, command=self.load_subst_key, fg_color="#1f538d")
        self.btn_load_key.pack(side="left", padx=5)

        self.btn_clear_key = ctk.CTkButton(subst_header, width=90, command=self.clear_substitution_key, fg_color="#555555")
        self.btn_clear_key.pack(side="left", padx=5)

        self.subst_grid_frame = ctk.CTkFrame(self.subst_frame)
        self.subst_grid_frame.pack(pady=10, padx=10)

        self.btn_clear_all = ctk.CTkButton(bottom_panel, width=200, height=35, command=self.clear_all, fg_color="#C92C2C", hover_color="#962020", text_color="white", font=("Arial", 13, "bold"))
        self.btn_clear_all.pack(pady=(0, 15))

    def update_ui(self):
        t = self.app.translations[self.app.current_lang_str]
        self.input_label.configure(text=t["input_label"])
        self.output_label.configure(text=t["output_label"])
        self.btn_load.configure(text=t["btn_load"])
        self.btn_copy_in.configure(text=t["btn_copy"])
        self.btn_copy_out.configure(text=t["btn_copy"])
        self.btn_save_res.configure(text=t["btn_save_res"])
        self.cipher_label.configure(text=t["cipher_type"])

        current_cipher = self.cipher_mode.get()
        is_caesar = current_cipher in ["Шифр Цезаря", "Caesar Cipher", ""]
        self.cipher_mode.configure(values=[t["cipher_caesar"], t["cipher_subst"]])
        self.cipher_mode.set(t["cipher_caesar"] if is_caesar else t["cipher_subst"])
        self.toggle_cipher_ui(self.cipher_mode.get())

        self.key_label.configure(text=t["key_label"])
        self.btn_gen.configure(text=t["btn_gen_key"])
        self.btn_save_key.configure(text=t["btn_save_key"])
        self.btn_load_key.configure(text=t["btn_load_key"])
        self.btn_clear_key.configure(text=t["btn_clear_key"])
        self.btn_encrypt.configure(text=t["btn_encrypt"])
        self.btn_decrypt.configure(text=t["btn_decrypt"])
        self.btn_swap.configure(text=t["btn_swap"])
        self.btn_clear_all.configure(text=t["btn_clear"])

        alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
        self.caesar_slider.configure(to=len(alphabet) - 1, number_of_steps=len(alphabet) - 1)
        self.update_slider_label(self.caesar_slider.get())
        self.build_substitution_grid()

    def toggle_cipher_ui(self, value):
        self.caesar_frame.pack_forget()
        self.subst_frame.pack_forget()
        t = self.app.translations[self.app.current_lang_str]
        if value == t["cipher_caesar"]:
            self.caesar_frame.pack(pady=10)
        else:
            self.subst_frame.pack(pady=10, fill="x")

    def update_slider_label(self, value):
        t = self.app.translations[self.app.current_lang_str]
        self.caesar_label.configure(text=str(t["shift_label"]) + " " + str(int(value)))

    def clear_all(self):
        self.input_text.delete("1.0", "end")
        self.output_text.delete("1.0", "end")
        self.clear_substitution_key()
        self.caesar_slider.set(0)
        self.update_slider_label(0)

    def swap_texts(self):
        in_text = self.input_text.get("1.0", "end-1c")
        out_text = self.output_text.get("1.0", "end-1c")
        self.input_text.delete("1.0", "end")
        self.input_text.insert("1.0", out_text)
        self.output_text.delete("1.0", "end")
        self.output_text.insert("1.0", in_text)

    def process_cipher(self, encrypt=True):
        text = self.input_text.get("1.0", "end-1c")
        t = self.app.translations[self.app.current_lang_str]
        alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
        mode = self.cipher_mode.get()

        if mode == t["cipher_caesar"]:
            shift = int(self.caesar_slider.get())
            if not encrypt: shift = -shift
            shifted_alphabet = alphabet[shift % len(alphabet):] + alphabet[:shift % len(alphabet)]
            trans_table = str.maketrans(alphabet + alphabet.lower(), shifted_alphabet + shifted_alphabet.lower())
            result = text.translate(trans_table)
        else:
            key = self.get_substitution_key_string()
            if not key:
                messagebox.showerror("Error", t["error_len"])
                return
            if len(set(key)) != len(key):
                messagebox.showerror("Error", t["error_unique"])
                return
            if encrypt:
                trans_table = str.maketrans(alphabet + alphabet.lower(), key + key.lower())
            else:
                trans_table = str.maketrans(key + key.lower(), alphabet + alphabet.lower())
            result = text.translate(trans_table)

        self.output_text.delete("1.0", "end")
        self.output_text.insert("1.0", result)

    def build_substitution_grid(self):
        for widget in self.subst_grid_frame.winfo_children():
            widget.destroy()
        self.mapping_entries.clear()
        alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
        cols = 11 if len(alphabet) > 26 else 13
        for i, char in enumerate(alphabet):
            row = (i // cols) * 2
            col = i % cols
            lbl = ctk.CTkLabel(self.subst_grid_frame, text=char, font=("Arial", 16, "bold"), text_color="#2CC985")
            lbl.grid(row=row, column=col, padx=4, pady=(10, 0))
            ent = ctk.CTkEntry(self.subst_grid_frame, width=35, height=35, font=("Arial", 16), justify="center")
            ent.grid(row=row + 1, column=col, padx=4, pady=(2, 10))
            ent.bind("<KeyRelease>", self.validate_substitution_key)
            self.mapping_entries[char] = ent

    def validate_substitution_key(self, event=None):
        counts = Counter()
        for char, ent in self.mapping_entries.items():
            val = ent.get().upper()
            if len(val) > 1:
                ent.delete(1, "end")
                val = ent.get().upper()
            if val:
                counts[val] += 1
                ent.delete(0, "end")
                ent.insert(0, val)
        for char, ent in self.mapping_entries.items():
            val = ent.get()
            if val and counts[val] > 1:
                ent.configure(text_color="#FF4040")
            else:
                ent.configure(text_color=["black", "white"])

    def generate_key(self):
        alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
        shuffled = list(alphabet)
        random.shuffle(shuffled)
        for i, char in enumerate(alphabet):
            ent = self.mapping_entries[char]
            ent.delete(0, "end")
            ent.insert(0, shuffled[i])
        self.validate_substitution_key()

    def clear_substitution_key(self):
        for ent in self.mapping_entries.values():
            ent.delete(0, "end")
            ent.configure(text_color=["black", "white"])

    def get_substitution_key_string(self):
        alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
        key = ""
        for char in alphabet:
            val = self.mapping_entries[char].get()
            if not val: return None
            key += val
        return key

    def save_subst_key(self):
        t = self.app.translations[self.app.current_lang_str]
        key = self.get_substitution_key_string()
        if not key:
            messagebox.showerror("Error", t["error_len"])
            return
        if len(set(key)) != len(key):
            messagebox.showerror("Error", t["error_unique"])
            return
        file_path = filedialog.asksaveasfilename(defaultextension=".txt", filetypes=[("Text Files", "*.txt")])
        if file_path:
            try:
                with open(file_path, "w", encoding="utf-8") as f:
                    f.write(key)
                messagebox.showinfo("Success", t["success_save"])
            except Exception as e:
                messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))

    def load_subst_key(self):
        t = self.app.translations[self.app.current_lang_str]
        file_path = filedialog.askopenfilename(filetypes=[("Text Files", "*.txt")])
        if file_path:
            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    key = f.read().strip().upper()
                alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
                if len(key) != len(alphabet):
                    messagebox.showerror("Error", t["error_len"])
                    return
                for i, char in enumerate(alphabet):
                    ent = self.mapping_entries[char]
                    ent.delete(0, "end")
                    ent.insert(0, key[i])
                self.validate_substitution_key()
                messagebox.showinfo("Success", t["success_load"])
            except Exception as e:
                messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))