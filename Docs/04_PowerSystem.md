# TDD-04. Энергетическая система

## Производство

PowerGenerationFeatureDefinition:

- MaximumPowerOutput;
- FuelConsumptionPerTurn.

## Непрерывное потребление

ModuleDefinition содержит:

- IdlePowerConsumption;
- ActivePowerConsumption.

ActivePowerConsumption является полной активной нагрузкой.

## Проектный баланс

BastionPowerBalance описывает номинальную конструкцию и содержит производство, ожидание, активный спрос, резервы и признаки устойчивости нагрузки.

## Режимы

Модуль различает:

- RequestedPowerMode — приказ игрока;
- EffectivePowerMode — фактически предоставленный режим.

Режимы:

- Offline;
- Standby;
- Active.

Распределитель может только понизить режим относительно запроса.

## Приоритеты

Порядок:

1. Critical;
2. High;
3. Normal;
4. Low.

При нехватке Active система пытается сохранить Standby, затем Offline.

Модули с нулевым расходом могут получить Active без затрат, но подчиняются ручному режиму.

## Операционный баланс

Уничтоженный или не контролируемый генератор не должен обеспечивать фактическую сеть. Распределение выполняется до операционной оценки модулей.

## Энергия действий

Будущие системы могут иметь EnergyPerShot, ChargePowerPerAction, RequiredChargeActions, StoredEnergy и CooldownActions.

Непрерывная нагрузка и импульсное потребление должны оставаться разными моделями.
