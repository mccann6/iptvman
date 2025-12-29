# IptvMan

A simple IPTV proxy with filtering, caching, and a web-based management UI.

## Features

- **Web Management UI** - Configure accounts and manage category filters through a clean interface
- **Advanced Category Filtering** - Granular control over Live TV, VOD, and Series categories per account
- **Adult Content Filter** - Simple toggle to filter adult content
- **Caching** - Configurable caching for API calls, EPG, and M3U files
- **Multi-Account Support** - Manage multiple IPTV providers from one instance

## Quick Start

### Using Docker (Recommended)

Pull and run the latest image:
```bash
docker pull anthonymccann90/iptvman
docker run -p 8080:8080 anthonymccann90/iptvman
```

Or build from source:
```bash
docker build -t iptvman .
docker run -p 8080:8080 iptvman
```

Access the management UI at `http://localhost:8080`

### Configuration

1. Open `http://localhost:8080` in your browser
2. Click "Add Account" to create a new IPTV provider account
3. Configure filters by clicking "Manage Categories" for each stream type
4. Toggle adult filter as needed

#### Optional Environment Variables

```bash
ENV CACHE_TIME="60"        # API cache time in minutes (default: 60)
ENV EPG_TIME="1440"        # EPG cache time in minutes (default: 1440)
ENV M3U_TIME="720"         # M3U cache time in minutes (default: 720)
```

## Usage

Once configured, your accounts are available at:
- **Host**: `http://localhost:8080/AccountName`
- **Username**: Your Account Username
- **Password**: Your Account Password

Use these details in your IPTV player (TiviMate, Perfect Player, etc.)

## API Endpoints

- `GET /AccountName/player_api.php` - Xtream Codes compatible API
- `GET /AccountName/xmltv.php` - EPG data
- `GET /AccountName/get.php` - M3U playlist
- `GET /health` - Health check
- `GET /api/accounts` - Account management API

## Development

### Backend (.NET)
```bash
cd IptvMan
dotnet build
dotnet run
```

### Frontend (SvelteKit)
```bash
cd IptvManFrontend
npm install
npm run dev
```

## License

See LICENSE file for details.