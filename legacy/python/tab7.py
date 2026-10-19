import customtkinter as ctk
from tkinter import messagebox
import random

class Tab7(ctk.CTkFrame):
    def __init__(self, master_widget, application_instance):
        super().__init__(master_widget, fg_color="transparent")
        self.application_instance = application_instance
        self.setup_user_interface()

    def setup_user_interface(self):
        self.pack(fill="both", expand=True, padx=10, pady=10)
        self.inner_tabs = ctk.CTkTabview(self)
        self.inner_tabs.pack(fill="both", expand=True)
        self.tab_jacobi_symbol = self.inner_tabs.add("Jacobi Symbol")
        self.tab_primality_test = self.inner_tabs.add("Solovay-Strassen Primality")
        self.build_jacobi_interface()
        self.build_primality_interface()

    def build_jacobi_interface(self):
        self.jacobi_label_value_a = ctk.CTkLabel(self.tab_jacobi_symbol, font=("Arial", 14, "bold"))
        self.jacobi_label_value_a.pack(pady=(20, 5))
        self.jacobi_entry_value_a = ctk.CTkEntry(self.tab_jacobi_symbol, width=300, font=("Arial", 14))
        self.jacobi_entry_value_a.pack(pady=(0, 15))
        self.jacobi_label_value_n = ctk.CTkLabel(self.tab_jacobi_symbol, font=("Arial", 14, "bold"))
        self.jacobi_label_value_n.pack(pady=(0, 5))
        self.jacobi_entry_value_n = ctk.CTkEntry(self.tab_jacobi_symbol, width=300, font=("Arial", 14))
        self.jacobi_entry_value_n.pack(pady=(0, 15))
        self.jacobi_button_calculate = ctk.CTkButton(self.tab_jacobi_symbol, width=220, height=40, font=("Arial", 14, "bold"), fg_color="#1f538d", hover_color="#174476", command=self.execute_jacobi_calculation)
        self.jacobi_button_calculate.pack(pady=10)
        self.jacobi_label_result = ctk.CTkLabel(self.tab_jacobi_symbol, font=("Arial", 16, "bold"), text_color="#2CC985", text="")
        self.jacobi_label_result.pack(pady=20)

    def build_primality_interface(self):
        self.primality_label_value_n = ctk.CTkLabel(self.tab_primality_test, font=("Arial", 14, "bold"))
        self.primality_label_value_n.pack(pady=(20, 5))
        self.primality_entry_value_n = ctk.CTkEntry(self.tab_primality_test, width=300, font=("Arial", 14))
        self.primality_entry_value_n.pack(pady=(0, 15))
        self.primality_label_value_k = ctk.CTkLabel(self.tab_primality_test, font=("Arial", 14, "bold"))
        self.primality_label_value_k.pack(pady=(0, 5))
        self.primality_entry_value_k = ctk.CTkEntry(self.tab_primality_test, width=300, font=("Arial", 14))
        self.primality_entry_value_k.insert(0, "10")
        self.primality_entry_value_k.pack(pady=(0, 15))
        self.primality_button_test = ctk.CTkButton(self.tab_primality_test, width=220, height=40, font=("Arial", 14, "bold"), fg_color="#4B0082", hover_color="#3A0066", command=self.execute_primality_test)
        self.primality_button_test.pack(pady=10)
        self.primality_label_result = ctk.CTkLabel(self.tab_primality_test, font=("Arial", 16, "bold"), text_color="#FFA500", text="")
        self.primality_label_result.pack(pady=20)

    def calculate_jacobi_algorithm(self, integer_a, integer_n):
        if integer_n <= 0 or integer_n % 2 == 0:
            return None
        integer_a = integer_a % integer_n
        jacobi_result = 1
        while integer_a != 0:
            while integer_a % 2 == 0:
                integer_a = integer_a // 2
                remainder_eight = integer_n % 8
                if remainder_eight == 3 or remainder_eight == 5:
                    jacobi_result = -jacobi_result
            integer_a, integer_n = integer_n, integer_a
            if integer_a % 4 == 3 and integer_n % 4 == 3:
                jacobi_result = -jacobi_result
            integer_a = integer_a % integer_n
        if integer_n == 1:
            return jacobi_result
        return 0

    def execute_jacobi_calculation(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        input_string_a = self.jacobi_entry_value_a.get().strip()
        input_string_n = self.jacobi_entry_value_n.get().strip()
        if not input_string_a.lstrip('-').isdigit() or not input_string_n.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return
        integer_a = int(input_string_a)
        integer_n = int(input_string_n)
        jacobi_final_value = self.calculate_jacobi_algorithm(integer_a, integer_n)
        if jacobi_final_value is None:
            messagebox.showwarning("Warning", translations_dictionary["err_jacobi_n"])
            return
        self.jacobi_label_result.configure(text=translations_dictionary["lbl_res_jacobi"] + " " + str(jacobi_final_value))

    def execute_primality_test(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        input_string_n = self.primality_entry_value_n.get().strip()
        input_string_k = self.primality_entry_value_k.get().strip()
        if not input_string_n.isdigit() or not input_string_k.isdigit():
            messagebox.showwarning("Warning", translations_dictionary["err_invalid_num"])
            return
        candidate_prime = int(input_string_n)
        iterations_count = int(input_string_k)
        is_candidate_prime = self.perform_solovay_strassen(candidate_prime, iterations_count)
        if is_candidate_prime:
            result_text_output = translations_dictionary["res_prime_true"]
            color_output = "#2CC985"
        else:
            result_text_output = translations_dictionary["res_prime_false"]
            color_output = "#C92C2C"
        self.primality_label_result.configure(text=translations_dictionary["lbl_res_prime"] + " " + result_text_output, text_color=color_output)

    def perform_solovay_strassen(self, target_number, test_iterations):
        if target_number < 2:
            return False
        if target_number == 2 or target_number == 3:
            return True
        if target_number % 2 == 0:
            return False
        for _ in range(test_iterations):
            random_base_a = random.randrange(2, target_number - 1)
            calculated_jacobi = self.calculate_jacobi_algorithm(random_base_a, target_number)
            modular_exponentiation_result = pow(random_base_a, (target_number - 1) // 2, target_number)
            if calculated_jacobi == 0 or pow(calculated_jacobi, 1, target_number) != modular_exponentiation_result:
                return False
        return True

    def update_ui(self):
        translations_dictionary = self.application_instance.translations[self.application_instance.current_lang_str]
        self.inner_tabs._segmented_button._buttons_dict["Jacobi Symbol"].configure(text=translations_dictionary["jacobi_title"])
        self.inner_tabs._segmented_button._buttons_dict["Solovay-Strassen Primality"].configure(text=translations_dictionary["primality_title"])
        self.jacobi_label_value_a.configure(text=translations_dictionary["lbl_jacobi_a"])
        self.jacobi_label_value_n.configure(text=translations_dictionary["lbl_jacobi_n"])
        self.jacobi_button_calculate.configure(text=translations_dictionary["btn_calc_jacobi"])
        self.primality_label_value_n.configure(text=translations_dictionary["lbl_prime_n"])
        self.primality_label_value_k.configure(text=translations_dictionary["lbl_prime_k"])
        self.primality_button_test.configure(text=translations_dictionary["btn_test_prime"])
        if not self.jacobi_label_result.cget("text").endswith(("1", "0", "-1")):
            self.jacobi_label_result.configure(text=translations_dictionary["lbl_res_jacobi"])
        if not self.primality_label_result.cget("text"):
            self.primality_label_result.configure(text=translations_dictionary["lbl_res_prime"])