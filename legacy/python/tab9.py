import customtkinter as ctk
from tkinter import messagebox, filedialog
import random
import hashlib
import os


class Tab9(ctk.CTkFrame):
    def __init__(self, master_widget, application_instance):
        super().__init__(master_widget, fg_color="transparent")
        self.application_instance = application_instance
        self.signature_public_exponent = None
        self.signature_modulo_n = None
        self.signature_private_exponent = None
        self.target_file_path = None  # Змінна для зберігання шляху до файлу
        self.setup_user_interface()

    def setup_user_interface(self):
        self.pack(fill="both", expand=True, padx=10, pady=10)
        self.grid_columnconfigure(0, weight=1)
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        self.frame_keygen = ctk.CTkFrame(self, corner_radius=10, fg_color="#1e1e21")
        self.frame_keygen.grid(row=0, column=0, sticky="nsew", padx=(0, 10))

        self.label_keygen_title = ctk.CTkLabel(self.frame_keygen, font=("Segoe UI", 16, "bold"), text_color="#4287f5")
        self.label_keygen_title.pack(pady=(20, 15))

        self.label_prime_p = ctk.CTkLabel(self.frame_keygen, font=("Segoe UI", 14, "bold"))
        self.label_prime_p.pack(pady=(10, 5))
        self.entry_prime_p = ctk.CTkEntry(self.frame_keygen, width=250, font=("Segoe UI", 14))
        self.entry_prime_p.pack(pady=(0, 10))

        self.label_prime_q = ctk.CTkLabel(self.frame_keygen, font=("Segoe UI", 14, "bold"))
        self.label_prime_q.pack(pady=(5, 5))
        self.entry_prime_q = ctk.CTkEntry(self.frame_keygen, width=250, font=("Segoe UI", 14))
        self.entry_prime_q.pack(pady=(0, 15))

        self.button_generate_primes = ctk.CTkButton(self.frame_keygen, width=200, height=35,
                                                    font=("Segoe UI", 13, "bold"), fg_color="#4B0082",
                                                    hover_color="#3A0066", command=self.execute_generate_primes)
        self.button_generate_primes.pack(pady=5)

        self.button_calculate_keys = ctk.CTkButton(self.frame_keygen, width=200, height=35,
                                                   font=("Segoe UI", 13, "bold"), fg_color="#1f538d",
                                                   hover_color="#174476", command=self.execute_calculate_keys)
        self.button_calculate_keys.pack(pady=10)

        self.label_public_key = ctk.CTkLabel(self.frame_keygen, font=("Segoe UI", 13, "bold"), text_color="#2CC985")
        self.label_public_key.pack(pady=(20, 5))
        self.textbox_public_key = ctk.CTkTextbox(self.frame_keygen, width=300, height=60, font=("Courier", 12))
        self.textbox_public_key.pack(pady=(0, 10))

        self.label_private_key = ctk.CTkLabel(self.frame_keygen, font=("Segoe UI", 13, "bold"), text_color="#FFA500")
        self.label_private_key.pack(pady=(10, 5))
        self.textbox_private_key = ctk.CTkTextbox(self.frame_keygen, width=300, height=60, font=("Courier", 12))
        self.textbox_private_key.pack(pady=(0, 20))

        self.frame_signature = ctk.CTkFrame(self, corner_radius=10, fg_color="#1e1e21")
        self.frame_signature.grid(row=0, column=1, sticky="nsew", padx=(10, 0))

        self.label_signature_title = ctk.CTkLabel(self.frame_signature, font=("Segoe UI", 16, "bold"),
                                                  text_color="#4287f5")
        self.label_signature_title.pack(pady=(20, 15))

        # Замість текстового поля тепер інтерфейс вибору файлу
        self.label_message = ctk.CTkLabel(self.frame_signature, font=("Segoe UI", 14, "bold"))
        self.label_message.pack(pady=(10, 5))

        self.button_select_file = ctk.CTkButton(self.frame_signature, width=200, height=40,
                                                font=("Segoe UI", 14, "bold"), fg_color="#4B0082",
                                                hover_color="#3A0066", command=self.execute_select_file)
        self.button_select_file.pack(pady=(0, 10))

        self.label_file_path = ctk.CTkLabel(self.frame_signature, font=("Segoe UI", 12), text_color="gray")
        self.label_file_path.pack(pady=(0, 15))

        self.frame_signature_buttons = ctk.CTkFrame(self.frame_signature, fg_color="transparent")
        self.frame_signature_buttons.pack(pady=10)

        self.button_sign = ctk.CTkButton(self.frame_signature_buttons, width=150, height=40,
                                         font=("Segoe UI", 14, "bold"), fg_color="#B8860B", hover_color="#8B6508",
                                         text_color="white", command=self.execute_sign_document)
        self.button_sign.pack(side="left", padx=10)

        self.button_verify = ctk.CTkButton(self.frame_signature_buttons, width=150, height=40,
                                           font=("Segoe UI", 14, "bold"), fg_color="#2CC985", hover_color="#209662",
                                           text_color="black", command=self.execute_verify_signature)
        self.button_verify.pack(side="left", padx=10)

        self.label_signature_result = ctk.CTkLabel(self.frame_signature, font=("Segoe UI", 14, "bold"))
        self.label_signature_result.pack(pady=(20, 5))
        self.textbox_signature = ctk.CTkTextbox(self.frame_signature, width=400, height=120, font=("Courier", 12))
        self.textbox_signature.pack(pady=(0, 20))

        self.label_verification_status = ctk.CTkLabel(self.frame_signature, font=("Segoe UI", 18, "bold"), text="")
        self.label_verification_status.pack(pady=10)

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
        self.signature_modulo_n = integer_p * integer_q
        euler_totient = (integer_p - 1) * (integer_q - 1)
        self.signature_public_exponent = 65537
        current_gcd, coeff_x, coeff_y = self.perform_extended_gcd(self.signature_public_exponent, euler_totient)
        if current_gcd != 1:
            self.signature_public_exponent = 3
            while self.perform_extended_gcd(self.signature_public_exponent, euler_totient)[0] != 1:
                self.signature_public_exponent += 2
            coeff_x = self.perform_extended_gcd(self.signature_public_exponent, euler_totient)[1]
        self.signature_private_exponent = coeff_x % euler_totient
        self.textbox_public_key.delete("1.0", "end")
        self.textbox_public_key.insert("1.0",
                                       str(self.signature_public_exponent) + " , " + str(self.signature_modulo_n))
        self.textbox_private_key.delete("1.0", "end")
        self.textbox_private_key.insert("1.0",
                                        str(self.signature_private_exponent) + " , " + str(self.signature_modulo_n))

    def execute_select_file(self):
        file_path = filedialog.askopenfilename()
        if file_path:
            self.target_file_path = file_path
            display_path = file_path if len(file_path) < 45 else "..." + file_path[-42:]
            self.label_file_path.configure(text=display_path, text_color="white")

    def execute_sign_document(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        if not self.signature_modulo_n or not self.signature_private_exponent:
            messagebox.showwarning("Warning", translations_dictionary["err_rsa_keys"])
            return

        if not self.target_file_path:
            msg = "Будь ласка, виберіть файл!" if self.application_instance.current_lang_str == "Ukr" else "Please select a file!"
            messagebox.showwarning("Warning", msg)
            return

        try:
            with open(self.target_file_path, "rb") as file:
                file_data = file.read()

            hash_object = hashlib.sha256(file_data)
            hash_hexadecimal = hash_object.hexdigest()
            hash_integer = int(hash_hexadecimal, 16)

            digital_signature = pow(hash_integer, self.signature_private_exponent, self.signature_modulo_n)
            self.textbox_signature.delete("1.0", "end")
            self.textbox_signature.insert("1.0", str(digital_signature))
            self.label_verification_status.configure(text="")

        except Exception as e:
            messagebox.showerror("Error", f"Помилка читання файлу: {e}")

    def execute_verify_signature(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        if not self.signature_modulo_n or not self.signature_public_exponent:
            messagebox.showwarning("Warning", translations_dictionary["err_rsa_keys"])
            return

        if not self.target_file_path:
            msg = "Будь ласка, виберіть файл!" if self.application_instance.current_lang_str == "Ukr" else "Please select a file!"
            messagebox.showwarning("Warning", msg)
            return

        input_signature_string = self.textbox_signature.get("1.0", "end-1c").strip()
        if not input_signature_string:
            messagebox.showwarning("Warning", translations_dictionary["no_text"])
            return

        if not input_signature_string.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_rsa_dec"])
            return

        try:
            with open(self.target_file_path, "rb") as file:
                file_data = file.read()

            hash_object = hashlib.sha256(file_data)
            hash_hexadecimal = hash_object.hexdigest()
            expected_hash_integer = int(hash_hexadecimal, 16) % self.signature_modulo_n

            provided_signature_integer = int(input_signature_string)
            decrypted_hash_integer = pow(provided_signature_integer, self.signature_public_exponent,
                                         self.signature_modulo_n)

            if decrypted_hash_integer == expected_hash_integer:
                self.label_verification_status.configure(text=translations_dictionary["msg_valid"],
                                                         text_color="#2CC985")
            else:
                self.label_verification_status.configure(text=translations_dictionary["msg_invalid"],
                                                         text_color="#C92C2C")

        except Exception as e:
            messagebox.showerror("Error", f"Помилка: {e}")

    def update_ui(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        self.label_keygen_title.configure(text=translations_dictionary["rsa_keygen_title"])
        self.label_prime_p.configure(text=translations_dictionary["lbl_p"])
        self.label_prime_q.configure(text=translations_dictionary["lbl_q"])
        self.button_generate_primes.configure(text=translations_dictionary["btn_gen_pq"])
        self.button_calculate_keys.configure(text=translations_dictionary["btn_calc_keys"])
        self.label_public_key.configure(text=translations_dictionary["lbl_pub_key"])
        self.label_private_key.configure(text=translations_dictionary["lbl_priv_key"])
        self.label_signature_title.configure(text=translations_dictionary["rsa_sign_title"])

        # Динамічний переклад для нових елементів вибору файлу
        is_ukr = self.application_instance.current_lang_str == "Ukr"
        self.label_message.configure(text="Файл для підпису:" if is_ukr else "File to sign:")
        self.button_select_file.configure(text="Вибрати файл" if is_ukr else "Select File")
        if not self.target_file_path:
            self.label_file_path.configure(text="Файл не вибрано" if is_ukr else "No file selected")

        self.button_sign.configure(text=translations_dictionary["btn_sign"])
        self.button_verify.configure(text=translations_dictionary["btn_verify"])
        self.label_signature_result.configure(text=translations_dictionary["lbl_signature"])

        if self.label_verification_status.cget("text") == self.application_instance.translations["Ukr"][
            "msg_valid"] or self.label_verification_status.cget("text") == \
                self.application_instance.translations["Eng"]["msg_valid"]:
            self.label_verification_status.configure(text=translations_dictionary["msg_valid"])
        elif self.label_verification_status.cget("text") == self.application_instance.translations["Ukr"][
            "msg_invalid"] or self.label_verification_status.cget("text") == \
                self.application_instance.translations["Eng"]["msg_invalid"]:
            self.label_verification_status.configure(text=translations_dictionary["msg_invalid"])