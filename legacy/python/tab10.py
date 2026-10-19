import customtkinter as ctk
from tkinter import messagebox
import random


class Tab10(ctk.CTkFrame):
    def __init__(self, master_widget, application_instance):
        super().__init__(master_widget, fg_color="transparent")
        self.application_instance = application_instance
        self.setup_user_interface()

    def setup_user_interface(self):
        self.pack(fill="both", expand=True, padx=10, pady=10)
        self.grid_columnconfigure(0, weight=1)
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        self.frame_split_secret = ctk.CTkFrame(self, corner_radius=10, fg_color="#1e1e21")
        self.frame_split_secret.grid(row=0, column=0, sticky="nsew", padx=(0, 10))

        self.label_split_title = ctk.CTkLabel(self.frame_split_secret, font=("Segoe UI", 16, "bold"),
                                              text_color="#4287f5")
        self.label_split_title.pack(pady=(20, 15))

        self.label_secret_value = ctk.CTkLabel(self.frame_split_secret, font=("Segoe UI", 14, "bold"))
        self.label_secret_value.pack(pady=(5, 5))
        self.entry_secret_value = ctk.CTkEntry(self.frame_split_secret, width=250, font=("Segoe UI", 14))
        self.entry_secret_value.pack(pady=(0, 10))

        self.label_total_shares_n = ctk.CTkLabel(self.frame_split_secret, font=("Segoe UI", 14, "bold"))
        self.label_total_shares_n.pack(pady=(5, 5))
        self.entry_total_shares_n = ctk.CTkEntry(self.frame_split_secret, width=250, font=("Segoe UI", 14))
        self.entry_total_shares_n.pack(pady=(0, 10))

        self.label_threshold_k = ctk.CTkLabel(self.frame_split_secret, font=("Segoe UI", 14, "bold"))
        self.label_threshold_k.pack(pady=(5, 5))
        self.entry_threshold_k = ctk.CTkEntry(self.frame_split_secret, width=250, font=("Segoe UI", 14))
        self.entry_threshold_k.pack(pady=(0, 10))

        self.label_prime_p = ctk.CTkLabel(self.frame_split_secret, font=("Segoe UI", 14, "bold"))
        self.label_prime_p.pack(pady=(5, 5))
        self.entry_prime_p = ctk.CTkEntry(self.frame_split_secret, width=250, font=("Segoe UI", 14))
        self.entry_prime_p.pack(pady=(0, 15))

        self.button_split_secret = ctk.CTkButton(self.frame_split_secret, width=200, height=35,
                                                 font=("Segoe UI", 13, "bold"), fg_color="#1f538d",
                                                 hover_color="#174476", command=self.execute_split_secret)
        self.button_split_secret.pack(pady=10)

        self.label_generated_shares = ctk.CTkLabel(self.frame_split_secret, font=("Segoe UI", 13, "bold"),
                                                   text_color="#2CC985")
        self.label_generated_shares.pack(pady=(10, 5))
        self.textbox_generated_shares = ctk.CTkTextbox(self.frame_split_secret, width=300, height=120,
                                                       font=("Courier", 12))
        self.textbox_generated_shares.pack(pady=(0, 20))

        self.frame_recover_secret = ctk.CTkFrame(self, corner_radius=10, fg_color="#1e1e21")
        self.frame_recover_secret.grid(row=0, column=1, sticky="nsew", padx=(10, 0))

        self.label_recover_title = ctk.CTkLabel(self.frame_recover_secret, font=("Segoe UI", 16, "bold"),
                                                text_color="#4287f5")
        self.label_recover_title.pack(pady=(20, 15))

        self.label_input_prime_p = ctk.CTkLabel(self.frame_recover_secret, font=("Segoe UI", 14, "bold"))
        self.label_input_prime_p.pack(pady=(5, 5))
        self.entry_input_prime_p = ctk.CTkEntry(self.frame_recover_secret, width=250, font=("Segoe UI", 14))
        self.entry_input_prime_p.pack(pady=(0, 15))

        self.label_input_shares = ctk.CTkLabel(self.frame_recover_secret, font=("Segoe UI", 14, "bold"))
        self.label_input_shares.pack(pady=(5, 5))
        self.textbox_input_shares = ctk.CTkTextbox(self.frame_recover_secret, width=300, height=150,
                                                   font=("Courier", 12))
        self.textbox_input_shares.pack(pady=(0, 15))

        self.button_recover_secret = ctk.CTkButton(self.frame_recover_secret, width=200, height=35,
                                                   font=("Segoe UI", 13, "bold"), fg_color="#B8860B",
                                                   hover_color="#8B6508", text_color="white",
                                                   command=self.execute_recover_secret)
        self.button_recover_secret.pack(pady=10)

        self.label_recovered_secret_title = ctk.CTkLabel(self.frame_recover_secret, font=("Segoe UI", 14, "bold"))
        self.label_recovered_secret_title.pack(pady=(20, 5))
        self.label_recovered_value = ctk.CTkLabel(self.frame_recover_secret, font=("Segoe UI", 24, "bold"),
                                                  text_color="#FFA500", text="")
        self.label_recovered_value.pack(pady=(0, 20))

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

    def calculate_modular_inverse(self, target_number, modulo_p):
        current_gcd, coeff_x, coeff_y = self.perform_extended_gcd(target_number, modulo_p)
        if current_gcd != 1:
            return None
        return coeff_x % modulo_p

    def execute_split_secret(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        string_secret = self.entry_secret_value.get().strip()
        string_n = self.entry_total_shares_n.get().strip()
        string_k = self.entry_threshold_k.get().strip()
        string_p = self.entry_prime_p.get().strip()

        if not string_secret.isdigit() or not string_n.isdigit() or not string_k.isdigit() or not string_p.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return

        integer_secret = int(string_secret)
        total_shares = int(string_n)
        threshold_value = int(string_k)
        prime_modulo = int(string_p)

        if threshold_value > total_shares or prime_modulo <= integer_secret or prime_modulo <= total_shares:
            messagebox.showwarning("Warning", translations_dictionary["err_sss_params"])
            return

        if not self.perform_primality_check(prime_modulo):
            messagebox.showwarning("Warning", translations_dictionary["err_sss_prime"])
            return

        polynomial_coefficients = [integer_secret]
        for _ in range(threshold_value - 1):
            random_coefficient = random.randint(1, prime_modulo - 1)
            polynomial_coefficients.append(random_coefficient)

        generated_shares_list = []
        for x_coordinate in range(1, total_shares + 1):
            y_coordinate = 0
            for exponent_power, current_coefficient in enumerate(polynomial_coefficients):
                term_value = (current_coefficient * pow(x_coordinate, exponent_power, prime_modulo)) % prime_modulo
                y_coordinate = (y_coordinate + term_value) % prime_modulo
            generated_shares_list.append(str(x_coordinate) + ", " + str(y_coordinate))

        self.textbox_generated_shares.delete("1.0", "end")
        self.textbox_generated_shares.insert("1.0", "\n".join(generated_shares_list))

    def execute_recover_secret(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        string_p = self.entry_input_prime_p.get().strip()
        input_shares_text = self.textbox_input_shares.get("1.0", "end-1c").strip()

        if not string_p.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return

        prime_modulo = int(string_p)
        shares_data_points = []

        for text_line in input_shares_text.split("\n"):
            if text_line.strip():
                parts = text_line.split(",")
                if len(parts) == 2:
                    try:
                        x_val = int(parts[0].strip())
                        y_val = int(parts[1].strip())
                        shares_data_points.append((x_val, y_val))
                    except ValueError:
                        messagebox.showwarning("Warning", translations_dictionary["err_sss_format"])
                        return

        if len(shares_data_points) < 2:
            messagebox.showwarning("Warning", translations_dictionary["err_sss_k_parts"])
            return

        recovered_secret_value = 0
        for i_index in range(len(shares_data_points)):
            x_i, y_i = shares_data_points[i_index]
            numerator_value = 1
            denominator_value = 1
            for j_index in range(len(shares_data_points)):
                if i_index != j_index:
                    x_j, y_j = shares_data_points[j_index]
                    numerator_value = (numerator_value * (-x_j)) % prime_modulo
                    denominator_value = (denominator_value * (x_i - x_j)) % prime_modulo

            inverse_denominator = self.calculate_modular_inverse(denominator_value, prime_modulo)
            if inverse_denominator is None:
                messagebox.showerror("Error", "Math Error: Inverse does not exist.")
                return

            lagrange_term = (y_i * numerator_value * inverse_denominator) % prime_modulo
            recovered_secret_value = (recovered_secret_value + lagrange_term) % prime_modulo

        recovered_secret_value = (recovered_secret_value + prime_modulo) % prime_modulo
        self.label_recovered_value.configure(text=str(recovered_secret_value))

    def update_ui(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        self.label_split_title.configure(text=translations_dictionary["sss_split_title"])
        self.label_secret_value.configure(text=translations_dictionary["lbl_sss_secret"])
        self.label_total_shares_n.configure(text=translations_dictionary["lbl_sss_n"])
        self.label_threshold_k.configure(text=translations_dictionary["lbl_sss_k"])
        self.label_prime_p.configure(text=translations_dictionary["lbl_sss_p"])
        self.button_split_secret.configure(text=translations_dictionary["btn_sss_split"])
        self.label_generated_shares.configure(text=translations_dictionary["lbl_sss_shares"])

        self.label_recover_title.configure(text=translations_dictionary["sss_recover_title"])
        self.label_input_prime_p.configure(text=translations_dictionary["lbl_sss_input_p"])
        self.label_input_shares.configure(text=translations_dictionary["lbl_sss_input_shares"])
        self.button_recover_secret.configure(text=translations_dictionary["btn_sss_recover"])
        self.label_recovered_secret_title.configure(text=translations_dictionary["lbl_sss_recovered"])