# Quick Start Guide

Get the Wildboar Dashboard running locally in 5 minutes!

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 18+](https://nodejs.org/)
- Azure Storage Account (or [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite))

## Step 1: Configure Backend

```bash
cd Web/ML-Wildboar.Functions.Dashboard
```

Update `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureStorage:ConnectionString": "YOUR_CONNECTION_STRING_HERE",
    "AzureStorage:TableName": "images",
    "AzureStorage:BlobContainerName": "images"
  }
}
```

## Step 2: Configure Frontend

```bash
cd ../ML-Wildboar.Dashboard
```

Create `.env.local`:

```env
REACT_APP_API_URL=http://localhost:7260/api
```

## Step 3: Install Dependencies

```bash
# Install frontend dependencies
npm install
```

Backend dependencies are restored automatically on build.

## Step 4: Run

### Option A: Separate Terminals

**Terminal 1 - Backend**:
```bash
cd Web/ML-Wildboar.Functions.Dashboard
func start
# or
dotnet run
```

**Terminal 2 - Frontend**:
```bash
cd Web/ML-Wildboar.Dashboard
npm start
```

Access at `http://localhost:3000`

### Option B: Azure SWA CLI (Recommended)

```bash
# Install SWA CLI (once)
npm install -g @azure/static-web-apps-cli

# From ML-Wildboar.Dashboard directory
cd Web/ML-Wildboar.Dashboard

# Terminal 1 - Start React dev server
npm start

# Terminal 2 - Start SWA CLI
npm run swa:start
```

Access at `http://localhost:4280`

## Step 5: Test

1. **Dashboard loads**: Should see charts (may be empty without data)
2. **Click date range**: Select "Last 7 Days"
3. **Click a chart**: Should show "No images" message (normal without data)

## Adding Test Data

To see the dashboard in action, add some test data to your Azure Storage:

1. Upload images to blob container `images`
2. Add records to table `images` with:
   - `PartitionKey`: Date (yyyy-MM-dd)
   - `RowKey`: GUID
   - `BlobStorageUrl`: URL to blob
   - `CapturedAt`: DateTime
   - `ContainsWildboar`: true/false
   - `ConfidenceScore`: 0.0-1.0
   - `IsProcessed`: true

## Troubleshooting

### "Connection string not configured"
→ Check `local.settings.json` has correct Azure Storage connection

### API returns 404
→ Ensure Functions are running on port 7260

### CORS errors
→ Use SWA CLI (`npm run swa:start`) instead of direct `npm start`

### Images not loading
→ Verify blob storage has images and connection string is correct

## Next Steps

- [Read full README](../README.md)
- [Deploy to Azure](./DEPLOYMENT.md)
- Add real wildboar detection data
- Customize charts and filters

## Useful Commands

```bash
# Build frontend
npm run build

# Run tests
npm test

# Start with SWA CLI
npm run swa:start

# Build Azure Functions
cd ../ML-Wildboar.Functions.Dashboard
dotnet build
```

Enjoy your Wildboar Monitoring Dashboard! 🐗
