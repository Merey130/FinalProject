# StudyFlow backend: тестирование API

Все URL ниже локальные — **только при запущенном .NET сервере в режиме Development**.

| Метод | URL | Назначение |
|---|---|---|
| GET | `/api/health` | Сервер запущен |
| GET | `/api/db-health` | Есть соединение с Supabase |
| GET | `/api/tasks` | Задания |
| POST | `/api/tasks` | Добавить задание |
| PUT | `/api/tasks/{id}` | Изменить задание |
| PATCH | `/api/tasks/{id}/toggle` | Выполнено/не выполнено |
| DELETE | `/api/tasks/{id}` | Удалить задание |
| GET | `/api/progress` | Общая статистика заданий |
| GET | `/api/students` | Ученики (только тестовые данные) |
| POST | `/api/students` | Создать тестового ученика |
| GET | `/api/students/{id}` | Один ученик |
| GET | `/api/exams?studentId=1` | Пробные тесты ученика |
| POST | `/api/exams` | Сохранить пробный тест |
| DELETE | `/api/exams/{id}` | Удалить пробный тест |
| GET | `/api/students/{id}/progress` | Лучший/последний пробный и предметные баллы |

Проверка GET в браузере:

- http://localhost:5011/api/health
- http://localhost:5011/api/db-health
- http://localhost:5011/api/tasks
- http://localhost:5011/api/progress

**Примеры запросов в Mac/Linux Terminal:**

```bash
curl -X POST http://localhost:5011/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{"title":"Homework 1","subject":"Informatics","deadline":"2026-12-01"}'

curl -X POST http://localhost:5011/api/students \
  -H 'Content-Type: application/json' \
  -d '{"name":"Demo Student"}'
```

Скопировать `id` созданного ученика из ответа и указать его в следующем запросе (ниже пример с `1`):

```bash
curl -X POST http://localhost:5011/api/exams \
  -H 'Content-Type: application/json' \
  -d '{
    "studentId": 1,
    "title": "Trial exam",
    "takenOn": "2026-11-15",
    "subjects": [
      {"subject":"Mathematics","score":41,"maxScore":50},
      {"subject":"Informatics","score":39,"maxScore":50}
    ]
  }'
```

В примере общий балл — 80 из 100. Для Windows используйте REST Client в VS Code или PowerShell `Invoke-RestMethod`.

Не путать `/api/health` (сервер доступен) и `/api/db-health` (сама база доступна). Если БД не подключена, второй запрос вернёт HTTP 503.
