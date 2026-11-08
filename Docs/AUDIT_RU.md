# Аудит пользовательских строк — BastionMarch

База: GitHub `master`, коммит `5fb672a8d01fec1aa2c9fd0c94b40d4dc20aac72`.
Область: 28 игровых файлов C# в Presentation, 140 игровых файлов C# в Simulation, три префаба, `BastionPrototype.unity`, а также `SampleScene.unity`, `ConstructorScene.unity` и список сцен сборки.

## Найдены пользовательские строки

| Источник | Содержание | Исправление |
| --- | --- | --- |
| Presentation/Prototype/PrototypeMovePlanningController.cs | Заголовки, кнопки, статус, выбор фазы и цели, текст ошибок, названия типов бригад через ToString | `UiTextKeys`, `BastionTexts`, локализованные enum; статус хранится ключом и кодом отказа |
| Presentation/Bastion/TurnPlanView.cs | Заголовок шкалы, названия фаз, пустые состояния и счётчик | `BastionTexts` |
| Presentation/Bastion/State/TurnPlanPresentationStateFactory.cs | Русское описание приказа, имя C#-типа как fallback | Структурированный immutable snapshot |
| Presentation/UI/ModuleDebugPanel.cs | 18 заголовков и подписей, значения enum, название модуля | `BastionTexts` + подписка на `SelectedLocaleChanged` |
| Presentation/Prototype/PrototypeBastionFactory.cs | Прототипное прозвище «Молот» | Рекомендуется убрать фиксированный RU-текст; прозвища, введённые игроком, не переводятся |
| Scenes/BastionPrototype.unity | TMP-плейсхолдер `Select a module` | Очистить поле в Inspector, runtime заполняет локализованная панель |

## Специально НЕ локализуем

- Тексты диагностики: `throw`, `Debug.Log`, сообщения внутренних исключений (остаются на английском).
- Строки API/ресурсов: шейдеры, пути, константы типов, имена и GUID.
- Unity `[Header]`, `[Tooltip]`: описания полей в Inspector предназначены разработчикам, не игрокам.
- `GameObject.name`, имена генерируемых объектов и префабов.
- `BrigadeView.TechnicalLabel` (`#7 Mechanic 5/6`): текущие обращения — разработческие тесты; игровой UI это свойство не выводит.
- Вводимые игроком имена и прозвища (`Brigade.Nickname`), как пользовательские данные.

## Ключи и импорт

- `UI.csv` содержит 44 обычных UI-ключа и 57 ключей `enum.<EnumType>.<Value>`.
- `Modules.csv` содержит все 7 ключей из `ModuleLocalizationKeys.cs`.
- В Unity импортировать **Merge**, не Replace, чтобы сохранить любые уже созданные локальные ключи.
- Для динамических строк с `{0}`, `{1}` поддерживаются Smart Strings; рекомендуется включить `Smart`.

## Проверка остаточных строк в ЛОКАЛЬНОМ рабочем дереве

GitHub не показывает незакоммиченные изменения. После применения посмотрите потенциальные оставшиеся пользовательские литералы:

```powershell
Get-ChildItem 'Assets/BastionMarch/Scripts/Presentation' -Recurse -Filter '*.cs' |
  Select-String -Pattern 'GUILayout\.(Label|Button)\s*\(\s*\$?"|_moduleInfoText\.text\s*=\s*\$?"|_status\s*=\s*\$?"|entry\.Description|nickname:\s*"'
```

Результат — эвристика, не C#-парсер; повторные результаты в диагностических/технических строках требуют ручной классификации.

После каждого нового UI-класса желательно добавлять проверку, что его видимые строки берутся из таблиц.
