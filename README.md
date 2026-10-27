# Уровни изоляции транзакций: EF Core и PostgreSQL

Код к видео [«Уровни изоляции транзакций в SQL: от теории до кода»](https://www.youtube.com/watch?v=_IVRzj94z4w) с канала «Папочка Разработки».

Внутри сервис продажи билетов на концерт: ASP.NET Core 10, EF Core и PostgreSQL 17. Два покупателя берут одно место в одну секунду. Организатор открывает отчёт посреди продаж. Покупатель обходит лимит «четыре билета в одни руки» из двух вкладок. Каждую гонку запускает скрипт и показывает, что легло в базу: сначала с ошибкой из видео, потом с починкой.

У каждого раздела видео свой тег. Переключись на тег, и перед тобой код, который был на экране в конце раздела.

## Что нужно

- **Docker** — Docker Desktop или Docker Engine с Compose v2. Для запуска больше ничего не нужно, .NET ставить не обязательно.
- **bash и curl** — для сценариев из `scripts/`. На macOS и Linux они есть, на Windows — Git Bash (ставится вместе с Git) или WSL.
- **.NET 10 SDK** — только если запускаешь сервис из IDE.

## Быстрый старт

```bash
git clone https://github.com/dotnetdad/transaction-isolation-efcore.git
cd transaction-isolation-efcore
docker compose up --build
```

Первая сборка скачивает образы .NET и PostgreSQL, это несколько минут. Сервис готов, когда в логе появилась строка `Application started`. Он слушает `http://localhost:5080`, база — `localhost:5432` (пользователь, пароль и имя базы — `tickets`). При старте сервис применяет миграции и заводит места: концерт 7 — места 1–200, концерт 8 — места 201–400, всё по 3000.

На чистой базе первый старт пишет в лог одну строку `fail: Microsoft.EntityFrameworkCore.Database.Command` о запросе к `__EFMigrationsHistory`: EF Core ищет таблицу миграций до того, как создать её. Это не сбой, следующая строка лога — применение миграций.

Во втором терминале, в той же папке:

```bash
bash scripts/two-buyers.sh
```

Аня и Борис покупают место 14 в одну секунду. На `main` Аня получает 200, Борис — 409 «Место занято», место за Аней. На теге `00-start` оба получают 200, и место молча уходит Борису.

Одна покупка руками:

```bash
curl -i -X POST http://localhost:5080/buy -H 'Content-Type: application/json' \
  -d '{"concertId":7,"seatId":20,"phone":"+79990000001"}'
curl http://localhost:5080/concerts/7/report
```

В PowerShell:

```powershell
Invoke-RestMethod -Method Post http://localhost:5080/buy -ContentType application/json -Body '{"concertId":7,"seatId":20,"phone":"+79990000001"}'
Invoke-RestMethod http://localhost:5080/concerts/7/report
```

Остановить: Ctrl+C, потом `docker compose down -v`. Флаг `-v` удаляет базу, и следующий старт заведёт места заново.

## Теги

| Тег | Раздел видео | Сценарии |
|---|---|---|
| `00-start` | Вступление: место 14 продали двоим | `two-buyers.sh` |
| `01-conditional-update` | Место 14: UPDATE с условием | `two-buyers.sh` |
| `02-report-snapshot` | Отчёт организатора на одном снимке | `report-during-buy.sh` |
| `03-limit-serializable` | Лимит в одни руки: Serializable с повтором | `two-tabs.sh`, `start-of-sales.sh` |

В `main` код последнего раздела и этот README. Переключиться на раздел, например на первый:

```bash
docker compose down -v
git checkout 01-conditional-update
docker compose up --build
```

`down -v` нужен: у каждого тега своя схема, и создаётся она на старте. На теге лежит README того времени, полный — этот, в `main` (`git show main:README.md`). Вернуться: `git checkout main`.

## Как устроен код

- `Program.cs` — эндпоинты `POST /buy` и `GET /concerts/{id}/report`, EF Core с повтором при сбоях.
- `Purchases/` — покупка: `BuyHandler.cs` и формат телефона `Phones.cs`.
- `Reports/` — отчёт организатора (с тега `02`).
- `Sms/` — очередь СМС «Билет ваш» (с тега `03`), учебный провайдер пишет СМС в лог.
- `Data/` — контекст, сущность `Seat`, подготовка базы при старте; `Migrations/` — миграции EF Core.
- `Mistakes/` — анти-примеры из видео, по классу на ошибку.
- `Demo/PauseAfterQuery.cs` — пауза после запросов к базе, чтобы гонку было видно.
- `scripts/` — сценарии «Что попробовать».

## Пауза, анти-примеры, сценарии

**Пауза для гонок.** Гонка двух покупок длится миллисекунды, и руками её не поймать. Поэтому в `docker compose` сервис после каждого запроса к базе ждёт 500 мс (`Demo__PauseMs`, класс `Demo/PauseAfterQuery.cs`). В коде покупки её нет, она только раздвигает запросы во времени. Без паузы: `Demo__PauseMs=0 docker compose up`.

**Анти-примеры.** Ошибки разделов гайда лежат классами в `Mistakes/` и включаются настройкой `Mistakes__Имя=true`, где имя — класс: `Mistakes__LostUpdate=true docker compose up -d` (в PowerShell — `$env:Mistakes__LostUpdate="true"; docker compose up -d`, потом `Remove-Item env:Mistakes__LostUpdate`). Эндпоинт берёт `IBuyHandler`, а `Mistakes/MistakeSwitch.cs` регистрирует настоящий обработчик или анти-пример по флагу; включённый анти-пример сервис пишет в лог при старте. Включай по одному: два флага одного обработчика или флаг, которого на теге нет, — и сервис не стартует, а в логе написано почему. Вернуть настоящий обработчик — `docker compose up -d` без флага. Анти-пример воспроизводится на теге своего раздела и дальше.

**Сценарии «Что попробовать»** — скрипты в `scripts/` на bash и curl. Они сами сбрасывают места через `docker compose exec db psql` и печатают коды ответов и то, что лежит в базе. Запускай их из корня репозитория, когда сервис уже стартовал.

## Запуск из IDE

База — из compose, сервис — из IDE или консоли:

```bash
docker compose up -d --wait db
dotnet run --urls http://localhost:5080 --Demo:PauseMs=500
```

В Rider или Visual Studio открой `Tickets.csproj` и задай в конфигурации запуска переменные `ASPNETCORE_URLS=http://localhost:5080` и `Demo__PauseMs=500`: без паузы гонку не поймать. Строка подключения в `appsettings.json` уже смотрит на базу из compose. Анти-пример из консоли — `--Mistakes:LostUpdate=true`, из IDE — переменная `Mistakes__LostUpdate=true`.

Сценарии работают так же. Только счётчики СМС и повторов в `two-tabs.sh` и `start-of-sales.sh` читают лог контейнера `api` и покажут 0: смотри эти строки в консоли сервиса.

## Если что-то не так

- **Порт занят** (`port is already allocated`, `address already in use`) — у тебя уже работает свой PostgreSQL или другой сервис. Подними на других портах: `API_PORT=5090 DB_PORT=5433 docker compose up --build`, и сценарии на новом адресе: `API=http://localhost:5090 bash scripts/two-buyers.sh`. В PowerShell — `$env:API_PORT=5090; $env:DB_PORT=5433; docker compose up --build`.
- **Запросы на `localhost` висят, а на `127.0.0.1` отвечают** — порт на IPv6 (`::1`) держит другой процесс, на Windows часто WSL. Ходи на `127.0.0.1`: `API=http://127.0.0.1:5080 bash scripts/two-buyers.sh`. Из IDE то же для базы: `Host=127.0.0.1` в строке подключения.
- **Скрипт пишет `relation "seats" does not exist`** — сервис ещё не стартовал и не создал таблицы. Дождись `Application started` в логе.

## `00-start` — наивная покупка

**Что изменилось.** Сервис, таблица `seats` (`Data/Seat.cs`), эндпоинт `POST /buy`. Покупка (`Purchases/BuyHandler.cs`) открывает транзакцию, читает место, проверяет в C#, что оно свободно, записывает покупателя и фиксирует. Уровень в коде не указан, и Npgsql открывает транзакцию с Read Committed явно: `BEGIN TRANSACTION ISOLATION LEVEL READ COMMITTED`. Запрос без телефона — 400, ошибки — ProblemDetails.

**Что попробовать.** Аня и Борис покупают место 14 в одну секунду:

```bash
bash scripts/two-buyers.sh
```

Оба получают 200, а в базе место 14 за Борисом: покупка Ани затёрта, в логе ни одной ошибки. Это потерянное обновление из вступления.

**Дальше.**

- **Какой уровень на самом деле.** `SHOW transaction_isolation` внутри транзакции — её уровень, `SHOW default_transaction_isolation` — умолчание сессии. Умолчание меняют `ALTER DATABASE … SET default_transaction_isolation` и `ALTER ROLE … SET …`, но действует оно только на команды без своей транзакции: одиночный UPDATE, SaveChanges с одной командой. Транзакцию EF Core Npgsql открывает с уровнем прямо в BEGIN, и без уровня это Read Committed, какое бы умолчание ни стояло у базы. Увидеть, что уходит в базу, — `log_statement = 'all'` в PostgreSQL.
- **Уровень через пул подключений.** Npgsql задаёт уровень в BEGIN транзакции, а не SET сессии, поэтому через пул он не утекает. Справкой: в SQL Server уровень, выставленный на соединении, переживает возврат в пул; PgBouncer в режиме transaction — уровень только внутри транзакции.
- **Распределённая транзакция.** Две базы или два открытых подключения в одном `TransactionScope` поднимают транзакцию до распределённой; в .NET 7+ это поддержано только на Windows (EF Core, «Using transactions»). В проекте одна база и один контекст.
- **NOLOCK в SQL Server.** Кроме незафиксированных данных, чтение без блокировок при разбиении страниц может пропустить строку или прочитать её дважды; замена — `READ_COMMITTED_SNAPSHOT ON` или SNAPSHOT.

## `01-conditional-update` — место 14: UPDATE с условием

**Что изменилось.** Покупка (`Purchases/BuyHandler.cs`) — один UPDATE с условием через `ExecuteUpdateAsync`: «поставь покупателя туда, где место с этим номером, из этого концерта и ещё свободно». Своей транзакции и проверки в C# больше нет. Изменилась одна строка — «билет ваш», ноль строк — второй запрос отличает «место занято» (409) от «места нет» (404). Наивная покупка из вступления переехала в анти-пример `Mistakes/LostUpdate.cs`, рядом — `Mistakes/RepeatableReadBuy.cs`: та же покупка на Repeatable Read.

**Что попробовать.**

```bash
bash scripts/two-buyers.sh
```

Аня — 200, Борис — 409 «место занято», место за Аней. Место из другого концерта — 404:

```bash
curl -i -X POST http://localhost:5080/buy -H 'Content-Type: application/json'   -d '{"concertId":8,"seatId":14,"phone":"+79990000001"}'
```

Ошибки раздела:

- `Mistakes__LostUpdate=true docker compose up -d`, потом `bash scripts/two-buyers.sh` — оба 200, место за Борисом, как на теге `00-start`.
- `Mistakes__RepeatableReadBuy=true docker compose up -d`, потом `bash scripts/two-buyers.sh` — двойной продажи нет, место за Аней, но Борис получает 500, а в логе сервиса `40001: could not serialize access due to concurrent update`.

**Дальше.**

- **ExecuteUpdate идёт мимо трекера изменений.** Сущность `Seat`, уже загруженная в этот контекст, после `ExecuteUpdateAsync` остаётся старой: перечитай её или не смешивай в одном контексте.
- **Токен конкурентности xmin в текущем Npgsql** — свойство `uint` с `.IsRowVersion()` (или `[Timestamp]`); `UseXminAsConcurrencyToken` устарел.
- **Ограничения базы как последняя линия.** В модели проекта место — одна строка с одним владельцем, второй билет на место невозможен по схеме; лимит по телефону ограничением не выразить — его держит тег `03`.
- **Тест гонки.** Интеграционный тест с Testcontainers: две параллельные транзакции на одно место, ожидание — одна покупка и один «место занято».

## `02-report-snapshot` — отчёт организатора на одном снимке

**Что изменилось.** Отчёт по концерту `GET /concerts/{id}/report` (`Reports/ReportHandler.cs`): итог по залу одним запросом (продано, выручка, свободно) и список проданных мест для входа вторым. Оба запроса — в одной транзакции Repeatable Read, и читают один снимок. Тот же отчёт без транзакции — анти-пример `Mistakes/ReportTwoSnapshots.cs`.

**Что попробовать.** На концерте 7 продано 120 мест из 200; организатор открывает отчёт, а между двумя его запросами Аня покупает место 14:

```bash
bash scripts/report-during-buy.sh
```

Отчёт посреди покупки: продано 120, в списке на вход 120 — итог и список сходятся, покупка Ани в снимок не попала. Следующий отчёт видит 121.

Ошибка раздела: `Mistakes__ReportTwoSnapshots=true docker compose up -d`, потом `bash scripts/report-during-buy.sh` — итог за 120 мест, а в списке на вход 121: у каждого запроса свой снимок, и между ними прошла покупка. Ошибки в логе нет.

**Дальше.**

- **Долгий отчёт.** `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE, READ ONLY, DEFERRABLE` — транзакция только на чтение ждёт безопасный снимок и потом не получает 40001 и не мешает остальным; для тяжёлых выгрузок.
- **Отчёт с реплики.** Реплика отстаёт от основной базы: отчёт с неё согласован внутри себя, но может не видеть последних покупок.
- **Доступ к отчёту.** В отчёте телефоны всех покупателей, а эндпоинт открыт всем. В проде он за `RequireAuthorization` с политикой организатора, а телефон в списке на вход маскируется (`+7 999 ***-**-01`).
- **Концерт, которого нет.** Таблицы концертов в проекте нет, и отчёт по несуществующему концерту отдаёт 200 с нулями — для модели «места без концерта» это «ноль мест». С таблицей концертов отчёт сначала находит концерт и на пустой ответ отвечает 404 (`Results.Problem` со `statusCode: 404`).

## `03-limit-serializable` — лимит в одни руки: Serializable с повтором

**Что изменилось.**

- **Лимит.** Не больше четырёх билетов на телефон на концерт. Покупка (`Purchases/BuyHandler.cs`) — транзакция Serializable: сначала проверка «место уже за этим телефоном» (повтор после обрыва на коммите, который успел), потом подсчёт билетов телефона и тот же UPDATE с условием «место свободно». Эндпоинт приводит телефон к одному формату (E.164, `Purchases/Phones.cs`), иначе лимит обходится записью номера через 8; пустой или неразборный телефон — 400. Все ошибки — ProblemDetails.
- **Повтор.** Повтор при сбоях на весь контекст (`Program.cs`: 3 попытки, пауза до 1 с). Покупка целиком идёт внутри стратегии выполнения: на 40001 стратегия запускает метод заново, с новой транзакцией и новым снимком. Попытки кончились — 503 с `Retry-After: 1`.
- **СМС.** СМС «Билет ваш» ставится в очередь в памяти после стратегии, когда она вернула успех (`Sms/`): одна СМС на покупку, сколько бы ни было попыток. Учебный провайдер пишет СМС в лог.
- **Отчёт.** Отчёт из прошлого раздела тоже идёт через стратегию: с повтором на контексте транзакцию мимо неё открыть нельзя. Уровень у отчёта прежний, Repeatable Read.
- **Индекс.** Миграция `LimitIndex` — индекс по концерту и телефону только по проданным местам (`Data/TicketsDb.cs`), под подсчёт лимита.
- **Анти-примеры.** `Mistakes/LimitReadCommitted.cs` (подсчёт и запись на Read Committed), `Mistakes/SerializableNoRetry.cs` (Serializable без повтора), `Mistakes/ReportOutsideStrategy.cs` (отчёт с транзакцией мимо стратегии). При анти-примерах покупки — `LostUpdate`, `RepeatableReadBuy`, `LimitReadCommitted`, `SerializableNoRetry` — контекст регистрируется без повтора: они открывают транзакции мимо стратегии и иначе упали бы с InvalidOperationException, а не показали свой симптом. Анти-примеры отчёта повтор не трогают: `ReportTwoSnapshots` своей транзакции не открывает, а у `ReportOutsideStrategy` это исключение и есть симптом, поэтому вместе с анти-примером покупки он не включается.

**Что попробовать.** У Бориса три билета на концерт 7, и он покупает в двух вкладках сразу:

```bash
bash scripts/two-tabs.sh
```

Первая вкладка — 200, вторая — 409 «Лимит: 4 билета». У Бориса четыре билета, в логе одна СМС и один повтор стратегии: вторая вкладка получила 40001, повтор увидел четыре билета и ответил «лимит».

Место 14 теперь тоже покупается на Serializable: `bash scripts/two-buyers.sh` — Аня 200, Борис 409 «Место занято». В логе у Бориса 40001 и повтор: условие «место свободно» в UPDATE дало на повторе ноль строк.

Отчёт из прошлого раздела: `bash scripts/report-during-buy.sh` — итог и список по-прежнему сходятся, а с `Mistakes__ReportTwoSnapshots=true` итог за 120 мест и 121 в списке. Покупка на этом теге — три запроса, и скрипт запускает её раньше отчёта, чтобы фиксация пришлась между его запросами.

Ошибки раздела:

- `Mistakes__LimitReadCommitted=true docker compose up -d`, потом `bash scripts/two-tabs.sh` — обе вкладки 200, у Бориса пять билетов: write skew, ошибки в логе нет.
- `Mistakes__SerializableNoRetry=true docker compose up -d`, потом `bash scripts/two-tabs.sh` — лимит держится, четыре билета, но вторая вкладка получает 500, а в логе `40001: could not serialize access due to read/write dependencies among transactions`.
- `Mistakes__ReportOutsideStrategy=true docker compose up -d`, потом `curl -i http://localhost:5080/concerts/7/report` — 500, в логе `InvalidOperationException: The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions`.

Ложные конфликты и худший случай: `bash scripts/start-of-sales.sh` — восемь разных покупателей в одну секунду берут восемь разных мест. Часть из них получает 40001, хотя лимит друг друга они не трогают, и стратегия повторяет; у кого кончились попытки — 503 «Попробуйте ещё раз» с `Retry-After: 1`. Скрипт печатает и план подсчёта лимита: он идёт по индексу, но на тестовых данных весь индекс — одна страница, и все покупатели — соседи по ней. Пауза в 500 мс растягивает транзакции так, что они идут одновременно; с `Demo__PauseMs=0` повторы редки и 503 обычно нет.

**Дальше.**

- **Обрыв на коммите.** Проект проверяет в начале блока, не за этим ли телефоном уже место. Общий приём EF Core — `ExecuteInTransactionAsync` с `verifySucceeded` или ключ идемпотентности покупки (EF Core, «Connection resiliency», «Transaction commit failure and the idempotency issue»).
- **Ложные конфликты на маленькой таблице.** На тестовых данных планировщик читает таблицу целиком и при индексе — ложные 40001 остаются; на объёме прода индекс работает. Проверяй план подсчёта (`EXPLAIN`) на данных, похожих на прод. Размер предикатных блокировок — `max_pred_locks_per_transaction`.
- **Частота повторов в проде.** EF Core пишет предупреждение `CoreEventId.ExecutionStrategyRetrying` на каждый повтор — на нём метрика и алерт; иначе ложные конфликты заметят по 503.
- **Взаимные блокировки и ожидание.** Npgsql повторяет и 40P01; `lock_timeout` и `statement_timeout` ограничивают ожидание блокировки и запрос.
- **Контекст на попытку.** Покупка не держит отслеживаемых сущностей (`ExecuteUpdate`, `Count`); если в блоке есть `Add` и `SaveChanges`, на повторе в трекере останутся сущности прошлой попытки — сбрасывай `ChangeTracker.Clear()` или бери новый контекст.
- **Повтор запроса клиентом.** Двойной клик или повтор после таймаута проходит ту же проверку «место уже за этим телефоном»: ответ 200 и вторая СМС «Билет ваш». Убрать СМС с этой ветки нельзя — по ней же идёт повтор стратегии после обрыва на коммите, который успел, и СМС пропала бы. Дубли убирает ключ идемпотентности покупки или outbox с уникальностью по (концерт, место, телефон).
- **Надёжная СМС.** Очередь в памяти теряет СМС при падении сервиса между коммитом и отправкой; надёжно — outbox: запись о СМС в той же транзакции, что покупка.
- **Покупатель отдельной сущностью.** Телефон нормализуется в E.164 на входе; в настоящем сервисе покупатель или покупка — своя таблица, а не телефон в `seats.sold_to`.
- **SQL Server.** Аналог 40001 — 3960 (конфликт обновления на SNAPSHOT), взаимная блокировка — 1205; обе в списке временных ошибок стратегии SQL Server (`SqlServerTransientExceptionDetector`).
- **Тест гонки.** Testcontainers: две вкладки одного телефона на Serializable — один 40001, повтор, итог 4 билета.
- **Ключ блокировки по ключу.** `pg_advisory_xact_lock(concert_id, key)` — ключ из телефона считай одинаково во всех экземплярах сервиса: `string.GetHashCode` между процессами не стабилен — два запуска одной программы могут дать разные значения (документация `String.GetHashCode`, Remarks), и у двух экземпляров ключи разойдутся — блокировка их не упорядочит, лимит пробит без ошибки. Бери детерминированный хэш (например, `XxHash32` из `System.IO.Hashing`) или считай ключ в SQL; в проекте блокировки по ключу нет.
- **Текст отказа и константа лимита.** `"Лимит: 4 билета"` в эндпоинте и `MaxSeatsPerPhone` в покупке — две точки одного числа; в своём сервисе бери текст из той же константы или отдавай число полем ProblemDetails.

## Источники

Документация и исходники, на которые опирается видео: умолчания баз и драйверов, поведение уровней, решения команд PostgreSQL, Npgsql и EF Core. В порядке видео.

- [PostgreSQL: транзакция — всё или ничего](https://www.postgresql.org/docs/17/tutorial-transactions.html)
- [PostgreSQL: аномалии и уровни изоляции, Read Committed по умолчанию, перепроверка WHERE](https://www.postgresql.org/docs/17/transaction-iso.html)
- [PostgreSQL: версии строк (MVCC) — чтение не ждёт записи, запись не ждёт чтения](https://www.postgresql.org/docs/17/mvcc-intro.html)
- [PostgreSQL: как Serializable запоминает прочитанное (README-SSI)](https://github.com/postgres/postgres/blob/REL_17_11/src/backend/storage/lmgr/README-SSI)
- [PostgreSQL: ошибка сериализации на коммите (predicate.c)](https://github.com/postgres/postgres/blob/REL_17_11/src/backend/storage/lmgr/predicate.c#L4716)
- [PostgreSQL: отчёт на одном снапшоте, Serializable защищает только от транзакций того же уровня](https://www.postgresql.org/docs/17/applevel-consistency.html)
- [PostgreSQL: default_transaction_isolation](https://www.postgresql.org/docs/17/runtime-config-client.html)
- [MySQL InnoDB: Repeatable Read по умолчанию](https://dev.mysql.com/doc/refman/8.4/en/innodb-transaction-isolation-levels.html)
- [MySQL InnoDB: SELECT читает снапшот, UPDATE — последнюю закоммиченную версию](https://dev.mysql.com/doc/refman/8.4/en/innodb-consistent-read.html)
- [MySQL InnoDB: версии строк](https://dev.mysql.com/doc/refman/8.4/en/innodb-multi-versioning.html)
- [SQL Server: Read Committed на блокировках, READ_COMMITTED_SNAPSHOT, умолчание Azure SQL Database](https://learn.microsoft.com/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide)
- [SQL Server: NOLOCK — это READUNCOMMITTED](https://learn.microsoft.com/sql/t-sql/queries/hints-transact-sql-table)
- [SQL Server: ускоренное восстановление (ADR) — версии строк в самой базе](https://learn.microsoft.com/sql/relational-databases/accelerated-database-recovery-concepts)
- [Npgsql: транзакция без уровня открывается на Read Committed](https://github.com/npgsql/npgsql/blob/v10.0.3/src/Npgsql/NpgsqlTransaction.cs#L67)
- [EF Core: SaveChanges и своя транзакция](https://learn.microsoft.com/ef/core/saving/transactions)
- [EF Core 7: одна команда SaveChanges идёт без явной транзакции](https://learn.microsoft.com/ef/core/what-is-new/ef-core-7.0/whatsnew)
- [.NET: TransactionScope без параметров — Serializable](https://learn.microsoft.com/dotnet/framework/data/transactions/implementing-an-implicit-transaction-using-transaction-scope)
- [Npgsql: транзакция с уровнем, который попросил TransactionScope](https://github.com/npgsql/npgsql/blob/v10.0.3/src/Npgsql/VolatileResourceManager.cs#L43)
- [PostgreSQL: SELECT FOR UPDATE и блокировки по ключу (advisory lock)](https://www.postgresql.org/docs/17/explicit-locking.html)
- [PostgreSQL: VACUUM не убирает версии строк, пока их видит открытый снапшот](https://www.postgresql.org/docs/17/routine-vacuuming.html)
- [Npgsql EF Core: токен конкурентности xmin](https://www.npgsql.org/efcore/modeling/concurrency.html)
- [ASP.NET Core: ответы с ошибкой в API (ProblemDetails)](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)
- [EF Core: стратегия выполнения — повтор и транзакции](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)
- [Npgsql: ошибка сериализации 40001 — временный сбой](https://github.com/npgsql/npgsql/blob/v10.0.3/src/Npgsql/PostgresException.cs#L219)
- [Npgsql EF Core: стратегия выполнения с повтором](https://github.com/npgsql/efcore.pg/blob/v10.0.3/src/EFCore.PG/NpgsqlRetryingExecutionStrategy.cs)
- [EF Core: по умолчанию шесть повторов и пауза до 30 секунд](https://github.com/dotnet/efcore/blob/v10.0.13/src/EFCore/Storage/ExecutionStrategy.cs#L32)
- [EF Core: повторы кончились — RetryLimitExceededException](https://github.com/dotnet/efcore/blob/v10.0.13/src/EFCore/Storage/ExecutionStrategy.cs#L251)
