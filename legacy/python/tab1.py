import customtkinter as ctk
from tkinter import ttk, filedialog, messagebox
import re
import csv
from collections import Counter
import utils

class Tab1(ctk.CTkFrame):
    def __init__(self, master, app):
        super().__init__(master, fg_color="transparent")
        self.app = app
        self.setup_ui()

    def setup_ui(self):
        self.grid_columnconfigure(0, weight=1)
        self.grid_columnconfigure(1, weight=0)
        self.grid_columnconfigure(2, weight=1)
        self.grid_rowconfigure(0, weight=1)

        self.input_label = ctk.CTkLabel(self, anchor="w")
        self.input_label.grid(row=0, column=0, sticky="nw", padx=10, pady=(10, 0))
        self.input_text = ctk.CTkTextbox(self)
        self.input_text.grid(row=0, column=0, sticky="nsew", padx=10, pady=(35, 10))

        self.left_frame = ctk.CTkFrame(self, fg_color="transparent")
        self.left_frame.grid(row=1, column=0, pady=10)
        self.btn_load = ctk.CTkButton(self.left_frame, command=lambda: utils.load_file(self.app, self.input_text))
        self.btn_load.pack(side="left", padx=5)
        self.btn_save = ctk.CTkButton(self.left_frame, command=lambda: utils.save_file(self.app, self.input_text), fg_color="transparent", border_width=2, text_color=("black", "white"))
        self.btn_save.pack(side="left", padx=5)

        self.center_frame = ctk.CTkFrame(self, fg_color="transparent")
        self.center_frame.grid(row=0, column=1, rowspan=2, padx=5)
        self.btn_run = ctk.CTkButton(self.center_frame, width=120, height=50, command=self.run_analysis, fg_color="#2CC985", hover_color="#209662", text_color="black", font=("Arial", 14, "bold"))
        self.btn_run.pack(pady=10)
        self.btn_clear = ctk.CTkButton(self.center_frame, width=120, height=30, command=self.clear_all, fg_color="#C92C2C", hover_color="#962020", text_color="white")
        self.btn_clear.pack(pady=10)

        self.output_label = ctk.CTkLabel(self, anchor="w")
        self.output_label.grid(row=0, column=2, sticky="nw", padx=10, pady=(10, 0))

        self.table_frame = ctk.CTkFrame(self, fg_color="transparent")
        self.table_frame.grid(row=0, column=2, sticky="nsew", padx=10, pady=(35, 10))

        columns = ("char", "count", "freq")
        self.tree = ttk.Treeview(self.table_frame, columns=columns, show="headings")
        self.tree.column("char", anchor="center", width=80)
        self.tree.column("count", anchor="center", width=80)
        self.tree.column("freq", anchor="center", width=100)

        self.scrollbar = ttk.Scrollbar(self.table_frame, orient="vertical", command=self.tree.yview)
        self.tree.configure(yscrollcommand=self.scrollbar.set)
        self.tree.pack(side="left", fill="both", expand=True)
        self.scrollbar.pack(side="right", fill="y")

        for col in columns:
            self.tree.heading(col, command=lambda c=col: self.treeview_sort_column(c, False))

        self.total_label = ctk.CTkLabel(self, text="", text_color=("black", "white"))
        self.total_label.grid(row=0, column=2, sticky="sw", padx=10, pady=5)

        self.right_frame = ctk.CTkFrame(self, fg_color="transparent")
        self.right_frame.grid(row=1, column=2, pady=10)
        self.btn_load_res = ctk.CTkButton(self.right_frame, command=self.load_output_file)
        self.btn_load_res.pack(side="left", padx=5)
        self.btn_save_res = ctk.CTkButton(self.right_frame, command=self.save_output_file, fg_color="transparent", border_width=2, text_color=("black", "white"))
        self.btn_save_res.pack(side="left", padx=5)

    def update_ui(self):
        t = self.app.translations[self.app.current_lang_str]
        self.input_label.configure(text=t["input_label"])
        self.output_label.configure(text=t["output_label"])
        self.btn_load.configure(text=t["btn_load"])
        self.btn_save.configure(text=t["btn_save"])
        self.btn_run.configure(text=t["btn_run"])
        self.btn_clear.configure(text=t["btn_clear"])
        self.btn_load_res.configure(text=t["btn_load_res"])
        self.btn_save_res.configure(text=t["btn_save_res"])
        self.tree.heading("char", text=t["col_symbol"])
        self.tree.heading("count", text=t["col_count"])
        self.tree.heading("freq", text=t["col_freq"])

    def clear_all(self):
        self.input_text.delete("1.0", "end")
        for item in self.tree.get_children(): self.tree.delete(item)
        self.total_label.configure(text="")

    def run_analysis(self):
        text = self.input_text.get("1.0", "end-1c")
        t = self.app.translations[self.app.current_lang_str]
        for item in self.tree.get_children(): self.tree.delete(item)
        self.total_label.configure(text="")
        if not text:
            messagebox.showwarning("Warning", t["no_text"])
            return

        pattern = re.compile(r"[а-яА-ЯіІїЇєЄґҐ]") if self.app.current_lang_str == "Ukr" else re.compile(r"[a-zA-Z]")
        filtered = pattern.findall(text)

        if not filtered:
            messagebox.showinfo("Info", str(t["no_symbols"]) + " " + str(self.app.current_lang_str))
            return

        total = len(filtered)
        for char, count in sorted(Counter(filtered).items(), key=lambda x: (-x[1], x[0])):
            self.tree.insert("", "end", values=("'" + char + "'", count, str(round((count / total) * 100, 2)) + "%"))
        self.total_label.configure(text=str(t["total"]) + " " + str(total))

    def treeview_sort_column(self, col, reverse):
        l = [(self.tree.set(k, col), k) for k in self.tree.get_children("")]
        try:
            if col == "count":
                l.sort(key=lambda x: int(x[0]), reverse=reverse)
            elif col == "freq":
                l.sort(key=lambda x: float(str(x[0]).replace("%", "")), reverse=reverse)
            else:
                l.sort(key=lambda x: str(x[0]).lower(), reverse=reverse)
        except ValueError:
            l.sort(key=lambda x: x[0], reverse=reverse)
        for index, (_, k) in enumerate(l): self.tree.move(k, "", index)
        self.tree.heading(col, command=lambda: self.treeview_sort_column(col, not reverse))

    def load_output_file(self):
        t = self.app.translations[self.app.current_lang_str]
        file_path = filedialog.askopenfilename(filetypes=[("CSV Files", "*.csv"), ("Text Files", "*.txt")])
        if not file_path: return
        for item in self.tree.get_children(): self.tree.delete(item)
        try:
            if file_path.lower().endswith(".csv"):
                with open(file_path, newline="", encoding="utf-8-sig") as f:
                    reader = csv.reader(f, csv.Sniffer().sniff(f.read(1024)) if f.seek(0) or True else csv.excel)
                    next(reader, None)
                    count = sum(int(row[1]) for row in reader if len(row) >= 3 and self.tree.insert("", "end", values=(row[0], row[1], row[2])) or True)
                    self.total_label.configure(text=str(t["total"]) + " " + str(count))
            else:
                with open(file_path, "r", encoding="utf-8") as f:
                    for line in f:
                        if "|" in line and "---" not in line:
                            parts = [p.strip() for p in line.split("|")]
                            if len(parts) == 3 and parts[0] not in ("Symbol", "Символ"):
                                self.tree.insert("", "end", values=(parts[0], parts[1], parts[2]))
                        elif "Total" in line or "Всього" in line:
                            self.total_label.configure(text=line.strip())
        except Exception as e:
            messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))

    def save_output_file(self):
        children = self.tree.get_children()
        if not children: return
        file_path = filedialog.asksaveasfilename(defaultextension=".txt", filetypes=[("CSV", "*.csv"), ("Text", "*.txt")])
        if not file_path: return
        t = self.app.translations[self.app.current_lang_str]
        try:
            if file_path.lower().endswith(".csv"):
                with open(file_path, "w", newline="", encoding="utf-8-sig") as f:
                    writer = csv.writer(f, delimiter=";")
                    writer.writerow([t["col_symbol"], t["col_count"], t["col_freq"]])
                    for item in children: writer.writerow(self.tree.item(item)["values"])
            else:
                with open(file_path, "w", encoding="utf-8") as f:
                    f.write("{:^10} | {:^10} | {:^15}\n".format(t["col_symbol"], t["col_count"], t["col_freq"]) + "-" * 45 + "\n")
                    for item in children:
                        v = self.tree.item(item)["values"]
                        f.write("{:^10} | {:^10} | {:^15}\n".format(str(v[0]), str(v[1]), str(v[2])))
                    f.write("-" * 45 + "\n" + self.total_label.cget("text"))
        except Exception as e:
            messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))