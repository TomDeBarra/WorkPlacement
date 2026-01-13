# WorkPlacement
Work Placement Assignment

When the Grafana stack is installed via terminal + Docker, program can be run and it will locally send logs, traces and metrics to Tempo, Loki and Prometheus respectively. The controller makes this easier.
The sent logs, traces and matrices, sent every 5 seconds, can be observed via Grafana. 

How to work:
Requirements: Docker Desktop

How to run: 
Open terminal/powershell and type

"cd observability" and
"docker compose up -d --build"

Type URL "http://localhost:3000" into browser, login to Granafa (name: admin, password: admin)
Explore -> add Prometheus, Tempo and Loki "http://Prometheus:9090", "http://tempo:3200", "http://loki:3100"

Look at output in Prometheus, Tempo and Loki

Prometheus: worker_ticks_total

Loki: {job=~".+"} |= "Worker tick"

Tempo: { resource.service.name = "demo-worker" }
