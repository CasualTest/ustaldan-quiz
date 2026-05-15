# UstAldan Quiz — CLAUDE.md

Инструкции и соглашения проекта для Claude Code.

---

## Архитектура

### Вопросы
- Вопросы хранятся в `Assets/Resources/questions.json` (один файл, не .asset)
- Импорт через **UstAldan Quiz → Google Sheets → ↓ Импортировать всё** или из локального CSV
- `QuestionDatabase.EnsureRuntimeQuestionsLoaded()` загружает JSON в память при старте сцены
- Категории остаются как `.asset` файлы в `Assets/ScriptableObjects/Categories/` — нужны для иконок и отображения
- `allQuestions` в `QuestionDatabase.asset` всегда пустой на диске — заполняется только в рантайме

### Сцены
| Индекс | Сцена | Описание |
|--------|-------|----------|
| 0 | Intro | Заставка |
| 1 | MainMenu | Главное меню, выбор категории |
| 2 | QuestionMap | Карта вопросов (сетка плиток) |
| 3 | Results | Итоги сессии |
| 4 | Roadmap | Режим «Аркада» — змейка из вопросов |

Сцены пересобираются через **UstAldan Quiz → Game Setup → N — Build ...**.  
После изменения сцен-билдера пересобирать нужно вручную.

### Менеджеры (DontDestroyOnLoad)
- `GameManager` — синглтон, хранит текущую сессию (`SelectedCategory`, `SessionQuestions`)
- `SceneTransition` — fade-анимация между сценами, создаётся через `[RuntimeInitializeOnLoadMethod]`
- `AudioManager`, `LocaleManager`, `SettingsManager`, `SaveManager` — статические или синглтоны

---

## Паттерны UI

### BaseWindow
Все попапы и модальные окна наследуют `BaseWindow`:
- `Open()` — scale + fade анимация появления
- `Close()` — анимация закрытия, затем `panel.SetActive(false)`
- Поля: `panel`, `btnClose`, `sheetRect`, `sheetGroup`, `overlayGroup`
- Нельзя вызывать `Close()` на неактивном объекте — `BaseWindow.Close()` проверяет `panel.activeSelf`

### QuestionWindow : BaseWindow
Единое окно вопроса, используется в QuestionMap и Roadmap:
- Три зоны: `Zone_Question` (спрайт `popups_3`), `Zone_Media`, `Zone_Answers`
- `Zone_Bottom` — фиксированная зона 200px для `ResultFeedback` + `BtnContinue` (не вызывает сдвига при появлении)
- Ответы — кнопки с `popups_3` (9-slice, stretched), `ButtonSpringAnim` на каждой
- Событие `OnClosed` — сцена подписывается вместо прямого управления `SetActive`

### Попапы
- `FactPopup : BaseWindow` — показывает факт после неправильного ответа
- `NoQuestionsPopup` — попап если категория пустая

---

## Code Conventions

### Именование
- Классы, методы, свойства — `PascalCase`
- Приватные поля — `camelCase` с подчёркиванием: `_activeTile`, `_shuffledIndices`
- Сериализованные поля — `camelCase` без подчёркивания: `questionDatabase`, `tilePrefab`
- Константы — `PascalCase` или `UPPER_CASE` для `const`
- События — `OnSomething`: `OnTileClicked`, `OnClosed`

### Пространства имён
```
UstAldanQuiz.Data      — ScriptableObject данные (QuestionData, QuestionCategory, QuestionDatabase)
UstAldanQuiz.UI        — MonoBehaviour компоненты UI
UstAldanQuiz.Managers  — Синглтоны и менеджеры
UstAldanQuiz.Utils     — Утилиты (HapticManager, SafeArea и т.д.)
```
Editor-скрипты — без namespace, `partial class GameSceneBuilder`.

### Структура скриптов
Разделы внутри класса отделяются комментарием `// ── Название ─────`:
```csharp
// ── Unity lifecycle ────────────────────────────────────────────────
// ── Публичный API ─────────────────────────────────────────────────
// ── Helpers ───────────────────────────────────────────────────────
```

### Комментарии
- Комментарии только если логика неочевидна
- Нет docstring-блоков на каждый метод
- Нет комментариев объясняющих что делает код (имена методов делают это сами)

### Сериализация
- `[SerializeField] private` — для полей Inspector
- `[Header("...")]` — группировка полей в Inspector
- Публичные поля только если нужен доступ извне (например поля `QuestionWindow`)

---

## Сцен-билдер (Editor)

`GameSceneBuilder` — partial class, по одному файлу на сцену:
- `GameSceneBuilder.cs` — меню, константы цветов
- `GameSceneBuilder.Utils.cs` — вспомогательные методы (`MakeGO`, `MakeTMP`, `SetLE`, `Prop`, `LoadSprite`, `EnsureReadable` и др.)
- `GameSceneBuilder.MainMenu.cs`, `.QuestionMap.cs`, `.Results.cs`, `.Roadmap.cs`, `.Intro.cs`

Цвета проекта (светлая тема):
```
C_BG       #F5F0E8   фон
C_PRIMARY  #2D6040   основной зелёный
C_SECONDARY #C8A84B  акцент золотой
C_TEXT     #1A2A1A   текст
C_CORRECT  #4CAF50   правильный ответ
C_WRONG    #F44336   неправильный ответ
```

Спрайты кнопок: `Assets/Images/Sprites/buttons.png`, `additional controls.png`, `popups.png`  
Для `alphaHitTestMinimumThreshold` текстура должна быть readable — вызывай `EnsureReadable(atlasPath)`.

---

## Локализация

Языки: `ru` (русский), `sah` (якутский).  
Файлы: `Assets/Resources/Locales/ru.txt`, `sah.txt` — формат `key=value`.  
Импорт через Google Sheets (лист «Локализация»).  
`LocaleManager.Get("key")` — получить строку, `LocaleManager.CurrentLanguage` — текущий язык.

---

## Чего не делать

- Не добавлять `.asset` файлы вопросов — только JSON
- Не вызывать `DontDestroyOnLoad` на дочерних объектах
- Не запускать корутину на неактивном GameObject
- Не использовать `Directory.CreateDirectory` для папок в Assets — только `AssetDatabase.CreateFolder`
- Не коммитить `Library/`, `Temp/`, `obj/`, `Logs/`, `.claude/`
