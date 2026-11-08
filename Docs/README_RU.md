# BastionMarch — миграция пользовательских строк в RU/EN

## Основа

Аудит составлен по GitHub `master` на коммите `5fb672a8d01fec1aa2c9fd0c94b40d4dc20aac72`.
Исходники в каталоге `Patch/` — предлагаемые замены только для файлов, которые уже существуют в проекте, **кроме** новых `Localization`-классов и нового PlayMode-теста. Локальные незакоммиченные изменения в GitHub не видны: **сверяйте диффы перед заменой**.

## Содержимое

- `UI.csv`: 101 строка, включая 44 ключа обычных пользовательских надписей и 57 названий enum.
- `Modules.csv`: 7 существующих имён модулей, ключи совпадают с `ModuleLocalizationKeys.cs`.
- `Patch/Assets/BastionMarch/Scripts/Presentation/Localization/BastionTexts.cs`: единая точка доступа к Unity Localization и локализация enum.
- `Patch/Assets/BastionMarch/Scripts/Presentation/Localization/UiTextKeys.cs`: константы строк интерфейса.
- `Patch/Assets/BastionMarch/Scripts/Presentation/Prototype/PrototypeMovePlanningController.cs`: все пользовательские строки через `BastionTexts`, реакция на смену языка без сохранения готовой строки.
- `Patch/Assets/BastionMarch/Scripts/Presentation/Bastion/TurnPlanView.cs`: локализованная шкала фаз.
- `Patch/Assets/BastionMarch/Scripts/Presentation/Bastion/State/TurnPlanPresentationState.cs` и `TurnPlanPresentationStateFactory.cs`: структурированные данные вместо русской `Description`.
- `Patch/Assets/BastionMarch/Scripts/Presentation/UI/ModuleDebugPanel.cs`: локализованы видимые подписи, названия модулей, типы, статусы и режимы питания; смена языка обновляет TextMeshPro панель.
- `Patch/Assets/BastionMarch/Tests/PlayMode/TurnPlanPresentationStateTests.cs`: обновлён тест (больше не ожидает `Description`).
- `Patch/Assets/BastionMarch/Tests/PlayMode/BastionLocalizationCatalogTests.cs`: новые проверки RU/EN и полноты ключей для перечислений.

## Порядок применения

1. Убедитесь, что установлен `com.unity.localization` и в `BastionMarch.Presentation.asmdef` есть `Unity.Localization` и `Unity.ResourceManager`.
2. В Unity создайте `Localization Settings` и две локали: Russian (ru), English (en). Выберите English как fallback язык.
3. Создайте пустые **String Table Collections** с именами `UI` и `Modules`.
4. Откройте коллекцию `UI` в Localization Tables и импортируйте `UI.csv` режимом **CSV (Merge)**. Аналогично импортируйте `Modules.csv` в `Modules`. Нельзя использовать Replace на таблице, в которой уже есть другие ключи — он удалит отсутствующие в CSV.
5. Для строк с `{0}`, `{1}`, ... включите флаг **Smart** в таблицах. (Для простых позиционных аргументов Unity умеет и String.Format, но Smart потребуется позже для плюрализации.)
6. Скопируйте/сопоставьте изменения из `Patch/` в соответствующие пути проекта. Старые `.meta` не удаляйте; для новых файлов Unity создаст `.meta` самостоятельно.
7. В `Assets/BastionMarch/Scripts/Presentation/Prototype/PrototypeBastionFactory.cs`: уберите жёстко заданное русское прозвище: `nickname: "Молот"` → `nickname: string.Empty`. Прозвища, которые **ввел игрок**, оставляйте пользовательскими данными, не переводите автоматически.
8. В Unity откройте `BastionPrototype.unity`, найдите `ModuleInfoText` (TMP) в Hierarchy, очистите текстовое поле `Select a module` в Inspector, сохраните сцену. В runtime это поле заполняется `ModuleDebugPanel` через локализацию.
9. Запустите EditMode и PlayMode. В базовом GitHub 609 EditMode/39 PlayMode. Добавлены 2 PlayMode-теста из этого пакета; если прежние 2 теста локализации уже добавлены локально, итоговое количество будет ещё на 2 больше.
10. В `Docs/00_DevelopmentRoadmap.md` отметьте P3.3 выполненным, добавьте отдельный подэтап локализации и зафиксируйте принцип: все новые отображаемые строки через таблицы.

## Что оставлено как техническое

- `throw new ...`, логи, `Debug.*`, имена `GameObject`, Unity `[Header]`, `[Tooltip]`, имена шейдеров, служебные ключи/ID.
- `BrigadeView.TechnicalLabel`: используется разработческими тестами; не является частью текущего интерфейса игрока (проверено через поиск использований).
- Неизменяемые имена типа `MoveBrigadeOrder` и числовые значения не отображаются игроку после миграции.
- Сценки `SampleScene` и `ConstructorScene` не содержат собственных пользовательских TMP-строк.

## Контроль изменений

```powershell
git status --short
git diff --check
git diff --stat
```

В архив не включены изменения непосредственно в GitHub или локальном репозитории пользователя. Всё применение выполняется вручную.
