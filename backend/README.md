# Backend

Backend автоматически запускается в Live-режиме через `start-live.cmd`.
Он принимает события Unity по WebSocket, собирает подтверждённые метрики Cortex
`met` и рассчитывает объяснимый индекс признаков утомления.

## Проверка
Тесты сервиса запускаются командой python -m unittest backend.test_server -v.
