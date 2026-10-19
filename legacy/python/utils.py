from tkinter import filedialog, messagebox

def copy_to_clipboard(app, text):
    if text:
        app.clipboard_clear()
        app.clipboard_append(text)

def load_file(app, textbox):
    file_path = filedialog.askopenfilename(filetypes=[("Text Files", "*.txt"), ("All Files", "*.*")])
    if file_path:
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                textbox.delete("1.0", "end")
                textbox.insert("1.0", f.read())
        except Exception as e:
            t = app.translations[app.current_lang_str]
            messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))

def save_file(app, textbox):
    file_path = filedialog.asksaveasfilename(defaultextension=".txt", filetypes=[("Text Files", "*.txt")])
    if file_path:
        try:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(textbox.get("1.0", "end-1c"))
        except Exception as e:
            t = app.translations[app.current_lang_str]
            messagebox.showerror("Error", str(t["error_file"]) + " " + str(e))