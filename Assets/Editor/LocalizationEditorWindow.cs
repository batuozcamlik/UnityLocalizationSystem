#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;

public class LocalizationEditorWindow : EditorWindow
{
    private string relativeJsonPath = "localization.json";
    private LocalizationData data = new LocalizationData();

    private Vector2 scrollPosHeader;
    private Vector2 scrollPosGrid;
    private float cellWidth = 300f;
    private float rowHeight = 22f;
    private float handleWidth = 40f;

    private string newLanguageName = "";
    private string filterText = "";
    private int filterLanguageIndex = 0;

    private LocalizationManager detectedManager;
    private string fallbackDefaultName = "English";

    private bool isDirty = false;

    private static bool _isReopening = false;
    private static string _reopenJsonCache = null;

    private List<int> orderList = new List<int>();
    private ReorderableList reorderList;

    [MenuItem("Window/Localization/Editor")]
    public static void ShowWindow()
    {
        var win = GetWindow<LocalizationEditorWindow>();
        win.UpdateTitle();
        win.Show();
    }

    private void OnEnable()
    {
        DetectManagerAndPath();
        SafeLoad();
        EnsureReorderList();
        UpdateTitle();
    }

    private void DetectManagerAndPath()
    {
        detectedManager = FindObjectOfType<LocalizationManager>();
        if (detectedManager != null)
        {
            if (!string.IsNullOrEmpty(detectedManager.relativeJsonPath))
                relativeJsonPath = Path.GetFileName(detectedManager.relativeJsonPath);

            if (!string.IsNullOrWhiteSpace(detectedManager.defaultLanguageName))
                fallbackDefaultName = detectedManager.defaultLanguageName;
        }
    }

    private string FullPath
    {
        get
        {
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");

            if (!Directory.Exists(resourcesPath))
                Directory.CreateDirectory(resourcesPath);

            return Path.Combine(resourcesPath, relativeJsonPath).Replace("\\", "/");
        }
    }

    private void UpdateTitle()
    {
        titleContent = new GUIContent(isDirty ? "Localization Grid *" : "Localization Grid");
    }

    private void MarkDirty()
    {
        if (!isDirty)
        {
            isDirty = true;
            UpdateTitle();
            Repaint();
        }
    }

    private void SafeLoad()
    {
        try
        {
            string dir = Path.GetDirectoryName(FullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(FullPath))
            {
                var json = File.ReadAllText(FullPath);
                var loaded = JsonUtility.FromJson<LocalizationData>(json);
                data = loaded ?? new LocalizationData();

                if (data.languages.Count == 0)
                {
                    data.languages.Add(new LanguageInfo { name = fallbackDefaultName });
                    data.words.Add(new WordColumn());
                }

                NormalizeLengths();
                isDirty = false;
                UpdateTitle();
            }
            else
            {
                if (data.languages.Count == 0)
                {
                    data.languages.Add(new LanguageInfo { name = fallbackDefaultName });
                    data.words.Add(new WordColumn());
                }

                NormalizeLengths();
                Save();
            }

            if (filterLanguageIndex < 0 || filterLanguageIndex >= data.languages.Count)
                filterLanguageIndex = 0;

            EnsureReorderList();
        }
        catch
        {
            data = new LocalizationData();
            isDirty = false;
            UpdateTitle();
            EnsureReorderList();
        }
    }

    private void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(FullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FullPath, json);

            AssetDatabase.Refresh();

            isDirty = false;
            UpdateTitle();
            Repaint();
            Debug.Log($"Localization Saved to: {FullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Save Error: " + ex.Message);
        }
    }

    private void NormalizeLengths()
    {
        while (data.words.Count < data.languages.Count)
            data.words.Add(new WordColumn());

        int maxLen = 0;
        for (int l = 0; l < data.languages.Count; l++)
            maxLen = Mathf.Max(maxLen, data.words[l].items.Count);

        for (int l = 0; l < data.languages.Count; l++)
            while (data.words[l].items.Count < maxLen)
                data.words[l].items.Add("__MISSING__");

        if (orderList == null) orderList = new List<int>();

        ApplyFilterAndRebuildOrderList();
    }

    private void EnsureReorderList()
    {
        if (orderList == null) orderList = new List<int>();

        ApplyFilterAndRebuildOrderList();

        bool isDraggable = string.IsNullOrWhiteSpace(filterText);

        reorderList = new ReorderableList(orderList, typeof(int), isDraggable, true, false, false);

        reorderList.elementHeight = rowHeight;

        reorderList.drawHeaderCallback = (Rect rect) =>
        {
            float x = handleWidth;
            float internalPadding = 28f;
            float rightPadding = 16f;

            for (int lang = 0; lang < data.languages.Count; lang++)
            {
                float colX = rect.x + x + internalPadding;
                float colWidth = cellWidth - rightPadding - internalPadding;

                Rect colRect = new Rect(colX, rect.y, colWidth, rect.height);
                float btnWidth = 44f;
                Rect labelRect = new Rect(colRect.x, colRect.y, colRect.width - btnWidth - 5, colRect.height);
                Rect btnRect = new Rect(colRect.xMax - btnWidth, colRect.y + 1, btnWidth, colRect.height - 2);

                EditorGUI.LabelField(labelRect, $"[{lang}] {SafeLangName(lang)}");

                using (new EditorGUI.DisabledScope(lang == 0))
                {
                    if (GUI.Button(btnRect, "Del"))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Delete Language",
                            $"Are you sure you want to delete the language '{SafeLangName(lang)}'?",
                            "Yes, delete", "Cancel"))
                        {
                            int langToRemove = lang;
                            EditorApplication.delayCall += () =>
                            {
                                RemoveLanguageAt(langToRemove);
                                MarkDirty();
                                EnsureReorderList();
                                Repaint();
                            };
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                x += cellWidth;
            }
        };

        reorderList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index < 0 || index >= orderList.Count) return;

            int actualIdx = orderList[index];

            float x = handleWidth;
            float deleteBtnWidth = 66f;
            float padding = 4f;
            float internalPadding = 15f;
            float rightPadding = 30f;

            for (int lang = 0; lang < data.languages.Count; lang++)
            {
                string oldVal = "__MISSING__";
                if (data.words.Count > lang && data.words[lang].items != null && actualIdx >= 0 && actualIdx < data.words[lang].items.Count)
                    oldVal = data.words[lang].items[actualIdx];

                string newVal = oldVal;

                if (lang == 0)
                {
                    float colX = rect.x + x + internalPadding;
                    float textWidth = cellWidth - deleteBtnWidth - padding - rightPadding - internalPadding;
                    Rect textRect = new Rect(colX, rect.y + 2, textWidth, rect.height - 4);
                    Rect btnRect = new Rect(textRect.xMax + padding, rect.y + 2, deleteBtnWidth, rect.height - 4);

                    EditorGUI.BeginChangeCheck();
                    newVal = EditorGUI.TextField(textRect, oldVal);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.words[lang].items[actualIdx] = newVal;
                        MarkDirty();
                    }

                    if (GUI.Button(btnRect, "Delete"))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Delete Word",
                            $"Are you sure you want to delete this row (index {actualIdx}) from all languages?",
                            "Yes, delete", "Cancel"))
                        {
                            int indexToRemove = actualIdx;
                            EditorApplication.delayCall += () =>
                            {
                                RemoveWordAt(indexToRemove);
                                ApplyFilterAndRebuildOrderList();
                                EnsureReorderList();
                                MarkDirty();
                                Repaint();
                            };
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                else
                {
                    float colX = rect.x + x + internalPadding;
                    float colWidth = cellWidth - rightPadding - internalPadding;
                    Rect textRect = new Rect(colX, rect.y + 2, colWidth, rect.height - 4);

                    EditorGUI.BeginChangeCheck();
                    newVal = EditorGUI.TextField(textRect, oldVal);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.words[lang].items[actualIdx] = newVal;
                        MarkDirty();
                    }
                }

                x += cellWidth;
            }
        };

        reorderList.onAddCallback = (ReorderableList list) =>
        {
            int newIndex = data.words.Count > 0 ? data.words[0].items.Count : 0;
            for (int l = 0; l < data.languages.Count; l++)
            {
                if (data.words.Count <= l) data.words.Add(new WordColumn());
                data.words[l].items.Add(l == 0 ? "NewWord" : "__MISSING__");
            }

            ApplyFilterAndRebuildOrderList();
            EnsureReorderList();
            MarkDirty();
        };

        reorderList.onRemoveCallback = (ReorderableList list) => { };

        reorderList.onReorderCallback = (ReorderableList list) =>
        {
            int n = orderList.Count;
            if (n == 0) return;

            for (int l = 0; l < data.languages.Count; l++)
            {
                var oldCol = data.words[l].items;
                var newCol = new List<string>(n);
                for (int i = 0; i < orderList.Count; i++)
                {
                    int srcIdx = orderList[i];
                    if (srcIdx >= 0 && srcIdx < oldCol.Count)
                        newCol.Add(oldCol[srcIdx]);
                    else
                        newCol.Add("__MISSING__");
                }
                data.words[l].items = newCol;
            }

            ApplyFilterAndRebuildOrderList();
            EnsureReorderList();
            MarkDirty();
        };
    }

    private void ApplyFilterAndRebuildOrderList()
    {
        if (orderList == null)
            orderList = new List<int>();

        orderList.Clear();

        int totalWordCount = data.words.Count > 0 ? data.words[0].items.Count : 0;
        if (totalWordCount == 0)
        {
            if (reorderList != null)
                reorderList.list = orderList;
            return;
        }

        string filter = filterText.Trim();

        if (string.IsNullOrWhiteSpace(filter))
        {
            for (int i = 0; i < totalWordCount; i++)
                orderList.Add(i);
        }
        else
        {
            if (filterLanguageIndex < 0 || filterLanguageIndex >= data.words.Count)
                filterLanguageIndex = 0;

            var columnToFilter = data.words[filterLanguageIndex].items;
            string lowerFilter = filter.ToLowerInvariant();

            for (int i = 0; i < columnToFilter.Count; i++)
            {
                string item = columnToFilter[i] ?? "";
                if (item.ToLowerInvariant().Contains(lowerFilter))
                {
                    orderList.Add(i);
                }
            }
        }

        if (reorderList != null)
            reorderList.list = orderList;
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);
        DrawGrid();

        UpdateTitle();

        float padX = 10f;
        float padY = 8f;
        string leftText = "Created by Batu Özçamlık";
        string rightText = " |   www.batuozcamlik.com";
        GUIStyle leftStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle rightStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 13, fontStyle = FontStyle.Normal };
        Vector2 sizeLeft = leftStyle.CalcSize(new GUIContent(leftText));
        Vector2 sizeRight = rightStyle.CalcSize(new GUIContent(rightText));
        float totalW = sizeLeft.x + sizeRight.x;
        float lineH = Mathf.Max(sizeLeft.y, sizeRight.y);
        Rect area = new Rect(position.width - totalW - padX, position.height - lineH - padY, totalW, lineH);
        GUI.Label(new Rect(area.x, area.y, sizeLeft.x, lineH), leftText, leftStyle);
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.40f);
        GUI.Label(new Rect(area.x + sizeLeft.x, area.y, sizeRight.x, lineH), rightText, rightStyle);
        GUI.color = prev;
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField("JSON Name:", GUILayout.Width(75));
            string newPath = EditorGUILayout.TextField(relativeJsonPath, GUILayout.MinWidth(150));
            if (newPath != relativeJsonPath)
                relativeJsonPath = newPath;

            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(60)))
                SafeLoad();

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Save();

            GUILayout.FlexibleSpace();

            int clamped = Mathf.Clamp(
                data.selectedLanguageIndex,
                0,
                Mathf.Max(0, data.languages.Count - 1)
            );

            float originalLabelW = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120f;

            int newSel = EditorGUILayout.Popup(
                "Selected Language",
                clamped,
                GetLangNames(),
                GUILayout.Width(280)
            );

            EditorGUIUtility.labelWidth = originalLabelW;

            if (newSel != data.selectedLanguageIndex)
            {
                data.selectedLanguageIndex = newSel;
                Repaint();
            }

            if (isDirty)
            {
                GUILayout.Space(12);
                GUILayout.Label("⚠ Unsaved", EditorStyles.boldLabel, GUILayout.Width(120));
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(data.languages.Count == 0))
            {
                string current = (data.languages.Count > 0) ? (data.languages[0].name ?? "") : "";

                float defaultLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 210f;

                string edited = EditorGUILayout.TextField("Default Language Name (Index 0)", current, GUILayout.MinWidth(300), GUILayout.ExpandWidth(true));

                EditorGUIUtility.labelWidth = defaultLabelWidth;

                if (data.languages.Count > 0 && edited != current)
                {
                    data.languages[0].name = edited;
                    MarkDirty();
                    Repaint();
                }
            }
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            newLanguageName = EditorGUILayout.TextField("New Language Name", newLanguageName, GUILayout.MinWidth(300), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Add Language", GUILayout.Width(100)))
            {
                if (!string.IsNullOrWhiteSpace(newLanguageName))
                {
                    data.languages.Add(new LanguageInfo { name = newLanguageName.Trim() });
                    var newCol = new WordColumn();
                    int wordCount = data.words.Count > 0 ? data.words[0].items.Count : 0;

                    for (int i = 0; i < wordCount; i++)
                        newCol.items.Add("__MISSING__");

                    data.words.Add(newCol);
                    newLanguageName = "";
                    MarkDirty();

                    if (filterLanguageIndex < 0 || filterLanguageIndex >= data.languages.Count)
                        filterLanguageIndex = 0;

                    NormalizeLengths();
                    EnsureReorderList();
                    Repaint();
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(data.languages.Count == 0))
            {
                EditorGUI.BeginChangeCheck();

                float oldLW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 100f;

                filterLanguageIndex = EditorGUILayout.Popup(
                    new GUIContent("Filter Language"),
                    Mathf.Clamp(
                        filterLanguageIndex,
                        0,
                        Mathf.Max(0, data.languages.Count - 1)
                    ),
                    GetLangNames(),
                    GUILayout.Width(260)
                );

                EditorGUIUtility.labelWidth = oldLW;

                filterText = EditorGUILayout.TextField(
                    $"Filter ({SafeLangName(filterLanguageIndex)})",
                    filterText,
                    GUILayout.Width(280)
                );

                if (EditorGUI.EndChangeCheck())
                {
                    ApplyFilterAndRebuildOrderList();
                    EnsureReorderList();
                }
            }
        }
    }

    private string SafeLangName(int idx)
    {
        if (data.languages == null || data.languages.Count == 0) return "—";
        if (idx < 0 || idx >= data.languages.Count) idx = 0;
        return data.languages[idx].name;
    }

    private string[] GetLangNames()
    {
        var arr = new string[data.languages.Count];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = $"[{i}] {data.languages[i].name}";
        return arr;
    }

    private void DrawGrid()
    {
        Rect totalRect = GUILayoutUtility.GetRect(
            0, 0,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true)
        );

        if (reorderList == null)
            EnsureReorderList();

        float totalColumnsWidth = Mathf.Max((data.languages.Count * cellWidth) + handleWidth, totalRect.width);
        float totalContentHeight = (reorderList != null) ? reorderList.GetHeight() : totalRect.height;

        float scrollContentHeight = Mathf.Max(totalContentHeight, totalRect.height);

        scrollPosGrid = GUI.BeginScrollView(
            totalRect,
            scrollPosGrid,
            new Rect(0, 0, totalColumnsWidth, scrollContentHeight),
            true,
            true
        );

        GUILayout.BeginArea(new Rect(0, 0, totalColumnsWidth, totalContentHeight));

        try
        {
            if (reorderList != null)
                reorderList.DoLayoutList();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("ReorderableList draw error: " + ex.Message);
            EnsureReorderList();
        }

        GUILayout.EndArea();
        GUI.EndScrollView();

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add New Word"))
            {
                if (reorderList != null && reorderList.onAddCallback != null)
                {
                    reorderList.onAddCallback(reorderList);
                }
            }

            if (GUILayout.Button("Save"))
                Save();

            GUILayout.FlexibleSpace();
        }
    }

    private void RemoveLanguageAt(int langIndex)
    {
        if (langIndex <= 0 || langIndex >= data.languages.Count) return;

        data.languages.RemoveAt(langIndex);
        if (langIndex < data.words.Count)
            data.words.RemoveAt(langIndex);

        if (data.selectedLanguageIndex == langIndex)
            data.selectedLanguageIndex = 0;
        else if (langIndex < data.selectedLanguageIndex)
            data.selectedLanguageIndex = Mathf.Max(0, data.selectedLanguageIndex - 1);

        if (filterLanguageIndex >= data.languages.Count)
            filterLanguageIndex = Mathf.Clamp(filterLanguageIndex, 0, Mathf.Max(0, data.languages.Count - 1));

        NormalizeLengths();
        EnsureReorderList();
    }

    private void RemoveWordAt(int wordIndex)
    {
        if (wordIndex < 0) return;

        for (int l = 0; l < data.languages.Count; l++)
        {
            var col = data.words[l].items;
            if (wordIndex < col.Count)
                col.RemoveAt(wordIndex);
        }
        NormalizeLengths();
    }

    private void OnDestroy()
    {
        if (_isReopening || EditorApplication.isCompiling)
            return;

        if (!isDirty)
            return;

        string currentJson = JsonUtility.ToJson(data, true);

        int choice = EditorUtility.DisplayDialogComplex(
            "Unsaved Changes",
            "You are about to close without saving. What would you like to do?",
            "Save and Close",
            "Close (Don't Save)",
            "Cancel"
        );

        if (choice == 0)
        {
            Save();
        }
        else if (choice == 1)
        {
        }
        else
        {
            _isReopening = true;
            _reopenJsonCache = currentJson;

            EditorApplication.delayCall += () =>
            {
                var win = GetWindow<LocalizationEditorWindow>();
                if (!string.IsNullOrEmpty(_reopenJsonCache))
                {
                    try
                    {
                        var restored = JsonUtility.FromJson<LocalizationData>(_reopenJsonCache);
                        win.data = restored ?? new LocalizationData();
                        win.NormalizeLengths();
                        win.isDirty = true;
                        win.UpdateTitle();
                    }
                    catch { }
                }

                _reopenJsonCache = null;
                _isReopening = false;
                win.Show();
                win.Focus();
            };
        }
    }
}
#endif