# TDD-15. Планирование хода

## 1. Назначение

Документ фиксирует архитектуру Stage 13: редактируемый план хода, reservations, Orders, Activity contract и подтверждение плана.

Главная граница:

```text
TurnCycle = время
TurnPlanDraft = намерения
ConfirmedTurnPlan = подтверждённый неизменяемый план
```

`TurnCycle` не владеет планом.

---

## 2. Основные цели Stage 13

Stage 13 должен позволить:

- создать draft для конкретного TurnNumber;
- использовать любое положительное `ActionPhaseCount`;
- привязать план к participant snapshot текущего хода;
- добавлять immutable Orders;
- резервировать brigade Action Phase;
- запретить конфликтующие reservations;
- представить Brigade-scoped и Bastion-scoped Orders;
- представить многофазный Order;
- представить carry-over commitment следующего хода;
- диагностировать невалидный draft;
- получить immutable `ConfirmedTurnPlan`;
- только после успешной проверки перевести `TurnCycle` из Planning.

Stage 13 НЕ исполняет игровые Orders и не мутирует Bastion.

---

## 3. TurnPlanDraft

`TurnPlanDraft` является отдельным редактируемым агрегатом.

Минимальное состояние:

- `TurnNumber`;
- `ActionPhaseCount`;
- immutable/read-only participant snapshot;
- Orders;
- PhaseReservations.

Он не содержит ссылку на:

- `TurnCycle`;
- `Bastion`;
- Unity Presentation.

Draft относится только к одному ходу.

### 3.1. Participant snapshot

План использует тот же состав участников, что и `TurnCycle`.

Минимальная identity участника остаётся:

- `BrigadeId`;
- `BrigadeNumber`.

Не добавлять в `TurnBrigadeParticipant`:

- CurrentModuleId;
- IsWorking;
- CurrentPersonnel;
- Activity;
- изменяемые tactical state.

Изменяемые ожидания конкретного Order должны храниться в самом Order или его execution preconditions.

---

## 4. PhaseReservation

`PhaseReservation` — единый источник истины о занятости бригады blocking Order в конкретной Action Phase.

Для Stage 13 минимальная identity:

- `OrderId`;
- `ActionPhase`;
- `BrigadeId`.

Reservation является value-like immutable объектом.

### 4.1. Конфликт

Нельзя иметь две несовместимые reservations:

```text
same BrigadeId
+
same ActionPhase
```

для разных blocking Orders.

Не создавать второй mutable источник истины в виде:

```text
BrigadePlan.Slot1
BrigadePlan.Slot2
...
```

UI-представление временной шкалы строится как проекция reservations.

### 4.2. Bastion-scoped Order

Один Bastion-scoped Order может иметь один `OrderId`, но несколько reservations:

```text
Order A

Phase 2:
Brigade 1 → A
Brigade 2 → A
Brigade 3 → A
```

Resolver позднее видит универсальную занятость бригад и не требует специального `if order is global` для каждого участника.

---

## 5. Immutable Order

Order описывает намерение, а не mutable progress.

Минимальный общий контракт:

- `OrderId`;
- `RequiredPhases`.

Общий контракт НЕ содержит обязательный:

- `ExecutorBrigadeId`;
- `TargetId`.

Причина:

- Brigade-scoped Order имеет конкретную Brigade;
- Bastion-scoped Order может затрагивать много бригад;
- будущие Module/Weapon/System Orders имеют другую identity.

Избегать имени `Action`.

Предпочтительное базовое имя:

```text
ITurnOrder
```

Конкретные типы:

```text
MoveBrigadeOrder
BraceForImpactOrder
...
```

появляются только при реальном игровом применении.

---

## 6. Order scope

На Stage 13 требуются:

- Brigade-scoped;
- Bastion-scoped.

Scope не обязан быть единым универсальным enum.

Допустимо, чтобы concrete Order сам выражал свою область через нужные поля.

Не вводить заранее:

- Module-scoped;
- Weapon-scoped;
- System-scoped;

пока нет реальных Orders такого типа.

---

## 7. Activity contract

Activity — постоянная деятельность бригады, а не Order.

Базовое правило resolver:

```text
blocking reservation есть
→ Order tick

blocking reservation отсутствует
→ Activity tick
```

Stage 13 фиксирует этот контракт, но НЕ создаёт новый persistent storage текущих Activities.

### 7.1. Existing Working state

В текущей Simulation работа уже представлена:

- `Bastion._workingBrigadeIds`;
- `ModuleInstance.WorkingBrigadeIds`;
- `BrigadeOperationalState.IsWorking`;
- `TryStartBrigadeWork`;
- `TryStopBrigadeWork`.

Это существующая operational-механика, а не универсальная Activity system.

Первая `ModuleWorkActivity` позднее может быть тонким слоем над этим состоянием.

Не создавать на Stage 13 третий mutable registry вроде:

```text
_activityByBrigadeId
```

только ради будущего расширения.

---

## 8. Multi-phase Orders

Order может иметь:

```text
RequiredPhases > ActionPhaseCount
```

Например:

```text
RequiredPhases = 5

Turn 10:
P1 → progress 1/5
P2 → progress 2/5

Turn 11:
P1 → progress 3/5
P2 → progress 4/5

Turn 12:
P1 → progress 5/5
```

Immutable Order не хранит `CompletedPhases`.

Фактический mutable/replaceable progress относится к execution layer Stage 14.

---

## 9. Carry-over commitments

Новый ход может начинаться с уже занятыми фазами из ранее начатого Order.

`TurnPlanDraft` должен уметь получить carry-over commitment и превратить его в начальные reservations.

Draft НЕ должен сохранять предыдущий `TurnPlan` целиком.

Минимальный смысл carry-over:

- `OrderId`;
- оставшаяся длительность или требуемые phase ticks;
- affected BrigadeIds;
- правило interruptibility, когда оно появится.

Точный runtime-тип carry-over допускается определить в Stage 14 вместе с `OrderExecutionState`.

На Stage 13 достаточно не блокировать такую модель архитектурно.

---

## 10. Planning assessment

Проверка draft выполняется read-only.

Предпочтительный паттерн проекта:

```text
TurnPlanAssessment
TurnPlanFailureReason
```

или более конкретные assessment-result types по подоперациям.

Минимальные проверки:

- draft относится к текущему TurnNumber;
- `ActionPhaseCount` совпадает с TurnCycle;
- participant snapshot совместим;
- `ActionPhase` находится в диапазоне;
- BrigadeId присутствует в participant snapshot;
- `RequiredPhases >= 1`;
- OrderId уникален;
- reservation ссылается на существующий Order;
- duplicate/conflicting reservation запрещена;
- reservations конкретного Order согласованы с его длительностью/областью.

Expected business failures возвращаются как result/assessment, не exception.

Exception остаётся для нарушения программного контракта.

---

## 11. ConfirmedTurnPlan

`ConfirmedTurnPlan` — неизменяемый snapshot валидного draft.

Он содержит:

- TurnNumber;
- ActionPhaseCount;
- participant snapshot;
- immutable Orders;
- immutable reservations.

Он не хранит ссылку на `TurnPlanDraft`.

После создания:

```text
изменение draft
≠ изменение ConfirmedTurnPlan
```

Confirmed plan используется будущим resolver.

---

## 12. TurnPlanningCoordinator

Presentation/Application не должен напрямую строить такой workflow:

```text
TurnCycle.TryConfirmPlanning()
→ validate draft
```

Правильный порядок:

```text
validate draft
→ freeze ConfirmedTurnPlan
→ TurnCycle.TryConfirmPlanning()
→ вернуть confirmed plan
```

Для этого вводится orchestration-компонент уровня:

```text
TurnPlanningCoordinator
```

Он не должен превращать `TurnCycle` в владельца плана.

### 12.1. Atomicity

До вызова `TryConfirmPlanning()` никакая world mutation не выполняется.

Если validation неуспешен:

- TurnCycle остаётся Planning;
- ConfirmedTurnPlan не возвращается.

Если `TryConfirmPlanning()` неуспешен:

- ConfirmedTurnPlan наружу не публикуется;
- draft остаётся доступен для диагностики.

---

## 13. Рекомендованные имена

Предварительно подтверждены:

```text
TurnPlanDraft
ConfirmedTurnPlan
PhaseReservation
ITurnOrder
IBrigadeActivity
TurnPlanAssessment
TurnPlanFailureReason
TurnPlanningCoordinator
OrderExecutionState       # Stage 14
PhaseResolutionResult     # Stage 14
```

Не использовать:

```text
Action
Command
```

как базовые имена приказа.

`Command` уже имеет доменную семантику в существующих enum/value names.

---

## 14. Предполагаемая структура файлов

Без нового asmdef:

```text
Assets/BastionMarch/Scripts/Simulation/Turns/
├── Planning/
│   ├── TurnPlanDraft.cs
│   ├── ConfirmedTurnPlan.cs
│   ├── PhaseReservation.cs
│   ├── TurnPlanAssessment.cs
│   ├── TurnPlanFailureReason.cs
│   └── TurnPlanningCoordinator.cs
├── Orders/
│   └── ITurnOrder.cs
└── Activities/
    └── IBrigadeActivity.cs
```

Точная разбивка может уточняться по мере реализации.

---

## 15. Test strategy Stage 13

### TurnPlanDraftTests

Pure unit.

Проверяют:

- TurnNumber;
- ActionPhaseCount;
- копирование participant snapshot;
- редактирование Orders;
- отсутствие зависимости от Bastion/TurnCycle.

### PhaseReservationTests

Pure unit.

Проверяют:

- диапазон phase;
- duplicate reservation;
- конфликт BrigadeId + phase;
- один Order на нескольких brigades.

### TurnOrderTests

Pure unit.

Проверяют:

- OrderId;
- RequiredPhases;
- immutable contract;
- отсутствие обязательного executor/target в базовом интерфейсе.

### BrigadeActivityTests

Pure unit.

Проверяют только контракт Activity.

Не тестируют ещё ModuleWorkActivity.

### TurnPlanValidationTests

Pure unit + небольшие integration cases.

Проверяют:

- несовпадение TurnNumber;
- participant mismatch;
- invalid reservation;
- orphan Order/reservation;
- diagnostics.

### ConfirmedTurnPlanTests

Pure unit.

Проверяют:

- deep-enough snapshot;
- неизменяемость;
- независимость от последующей правки draft.

### TurnPlanningCoordinatorTests

Integration TurnCycle + planning model.

Проверяют:

- invalid draft не подтверждает cycle;
- valid draft создаёт ConfirmedTurnPlan;
- затем cycle переходит к ActionResolution phase 1;
- повторное подтверждение корректно диагностируется.

---

## 16. Non-goals Stage 13

Не реализовывать:

- MoveBrigadeOrder execution;
- actual ModuleWorkActivity;
- OrderExecutionState mutation;
- combat resolver;
- initiative;
- world mutation batch;
- StatusEffect;
- weapon actions;
- AI;
- Unity UI.

---

## 17. Критерий завершения Stage 13

Stage 13 завершён, когда:

1. draft хода можно создать для текущего TurnCycle;
2. в него можно добавить Orders и reservations;
3. конфликт занятости бригады диагностируется;
4. Bastion-scoped Order может занять несколько бригад;
5. многофазная длительность не ограничена одним ходом;
6. Activity contract существует отдельно от Order;
7. валидный draft превращается в immutable ConfirmedTurnPlan;
8. coordinator подтверждает TurnCycle только после успешной validation;
9. все новые контракты остаются pure C# без UnityEngine.
