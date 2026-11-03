# TDD-16. Разрешение Action Phase

## 1. Назначение

Документ фиксирует архитектурные границы Stage 14.

Stage 14 превращает `ConfirmedTurnPlan` в фактические изменения Simulation по одной Action Phase.

Ключевой принцип:

```text
детерминированный порядок
≠ игровая инициатива
```

---

## 2. Семантика Action Phase

Action Phase — один дискретный момент игрового времени.

Бригады и системные процессы концептуально относятся к одной фазе.

Нельзя случайно получить инициативу только потому, что код прошёл коллекцию:

```text
Brigade 1
→ mutate world
Brigade 2
→ увидела уже изменённый мир
```

Если последовательность должна влиять на результат, она должна быть явным игровым правилом.

---

## 3. Базовый pipeline

Первая подтверждённая модель:

```text
1. Capture phase-start context.
2. Determine scheduled Order ticks.
3. Determine Activity fallback intents for Brigades
   without blocking Order reservations.
4. Assess all intents against the same phase-start context.
5. Resolve explicit conflicts.
6. Commit allowed mutations.
7. Advance persistent world processes.
8. Build immutable PhaseResolutionResult.
9. Advance TurnCycle only after successful
   phase resolution pipeline.
```

Конкретная реализация может объединять технические шаги, но не должна терять семантическую границу assessment и mutation.

---

## 4. Phase context

Resolver получает состояние начала Action Phase.

Контекст нужен, чтобы несколько intents оценивались относительно согласованной исходной точки.

На M1 допустим минимальный context без универсального snapshot всего Bastion.

Не копировать весь мир без необходимости.

---

## 5. Intents

Intent описывает попытку сделать изменение в текущей Action Phase.

Источники:

- Order tick;
- Activity tick;
- future persistent world process.

Intent не должен сам мутировать мир во время assessment.

Для первых M1 действий допустимы конкретные специализированные intent types.

Не строить универсальный ECS/command bus заранее.

---

## 6. Assessment

Перед commit повторно проверяются текущие условия.

Примеры:

- Brigade существует;
- Brigade не расформирована;
- Brigade находится в ожидаемом module;
- Passage существует;
- traversal разрешён;
- target существует;
- prerequisites Order ещё выполнены.

Planning validation не заменяет runtime revalidation.

Read-only проверки используют существующий стиль `...Assessment`.

---

## 7. Conflict resolution

Conflicts разрешаются явным правилом.

На M1 planning reservations уже устраняют:

```text
одна Brigade
→ два blocking Orders
→ одна Action Phase
```

Будущие конфликты:

- встречное движение;
- ограниченная вместимость;
- несколько сторон используют один объект;
- уничтожение участника в той же фазе;
- конкурирующие weapon/system operations.

Не вводить инициативу до отдельного игрового решения.

---

## 8. Commit

Только после assessment/conflict resolution разрешённые mutations применяются к Simulation.

Для M1 commit может вызывать конкретные атомарные Bastion operations.

Не вводить универсальный `CommandBuffer`, пока первый реальный набор конфликтов не покажет необходимость.

---

## 9. OrderExecutionState

Один `OrderId` соответствует одному execution process.

Order отдаётся один раз. Последующие Action Phase изменяют
`OrderExecutionState`, а не создают новые Orders.

Один уникальный Order в одной Action Phase получает не более
одного execution tick независимо от числа PhaseReservation
этого Order в данной фазе.

Execution process может переживать TurnEnd.

После завершения Order созданные им длительные объекты
и последствия могут продолжать существовать как persistent
world processes.

Например, артиллерийский Order может завершиться фактическим
выстрелом, после чего созданный снаряд продолжает полёт
независимо от завершённого OrderExecutionState.

`OrderExecutionState` отвечает за фактический progress:

- `OrderId`;
- completed phase ticks;
- remaining phase ticks;
- runtime status.

Execution state может переживать границу хода.

Предварительные outcomes:

- `Progressed`;
- `Completed`;
- `Failed`.

`Cancelled` добавляется только при подтверждённой runtime-семантике отмены.

---

## 10. Carry-over

Если Order не завершён к TurnEnd, execution state переживает границу хода.

На новом Planning из него формируются carry-over commitments/reservations.

Старый `ConfirmedTurnPlan` не становится владельцем ongoing progress.

---

## 11. Activity fallback

Для Brigade:

```text
blocking Order reservation есть
→ Order tick

blocking Order reservation отсутствует
→ Activity tick
```

Activity не требует reservation.

На M1 первой Activity станет `ModuleWorkActivity`, основанная на существующем operational work state.

---

## 12. Persistent world processes

После основных brigade/system intents могут обновляться процессы, которые существуют между Action Phase.

Примеры будущих систем:

- projectile in flight;
- missile;
- drone;
- fire;
- smoke;
- cooldown;
- other persistent threat.

Порядок между основными intents и persistent processes должен быть явным правилом конкретной системы.

---

## 13. PhaseResolutionResult

Каждая Action Phase должна вернуть immutable результат.

Он пригоден для:

- Presentation;
- replay;
- diagnostics;
- save/load;
- tests.

Result не обязан сразу быть единым универсальным event log всех будущих систем.

Начать с минимальных данных M1 и расширять по фактическим потребностям.

---

## 14. Детерминированность

Для одинаковых:

- Simulation state;
- ConfirmedTurnPlan;
- random seed/state, когда появится случайность;

resolver должен давать одинаковый результат.

Стабильная сортировка используется только для воспроизводимости.

---

## 15. Non-goals Stage 14

Не реализовывать заранее:

- боевую инициативу;
- универсальный event bus;
- универсальный command buffer;
- сетевую синхронизацию;
- weapon combat;
- full StatusEffect engine.

---

## 16. Критерий завершения Stage 14

Stage 14 завершён, когда:

1. ConfirmedTurnPlan можно разрешить по Action Phase;
2. Order и Activity не мутируют мир во время assessment;
3. runtime revalidation существует;
4. конфликтующие intents имеют явную точку разрешения;
5. commit отделён от assessment;
6. многофазный Order имеет execution progress;
7. незавершённый progress может перейти в следующий ход;
8. свободная Brigade phase приводит к Activity tick;
9. формируется детерминированный PhaseResolutionResult.

## Реализация 14.6. Activity fallback

Добавлены:

- BrigadeActivityPhaseIntent;
- BrigadeActivityPhaseIntentFactory.

Factory определяет свободные Action Phase участников
по ConfirmedTurnPlan.PhaseReservations.

Для каждой свободной Brigade её текущая Activity
запрашивается через внешний read-only resolver.

Если Activity отсутствует, специальный intent
не создаётся. Brigade остаётся в standby semantics.

Бригада с blocking reservation не получает
Activity intent в занятой фазе, даже если
Order позднее получит failed assessment.

Activity определяется заново в начале каждой
Action Phase, до mutation игрового мира.

Фабрика не владеет Activity state, не выполняет
Activity tick и не изменяет Bastion.

Реальное исполнение ModuleWorkActivity
остаётся задачей Stage 15.

## Реализация 14.7. Persistent world extension point

Добавлены:

- PersistentWorldPhaseContext;
- PersistentWorldPhaseResult;
- IPersistentWorldPhaseProcessor;
- NoOpPersistentWorldPhaseProcessor.

Persistent world processor не зависит от OrderId,
OrderExecutionState и TurnPlanDraft.

Процессы игрового мира могут продолжать существовать
после завершения породившего их Order.

Processor получает временную identity Action Phase
и возвращает immutable минимальный отчёт
об обработанных ProcessId.

На этапе 14.7 не реализованы:

- storage и lifecycle конкретных процессов;
- реальные снаряды, ракеты, пожары и другие угрозы;
- автоматическое продвижение TurnCycle;
- повторная защита от обработки одной фазы;
- world transaction / rollback.

Подключение processor к общему pipeline
и контроль продвижения времени относятся к 14.8.

Конкретные persistent processes появятся
на соответствующих игровых этапах.

## Реализация 14.8.1. Immutable phase journal

Добавлены OrderPhaseJournalEntry и PhaseResolutionResult.

Журнал хранит immutable результаты Order commits,
диагностику отклонённых Orders, идентификаторы выбранных
Activities и обработанных persistent processes.

Выбор Activity не считается фактическим исполнением.

PhaseResolutionResult не хранит mutable
OrderExecutionState и не владеет TurnCycle.

Записи упорядочены детерминированно, но порядок
не определяет игровую инициативу.

Единый TurnPhaseResolver, защита от повторной обработки
и управление изменениями мира относятся к 14.8.2.

## Реализация 14.8.2. TurnPhaseResolver

Добавлен TurnPhaseResolver для базового execution pipeline.

Последовательность:

1. Проверка TurnCycle и ConfirmedTurnPlan.
2. Построение Order intents и Activity intents.
3. Read-only execution assessment.
4. Conflict resolution.
5. Подготовка execution progress на копии registry.
6. Формирование и проверка PhaseResolutionResult.
7. Продвижение TurnCycle.
8. Публикация подготовленного execution registry.

Resolver не выполняет конкретные world actions.

Для persistent processes пока используется
NoOpPersistentWorldPhaseProcessor.

Однократность обработки обеспечивается проверкой
ожидаемой Action Phase и продвижением TurnCycle.

Runtime progress Orders может переживать TurnEnd.

Произвольная транзакционная world mutation,
Activity execution и конкретные игровые Orders
подключаются на соответствующих следующих этапах.

## Stage 15.7.2 — Persistent phase preparation boundary

TurnPhaseResolver принимает необязательный
IPersistentWorldPhaseProcessor.

По умолчанию используется
NoOpPersistentWorldPhaseProcessor.Instance.

Контракт текущей точки расширения изменён:

AdvancePhase → PreparePhase.

PreparePhase допускает только read-only подготовку
результата Action Phase.

Изменение Bastion, TurnCycle, OrderExecutionRegistry
и иных mutable world states в PreparePhase запрещено
контрактом.

Если подготовка завершается исключением либо возвращает
результат другой фазы, основной commit не выполняется.

Повторная попытка обработки той же фазы допустима
после отказа подготовки.

Ограничения:

- конкретные persistent процессы пока отсутствуют;
- PersistentWorldPhaseResult сохраняет существующий
  формат ProcessedProcessIds;
- этот формат пока не подтверждает фактическую
  world mutation;
- отдельный world commit и общий rollback не реализованы;
- нельзя подключать сюда изменяющие мир процессоры
  снарядов, пожаров и других игровых эффектов.

Перед боевой Simulation контракт будет расширяться
на основании реальных требований к world execution.