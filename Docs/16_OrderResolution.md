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
1. Capture phase context
2. Build intents
3. Assess intents
4. Resolve conflicts
5. Commit
6. Activity fallback / system continuation
7. Persistent world processes
8. PhaseResolutionResult
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

Immutable Order отделён от execution state.

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
