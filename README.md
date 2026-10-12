# TrialMicroservice

Two independently deployable microservices decomposed out of the `TrialModularMonolith` solution.

| Service | Port | Database | Owns |
| --- | --- | --- | --- |
| `TrialMicroservice.CoursesService` | http://localhost:5001 | `CoursesDb` (Postgres :5433) | `Courses`, `Lessons` |
| `TrialMicroservice.CommentsService` | http://localhost:5002 | `CommentsDb` (Postgres :5433) | `Comments` |

Shared infrastructure: Apache Kafka `localhost:9092`, Redis `localhost:6379`.

## Prerequisites

- .NET SDK 10
- Docker Desktop
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## 1. Start the infrastructure

```powershell
cd TrialMicroservice
docker compose up -d
```

`docker-compose.yml` starts **only PostgreSQL** (host port `5433`; `5432` is used by the
monolith). Kafka on `9092` and Redis on `6379` are expected to already exist on this machine
as the `local-kafka` / `local-redis` containers.

To bring up the entire stack from scratch on a clean machine, first stop anything on those
ports, then compose both files:

```powershell
docker compose -f docker-compose.yml -f docker-compose.infra.yml up -d
```

Wait for healthy:

```powershell
docker ps --filter "name=trial-postgres"
```

## 2. Create the Kafka topics (once)

```powershell
docker exec local-kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 `
  --create --if-not-exists --topic course-events   --partitions 1 --replication-factor 1
docker exec local-kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 `
  --create --if-not-exists --topic comment-events  --partitions 1 --replication-factor 1
```

## 3. Run both services concurrently

Open two terminals:

```powershell
# Terminal 1
cd TrialMicroservice\TrialMicroservice.CoursesService
dotnet run --project src\Api          # -> http://localhost:5001
```

```powershell
# Terminal 2
cd TrialMicroservice\TrialMicroservice.CommentsService
dotnet run --project src\Api          # -> http://localhost:5002
```

Each service applies its own migrations on startup. No service migrates another's database.

## 4. Endpoints

CoursesService (`:5001`)

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/courses` | `200` list with lesson counts |
| `GET` | `/api/courses/{id}` | `200` details, or `404` |
| `POST` | `/api/courses` | `201` + `Location`, publishes `CourseCreatedEvent` |

CommentsService (`:5002`)

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/comments/course/{courseId}` | `200` comments with the course title, or `404` if the course is unknown |
| `POST` | `/api/comments` | `201` + `Location`, or `404` if the course is unknown |

## 5. Verify the event-driven flow

```powershell
# 1. Publish a course. This writes CoursesDb and produces to course-events.
Invoke-RestMethod -Method Post -Uri http://localhost:5001/api/courses -ContentType application/json `
  -Body '{"title":"Redis Patterns","description":"Cache aside","lessons":[{"title":"TTL","content":"Expiry","order":1}]}'

# 2. The CommentsService consumer should have warmed Redis within a second.
docker exec local-redis redis-cli GET course:3      # -> {"courseId":3,"title":"Redis Patterns",...}
docker exec local-redis redis-cli TTL course:3      # -> ~600

# 3. Comment on it. Served from cache: no HTTP call to CoursesService.
Invoke-RestMethod -Method Post -Uri http://localhost:5002/api/comments -ContentType application/json `
  -Body '{"courseId":3,"author":"Budi","body":"Nice TTL demo"}'

# 4. Cache miss -> HTTP fallback -> re-cache.
docker exec local-redis redis-cli DEL course:3
Invoke-RestMethod -Uri http://localhost:5002/api/comments/course/3
docker exec local-redis redis-cli TTL course:3      # warmed again

# 5. Unknown course is rejected.
Invoke-RestMethod -Method Post -Uri http://localhost:5002/api/comments -ContentType application/json `
  -Body '{"courseId":999,"author":"Ghost","body":"nope"}'   # -> 404
```

Inspect the topics:

```powershell
docker exec local-kafka /opt/kafka/bin/kafka-console-consumer.sh `
  --bootstrap-server localhost:9092 --topic comment-events --from-beginning --timeout-ms 6000
```

Confirm the databases are genuinely separate (the Comments table must have **no** foreign key):

```powershell
'\d "Comments"' | docker exec -i trial-postgres psql -U admin -d CommentsDb
'\dt'          | docker exec -i trial-postgres psql -U admin -d CoursesDb
```

## How the services talk

```
CoursesService                                      CommentsService
     |                                                     |
  CoursesDb                                            CommentsDb
     |                  course-events (Kafka)               |
     +----------------------------> consumer warms Redis --+
     |                                                     |
     |<----- GET /api/courses/{id}  (only on cache miss) --+
     |                                                     |
  comment-events (Kafka) <----- POST /api/comments --------+
```

- `CommentsService` stores `CourseId` as a plain `int` and never references a `Course` entity,
  a `CourseDbContext`, or the CoursesService assembly.
- Course existence is proven through `ICourseCatalog`: Redis first, then HTTP with
  `IHttpClientFactory`, then a 600-second cache entry.
- `CourseCreatedEvent` is duplicated on the consumer side on purpose. Sharing one assembly for
  the contract would re-couple two services that must deploy independently.

## Layout

```
TrialMicroservice.CoursesService/
  TrialMicroservice.CoursesService.slnx
  src/Domain          Course, Lesson                          (no dependencies)
  src/Application     use cases, DTOs, ICourseStore, IEventPublisher
  src/Infrastructure  CourseDbContext, CourseStore, KafkaEventPublisher, DI
  src/Api             Program.cs, CoursesController, appsettings.json
```

`CommentsService` mirrors this, adding `Caching/RedisCourseCatalog.cs` and
`Messaging/KafkaCourseConsumerService.cs` in Infrastructure. The Application layer of each
service is a separate assembly, so the inward dependency direction is enforced by the compiler
rather than by convention.