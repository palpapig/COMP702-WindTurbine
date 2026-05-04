# Simulator

This folder hosts the OPC UA simulator for wind turbine data replay.

## Structure
- `config/`: simulator settings
- `data/`: source CSV datasets (Kelmarsh etc.)
- `server/`: simulator server implementation

## Quick Start
1. Put source CSV files under `data/`.
2. Update `config/simulator.settings.json`.
3. Run the server directly:
   - `dotnet run --project Simulator/server/Simulator.Server/Simulator.Server.csproj`

## Notes
- Simulator is separate from `Worker` so you can replace data source implementation later.
