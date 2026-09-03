#!/usr/bin/env bash
# Drives the Chatter samples: brings up the shared RabbitMQ infra, then runs the requested demo.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"
ROOT="$(cd .. && pwd)"

usage() {
  cat <<EOF
Usage: ./run.sh <demo>

  cqrs             Chatter.Samples.Cqrs (no broker, no infra needed)
  rabbitmq-scan    Chatter.Samples.RabbitMq, default reflection-based scanning
  rabbitmq-explicit Chatter.Samples.RabbitMq, AOT-safe explicit registration
  rabbitmq-aot     Chatter.Samples.RabbitMq.Aot, published and run as Native AOT

Broker demos bring up docker-compose automatically. Run './run.sh down' to tear it down.
EOF
}

up() {
  docker compose up -d
  echo "waiting for rabbitmq..."
  for _ in $(seq 1 20); do
    status=$(docker inspect --format='{{.State.Health.Status}}' samples-rabbitmq-1 2>/dev/null || echo "")
    [ "$status" = "healthy" ] && return 0
    sleep 3
  done
  echo "rabbitmq did not become healthy in time" >&2
  exit 1
}

case "${1:-}" in
  cqrs)
    dotnet run --project "$ROOT/samples/Chatter.Samples.Cqrs"
    ;;
  rabbitmq-scan)
    up
    dotnet run --project "$ROOT/samples/Chatter.Samples.RabbitMq" -- scan
    ;;
  rabbitmq-explicit)
    up
    dotnet run --project "$ROOT/samples/Chatter.Samples.RabbitMq" -- explicit
    ;;
  rabbitmq-aot)
    up
    dotnet publish "$ROOT/samples/Chatter.Samples.RabbitMq.Aot" -c Release -r linux-x64
    "$ROOT/samples/Chatter.Samples.RabbitMq.Aot/bin/Release/net10.0/linux-x64/publish/Chatter.Samples.RabbitMq.Aot"
    ;;
  down)
    docker compose down
    ;;
  *)
    usage
    exit 1
    ;;
esac
