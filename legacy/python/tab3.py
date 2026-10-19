import customtkinter as ctk
from tkinter import filedialog, messagebox
import re
from collections import Counter
import utils
from config import ALPHABET_UKR, ALPHABET_ENG, FREQ_UKR, FREQ_ENG

class Tab3(ctk.CTkFrame):
    def __init__(self, master, app):
        super().__init__(master, fg_color="transparent")
        self.app = app
        self.crypto_entries = {}
        self.sorted_cipher_chars = []
        self.cipher_text_original = ""
        self.char_counts = Counter()
        self.char_freqs = {}
        self._preview_after_id = None
        self.row_frames = {}
        self._sort_mode_val = "freq"
        self.setup_ui()

    def setup_ui(self):
        self.grid_columnconfigure(0, weight=5)
        self.grid_columnconfigure(1, weight=4)
        self.grid_rowconfigure(0, weight=1)

        left_panel = ctk.CTkFrame(self, fg_color="transparent")
        left_panel.grid(row=0, column=0, sticky="nsew", padx=(10, 5), pady=10)
        left_panel.grid_rowconfigure(1, weight=1)
        left_panel.grid_rowconfigure(4, weight=1)
        left_panel.grid_columnconfigure(0, weight=1)

        self.input_text = ctk.CTkTextbox(left_panel, height=160, font=("Courier", 11))
        self.input_text.grid(row=1, column=0, sticky="nsew")

        input_header = ctk.CTkFrame(left_panel, fg_color="transparent")
        input_header.grid(row=0, column=0, sticky="ew", pady=(0, 4))
        input_header.grid_columnconfigure(0, weight=1)

        self.input_label = ctk.CTkLabel(input_header, text="Ciphertext:", font=("Arial", 13, "bold"))
        self.input_label.pack(side="left")

        self.btn_load_text = ctk.CTkButton(input_header, width=110, height=28, command=self.load_text_file, fg_color="#1f538d", hover_color="#174476", font=("Arial", 11))
        self.btn_load_text.pack(side="right")

        btn_frame1 = ctk.CTkFrame(left_panel, fg_color="transparent")
        btn_frame1.grid(row=2, column=0, sticky="ew", pady=(6, 2))

        self.btn_analyze = ctk.CTkButton(btn_frame1, width=105, height=36, command=self.analyze_text, fg_color="#1f538d", hover_color="#174476", font=("Arial", 12, "bold"))
        self.btn_analyze.pack(side="left", padx=(0, 5))

        self.btn_auto = ctk.CTkButton(btn_frame1, width=105, height=36, command=self.auto_guess, fg_color="#4B0082", hover_color="#3A0066", font=("Arial", 12, "bold"))
        self.btn_auto.pack(side="left", padx=5)

        self.btn_reset = ctk.CTkButton(btn_frame1, width=85, height=36, command=self.reset_all, fg_color="#C92C2C", hover_color="#962020", font=("Arial", 12, "bold"))
        self.btn_reset.pack(side="left", padx=5)

        self.btn_clear_all = ctk.CTkButton(btn_frame1, width=100, height=36, command=self.clear_all_tab, fg_color="#962020", hover_color="#7A1515", font=("Arial", 12, "bold"))
        self.btn_clear_all.pack(side="left", padx=5)

        btn_frame2 = ctk.CTkFrame(left_panel, fg_color="transparent")
        btn_frame2.grid(row=3, column=0, sticky="ew", pady=(2, 6))

        self.btn_import = ctk.CTkButton(btn_frame2, width=120, height=32, command=self.load_key, fg_color="#555555", hover_color="#444444", font=("Arial", 11))
        self.btn_import.pack(side="left", padx=(0, 5))

        self.btn_export = ctk.CTkButton(btn_frame2, width=120, height=32, command=self.save_key, fg_color="#555555", hover_color="#444444", font=("Arial", 11))
        self.btn_export.pack(side="left", padx=5)

        self.btn_copy_result = ctk.CTkButton(btn_frame2, width=150, height=32, command=self.copy_result, fg_color="#2CC985", hover_color="#209662", text_color="black", font=("Arial", 11, "bold"))
        self.btn_copy_result.pack(side="right", padx=5)

        self.preview_label = ctk.CTkLabel(left_panel, text="", font=("Arial", 13, "bold"), text_color="#2CC985")
        self.preview_label.grid(row=4, column=0, sticky="nw", pady=(8, 2))

        preview_frame = ctk.CTkFrame(left_panel, fg_color="transparent")
        preview_frame.grid(row=4, column=0, sticky="nsew", pady=(30, 4))
        preview_frame.grid_columnconfigure(0, weight=1)
        preview_frame.grid_rowconfigure(0, weight=1)

        self.preview_text = ctk.CTkTextbox(preview_frame, font=("Courier", 11), fg_color="#1a2a1a", text_color="#2CC985", wrap="word")
        self.preview_text.grid(row=0, column=0, sticky="nsew")
        self.preview_text.configure(state="disabled")

        ngrams_frame = ctk.CTkFrame(left_panel, corner_radius=8)
        ngrams_frame.grid(row=5, column=0, sticky="ew", pady=(4, 0))

        self.ngrams_label = ctk.CTkLabel(ngrams_frame, anchor="w", font=("Arial", 11, "bold"))
        self.ngrams_label.pack(anchor="w", padx=10, pady=(8, 2))

        self.ngrams_text = ctk.CTkTextbox(ngrams_frame, height=70, font=("Courier", 10), fg_color="#252525", text_color="#aaaaaa")
        self.ngrams_text.pack(fill="x", padx=8, pady=(0, 8))

        right_panel = ctk.CTkFrame(self, corner_radius=10)
        right_panel.grid(row=0, column=1, sticky="nsew", padx=(5, 10), pady=10)
        right_panel.grid_rowconfigure(3, weight=1)
        right_panel.grid_columnconfigure(0, weight=1)

        self.stats_frame = ctk.CTkFrame(right_panel, fg_color="#1e1e1e", corner_radius=8)
        self.stats_frame.grid(row=0, column=0, sticky="ew", padx=8, pady=(8, 4))

        self.stats_label = ctk.CTkLabel(self.stats_frame, font=("Arial", 10, "bold"))
        self.stats_label.pack(side="left", padx=8, pady=4)

        self.stat_total = ctk.CTkLabel(self.stats_frame, text="Символів: 0", font=("Arial", 9), text_color="#888888")
        self.stat_total.pack(side="left", padx=6, pady=4)

        self.stat_unique = ctk.CTkLabel(self.stats_frame, text="Унікальних: 0", font=("Arial", 9), text_color="#888888")
        self.stat_unique.pack(side="left", padx=6, pady=4)

        self.stat_mapped = ctk.CTkLabel(self.stats_frame, text="Замінено: 0/0", font=("Arial", 9, "bold"), text_color="#FFA500")
        self.stat_mapped.pack(side="right", padx=8, pady=4)

        prog_row = ctk.CTkFrame(right_panel, fg_color="transparent")
        prog_row.grid(row=1, column=0, sticky="ew", padx=8, pady=(0, 4))
        prog_row.grid_columnconfigure(1, weight=1)

        self.progress_label = ctk.CTkLabel(prog_row, text="0%", font=("Arial", 9, "bold"), text_color="#888888", width=32)
        self.progress_label.grid(row=0, column=0, padx=(0, 4))

        self.progress_bar = ctk.CTkProgressBar(prog_row, height=7, corner_radius=4, progress_color="#2CC985")
        self.progress_bar.grid(row=0, column=1, sticky="ew")
        self.progress_bar.set(0)

        ctrl_row = ctk.CTkFrame(right_panel, fg_color="transparent")
        ctrl_row.grid(row=2, column=0, sticky="ew", padx=8, pady=(0, 4))

        ctk.CTkLabel(ctrl_row, text="Сорт:", font=("Arial", 10), text_color="#888888").pack(side="left", padx=(0, 4))
        self.sort_mode = ctk.CTkSegmentedButton(ctrl_row, values=["↕ Freq", "A-Z"], command=self.resort, width=110, height=24, font=("Arial", 10))
        self.sort_mode.set("↕ Freq")
        self.sort_mode.pack(side="left", padx=(0, 8))

        self.btn_jump_dup = ctk.CTkButton(ctrl_row, text="", width=80, height=24, font=("Arial", 10), fg_color="#8B3A3A", hover_color="#6B2A2A", command=self.jump_to_duplicate)
        self.btn_jump_dup.pack(side="left", padx=(0, 4))

        self.btn_load_table = ctk.CTkButton(ctrl_row, text="", width=70, height=24, font=("Arial", 10), fg_color="#2a4a6a", hover_color="#1e3a5a", command=self.load_table_file)
        self.btn_load_table.pack(side="right", padx=0)

        header_frame = ctk.CTkFrame(right_panel, fg_color="#252525", corner_radius=5)
        header_frame.grid(row=3, column=0, sticky="new", padx=8, pady=(0, 1))

        self.hdr_rank = ctk.CTkLabel(header_frame, text="#", width=22, font=("Arial", 9, "bold"), text_color="#555555", cursor="hand2")
        self.hdr_rank.pack(side="left", padx=(6, 0), pady=4)

        self.lbl_orig = ctk.CTkLabel(header_frame, width=36, font=("Arial", 9, "bold"), text_color="#E05050", cursor="hand2")
        self.lbl_orig.pack(side="left", padx=2, pady=4)

        self.hdr_freq = ctk.CTkLabel(header_frame, text="Freq", width=46, font=("Arial", 9, "bold"), text_color="#888888", cursor="hand2")
        self.hdr_freq.pack(side="left", padx=2, pady=4)

        self.lbl_rep = ctk.CTkLabel(header_frame, width=46, font=("Arial", 9, "bold"), text_color="#2CC985")
        self.lbl_rep.pack(side="left", padx=2, pady=4)

        self.lbl_hint = ctk.CTkLabel(header_frame, width=44, font=("Arial", 9, "bold"), text_color="#666666")
        self.lbl_hint.pack(side="left", padx=2, pady=4)

        self.hdr_freq.bind("<Button-1>", lambda e: self.resort("↕ Freq"))
        self.lbl_orig.bind("<Button-1>", lambda e: self.resort("A-Z"))

        self.scrollable_frame = ctk.CTkScrollableFrame(right_panel, corner_radius=5)
        self.scrollable_frame.grid(row=3, column=0, sticky="nsew", padx=8, pady=(28, 8))

        self.entries_container = ctk.CTkFrame(self.scrollable_frame, fg_color="transparent")
        self.entries_container.pack(fill="both", expand=True)

    def update_ui(self):
        t = self.app.translations[self.app.current_lang_str]
        self.input_label.configure(text=t["input_label"])
        self.btn_load_text.configure(text=t["tab3_load_text_btn"])
        self.btn_analyze.configure(text=t["tab3_analyze_btn"])
        self.btn_auto.configure(text=t["tab3_auto_btn"])
        self.btn_reset.configure(text=t["tab3_reset_btn"])
        self.btn_clear_all.configure(text=t["tab3_clear_all_btn"])
        self.btn_export.configure(text=t["tab3_export_btn"])
        self.btn_import.configure(text=t["tab3_import_btn"])
        self.btn_copy_result.configure(text=t["tab3_copy_result_btn"])
        self.ngrams_label.configure(text=t["tab3_ngrams_label"])
        self.preview_label.configure(text=t["tab3_preview_label"])
        self.stats_label.configure(text=t["tab3_stats_label"])
        self.lbl_orig.configure(text=t["tab3_table_orig"])
        self.lbl_rep.configure(text=t["tab3_table_rep"])
        self.lbl_hint.configure(text=t["tab3_table_hint"])
        self.btn_load_table.configure(text=t["tab3_load_table_btn"])
        self.highlight_duplicates()
        self.progress_label.configure(text=str(t["tab3_progress"]) + " 0%")
        self.stat_total.configure(text=str(t["tab3_total_chars"]) + " 0")
        self.stat_unique.configure(text=str(t["tab3_unique"]) + " 0")
        self.stat_mapped.configure(text=str(t["tab3_mapped"]) + " 0/0")
        if self.cipher_text_original:
            self.build_grid()

    def clear_all_tab(self):
        self.input_text.delete("1.0", "end")
        self.preview_text.configure(state="normal")
        self.preview_text.delete("1.0", "end")
        self.preview_text.configure(state="disabled")
        self.ngrams_text.configure(state="normal")
        self.ngrams_text.delete("1.0", "end")
        self.ngrams_text.configure(state="disabled")
        self.cipher_text_original = ""
        self.char_counts.clear()
        self.char_freqs.clear()
        self.sorted_cipher_chars.clear()
        self.crypto_entries.clear()
        self.row_frames.clear()
        for widget in self.entries_container.winfo_children():
            widget.destroy()
        t = self.app.translations[self.app.current_lang_str]
        self.stat_total.configure(text=t["tab3_total_chars"] + " 0")
        self.stat_unique.configure(text=t["tab3_unique"] + " 0")
        self.stat_mapped.configure(text=t["tab3_mapped"] + " 0/0")
        self.progress_bar.set(0)
        self.progress_label.configure(text=t["tab3_progress"] + " 0%")

    def load_text_file(self):
        file_path = filedialog.askopenfilename(filetypes=[("Text files", "*.txt"), ("All files", "*.*")])
        if file_path:
            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    text = f.read()
                self.input_text.delete("1.0", "end")
                self.input_text.insert("1.0", text)
                self.analyze_text()
            except Exception as e:
                t = self.app.translations[self.app.current_lang_str]
                messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))

    def analyze_text(self):
        raw = self.input_text.get("1.0", "end-1c")
        if not raw.strip(): return
        text = raw.upper()
        self.cipher_text_original = text
        alphabet = ALPHABET_UKR if self.app.current_lang_str == "Ukr" else ALPHABET_ENG
        pattern = re.compile("[" + re.escape(alphabet) + "]")
        filtered = pattern.findall(text)
        if not filtered: return
        total = len(filtered)
        counts = Counter(filtered)
        self.sorted_cipher_chars = [char for char, count in counts.most_common()]
        for char in alphabet:
            if char not in self.sorted_cipher_chars:
                self.sorted_cipher_chars.append(char)
        self.char_freqs = {char: counts.get(char, 0) / total * 100 for char in self.sorted_cipher_chars}
        self.char_counts = counts
        t = self.app.translations[self.app.current_lang_str]
        self.stat_total.configure(text=str(t["tab3_total_chars"]) + " " + str(total))
        self.stat_unique.configure(text=str(t["tab3_unique"]) + " " + str(len(counts)))
        self.build_grid()
        self.extract_ngrams(text, alphabet)
        self.refresh_preview()

    def extract_ngrams(self, text, alphabet):
        words = re.findall("[" + re.escape(alphabet) + "]+", text)
        bigrams = []
        trigrams = []
        for word in words:
            if len(word) >= 2:
                bigrams.extend([word[i:i + 2] for i in range(len(word) - 1)])
            if len(word) >= 3:
                trigrams.extend([word[i:i + 3] for i in range(len(word) - 2)])
        bi_counts = Counter(bigrams).most_common(8)
        tri_counts = Counter(trigrams).most_common(6)
        res_str = "Bigrams:  " + "".join([str(k) + "(" + str(v) + ")" for k, v in bi_counts]) + "\n"
        res_str += "Trigrams: " + "".join([str(k) + "(" + str(v) + ")" for k, v in tri_counts])
        self.ngrams_text.configure(state="normal")
        self.ngrams_text.delete("1.0", "end")
        self.ngrams_text.insert("1.0", res_str)
        self.ngrams_text.configure(state="disabled")

    def build_grid(self, preserve_values=False):
        saved = {ch: ent.get() for ch, ent in self.crypto_entries.items()} if preserve_values else {}
        for widget in self.entries_container.winfo_children(): widget.destroy()
        self.crypto_entries.clear()
        self.row_frames = {}
        ref_freq = FREQ_UKR if self.app.current_lang_str == "Ukr" else FREQ_ENG
        total_chars = sum(self.char_counts.values()) or 1
        sort_mode = getattr(self, "_sort_mode_val", "freq")
        if sort_mode == "alpha": display_chars = sorted(self.sorted_cipher_chars)
        else: display_chars = self.sorted_cipher_chars
        freq_rank = {ch: i + 1 for i, ch in enumerate(self.sorted_cipher_chars)}
        for i, char in enumerate(display_chars):
            freq = self.char_counts.get(char, 0) / total_chars * 100
            rank = freq_rank.get(char, i + 1)
            row_color = "#272727" if i % 2 == 0 else "#1f1f1f"
            row_frame = ctk.CTkFrame(self.entries_container, fg_color=row_color, corner_radius=3, height=30)
            row_frame.pack(fill="x", pady=1, padx=1)
            row_frame.pack_propagate(False)
            self.row_frames[char] = row_frame
            ctk.CTkLabel(row_frame, text=str(rank), width=22, font=("Arial", 9), text_color="#444444").pack(side="left", padx=(5, 0))
            ctk.CTkLabel(row_frame, text=char, width=30, font=("Arial", 13, "bold"), text_color="#E05050").pack(side="left", padx=2)
            freq_text = str(round(freq, 1)) + "%" if freq > 0 else "—"
            bar_pct = min(freq / 14.0, 1.0)
            bar_color = "#C84040" if freq > 8 else "#CC8800" if freq > 3 else "#444455"
            freq_outer = ctk.CTkFrame(row_frame, fg_color="transparent", width=52)
            freq_outer.pack(side="left", padx=2)
            freq_outer.pack_propagate(False)
            ctk.CTkLabel(freq_outer, text=freq_text, font=("Courier", 8), text_color="#777777").pack(anchor="e")
            bar_frame = ctk.CTkFrame(freq_outer, fg_color="#333333", height=3, corner_radius=1)
            bar_frame.pack(fill="x", padx=0)
            if bar_pct > 0:
                fill = ctk.CTkFrame(bar_frame, fg_color=bar_color, height=3, corner_radius=1, width=int(bar_pct * 46))
                fill.place(x=0, y=0)
            ent = ctk.CTkEntry(row_frame, width=38, height=24, font=("Arial", 12, "bold"), justify="center", text_color="#2CC985", fg_color="#1a2a1a", border_color="#2CC985", border_width=1, corner_radius=3)
            ent.pack(side="left", padx=5, pady=3)
            if char in saved and saved[char]: ent.insert(0, saved[char])
            ent.bind("<KeyRelease>", self.on_entry_change)
            ent.bind("<FocusOut>", self.on_entry_change)
            ent.bind("<Return>", lambda e, c=char: self.focus_next(c))
            ent.bind("<Tab>", lambda e, c=char: self.focus_next(c))
            self.crypto_entries[char] = ent
            hint_idx = self.sorted_cipher_chars.index(char) if char in self.sorted_cipher_chars else i
            hint_char = ref_freq[hint_idx] if hint_idx < len(ref_freq) else "?"
            ctk.CTkLabel(row_frame, text="(" + hint_char + ")", width=38, font=("Arial", 10), text_color="#555555").pack(side="left", padx=2)
        self.highlight_duplicates()
        self.update_stats()

    def focus_next(self, current_char):
        sort_mode = getattr(self, "_sort_mode_val", "freq")
        if sort_mode == "alpha": chars = sorted(self.sorted_cipher_chars)
        else: chars = self.sorted_cipher_chars
        try:
            idx = chars.index(current_char)
            next_char = chars[idx + 1] if idx + 1 < len(chars) else chars[0]
            self.crypto_entries[next_char].focus_set()
            self.scroll_to(next_char)
        except (ValueError, KeyError): pass
        return "break"

    def resort(self, val):
        self._sort_mode_val = "alpha" if val == "A-Z" else "freq"
        self.sort_mode.set(val)
        self.build_grid(preserve_values=True)

    def scroll_to(self, char):
        if char not in self.row_frames: return
        frame = self.row_frames[char]
        self.entries_container.update_idletasks()
        self.after(30, lambda: self.do_scroll_to(frame))

    def do_scroll_to(self, frame):
        try:
            container = self.entries_container
            scroll_widget = self.scrollable_frame
            container.update_idletasks()
            fy = frame.winfo_y()
            ch = container.winfo_height()
            if ch > 0: scroll_widget._parent_canvas.yview_moveto(fy / ch)
        except Exception: pass

    def jump_to_duplicate(self):
        counts = Counter()
        for ch, ent in self.crypto_entries.items():
            v = ent.get().upper()
            if v: counts[v] += 1
        dups = [ch for ch, ent in self.crypto_entries.items() if ent.get().upper() in counts and counts[ent.get().upper()] > 1]
        if dups:
            self.scroll_to(dups[0])
            for ch in dups:
                ent = self.crypto_entries[ch]
                ent.configure(fg_color="#3a1a1a")
                self.after(400, lambda e=ent: e.configure(fg_color="#1a2a1a"))
        t = self.app.translations[self.app.current_lang_str]
        self.btn_jump_dup.configure(fg_color="#8B3A3A" if dups else "#2a4a2a", text=t["tab3_dup_found"] if dups else t["tab3_dup_none"])

    def load_table_file(self):
        t = self.app.translations[self.app.current_lang_str]
        if not self.crypto_entries:
            messagebox.showwarning("Warning", t["tab3_warn_analyze"])
            return
        file_path = filedialog.askopenfilename(filetypes=[("CSV files", "*.csv"), ("Text files", "*.txt"), ("All files", "*.*")])
        if not file_path: return
        try:
            loaded = {}
            with open(file_path, "r", encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if not line or line.startswith("#"): continue
                    for sep in [":", ",", "->", "=", " "]:
                        if sep in line:
                            parts = line.split(sep, 1)
                            if len(parts) == 2:
                                cipher = parts[0].strip().upper()
                                plain = parts[1].strip().upper()
                                if len(cipher) == 1 and len(plain) == 1: loaded[cipher] = plain
                                break
            if not loaded: return
            applied = 0
            for cipher, plain in loaded.items():
                if cipher in self.crypto_entries:
                    ent = self.crypto_entries[cipher]
                    ent.delete(0, "end")
                    ent.insert(0, plain)
                    applied += 1
            self.highlight_duplicates()
            self.refresh_preview()
            self.update_stats()
        except Exception as e:
            messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))

    def on_entry_change(self, event=None):
        if event and hasattr(event, "widget"):
            ent = event.widget
            val = ent.get().upper()
            if len(val) > 1:
                ent.delete(0, "end")
                ent.insert(0, val[0])
            elif val and val != ent.get():
                ent.delete(0, "end")
                ent.insert(0, val)
        self.highlight_duplicates()
        self.update_stats()
        if self._preview_after_id: self.after_cancel(self._preview_after_id)
        self._preview_after_id = self.after(80, self.refresh_preview)

    def highlight_duplicates(self):
        counts = Counter()
        for char, ent in self.crypto_entries.items():
            val = ent.get().strip().upper()
            if val: counts[val] += 1
        has_dup = False
        for char, ent in self.crypto_entries.items():
            val = ent.get().strip().upper()
            if not val: ent.configure(border_color="#444444", text_color="#888888", fg_color="#1e1e1e")
            elif counts[val] > 1:
                ent.configure(border_color="#FF3333", text_color="#FF3333", fg_color="#2a1a1a")
                has_dup = True
            else: ent.configure(border_color="#2CC985", text_color="#2CC985", fg_color="#1a2a1a")
        if hasattr(self, "btn_jump_dup"):
            t = self.app.translations[self.app.current_lang_str]
            self.btn_jump_dup.configure(fg_color="#8B1A1A" if has_dup else "#2a3a2a", text=t["tab3_dup_found"] if has_dup else t["tab3_dup_none"])

    def refresh_preview(self):
        if not self.cipher_text_original: return
        text = self.cipher_text_original
        trans_dict = {}
        for cipher_char, ent in self.crypto_entries.items():
            plain_char = ent.get().strip().upper()
            if plain_char: trans_dict[cipher_char.upper()] = plain_char
        self.preview_text.configure(state="normal")
        self.preview_text.delete("1.0", "end")
        self.preview_text.tag_config("replaced", foreground="#2CC985")
        self.preview_text.tag_config("cipher", foreground="#888888")
        self.preview_text.tag_config("other", foreground="#cccccc")
        segments = []
        current_chunk = []
        current_tag = None
        for ch in text:
            ch_up = ch.upper()
            if ch_up in trans_dict:
                tag = "replaced"
                out_ch = trans_dict[ch_up]
            elif ch_up in self.sorted_cipher_chars:
                tag = "cipher"
                out_ch = ch
            else:
                tag = "other"
                out_ch = ch
            if tag == current_tag: current_chunk.append(out_ch)
            else:
                if current_chunk: segments.append(("".join(current_chunk), current_tag))
                current_chunk = [out_ch]
                current_tag = tag
        if current_chunk: segments.append(("".join(current_chunk), current_tag))
        for chunk_text, tag in segments: self.preview_text.insert("end", chunk_text, tag)
        self.preview_text.configure(state="disabled")

    def update_stats(self):
        total = len(self.sorted_cipher_chars)
        if total == 0: return
        present_chars = [c for c in self.sorted_cipher_chars if self.char_counts.get(c, 0) > 0]
        mapped = sum(1 for c in present_chars if self.crypto_entries.get(c, None) and self.crypto_entries[c].get())
        total_present = len(present_chars)
        progress = mapped / total_present if total_present > 0 else 0
        self.progress_bar.set(progress)
        t = self.app.translations[self.app.current_lang_str]
        self.progress_label.configure(text=str(t["tab3_progress"]) + " " + str(int(progress * 100)) + "%", text_color="#2CC985" if progress == 1 else "#FFA500" if progress > 0.5 else "#888888")
        self.stat_mapped.configure(text=str(t["tab3_mapped"]) + " " + str(mapped) + "/" + str(total_present), text_color="#2CC985" if mapped == total_present and total_present > 0 else "#FFA500")

    def auto_guess(self):
        if not self.sorted_cipher_chars:
            self.analyze_text()
            if not self.sorted_cipher_chars: return
        ref_freq = FREQ_UKR if self.app.current_lang_str == "Ukr" else FREQ_ENG
        present = [c for c in self.sorted_cipher_chars if self.char_counts.get(c, 0) > 0]
        for i, char in enumerate(present):
            if i < len(ref_freq):
                ent = self.crypto_entries.get(char)
                if ent:
                    ent.delete(0, "end")
                    ent.insert(0, ref_freq[i])
        self.highlight_duplicates()
        self.refresh_preview()
        self.update_stats()

    def reset_all(self):
        for ent in self.crypto_entries.values():
            ent.delete(0, "end")
            ent.configure(border_color="#2CC985", text_color="#2CC985")
        self.refresh_preview()
        self.update_stats()

    def copy_result(self):
        result = self.preview_text.get("1.0", "end-1c")
        if result: utils.copy_to_clipboard(self.app, result)

    def save_key(self):
        t = self.app.translations[self.app.current_lang_str]
        if not self.crypto_entries:
            messagebox.showwarning("Warning", t["tab3_warn_empty"])
            return
        key_data = {}
        for char, ent in self.crypto_entries.items():
            val = ent.get().upper()
            if val: key_data[char] = val
        if not key_data:
            messagebox.showwarning("Warning", t["error_len"])
            return
        file_path = filedialog.asksaveasfilename(defaultextension=".txt", filetypes=[("Text Files", "*.txt")])
        if file_path:
            try:
                with open(file_path, "w", encoding="utf-8") as f:
                    for cipher, plain in key_data.items(): f.write(str(cipher) + ":" + str(plain) + "\n")
                messagebox.showinfo("OK", t["success_save"])
            except Exception as e:
                messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))

    def load_key(self):
        t = self.app.translations[self.app.current_lang_str]
        if not self.crypto_entries:
            messagebox.showwarning("Warning", t["tab3_warn_analyze"])
            return
        file_path = filedialog.askopenfilename(filetypes=[("Text Files", "*.txt")])
        if file_path:
            try:
                with open(file_path, "r", encoding="utf-8") as f:
                    for line in f:
                        line = line.strip()
                        if ":" in line:
                            cipher, plain = line.split(":", 1)
                            cipher = cipher.strip().upper()
                            plain = plain.strip().upper()
                            if cipher in self.crypto_entries:
                                ent = self.crypto_entries[cipher]
                                ent.delete(0, "end")
                                ent.insert(0, plain[:1])
                self.highlight_duplicates()
                self.refresh_preview()
                self.update_stats()
                messagebox.showinfo("OK", t["success_load"])
            except Exception as e:
                messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))