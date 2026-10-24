# Контракт данных WebSocket

Текущая версия: `fatigue.v1`. Все временные значения — секунды от начала сессии, числа с плавающей точкой. Поля, которые зависят от Emotiv, передаются только после проверки фактического ответа Cortex.

## От Unity к Python

```json
{"schema":"fatigue.v1","type":"session_start","session_id":"s-001","duration_s":60}
```

```json
{"schema":"fatigue.v1","type":"behavior_event","session_id":"s-001","t_s":12.4,"event":"target_response","target_id":"B","reaction_time_s":1.12,"correct":true}
```

```json
{"schema":"fatigue.v1","type":"gaze_event","session_id":"s-001","t_s":12.4,"target_id":"B","gaze_x":0.51,"gaze_y":0.44,"event":"fixation_end","duration_s":0.74,"dispersion":0.08,"distracted":false}
```

```json
{"schema":"fatigue.v1","type":"session_finish","session_id":"s-001","t_s":60.0}
```

## От Python к Unity

```json
{"schema":"fatigue.v1","type":"service_status","source":"live","state":"ready"}
```

```json
{"schema":"fatigue.v1","type":"analysis_progress","session_id":"s-001","state":"running","progress":0.65}
```

```json
{"schema":"fatigue.v1","type":"error","session_id":"s-001","code":"emotiv_unavailable","recoverable":true,"message":"Источник Emotiv недоступен"}
```

```json
{"schema":"fatigue.v1","type":"session_result","session_id":"s-001","fatigue_score":0.68,"class":"medium","features":{"reaction_time_mean_s":1.42,"errors":3,"missed_targets":2,"fixation_duration_mean_s":0.74,"emotiv_samples":180,"emotiv_met_mean":{"attention":0.5,"eng":0.5,"exc":0.5,"lex":0.0,"str":0.5,"rel":0.5,"int":0.5}},"source":"live"}
```

Допустимые классы первой версии: `low`, `medium`, `high`.

Стартовое окно и окно результатов находятся в Unity. Python отправляет данные и коды состояний, а Unity переводит их в состояния интерфейса. Python не формирует элементы UI.

## От Cortex к Python

Адаптер Cortex сохраняет исходное сообщение в диагностический лог и преобразует только подтверждённые поля в нормализованный внутренний формат:

```json
{"type":"emotiv_metrics","stream":"met","t_s":12.4,"metrics":{"attention":0.5,"eng":0.5,"exc":0.5,"lex":0.0,"str":0.5,"rel":0.5,"int":0.5}}
```

Подтверждённые поля потока `met`: `attention`, `eng`, `exc`, `lex`, `str`, `rel`, `int`.

В Live-режиме `features` дополнительно содержит `emotiv_samples` и `emotiv_met_mean`. Имена Emotiv сохраняются по фактическому Cortex-контракту.
