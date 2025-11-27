# 🌍 Unity JSON Localization System

A lightweight, robust, and easy-to-use localization system for Unity. This tool allows you to manage multiple languages and translations through a custom **Editor Window** and access them efficiently at runtime using a JSON-based architecture.

## ✨ Features

* **Custom Editor Window:** Manage all your translations in a grid-based layout without leaving Unity.
* **JSON Based:** Human-readable data format stored in `Resources` for easy build inclusion.
* **Runtime & Editor Fallback:** Automatically handles data saving/loading between `PersistentDataPath` (Runtime) and `Resources` (Editor).
* **Reorderable Lists:** Drag and drop rows to organize your translation keys.
* **Filtering & Search:** Quickly find keys or languages using the built-in search bar.
* **Missing Key Handling:** Automatically fills missing entries with placeholders to ensure data integrity across languages.
* **Simple API:** Access translations with a single line of code.
* **TMPro Support:** Ready to use with TextMeshPro.

## 📦 Installation

1. Download the package and import it into your Unity project.
2. Ensure you have the `Resources` folder in your `Assets` directory (The system will create the JSON file here).
3. Add the `LocalizationManager` script to a GameObject in your first scene (e.g., a "GameManager" object).
4. *(Optional)* Add the `Test` script to check if everything is working correctly.

## 🛠️ Editor Usage

Go to **Window > Localization > Editor** to open the management window.

1. **Add Language:** Enter a name (e.g., "Spanish") in the "New Language Name" field and click **Add Language**.
2. **Add Words:** Click the **Add New Word** button at the bottom or use the `+` icon in the list.
3. **Edit Translations:**
   * The **Index 0** column is your "Key" (usually English).
   * Fill in the translations for other languages in the corresponding columns.
4. **Reorder:** Grab the handle on the left of any row to reorder words.
5. **Filter:** Use the filter dropdown and text box to search for specific words within a specific language.
6. **Save:** Click **Save** to write changes to `localization.json`.

> **Note:** The system automatically marks unsaved changes. Don't forget to save before closing the window!

## 💻 Scripting API

The system uses a Singleton pattern for easy access.

### 1. Get a Translation
To get a translated string based on the currently selected language:

```csharp
// "Hello" is the key (the word in the default language/Index 0)
string myText = LocalizationManager.Instance.Get("Hello");
Debug.Log(myText); 
```

### 2. Change Language
To switch the active language at runtime:

```csharp
// Set by Index (0 = Default/English, 1 = Turkish, etc.)
LocalizationManager.Instance.SetLanguage(1);

// Refresh your UI after changing language
myTextMeshPro.text = LocalizationManager.Instance.Get("Hello");
```

### 3. Check Initialization
It is recommended to check if the manager is loaded:

```csharp
if (LocalizationManager.Instance != null) {
    // Do localization logic
}
```

## 📂 File Structure

* **`LocalizationManager.cs`**: The core runtime engine. Handles loading/saving JSON, Singleton instance, and retrieval logic (`Get()`).
* **`LocalizationEditorWindow.cs`**: The custom Editor GUI logic. Handles the grid view, reordering, and file I/O operations in the editor.
* **`LocalizationTypes.cs`**: Serialized classes (`LocalizationData`, `LanguageInfo`) that define the data structure.
* **`Test.cs`**: A debug script to visualize all keys and values in the scene for testing purposes.

## 📝 License & Credits

This project was created by **Batu Özçamlık**.

* **Website:** [www.batuozcamlik.com](http://www.batuozcamlik.com)
* **License:** MIT License (Free to use in commercial and personal projects).
