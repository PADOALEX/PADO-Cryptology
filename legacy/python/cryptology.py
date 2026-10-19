import customtkinter as ctk
from tkinter import ttk, messagebox
import tkinter as tk
from config import TRANSLATIONS
from tab1 import Tab1
from tab2 import Tab2
from tab3 import Tab3
from tab4 import Tab4
from tab5 import Tab5
from tab6 import Tab6
from tab7 import Tab7
from tab8 import Tab8
from tab9 import Tab9
from tab10 import Tab10

ctk.set_appearance_mode("Dark")
ctk.set_default_color_theme("blue")


class FrequencyApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("Crypto & Frequency Tool")
        self.geometry("1300x900")
        self.minsize(1100, 800)
        self.configure(fg_color="#1a1a1c")
        self.translations = TRANSLATIONS
        self.current_lang_str = "Ukr"
        self.global_font_family = "Segoe UI"
        self.style = ttk.Style()
        self.style.theme_use("clam")
        self.setup_user_interface()
        self.bind_keyboard_shortcuts()
        self.update_user_interface_text("Ukr")
        self.apply_custom_table_style("Dark")
        self.theme_segment_switch.set(self.translations["Ukr"]["theme_values"][0])

    def setup_user_interface(self):
        self.top_navigation_bar = ctk.CTkFrame(self, corner_radius=12, fg_color="#232326", border_width=1,
                                               border_color="#36363a")
        self.top_navigation_bar.pack(side="top", fill="x", padx=15, pady=(15, 10))

        self.button_show_about = ctk.CTkButton(self.top_navigation_bar, width=90,
                                               command=self.display_about_information, fg_color="#2c2c30",
                                               hover_color="#3a3a3e", border_width=1, border_color="#4a4a4f",
                                               text_color="#ffffff", font=(self.global_font_family, 12, "bold"),
                                               corner_radius=6)
        self.button_show_about.pack(side="left", padx=10, pady=10)

        self.button_show_faq = ctk.CTkButton(self.top_navigation_bar, width=90, command=self.display_faq_information,
                                             fg_color="#2c2c30", hover_color="#3a3a3e", border_width=1,
                                             border_color="#4a4a4f", text_color="#ffffff",
                                             font=(self.global_font_family, 12, "bold"), corner_radius=6)
        self.button_show_faq.pack(side="left", padx=5, pady=10)

        self.button_global_reset = ctk.CTkButton(self.top_navigation_bar, width=120, command=self.execute_global_reset,
                                                 fg_color="#8c1c1c", hover_color="#a82222", text_color="#ffffff",
                                                 font=(self.global_font_family, 12, "bold"), corner_radius=6)
        self.button_global_reset.pack(side="left", padx=20, pady=10)

        self.theme_segment_switch = ctk.CTkSegmentedButton(self.top_navigation_bar,
                                                           command=self.change_application_theme,
                                                           font=(self.global_font_family, 12, "bold"),
                                                           selected_color="#4287f5", selected_hover_color="#326ecf")
        self.theme_segment_switch.pack(side="right", padx=15, pady=10)

        self.language_segment_switch = ctk.CTkSegmentedButton(self.top_navigation_bar, values=["Ukr", "Eng"],
                                                              command=self.update_user_interface_text,
                                                              font=(self.global_font_family, 12, "bold"),
                                                              selected_color="#4287f5", selected_hover_color="#326ecf")
        self.language_segment_switch.set("Ukr")
        self.language_segment_switch.pack(side="right", padx=10, pady=10)

        self.main_tabview_container = ctk.CTkTabview(self, corner_radius=12, fg_color="#1f1f22",
                                                     segmented_button_fg_color="#232326",
                                                     segmented_button_selected_color="#4287f5",
                                                     segmented_button_selected_hover_color="#326ecf")
        self.main_tabview_container._segmented_button.configure(font=(self.global_font_family, 13, "bold"))
        self.main_tabview_container.pack(fill="both", expand=True, padx=15, pady=(0, 15))

        self.tab_frequency_analysis = self.main_tabview_container.add("Frequency Analysis")
        self.tab_encryption = self.main_tabview_container.add("Encryption")
        self.tab_cryptoanalysis = self.main_tabview_container.add("Cryptoanalysis")
        self.tab_vigenere_cipher = self.main_tabview_container.add("Vigenere Cipher")
        self.tab_one_time_pad = self.main_tabview_container.add("One-Time Pad")
        self.tab_math_tools = self.main_tabview_container.add("Math Tools")
        self.tab_number_theory = self.main_tabview_container.add("Number Theory")
        self.tab_rsa_crypto = self.main_tabview_container.add("RSA")
        self.tab_rsa_signature = self.main_tabview_container.add("RSA Signature")
        self.tab_secret_sharing = self.main_tabview_container.add("Secret Sharing")

        self.tab_1_frame_instance = Tab1(self.tab_frequency_analysis, self)
        self.tab_1_frame_instance.pack(fill="both", expand=True)

        self.tab_2_frame_instance = Tab2(self.tab_encryption, self)
        self.tab_2_frame_instance.pack(fill="both", expand=True)

        self.tab_3_frame_instance = Tab3(self.tab_cryptoanalysis, self)
        self.tab_3_frame_instance.pack(fill="both", expand=True)

        self.tab_4_frame_instance = Tab4(self.tab_vigenere_cipher, self)
        self.tab_4_frame_instance.pack(fill="both", expand=True)

        self.tab_5_frame_instance = Tab5(self.tab_one_time_pad, self)
        self.tab_5_frame_instance.pack(fill="both", expand=True)

        self.tab_6_frame_instance = Tab6(self.tab_math_tools, self)
        self.tab_6_frame_instance.pack(fill="both", expand=True)

        self.tab_7_frame_instance = Tab7(self.tab_number_theory, self)
        self.tab_7_frame_instance.pack(fill="both", expand=True)

        self.tab_8_frame_instance = Tab8(self.tab_rsa_crypto, self)
        self.tab_8_frame_instance.pack(fill="both", expand=True)

        self.tab_9_frame_instance = Tab9(self.tab_rsa_signature, self)
        self.tab_9_frame_instance.pack(fill="both", expand=True)

        self.tab_10_frame_instance = Tab10(self.tab_secret_sharing, self)
        self.tab_10_frame_instance.pack(fill="both", expand=True)

        self.create_context_menu_system()

    def update_user_interface_text(self, selected_language):
        self.current_lang_str = selected_language
        translations_dictionary = self.translations[selected_language]
        self.title(translations_dictionary["window_title"])
        self.button_show_about.configure(text=translations_dictionary["about"])
        self.button_show_faq.configure(text=translations_dictionary["faq"])
        self.button_global_reset.configure(text="Скинути все" if selected_language == "Ukr" else "Reset All")
        self.theme_segment_switch.configure(values=translations_dictionary["theme_values"])
        self.theme_segment_switch.set(
            translations_dictionary["theme_values"][0] if ctk.get_appearance_mode() == "Dark" else
            translations_dictionary["theme_values"][1])

        self.main_tabview_container._segmented_button._buttons_dict["Frequency Analysis"].configure(
            text="Frequency Analysis" if selected_language == "Eng" else "Частотний Аналіз")
        self.main_tabview_container._segmented_button._buttons_dict["Encryption"].configure(
            text="Encryption" if selected_language == "Eng" else "Шифрування")
        self.main_tabview_container._segmented_button._buttons_dict["Cryptoanalysis"].configure(
            text="Cryptoanalysis" if selected_language == "Eng" else "Криптоаналіз")
        self.main_tabview_container._segmented_button._buttons_dict["Vigenere Cipher"].configure(
            text=translations_dictionary["tab4_title"])
        self.main_tabview_container._segmented_button._buttons_dict["One-Time Pad"].configure(
            text=translations_dictionary["tab5_title"])
        self.main_tabview_container._segmented_button._buttons_dict["Math Tools"].configure(
            text=translations_dictionary["tab6_title"])
        self.main_tabview_container._segmented_button._buttons_dict["Number Theory"].configure(
            text=translations_dictionary["tab7_title"])
        self.main_tabview_container._segmented_button._buttons_dict["RSA"].configure(
            text=translations_dictionary["tab8_title"])
        self.main_tabview_container._segmented_button._buttons_dict["RSA Signature"].configure(
            text=translations_dictionary["tab9_title"])
        self.main_tabview_container._segmented_button._buttons_dict["Secret Sharing"].configure(
            text=translations_dictionary["tab10_title"])

        self.tab_1_frame_instance.update_ui()
        self.tab_2_frame_instance.update_ui()
        self.tab_3_frame_instance.update_ui()
        self.tab_4_frame_instance.update_ui()
        self.tab_5_frame_instance.update_ui()
        self.tab_6_frame_instance.update_ui()
        self.tab_7_frame_instance.update_ui()
        self.tab_8_frame_instance.update_ui()
        self.tab_9_frame_instance.update_ui()
        self.tab_10_frame_instance.update_ui()

        self.application_context_menu.entryconfigure(0, label=translations_dictionary["menu_copy"])
        self.application_context_menu.entryconfigure(1, label=translations_dictionary["menu_paste"])
        self.application_context_menu.entryconfigure(2, label=translations_dictionary["menu_cut"])
        self.application_context_menu.entryconfigure(4, label=translations_dictionary["menu_select_all"])

    def execute_global_reset(self):
        if hasattr(self.tab_1_frame_instance, 'clear_all'): self.tab_1_frame_instance.clear_all()
        if hasattr(self.tab_2_frame_instance, 'clear_all'): self.tab_2_frame_instance.clear_all()
        if hasattr(self.tab_3_frame_instance, 'clear_all_tab'): self.tab_3_frame_instance.clear_all_tab()
        if hasattr(self.tab_4_frame_instance, 'clear_all'): self.tab_4_frame_instance.clear_all()
        if hasattr(self.tab_5_frame_instance, 'clear_all'): self.tab_5_frame_instance.clear_all()

        if hasattr(self.tab_6_frame_instance, 'euclidean_entry_number_a'):
            self.tab_6_frame_instance.euclidean_entry_number_a.delete(0, "end")
            self.tab_6_frame_instance.euclidean_entry_number_b.delete(0, "end")
            self.tab_6_frame_instance.exponentiation_entry_base.delete(0, "end")
            self.tab_6_frame_instance.exponentiation_entry_exponent.delete(0, "end")
            self.tab_6_frame_instance.exponentiation_entry_modulo.delete(0, "end")
            self.tab_6_frame_instance.random_entry_minimum.delete(0, "end")
            self.tab_6_frame_instance.random_entry_maximum.delete(0, "end")
            self.tab_6_frame_instance.random_entry_modulo_n.delete(0, "end")

        if hasattr(self.tab_7_frame_instance, 'jacobi_entry_value_a'):
            self.tab_7_frame_instance.jacobi_entry_value_a.delete(0, "end")
            self.tab_7_frame_instance.jacobi_entry_value_n.delete(0, "end")
            self.tab_7_frame_instance.primality_entry_value_n.delete(0, "end")
            self.tab_7_frame_instance.primality_entry_value_k.delete(0, "end")
            self.tab_7_frame_instance.primality_entry_value_k.insert(0, "10")

        if hasattr(self.tab_8_frame_instance, 'entry_prime_p'):
            self.tab_8_frame_instance.entry_prime_p.delete(0, "end")
            self.tab_8_frame_instance.entry_prime_q.delete(0, "end")
            self.tab_8_frame_instance.textbox_public_key.delete("1.0", "end")
            self.tab_8_frame_instance.textbox_private_key.delete("1.0", "end")
            self.tab_8_frame_instance.textbox_message.delete("1.0", "end")
            self.tab_8_frame_instance.textbox_result.delete("1.0", "end")
            self.tab_8_frame_instance.rsa_public_exponent = None
            self.tab_8_frame_instance.rsa_modulo_n = None
            self.tab_8_frame_instance.rsa_private_exponent = None

        if hasattr(self.tab_9_frame_instance, 'entry_prime_p'):
            self.tab_9_frame_instance.entry_prime_p.delete(0, "end")
            self.tab_9_frame_instance.entry_prime_q.delete(0, "end")
            self.tab_9_frame_instance.textbox_public_key.delete("1.0", "end")
            self.tab_9_frame_instance.textbox_private_key.delete("1.0", "end")

            # Очищуємо вибір файлу замість старого текстового поля
            self.tab_9_frame_instance.target_file_path = None
            self.tab_9_frame_instance.label_file_path.configure(
                text="Файл не вибрано" if self.current_lang_str == "Ukr" else "No file selected"
            )

            self.tab_9_frame_instance.textbox_signature.delete("1.0", "end")
            self.tab_9_frame_instance.label_verification_status.configure(text="")
            self.tab_9_frame_instance.signature_public_exponent = None
            self.tab_9_frame_instance.signature_modulo_n = None
            self.tab_9_frame_instance.signature_private_exponent = None

        if hasattr(self.tab_10_frame_instance, 'entry_secret_value'):
            self.tab_10_frame_instance.entry_secret_value.delete(0, "end")
            self.tab_10_frame_instance.entry_total_shares_n.delete(0, "end")
            self.tab_10_frame_instance.entry_threshold_k.delete(0, "end")
            self.tab_10_frame_instance.entry_prime_p.delete(0, "end")
            self.tab_10_frame_instance.textbox_generated_shares.delete("1.0", "end")
            self.tab_10_frame_instance.entry_input_prime_p.delete(0, "end")
            self.tab_10_frame_instance.textbox_input_shares.delete("1.0", "end")
            self.tab_10_frame_instance.label_recovered_value.configure(text="")

    def bind_keyboard_shortcuts(self):
        self.bind_all("<Control-a>", self.perform_select_all_action)
        self.bind_class("Treeview", "<Control-c>", self.copy_data_from_treeview)

    def create_context_menu_system(self):
        self.application_context_menu = tk.Menu(self, tearoff=0, bg="#2b2b2b", fg="#ffffff", activebackground="#4287f5",
                                                activeforeground="#ffffff", font=(self.global_font_family, 10))
        self.application_context_menu.add_command(label="Copy",
                                                  command=lambda: self.focus_get().event_generate("<<Copy>>"))
        self.application_context_menu.add_command(label="Paste",
                                                  command=lambda: self.focus_get().event_generate("<<Paste>>"))
        self.application_context_menu.add_command(label="Cut",
                                                  command=lambda: self.focus_get().event_generate("<<Cut>>"))
        self.application_context_menu.add_separator()
        self.application_context_menu.add_command(label="Select All", command=self.perform_select_all_action)
        self.bind_class("Text", "<Button-3>", self.show_context_menu_action)
        self.bind_class("Entry", "<Button-3>", self.show_context_menu_action)
        self.bind_class("CTkEntry", "<Button-3>", self.show_context_menu_action)
        self.bind_class("CTkTextbox", "<Button-3>", self.show_context_menu_action)

    def show_context_menu_action(self, event_data):
        try:
            self.application_context_menu.tk_popup(event_data.x_root, event_data.y_root)
        finally:
            self.application_context_menu.grab_release()

    def perform_select_all_action(self, event_data=None):
        focused_widget = self.focus_get()
        if hasattr(focused_widget, "tag_add"):
            focused_widget.tag_add("sel", "1.0", "end")
        elif hasattr(focused_widget, "select_range"):
            focused_widget.select_range(0, "end")
            focused_widget.icursor("end")
        return "break"

    def copy_data_from_treeview(self, event_data=None):
        try:
            selected_tree_items = self.tab_1_frame_instance.tree.selection()
            if not selected_tree_items: return
            copied_rows = [str(self.tab_1_frame_instance.tree.item(item)["values"][0]) + "\t" + str(
                self.tab_1_frame_instance.tree.item(item)["values"][1]) + "\t" + str(
                self.tab_1_frame_instance.tree.item(item)["values"][2]) for item in selected_tree_items]
            self.clipboard_clear()
            self.clipboard_append("\n".join(copied_rows))
        except:
            pass

    def apply_custom_table_style(self, theme_mode):
        background_color, foreground_color = ("#232326", "#ffffff") if theme_mode == "Dark" else ("#ffffff", "#000000")
        header_background, header_foreground = ("#2c2c30", "#ffffff") if theme_mode == "Dark" else ("#e0e0e0",
                                                                                                    "#000000")
        self.style.configure("Treeview", background=background_color, foreground=foreground_color,
                             fieldbackground=background_color, borderwidth=0, font=(self.global_font_family, 12))
        self.style.configure("Treeview.Heading", background=header_background, foreground=header_foreground,
                             relief="flat", font=(self.global_font_family, 12, "bold"))
        self.style.map("Treeview", background=[("selected", "#4287f5")], foreground=[("selected", "#ffffff")])

    def change_application_theme(self, selected_theme_value):
        is_dark_theme_selected = selected_theme_value in ["Dark", "Темна"]
        ctk.set_appearance_mode("Dark" if is_dark_theme_selected else "Light")
        self.apply_custom_table_style("Dark" if is_dark_theme_selected else "Light")

    def display_about_information(self):
        translations_dictionary = self.translations[self.current_lang_str]
        messagebox.showinfo(translations_dictionary["about"],
                            str(translations_dictionary["about_text"]).format(translations_dictionary["dev_name"]))

    def display_faq_information(self):
        translations_dictionary = self.translations[self.current_lang_str]
        messagebox.showinfo(translations_dictionary["faq"], translations_dictionary["faq_text"])


if __name__ == "__main__":
    application_instance = FrequencyApp()
    application_instance.mainloop()