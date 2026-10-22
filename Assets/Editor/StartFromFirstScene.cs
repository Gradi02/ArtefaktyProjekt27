using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ten skrypt edytora zapewnia, że gra zawsze startuje ze sceny 0 z Build Settings.
/// Zapisuje aktualnie otwartą scenę, przełącza na scenę 0 przed uruchomieniem gry,
/// a po zatrzymaniu trybu Play przywraca poprzednio otwartą scenę.
/// Działanie można włączyć/wyłączyć z menu Tools > Start From Scene 0.
/// </summary>
[InitializeOnLoad]
public static class StartFromFirstScene
{
    private const string MenuPath = "Tools/Start From Scene 0";
    private const string PreviousSceneKey = "StartFromFirstScene_PreviousScenePath";

    // Właściwość do łatwego sprawdzania i ustawiania, czy funkcja jest włączona
    private static bool IsEnabled
    {
        get { return EditorPrefs.GetBool(MenuPath, true); }
        set { EditorPrefs.SetBool(MenuPath, value); }
    }

    // Konstruktor statyczny, który uruchamia się raz przy starcie edytora
    static StartFromFirstScene()
    {
        // Subskrybujemy się do zdarzenia zmiany stanu trybu Play
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    // Metoda wywoływana przy kliknięciu opcji w menu
    [MenuItem(MenuPath)]
    private static void ToggleAction()
    {
        // Zmieniamy stan włączony/wyłączony
        IsEnabled = !IsEnabled;
        Debug.Log($"Funkcja 'Start From Scene 0' jest teraz {(IsEnabled ? "WŁĄCZONA" : "WYŁĄCZONA")}.");
    }

    // Metoda walidująca (sprawdzająca) stan opcji w menu (odpowiada za "ptaszek")
    [MenuItem(MenuPath, true)]
    private static bool ToggleActionValidate()
    {
        Menu.SetChecked(MenuPath, IsEnabled);
        return true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Jeśli funkcja jest wyłączona lub nie ma scen w Build Settings, nic nie robimy
        if (!IsEnabled || EditorBuildSettings.scenes.Length == 0)
        {
            return;
        }

        // Kiedy edytor ma ZARAZ wejść w tryb Play (ale jeszcze jest w trybie edycji)
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Zapisujemy zmiany w bieżącej scenie, jeśli użytkownik się zgodzi
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }

            // Zapisujemy ścieżkę do aktualnie otwartej sceny w preferencjach edytora
            EditorPrefs.SetString(PreviousSceneKey, EditorSceneManager.GetActiveScene().path);

            // Pobieramy ścieżkę do sceny o indeksie 0
            string scene0Path = EditorBuildSettings.scenes[0].path;

            // Sprawdzamy, czy ścieżka jest poprawna i otwieramy scenę 0
            if (!string.IsNullOrEmpty(scene0Path))
            {
                EditorSceneManager.OpenScene(scene0Path);
            }
        }
        // Kiedy edytor właśnie wrócił do trybu edycji
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Odczytujemy zapisaną ścieżkę do poprzedniej sceny
            string previousScenePath = EditorPrefs.GetString(PreviousSceneKey, null);

            // Jeśli ścieżka istnieje, otwieramy tę scenę
            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath);
            }

            // Czyścimy zapisaną ścieżkę, aby nie powodować problemów w przyszłości
            EditorPrefs.DeleteKey(PreviousSceneKey);
        }
    }
}