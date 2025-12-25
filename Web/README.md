# Wildboar Monitoring Dashboard

A full-stack wildboar monitoring dashboard with React frontend and Azure Functions backend, deployed on Azure Static Web Apps.

## Architecture

- **Frontend**: React 18 + TypeScript with Recharts and yet-another-react-lightbox
- **Backend**: Azure Functions (.NET 10) with HTTP triggers
- **Storage**: Azure Table Storage + Azure Blob Storage
- **Deployment**: Azure Static Web Apps

## Project Structure

```
Web/
├── ML-Wildboar.Dashboard/          # React Frontend
│   ├── src/
│   │   ├── components/             # React components
│   │   │   ├── charts/             # Recharts visualizations
│   │   │   ├── gallery/            # Image gallery & lightbox
│   │   │   ├── filters/            # Date range picker
│   │   │   └── shared/             # Reusable components
│   │   ├── hooks/                  # Custom React hooks
│   │   ├── services/               # API client & caching
│   │   ├── types/                  # TypeScript definitions
│   │   ├── context/                # React Context
│   │   ├── pages/                  # Page components
│   │   └── utils/                  # Helper functions
│   ├── public/
│   └── package.json
│
└── ML-Wildboar.Functions.Dashboard/  # Azure Functions API
    ├── Functions/                  # HTTP endpoints
    │   ├── GetDetections.cs
    │   ├── GetImages.cs
    │   └── GetImageSasToken.cs
    ├── Models/                     # Response DTOs
    ├── Services/                   # (Future) Business logic
    ├── Extensions/                 # DI extensions
    └── Program.cs
```

## Features

### Dashboard Features
- **Interactive Charts**:
  - Time-series detection chart (click to filter)
  - Daily detection bar chart (stacked)
  - Hourly distribution line chart

- **Image Gallery**:
  - Grid view with thumbnails
  - Lightbox with full-size images
  - Metadata display (timestamp, confidence)
  - Keyboard navigation (←/→, ESC)

- **Filters**:
  - Date range (7 days, 30 days, custom)
  - Minimum confidence threshold
  - Wildboar detection filter

### API Endpoints

**GET /api/detections**
- Query params: `startDate`, `endDate`, `minConfidence`, `groupBy`
- Returns: Aggregated detection statistics

**GET /api/images**
- Query params: `date`, `startHour`, `endHour`, `containsWildboar`, `minConfidence`, `pageSize`
- Returns: Paginated image gallery with SAS tokens

**GET /api/images/sas**
- Query params: `blobUrl`, `expiryMinutes`
- Returns: Fresh SAS token for image access

## Local Development

### Prerequisites
- .NET 10 SDK
- Node.js 18+ and npm
- Azure Storage Emulator (Azurite) or Azure Storage account
- Azure Static Web Apps CLI (optional)

### Backend Setup

1. Configure storage connection:
```bash
cd Web/ML-Wildboar.Functions.Dashboard
```

2. Update `local.settings.json`:
```json
{
  "Values": {
    "AzureStorage:ConnectionString": "YOUR_CONNECTION_STRING",
    "AzureStorage:TableName": "images",
    "AzureStorage:BlobContainerName": "images"
  }
}
```

3. Run Azure Functions:
```bash
func start
# or
dotnet run
```

Functions will run on `http://localhost:7260`

### Frontend Setup

1. Install dependencies:
```bash
cd Web/ML-Wildboar.Dashboard
npm install
```

2. Configure API URL in `.env.local`:
```env
REACT_APP_API_URL=http://localhost:7260/api
```

3. Run development server:
```bash
npm start
```

Frontend will run on `http://localhost:3000`

### Full Stack with SWA CLI

Run both frontend and backend together:

```bash
# Install SWA CLI globally
npm install -g @azure/static-web-apps-cli

# From Web/ML-Wildboar.Dashboard directory
swa start http://localhost:3000 --api-location ../ML-Wildboar.Functions.Dashboard
```

Access at `http://localhost:4280`

## Deployment to Azure

### Prerequisites
- Azure subscription
- GitHub repository

### Steps

1. **Create Azure Static Web App**:
```bash
az staticwebapp create \
  --name wildboar-dashboard \
  --resource-group YOUR_RESOURCE_GROUP \
  --source https://github.com/YOUR_USERNAME/ML-Wildboar \
  --location "West Europe" \
  --branch main \
  --app-location "Web/ML-Wildboar.Dashboard" \
  --api-location "Web/ML-Wildboar.Functions.Dashboard" \
  --output-location "build"
```

2. **Configure GitHub Secret**:
   - Get deployment token from Azure Portal (Static Web App → Manage deployment token)
   - Add to GitHub: Settings → Secrets → New repository secret
   - Name: `AZURE_STATIC_WEB_APPS_API_TOKEN`
   - Value: (paste token)

3. **Configure Application Settings** in Azure Portal:
   - Navigate to: Static Web App → Configuration → Application settings
   - Add:
     ```
     AzureStorage:ConnectionString = <your-storage-connection>
     AzureStorage:TableName = images
     AzureStorage:BlobContainerName = images
     KeyVaultUri = <your-keyvault-uri> (optional)
     ```

4. **Deploy**:
   - Push to main branch or manually trigger workflow
   - GitHub Actions will build and deploy automatically

### GitHub Actions Workflow

The workflow (`.github/workflows/deploy-dashboard-swa.yml`) triggers on:
- Push to `main` branch (when Web/ files change)
- Pull requests
- Manual dispatch

## Configuration

### Static Web App Configuration

`staticwebapp.config.json` includes:
- SPA routing (fallback to index.html)
- API route handling
- Security headers
- MIME types
- Cache control

### Environment Variables

**Local Development** (`.env.local`):
```env
REACT_APP_API_URL=http://localhost:7260/api
```

**Production** (Azure configuration):
- API URL is automatically `/api` (proxied by SWA)
- No frontend env vars needed

## Technology Stack

### Frontend
- React 18
- TypeScript 5
- Recharts (charts)
- yet-another-react-lightbox (image viewer)
- @tanstack/react-query (data fetching & caching)
- React Context API (state management)

### Backend
- .NET 10
- Azure Functions v4 (isolated worker)
- Azure.Data.Tables (Table Storage SDK)
- Azure.Storage.Blobs (Blob Storage SDK)
- Application Insights (monitoring)

### Infrastructure
- Azure Static Web Apps
- Azure Functions
- Azure Table Storage
- Azure Blob Storage
- Azure Key Vault (secrets)
- GitHub Actions (CI/CD)

## Performance

**Build Output**:
- Frontend bundle: ~193 kB gzipped
- Initial load: Fast (CDN + code splitting)
- Chart rendering: Optimized with React.memo
- Image loading: Lazy loading + SAS token caching

## Security

- **API**: CORS restricted to Static Web App domains
- **Images**: Time-limited SAS tokens (1-hour expiry)
- **Secrets**: Stored in Azure Key Vault
- **Headers**: Security headers configured (CSP, X-Frame-Options, etc.)
- **Authentication**: Currently anonymous (can add Azure AD B2C)

## Troubleshooting

### API not accessible
- Check `staticwebapp.config.json` routes configuration
- Verify Azure Functions are deployed (check Deployment Center)
- Check Application Settings in Azure Portal

### Images not loading
- Verify Azure Storage connection string
- Check blob container permissions
- Ensure SAS token generation is working

### Build fails
- Check Node version (18+)
- Clear node_modules and rebuild: `npm ci`
- Check .NET SDK version (10.0)

## Future Enhancements

- [ ] Add authentication (Azure AD B2C)
- [ ] Implement pagination for large datasets
- [ ] Add export functionality (CSV, PDF)
- [ ] Real-time updates (SignalR)
- [ ] Mobile app (React Native)
- [ ] Advanced filters (date range picker, time slider)
- [ ] Image annotations
- [ ] Activity heatmap
- [ ] Email notifications for detections

## License

[Your License]

## Contributing

[Contribution guidelines]
