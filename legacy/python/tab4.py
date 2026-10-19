import customtkinter as ctk
from tkinter import messagebox
import utils
from config import ALPHABET_UKR, ALPHABET_ENG

class Tab4(ctk.CTkFrame):
    def __init__(self, master, app):
        super().__init__(master, fg_color="transparent")
        self.app = app
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

        self.key_label = ctk.CTkLabel(center_inner, font=("Arial", 14, "bold"))
        self.key_label.pack(pady=(0, 5))

        self.key_entry = ctk.CTkEntry(center_inner, width=160, font=("Arial", 14), justify="center")
        self.key_entry.pack(pady=(0, 15))

        self.btn_encrypt = ctk.CTkButton(center_inner, width=160, height=45, command=lambda: self.process_vigenere(True), fg_color="#2CC985", hover_color="#209662", text_color="black", font=("Arial", 14, "bold"))
        self.btn_encrypt.pack(pady=10)

        self.btn_decrypt = ctk.CTkButton(center_inner, width=160, height=45, command=lambda: self.process_vigenere(False), fg_color="#FFA500", hover_color="#D98C00", text_color="black", font=("Arial", 14, "bold"))
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

        self.btn_clear_all = ctk.CTkButton(bottom_panel, width=200, height=35, command=self.clear_all, fg_color="#C92C2C", hover_color="#962020", text_color="white", font=("Arial", 13, "bold"))
        self.btn_clear_all.pack(pady=15)

    def update_ui(self):
        t = self.app.translations[self.app.current_lang_str]
        self.input_label.configure(text=t["input_label"])
        self.output_label.configure(text=t["output_label"])
        self.btn_load.configure(text=t["btn_load"])
        self.btn_copy_in.configure(text=t["btn_copy"])
        self.btn_copy_out.configure(text=t["btn_copy"])
        self.btn_save_res.configure(text=t["btn_save_res"])
        self.key_label.configure(text=t["vigenere_key_label"])
        self.btn_encrypt.configure(text=t["btn_encrypt"])
        self.btn_decrypt.configure(text=t["btn_decrypt"])
        self.btn_swap.configure(text=t["btn_swap"])
        self.btn_clear_all.configure(text=t["btn_clear"])

    def clear_all(self):
        self.input_text.delete("1.0", "end")
        self.output_text.delete("1.0", "end")
        self.key_entry.delete(0, "end")

    def swap_texts(self):
        in_text = self.input_text.get("1.0", "end-1c")
        out_text = self.output_text.get("1.0", "end-1c")
        self.input_text.delete("1.0", "end")
        self.input_text.insert("1.0", out_text)
        self.output_text.delete("1.0", "end")
        self.output_text.insert("1.0", in_text)

    def process_vigenere(self, is_encrypt):
        text = self.input_text.get("1.0", "end-1c")
        key = self.key_entry.get().upper()
        t = self.app.translations[self.app.current_lang_str]
        if not text:
            messagebox.showwarning("Warning", t["no_text"])
            return
        if not key:
            messagebox.showwarning("Warning", t["err_vig_key"])
            return
        alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
        filtered_key = "".join([c for c in key if c in alphabet])
        if not filtered_key:
            messagebox.showwarning("Warning", t["err_vig_key"])
            return
        result = []
        key_index = 0
        key_len = len(filtered_key)
        alpha_len = len(alphabet)
        for char in text:
            is_upper = char.isupper()
            char_upper = char.upper()
            if char_upper in alphabet:
                char_idx = alphabet.index(char_upper)
                key_char = filtered_key[key_index % key_len]
                key_idx = alphabet.index(key_char)
                if is_encrypt: new_idx = (char_idx + key_idx) % alpha_len
                else: new_idx = (char_idx - key_idx + alpha_len) % alpha_len
                new_char = alphabet[new_idx]
                result.append(new_char if is_upper else new_char.lower())
                key_index += 1
            else:
                result.append(char)
        self.output_text.delete("1.0", "end")
        self.output_text.insert("1.0", "".join(result))