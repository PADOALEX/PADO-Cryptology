import customtkinter as ctk
from tkinter import messagebox
import random

class Tab6(ctk.CTkFrame):
    def __init__(self, master_widget, application_instance):
        super().__init__(master_widget, fg_color="transparent")
        self.application_instance = application_instance
        self.setup_user_interface()

    def setup_user_interface(self):
        self.pack(fill="both", expand=True, padx=10, pady=10)
        self.inner_tabs = ctk.CTkTabview(self)
        self.inner_tabs.pack(fill="both", expand=True)
        self.tab_euclidean = self.inner_tabs.add("Extended Euclidean")
        self.tab_exponentiation = self.inner_tabs.add("Modular Exponentiation")
        self.tab_random = self.inner_tabs.add("Random Generator")
        self.build_euclidean_interface()
        self.build_exponentiation_interface()
        self.build_random_interface()

    def build_euclidean_interface(self):
        self.euclidean_label_number_a = ctk.CTkLabel(self.tab_euclidean, font=("Arial", 14, "bold"), text="Number A:")
        self.euclidean_label_number_a.pack(pady=(20, 5))
        self.euclidean_entry_number_a = ctk.CTkEntry(self.tab_euclidean, width=300, font=("Arial", 14))
        self.euclidean_entry_number_a.pack(pady=(0, 15))
        self.euclidean_label_number_b = ctk.CTkLabel(self.tab_euclidean, font=("Arial", 14, "bold"), text="Number B (Modulo):")
        self.euclidean_label_number_b.pack(pady=(0, 5))
        self.euclidean_entry_number_b = ctk.CTkEntry(self.tab_euclidean, width=300, font=("Arial", 14))
        self.euclidean_entry_number_b.pack(pady=(0, 15))
        self.euclidean_button_calculate = ctk.CTkButton(self.tab_euclidean, width=200, height=40, font=("Arial", 14, "bold"), fg_color="#1f538d", hover_color="#174476", text="Calculate", command=self.execute_extended_euclidean)
        self.euclidean_button_calculate.pack(pady=10)
        self.euclidean_label_result_gcd = ctk.CTkLabel(self.tab_euclidean, font=("Arial", 14), text="GCD: ")
        self.euclidean_label_result_gcd.pack(pady=(10, 5))
        self.euclidean_label_result_coefficients = ctk.CTkLabel(self.tab_euclidean, font=("Arial", 14), text="Coefficients (x, y): ")
        self.euclidean_label_result_coefficients.pack(pady=5)
        self.euclidean_label_result_inverse = ctk.CTkLabel(self.tab_euclidean, font=("Arial", 14, "bold"), text_color="#2CC985", text="Modular Inverse: ")
        self.euclidean_label_result_inverse.pack(pady=5)

    def build_exponentiation_interface(self):
        self.exponentiation_label_base = ctk.CTkLabel(self.tab_exponentiation, font=("Arial", 14, "bold"), text="Base:")
        self.exponentiation_label_base.pack(pady=(20, 5))
        self.exponentiation_entry_base = ctk.CTkEntry(self.tab_exponentiation, width=300, font=("Arial", 14))
        self.exponentiation_entry_base.pack(pady=(0, 15))
        self.exponentiation_label_exponent = ctk.CTkLabel(self.tab_exponentiation, font=("Arial", 14, "bold"), text="Exponent:")
        self.exponentiation_label_exponent.pack(pady=(0, 5))
        self.exponentiation_entry_exponent = ctk.CTkEntry(self.tab_exponentiation, width=300, font=("Arial", 14))
        self.exponentiation_entry_exponent.pack(pady=(0, 15))
        self.exponentiation_label_modulo = ctk.CTkLabel(self.tab_exponentiation, font=("Arial", 14, "bold"), text="Modulo:")
        self.exponentiation_label_modulo.pack(pady=(0, 5))
        self.exponentiation_entry_modulo = ctk.CTkEntry(self.tab_exponentiation, width=300, font=("Arial", 14))
        self.exponentiation_entry_modulo.pack(pady=(0, 15))
        self.exponentiation_button_calculate = ctk.CTkButton(self.tab_exponentiation, width=200, height=40, font=("Arial", 14, "bold"), fg_color="#2CC985", hover_color="#209662", text_color="black", text="Calculate Power", command=self.execute_modular_exponentiation)
        self.exponentiation_button_calculate.pack(pady=10)
        self.exponentiation_label_result = ctk.CTkLabel(self.tab_exponentiation, font=("Arial", 16, "bold"), text_color="#FFA500", text="Result: ")
        self.exponentiation_label_result.pack(pady=10)

    def build_random_interface(self):
        self.random_label_minimum = ctk.CTkLabel(self.tab_random, font=("Arial", 14, "bold"), text="Min Value:")
        self.random_label_minimum.pack(pady=(10, 5))
        self.random_entry_minimum = ctk.CTkEntry(self.tab_random, width=300, font=("Arial", 14))
        self.random_entry_minimum.pack(pady=(0, 5))
        self.random_label_maximum = ctk.CTkLabel(self.tab_random, font=("Arial", 14, "bold"), text="Max Value:")
        self.random_label_maximum.pack(pady=(0, 5))
        self.random_entry_maximum = ctk.CTkEntry(self.tab_random, width=300, font=("Arial", 14))
        self.random_entry_maximum.pack(pady=(0, 15))
        self.random_label_modulo_n = ctk.CTkLabel(self.tab_random, font=("Arial", 14, "bold"), text="Modulo N (for Z_n*):")
        self.random_label_modulo_n.pack(pady=(0, 5))
        self.random_entry_modulo_n = ctk.CTkEntry(self.tab_random, width=300, font=("Arial", 14))
        self.random_entry_modulo_n.pack(pady=(0, 15))
        self.button_frame_random = ctk.CTkFrame(self.tab_random, fg_color="transparent")
        self.button_frame_random.pack(pady=5)
        self.random_button_generate_normal = ctk.CTkButton(self.button_frame_random, width=130, height=40, font=("Arial", 12, "bold"), fg_color="#1f538d", hover_color="#174476", text="Generate Random", command=self.execute_generate_random)
        self.random_button_generate_normal.pack(side="left", padx=5)
        self.random_button_generate_prime = ctk.CTkButton(self.button_frame_random, width=130, height=40, font=("Arial", 12, "bold"), fg_color="#4B0082", hover_color="#3A0066", text="Generate Prime", command=self.execute_generate_prime)
        self.random_button_generate_prime.pack(side="left", padx=5)
        self.random_button_generate_coprime = ctk.CTkButton(self.button_frame_random, width=140, height=40, font=("Arial", 12, "bold"), fg_color="#B8860B", hover_color="#8B6508", text="Generate from Z_n*", command=self.execute_generate_coprime)
        self.random_button_generate_coprime.pack(side="left", padx=5)
        self.random_label_result = ctk.CTkLabel(self.tab_random, font=("Arial", 16, "bold"), text_color="#2CC985", text="Generated Number: ")
        self.random_label_result.pack(pady=20)

    def execute_extended_euclidean(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        input_string_a = self.euclidean_entry_number_a.get().strip()
        input_string_b = self.euclidean_entry_number_b.get().strip()
        if not input_string_a.lstrip('-').isdigit() or not input_string_b.lstrip('-').isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return
        integer_a = int(input_string_a)
        integer_b = int(input_string_b)
        greatest_common_divisor, coefficient_x, coefficient_y = self.calculate_extended_gcd(integer_a, integer_b)
        self.euclidean_label_result_gcd.configure(text=translations_dictionary["lbl_gcd"] + " " + str(greatest_common_divisor))
        self.euclidean_label_result_coefficients.configure(text=translations_dictionary["lbl_x_y"] + " (" + str(coefficient_x) + ", " + str(coefficient_y) + ")")
        if greatest_common_divisor == 1:
            modular_inverse = coefficient_x % integer_b
            self.euclidean_label_result_inverse.configure(text=translations_dictionary["lbl_inverse"] + " " + str(modular_inverse))
        else:
            self.euclidean_label_result_inverse.configure(text=translations_dictionary["err_no_inverse"])

    def calculate_extended_gcd(self, value_a, value_b):
        if value_a == 0:
            return value_b, 0, 1
        current_gcd, next_x, next_y = self.calculate_extended_gcd(value_b % value_a, value_a)
        calculated_x = next_y - (value_b // value_a) * next_x
        calculated_y = next_x
        return current_gcd, calculated_x, calculated_y

    def execute_modular_exponentiation(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        input_base = self.exponentiation_entry_base.get().strip()
        input_exponent = self.exponentiation_entry_exponent.get().strip()
        input_modulo = self.exponentiation_entry_modulo.get().strip()
        if not input_base.lstrip('-').isdigit() or not input_exponent.isdigit() or not input_modulo.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return
        integer_base = int(input_base)
        integer_exponent = int(input_exponent)
        integer_modulo = int(input_modulo)
        if integer_modulo == 0:
            messagebox.showwarning("Warning", "Modulo cannot be zero")
            return
        final_result = 1
        current_base = integer_base % integer_modulo
        current_exponent = integer_exponent
        while current_exponent > 0:
            if current_exponent % 2 == 1:
                final_result = (final_result * current_base) % integer_modulo
            current_base = (current_base * current_base) % integer_modulo
            current_exponent = current_exponent // 2
        self.exponentiation_label_result.configure(text=translations_dictionary["lbl_result_exp"] + " " + str(final_result))

    def execute_generate_random(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        string_minimum = self.random_entry_minimum.get().strip()
        string_maximum = self.random_entry_maximum.get().strip()
        if not string_minimum.lstrip('-').isdigit() or not string_maximum.lstrip('-').isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return
        limit_minimum = int(string_minimum)
        limit_maximum = int(string_maximum)
        if limit_minimum > limit_maximum:
            limit_minimum, limit_maximum = limit_maximum, limit_minimum
        random_integer = random.randint(limit_minimum, limit_maximum)
        self.random_label_result.configure(text=translations_dictionary["lbl_rand_res"] + " " + str(random_integer))

    def execute_generate_prime(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        string_minimum = self.random_entry_minimum.get().strip()
        string_maximum = self.random_entry_maximum.get().strip()
        if not string_minimum.isdigit() or not string_maximum.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return
        limit_minimum = int(string_minimum)
        limit_maximum = int(string_maximum)
        if limit_minimum > limit_maximum:
            limit_minimum, limit_maximum = limit_maximum, limit_minimum
        if limit_maximum < 2:
            messagebox.showwarning("Warning", "No primes in this range")
            return
        limit_minimum = max(2, limit_minimum)
        generated_prime = None
        maximum_attempts = 1000
        for _ in range(maximum_attempts):
            candidate_number = random.randint(limit_minimum, limit_maximum)
            if self.check_is_prime(candidate_number):
                generated_prime = candidate_number
                break
        if generated_prime is not None:
            self.random_label_result.configure(text=translations_dictionary["lbl_rand_res"] + " " + str(generated_prime))
        else:
            self.random_label_result.configure(text="Prime not found in range")

    def execute_generate_coprime(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        string_modulo = self.random_entry_modulo_n.get().strip()
        if not string_modulo.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_mod"])
            return
        integer_modulo = int(string_modulo)
        if integer_modulo < 2:
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_mod"])
            return
        generated_coprime = None
        maximum_attempts = 1000
        for _ in range(maximum_attempts):
            candidate_number = random.randint(1, integer_modulo - 1)
            current_gcd, _, _ = self.calculate_extended_gcd(candidate_number, integer_modulo)
            if current_gcd == 1:
                generated_coprime = candidate_number
                break
        if generated_coprime is not None:
            self.random_label_result.configure(text=translations_dictionary["lbl_rand_res"] + " " + str(generated_coprime))
        else:
            self.random_label_result.configure(text="Coprime not found")

    def check_is_prime(self, candidate_integer):
        if candidate_integer < 2:
            return False
        if candidate_integer in (2, 3):
            return True
        if candidate_integer % 2 == 0:
            return False
        factor_r = 0
        factor_s = candidate_integer - 1
        while factor_s % 2 == 0:
            factor_r += 1
            factor_s //= 2
        testing_iterations = 5
        for _ in range(testing_iterations):
            random_a = random.randrange(2, candidate_integer - 1)
            value_x = pow(random_a, factor_s, candidate_integer)
            if value_x == 1 or value_x == candidate_integer - 1:
                continue
            for _ in range(factor_r - 1):
                value_x = pow(value_x, 2, candidate_integer)
                if value_x == candidate_integer - 1:
                    break
            else:
                return False
        return True

    def update_ui(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        self.inner_tabs._segmented_button._buttons_dict["Extended Euclidean"].configure(text=translations_dictionary["ext_euclid_title"])
        self.inner_tabs._segmented_button._buttons_dict["Modular Exponentiation"].configure(text=translations_dictionary["mod_exp_title"])
        self.inner_tabs._segmented_button._buttons_dict["Random Generator"].configure(text=translations_dictionary["rand_gen_title"])
        self.euclidean_label_number_a.configure(text=translations_dictionary["lbl_number_a"])
        self.euclidean_label_number_b.configure(text=translations_dictionary["lbl_number_b"])
        self.euclidean_button_calculate.configure(text=translations_dictionary["btn_calc_euclid"])
        self.euclidean_label_result_gcd.configure(text=translations_dictionary["lbl_gcd"])
        self.euclidean_label_result_coefficients.configure(text=translations_dictionary["lbl_x_y"])
        self.euclidean_label_result_inverse.configure(text=translations_dictionary["lbl_inverse"])
        self.exponentiation_label_base.configure(text=translations_dictionary["lbl_base"])
        self.exponentiation_label_exponent.configure(text=translations_dictionary["lbl_exponent"])
        self.exponentiation_label_modulo.configure(text=translations_dictionary["lbl_modulo"])
        self.exponentiation_button_calculate.configure(text=translations_dictionary["btn_calc_exp"])
        self.exponentiation_label_result.configure(text=translations_dictionary["lbl_result_exp"])
        self.random_label_minimum.configure(text=translations_dictionary["lbl_min_val"])
        self.random_label_maximum.configure(text=translations_dictionary["lbl_max_val"])
        self.random_label_modulo_n.configure(text=translations_dictionary["lbl_modulo_n"])
        self.random_button_generate_normal.configure(text=translations_dictionary["btn_gen_rand"])
        self.random_button_generate_prime.configure(text=translations_dictionary["btn_gen_prime"])
        self.random_button_generate_coprime.configure(text=translations_dictionary["btn_gen_coprime"])
        self.random_label_result.configure(text=translations_dictionary["lbl_rand_res"])