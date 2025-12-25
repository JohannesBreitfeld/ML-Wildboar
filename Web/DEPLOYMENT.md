# Deployment Guide - Azure Static Web Apps

This guide walks you through deploying the Wildboar Monitoring Dashboard to Azure Static Web Apps.

## Prerequisites

- Azure subscription
- GitHub account with this repository
- Azure CLI (optional, for command-line deployment)

## Option 1: Deploy via Azure Portal (Recommended)

### Step 1: Create Static Web App

1. Log in to [Azure Portal](https://portal.azure.com)

2. Click **Create a resource** → Search for **Static Web App** → Click **Create**

3. Fill in the basic details:
   - **Subscription**: Select your subscription
   - **Resource Group**: Create new or select existing
   - **Name**: `wildboar-dashboard` (or your preferred name)
   - **Plan type**: Free (for development) or Standard (for production)
   - **Region**: Select closest to you (e.g., West Europe)

4. Configure deployment:
   - **Source**: GitHub
   - Click **Sign in with GitHub** and authorize Azure
   - **Organization**: Select your GitHub account
   - **Repository**: Select `ML-Wildboar`
   - **Branch**: `main` (or your deployment branch)

5. Build details:
   - **Build Presets**: React
   - **App location**: `Web/ML-Wildboar.Dashboard`
   - **Api location**: `Web/ML-Wildboar.Functions.Dashboard`
   - **Output location**: `build`

6. Click **Review + Create** → **Create**

7. Wait for deployment (2-3 minutes)

### Step 2: Get Deployment Token

After creation:

1. Go to your Static Web App resource
2. Click **Manage deployment token**
3. Copy the token
4. Go to GitHub repository → **Settings** → **Secrets and variables** → **Actions**
5. Click **New repository secret**:
   - **Name**: `AZURE_STATIC_WEB_APPS_API_TOKEN`
   - **Value**: Paste the token
6. Click **Add secret**

### Step 3: Configure Application Settings

1. In Azure Portal, go to your Static Web App
2. Click **Configuration** → **Application settings**
3. Add the following settings:

   | Name | Value |
   |------|-------|
   | `AzureStorage:ConnectionString` | Your Azure Storage connection string |
   | `AzureStorage:TableName` | `images` |
   | `AzureStorage:BlobContainerName` | `images` |
   | `KeyVaultUri` | Your Key Vault URI (if using) |

4. Click **Save**

### Step 4: Deploy

The GitHub Action will trigger automatically on your next push to `main`. To trigger manually:

1. Go to GitHub repository → **Actions**
2. Click **Deploy Dashboard to Azure Static Web Apps**
3. Click **Run workflow** → Select branch → **Run workflow**

### Step 5: Verify Deployment

1. Go to Azure Portal → Your Static Web App
2. Copy the **URL** (e.g., `https://wildboar-dashboard-xxx.azurestaticapps.net`)
3. Open in browser
4. You should see the dashboard!

---

## Option 2: Deploy via Azure CLI

### Prerequisites
```bash
# Install Azure CLI
# Windows: https://aka.ms/installazurecliwindows
# Mac: brew install azure-cli
# Linux: curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login
az login

# Install Static Web Apps extension
az extension add --name staticwebapp
```

### Create and Deploy

```bash
# Set variables
RESOURCE_GROUP="wildboar-rg"
LOCATION="westeurope"
SWA_NAME="wildboar-dashboard"
GITHUB_REPO="https://github.com/YOUR_USERNAME/ML-Wildboar"
GITHUB_TOKEN="YOUR_GITHUB_PAT"

# Create resource group (if needed)
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Static Web App
az staticwebapp create \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --source $GITHUB_REPO \
  --location $LOCATION \
  --branch main \
  --app-location "Web/ML-Wildboar.Dashboard" \
  --api-location "Web/ML-Wildboar.Functions.Dashboard" \
  --output-location "build" \
  --login-with-github

# Get deployment token
DEPLOYMENT_TOKEN=$(az staticwebapp secrets list \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv)

echo "Add this to GitHub Secrets as AZURE_STATIC_WEB_APPS_API_TOKEN:"
echo $DEPLOYMENT_TOKEN

# Configure app settings
az staticwebapp appsettings set \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --setting-names \
    "AzureStorage:ConnectionString=YOUR_CONNECTION_STRING" \
    "AzureStorage:TableName=images" \
    "AzureStorage:BlobContainerName=images"
```

---

## Local Testing with SWA CLI

Before deploying, test locally:

```bash
# Install SWA CLI
npm install -g @azure/static-web-apps-cli

# From ML-Wildboar.Dashboard directory
cd Web/ML-Wildboar.Dashboard

# Start in one terminal
npm start

# In another terminal
npm run swa:start

# Or manually
swa start http://localhost:3000 --api-location ../ML-Wildboar.Functions.Dashboard

# Access at http://localhost:4280
```

---

## Continuous Deployment

### Automatic Deployment

The GitHub Actions workflow automatically deploys when you push to `main`:

```bash
git add .
git commit -m "Update dashboard"
git push origin main
```

### Manual Deployment

Trigger deployment manually from GitHub Actions:

1. Go to **Actions** tab
2. Select **Deploy Dashboard to Azure Static Web Apps**
3. Click **Run workflow**

---

## Environment Configuration

### Local Development

Create `Web/ml-wildboar-dashboard/.env.local`:

```env
REACT_APP_API_URL=http://localhost:7260/api
```

### Production

No `.env` file needed! Azure Static Web Apps automatically proxies `/api` to your Functions.

---

## Troubleshooting

### Build Fails

**Error**: `npm build failed`

**Solution**:
1. Check Node.js version (must be 18+)
2. Verify `package.json` scripts
3. Check build logs in GitHub Actions

### API Not Working

**Error**: `API calls return 404`

**Solution**:
1. Verify `api_location` in workflow is correct
2. Check Functions deployed: Azure Portal → Static Web App → Functions
3. Verify Application Settings (connection strings, etc.)

### Images Not Loading

**Error**: `Failed to load images`

**Solution**:
1. Check Azure Storage connection string in Application Settings
2. Verify blob container exists and has images
3. Check SAS token generation (API endpoint `/api/images/sas`)

### CORS Errors

**Error**: `CORS policy blocked`

**Solution**:
1. Verify `staticwebapp.config.json` has correct routes
2. Check Functions CORS configuration in `Program.cs`
3. Ensure using `/api` prefix (not full URL) in production

---

## Monitoring and Logs

### View Logs

**Azure Portal**:
1. Go to Static Web App → **Log Stream**
2. Or Application Insights → **Logs**

**GitHub Actions**:
1. Go to **Actions** tab
2. Click on latest workflow run
3. View build/deploy logs

### Application Insights

Enable detailed monitoring:

1. Create Application Insights resource
2. Add connection string to Functions `local.settings.json`:
   ```json
   "ApplicationInsights:ConnectionString": "YOUR_AI_CONNECTION_STRING"
   ```
3. Add to Azure Static Web App Application Settings

---

## Custom Domain (Optional)

### Add Custom Domain

1. In Azure Portal → Static Web App → **Custom domains**
2. Click **Add**
3. Choose domain type (custom domain or Azure DNS)
4. Follow DNS configuration steps
5. Validate and apply

### SSL Certificate

SSL is automatically configured for both `*.azurestaticapps.net` and custom domains.

---

## Scaling and Performance

### Free Tier Limits
- Bandwidth: 100 GB/month
- App size: 250 MB
- Storage: 0.5 GB

### Standard Tier Features
- Bandwidth: 100 GB/month (more available)
- Custom authentication
- SLA: 99.95%
- Staging environments

---

## Cost Estimation

### Free Tier
- Static Web App: **Free**
- Azure Functions: **Free** (1M requests/month)
- Storage: ~$0.02/GB/month
- **Total**: ~$0-5/month

### Standard Tier
- Static Web App: **$9/month**
- Azure Functions: **Included**
- Storage: ~$0.02/GB/month
- **Total**: ~$10-15/month

---

## Security Checklist

- [ ] Connection strings stored in Application Settings (not code)
- [ ] Use Azure Key Vault for secrets (production)
- [ ] Enable Application Insights for monitoring
- [ ] Configure custom domain with SSL
- [ ] Review `staticwebapp.config.json` security headers
- [ ] Implement authentication (if needed)
- [ ] Set up alerts for errors/performance

---

## Next Steps

After successful deployment:

1. **Set up monitoring**: Configure Application Insights alerts
2. **Add authentication**: Implement Azure AD B2C (optional)
3. **Custom domain**: Configure your domain
4. **CI/CD improvements**: Add automated tests
5. **Performance**: Enable CDN caching
6. **Backup**: Set up storage backup strategy

---

## Support

- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Functions Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/)

---

## Deployment Checklist

Before deploying to production:

- [ ] Azure Storage account configured with images
- [ ] Azure Key Vault set up (if using)
- [ ] GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN` configured
- [ ] Application Settings configured in Azure
- [ ] Local testing completed with SWA CLI
- [ ] Code pushed to main branch
- [ ] GitHub Actions workflow executed successfully
- [ ] Application accessible at Azure URL
- [ ] API endpoints responding correctly
- [ ] Images loading properly
- [ ] Charts displaying data
- [ ] Lightbox working
- [ ] Mobile responsive
