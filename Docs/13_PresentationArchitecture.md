# TDD-13. Базовая архитектура Presentation

## 1. Назначение

Presentation не должен использовать изменяемые объекты
Simulation непосредственно внутри View.

Основной поток:

Simulation
→ Presentation State
→ Presenter
→ View

## 2. BastionPresenter

BastionPresenter хранит ссылку на исходный Bastion.

При обновлении он:

1. создаёт BastionPresentationState;
2. сохраняет его как CurrentState;
3. передаёт снимок в BastionView;
4. публикует StatePresented.

Presenter не выполняет игровых расчётов.

## 3. BastionView

BastionView хранит только BastionPresentationState.

При получении нового снимка он:

- обновляет размеры сетки;
- сохраняет существующие ModuleView;
- передаёт им новые снимки;
- создаёт View для новых модулей;
- удаляет View отсутствующих модулей.

## 4. ModuleView

ModuleView хранит ModulePresentationState.

Он не имеет прямой ссылки на ModuleInstance.

Изменение Simulation не проявляется во View,
пока Presenter явно не создаст и не передаст новый снимок.

## 5. Выбор

ModuleSelectionController публикует
ModulePresentationState.

Если выбранный модуль существует в новом снимке,
выбор сохраняется и диагностический UI обновляется.

Если модуль удалён, выбор очищается.

## 6. Диагностический UI

ModuleDebugPanel получает неизменяемый снимок.

Панель не читает ModuleInstance и не изменяет Simulation.

## 7. Прототипная сцена

BastionPrototypeBootstrap создаёт тестовую модель.

Затем BastionPresenter выполняет её первичное отображение.

Bootstrap не передаёт Bastion напрямую во View.

## 8. Обновление

На текущем этапе обновление выполняется явным вызовом:

BastionPresenter.RefreshPresentation()

Автоматические события Simulation пока не добавляются.

Явное обновление упрощает детерминированное
разрешение будущих фаз хода.

## 9. Следующие подэтапы

- PassagePresentationState и PassageView;
- BrigadePresentationState и BrigadeView;
- RoutePresentationState и RouteView;
- TurnPlanView;
- CombatEffectPresenter.

## 10. Снимки переходов

PassagePresentationState содержит:

- PassageId;
- SourceModuleId;
- TargetModuleId;
- GridBoundarySegment;
- ModulePassageType;
- ModulePassageTraversalMode;
- ModulePassageState.

Снимок перехода неизменяем.

Изменение ModulePassage после захвата не изменяет
уже переданный объект Presentation.

BastionPresentationState теперь содержит одновременно:

- Modules;
- Passages.

Переходы упорядочиваются детерминированно
по координатам общей границы, затем по PassageId.