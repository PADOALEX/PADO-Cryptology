import customtkinter as ctk
from tkinter import messagebox
import random

class Tab8(ctk.CTkFrame):
    def __init__(self, master_widget, application_instance):
        super().__init__(master_widget, fg_color="transparent")
        self.application_instance = application_instance
        self.rsa_public_exponent = None
        self.rsa_modulo_n = None
        self.rsa_private_exponent = None
        self.setup_user_interface()

    def setup_user_interface(self):
        self.pack(fill="both", expand=True, padx=10, pady=10)
        self.grid_columnconfigure(0, weight=1)
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        self.frame_key_generation = ctk.CTkFrame(self, corner_radius=10, fg_color="#1e1e21")
        self.frame_key_generation.grid(row=0, column=0, sticky="nsew", padx=(0, 10))

        self.label_keygen_title = ctk.CTkLabel(self.frame_key_generation, font=("Segoe UI", 16, "bold"), text_color="#4287f5")
        self.label_keygen_title.pack(pady=(20, 15))

        self.label_prime_p = ctk.CTkLabel(self.frame_key_generation, font=("Segoe UI", 14, "bold"))
        self.label_prime_p.pack(pady=(10, 5))
        self.entry_prime_p = ctk.CTkEntry(self.frame_key_generation, width=250, font=("Segoe UI", 14))
        self.entry_prime_p.pack(pady=(0, 10))

        self.label_prime_q = ctk.CTkLabel(self.frame_key_generation, font=("Segoe UI", 14, "bold"))
        self.label_prime_q.pack(pady=(5, 5))
        self.entry_prime_q = ctk.CTkEntry(self.frame_key_generation, width=250, font=("Segoe UI", 14))
        self.entry_prime_q.pack(pady=(0, 15))

        self.button_generate_primes = ctk.CTkButton(self.frame_key_generation, width=200, height=35, font=("Segoe UI", 13, "bold"), fg_color="#4B0082", hover_color="#3A0066", command=self.execute_generate_primes)
        self.button_generate_primes.pack(pady=5)

        self.button_calculate_keys = ctk.CTkButton(self.frame_key_generation, width=200, height=35, font=("Segoe UI", 13, "bold"), fg_color="#1f538d", hover_color="#174476", command=self.execute_calculate_keys)
        self.button_calculate_keys.pack(pady=10)

        self.label_public_key = ctk.CTkLabel(self.frame_key_generation, font=("Segoe UI", 13, "bold"), text_color="#2CC985")
        self.label_public_key.pack(pady=(20, 5))
        self.textbox_public_key = ctk.CTkTextbox(self.frame_key_generation, width=300, height=60, font=("Courier", 12))
        self.textbox_public_key.pack(pady=(0, 10))

        self.label_private_key = ctk.CTkLabel(self.frame_key_generation, font=("Segoe UI", 13, "bold"), text_color="#FFA500")
        self.label_private_key.pack(pady=(10, 5))
        self.textbox_private_key = ctk.CTkTextbox(self.frame_key_generation, width=300, height=60, font=("Courier", 12))
        self.textbox_private_key.pack(pady=(0, 20))

        self.frame_encryption = ctk.CTkFrame(self, corner_radius=10, fg_color="#1e1e21")
        self.frame_encryption.grid(row=0, column=1, sticky="nsew", padx=(10, 0))

        self.label_encryption_title = ctk.CTkLabel(self.frame_encryption, font=("Segoe UI", 16, "bold"), text_color="#4287f5")
        self.label_encryption_title.pack(pady=(20, 15))

        self.label_message = ctk.CTkLabel(self.frame_encryption, font=("Segoe UI", 14, "bold"))
        self.label_message.pack(pady=(10, 5))
        self.textbox_message = ctk.CTkTextbox(self.frame_encryption, width=400, height=120, font=("Segoe UI", 14))
        self.textbox_message.pack(pady=(0, 15))

        self.frame_encryption_buttons = ctk.CTkFrame(self.frame_encryption, fg_color="transparent")
        self.frame_encryption_buttons.pack(pady=10)

        self.button_encrypt = ctk.CTkButton(self.frame_encryption_buttons, width=150, height=40, font=("Segoe UI", 14, "bold"), fg_color="#2CC985", hover_color="#209662", text_color="black", command=self.execute_encryption)
        self.button_encrypt.pack(side="left", padx=10)

        self.button_decrypt = ctk.CTkButton(self.frame_encryption_buttons, width=150, height=40, font=("Segoe UI", 14, "bold"), fg_color="#FFA500", hover_color="#D98C00", text_color="black", command=self.execute_decryption)
        self.button_decrypt.pack(side="left", padx=10)

        self.label_result = ctk.CTkLabel(self.frame_encryption, font=("Segoe UI", 14, "bold"))
        self.label_result.pack(pady=(20, 5))
        self.textbox_result = ctk.CTkTextbox(self.frame_encryption, width=400, height=120, font=("Courier", 12))
        self.textbox_result.pack(pady=(0, 20))

    def perform_primality_check(self, integer_value):
        if integer_value < 2:
            return False
        if integer_value in (2, 3):
            return True
        if integer_value % 2 == 0:
            return False
        value_r = 0
        value_s = integer_value - 1
        while value_s % 2 == 0:
            value_r += 1
            value_s //= 2
        for _ in range(5):
            random_a = random.randrange(2, integer_value - 1)
            calculated_x = pow(random_a, value_s, integer_value)
            if calculated_x == 1 or calculated_x == integer_value - 1:
                continue
            for _ in range(value_r - 1):
                calculated_x = pow(calculated_x, 2, integer_value)
                if calculated_x == integer_value - 1:
                    break
            else:
                return False
        return True

    def perform_extended_gcd(self, value_a, value_b):
        if value_a == 0:
            return value_b, 0, 1
        current_gcd, next_x, next_y = self.perform_extended_gcd(value_b % value_a, value_a)
        calculated_x = next_y - (value_b // value_a) * next_x
        calculated_y = next_x
        return current_gcd, calculated_x, calculated_y

    def execute_generate_primes(self):
        prime_p = None
        prime_q = None
        while not prime_p:
            candidate = random.randint(1000, 5000)
            if self.perform_primality_check(candidate):
                prime_p = candidate
        while not prime_q:
            candidate = random.randint(1000, 5000)
            if self.perform_primality_check(candidate) and candidate != prime_p:
                prime_q = candidate
        self.entry_prime_p.delete(0, "end")
        self.entry_prime_p.insert(0, str(prime_p))
        self.entry_prime_q.delete(0, "end")
        self.entry_prime_q.insert(0, str(prime_q))

    def execute_calculate_keys(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        string_p = self.entry_prime_p.get().strip()
        string_q = self.entry_prime_q.get().strip()
        if not string_p.isdigit() or not string_q.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return
        integer_p = int(string_p)
        integer_q = int(string_q)
        if not self.perform_primality_check(integer_p) or not self.perform_primality_check(integer_q):
            messagebox.showwarning("Warning", translations_dictionary["err_rsa_primes"])
            return
        self.rsa_modulo_n = integer_p * integer_q
        euler_totient = (integer_p - 1) * (integer_q - 1)
        self.rsa_public_exponent = 65537
        current_gcd, coeff_x, coeff_y = self.perform_extended_gcd(self.rsa_public_exponent, euler_totient)
        if current_gcd != 1:
            self.rsa_public_exponent = 3
            while self.perform_extended_gcd(self.rsa_public_exponent, euler_totient)[0] != 1:
                self.rsa_public_exponent += 2
            coeff_x = self.perform_extended_gcd(self.rsa_public_exponent, euler_totient)[1]
        self.rsa_private_exponent = coeff_x % euler_totient
        self.textbox_public_key.delete("1.0", "end")
        self.textbox_public_key.insert("1.0", str(self.rsa_public_exponent) + " , " + str(self.rsa_modulo_n))
        self.textbox_private_key.delete("1.0", "end")
        self.textbox_private_key.insert("1.0", str(self.rsa_private_exponent) + " , " + str(self.rsa_modulo_n))

    def execute_encryption(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        if not self.rsa_modulo_n or not self.rsa_public_exponent:
            messagebox.showwarning("Warning", translations_dictionary["err_rsa_keys"])
            return
        input_message = self.textbox_message.get("1.0", "end-1c")
        if not input_message:
            messagebox.showwarning("Warning", translations_dictionary["no_text"])
            return
        encrypted_blocks = [str(pow(ord(character), self.rsa_public_exponent, self.rsa_modulo_n)) for character in input_message]
        encrypted_string = " ".join(encrypted_blocks)
        self.textbox_result.delete("1.0", "end")
        self.textbox_result.insert("1.0", encrypted_string)

    def execute_decryption(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        if not self.rsa_modulo_n or not self.rsa_private_exponent:
            messagebox.showwarning("Warning", translations_dictionary["err_rsa_keys"])
            return
        input_ciphertext = self.textbox_message.get("1.0", "end-1c")
        if not input_ciphertext:
            messagebox.showwarning("Warning", translations_dictionary["no_text"])
            return
        try:
            decrypted_characters = [chr(pow(int(block), self.rsa_private_exponent, self.rsa_modulo_n)) for block in input_ciphertext.split()]
            decrypted_string = "".join(decrypted_characters)
            self.textbox_result.delete("1.0", "end")
            self.textbox_result.insert("1.0", decrypted_string)
        except Exception:
            messagebox.showerror("Error", translations_dictionary["err_rsa_dec"])

    def update_ui(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        self.label_keygen_title.configure(text=translations_dictionary["rsa_keygen_title"])
        self.label_prime_p.configure(text=translations_dictionary["lbl_p"])
        self.label_prime_q.configure(text=translations_dictionary["lbl_q"])
        self.button_generate_primes.configure(text=translations_dictionary["btn_gen_pq"])
        self.button_calculate_keys.configure(text=translations_dictionary["btn_calc_keys"])
        self.label_public_key.configure(text=translations_dictionary["lbl_pub_key"])
        self.label_private_key.configure(text=translations_dictionary["lbl_priv_key"])
        self.label_encryption_title.configure(text=translations_dictionary["rsa_enc_dec_title"])
        self.label_message.configure(text=translations_dictionary["lbl_rsa_msg"])
        self.button_encrypt.configure(text=translations_dictionary["btn_rsa_enc"])
        self.button_decrypt.configure(text=translations_dictionary["btn_rsa_dec"])
        self.label_result.configure(text=translations_dictionary["lbl_rsa_res"])