# Live-website

https://delightful-meadow-07e919603.1.azurestaticapps.net/

# ML-Wildboar Architecture

## System Overview Flowchart

```mermaid
flowchart TB
    subgraph External["External Services"]
        Gmail[Gmail API<br/>Email Attachments]
    end

    subgraph Azure["Microsoft Azure Cloud"]
        subgraph Security["Security & Secrets"]
            KeyVault[Azure Key Vault<br/>wildboarmodel2463753574<br/><br/>Stores:<br/>- Storage Connection String<br/>- Gmail Client ID/Secret<br/>- Gmail Refresh Token<br/>- App Insights Key]
        end

        subgraph Storage["Azure Storage Account<br/>wildboarmodel9792119636"]
            BlobStorage[(Blob Storage<br/><br/>Container: images<br/><br/>Stores:<br/>- Raw image files<br/>- JPG/PNG attachments)]
            TableStorage[(Table Storage<br/><br/>Table: images<br/><br/>Stores:<br/>- Image metadata<br/>- Processing status<br/>- Detection results<br/>- Confidence scores)]
        end

        subgraph Functions["Azure Functions"]
            ImageIngest[ImageIngest Function<br/>Timer: Daily 6 AM CET<br/>.NET 10<br/><br/>Slot: Production]
        end

        subgraph Container["Docker Container"]
            ImageProcessor[Image Processor<br/>Batch Worker<br/>.NET 10<br/><br/>Runs: On-Demand]
            MLModel[ML.NET ONNX Model<br/>Wildboar Detection<br/><br/>Threshold: 0.60]
        end

        subgraph WebApp["Azure Static Web Apps<br/>delightful-meadow-07e919603"]
            Dashboard[React Dashboard<br/>React 19.2.3<br/><br/>Features:<br/>- Charts & Analytics<br/>- Image Gallery<br/>- Filters]
            DashboardAPI[Dashboard API Functions<br/>.NET 8<br/><br/>Endpoints:<br/>- GetDetections<br/>- GetImages<br/>- GetImageSasToken]
        end

        AppInsights[Application Insights<br/>Monitoring & Telemetry<br/><br/>Tracks:<br/>- Custom Metrics<br/>- Request Traces<br/>- Errors & Failures]
    end

    %% Key Vault Connections
    KeyVault -.->|Provides Secrets| ImageIngest
    KeyVault -.->|Provides Secrets| ImageProcessor
    KeyVault -.->|Provides Secrets| DashboardAPI

    %% Data Flow - Ingestion Pipeline
    Gmail -->|OAuth2 Authentication<br/>Fetch Attachments| ImageIngest
    ImageIngest -->|Upload Image Blobs| BlobStorage
    ImageIngest -->|Save Metadata<br/>PartitionKey: yyyy-MM-dd<br/>RowKey: GUID<br/>IsProcessed: false| TableStorage
    ImageIngest -.->|Send Telemetry| AppInsights

    %% Data Flow - Processing Pipeline
    TableStorage -->|Query Unprocessed<br/>WHERE IsProcessed = false| ImageProcessor
    BlobStorage -->|Download Image Bytes| ImageProcessor
    ImageProcessor -->|ML Inference<br/>Input: Image<br/>Output: Label + Score| MLModel
    MLModel -->|Predictions| ImageProcessor
    ImageProcessor -->|Update Records<br/>IsProcessed: true<br/>ContainsWildboar: bool<br/>ConfidenceScore: 0.0-1.0| TableStorage
    ImageProcessor -.->|Send Metrics<br/>- ImagesProcessed<br/>- WildboarDetected<br/>- ProcessingFailures| AppInsights

    %% Data Flow - Dashboard
    Dashboard -->|HTTP Requests<br/>GET /api/detections<br/>GET /api/images<br/>GET /api/images/sas| DashboardAPI
    DashboardAPI -->|Query Metadata<br/>Filter by Date/Time<br/>Aggregate Stats| TableStorage
    DashboardAPI -->|Generate SAS Tokens<br/>Expiry: 60 minutes| BlobStorage
    DashboardAPI -->|JSON Response<br/>Time-series Data<br/>Image Gallery<br/>Signed URLs| Dashboard
    DashboardAPI -.->|Send Telemetry| AppInsights

    %% Styling
    classDef storage fill:#ff8c00,stroke:#cc6600,stroke-width:3px,color:#fff
    classDef compute fill:#50e6ff,stroke:#00b4d8,stroke-width:2px,color:#000
    classDef external fill:#7fba00,stroke:#5c8a00,stroke-width:2px,color:#fff
    classDef security fill:#d13438,stroke:#a02428,stroke-width:3px,color:#fff
    classDef monitor fill:#ffa500,stroke:#cc8400,stroke-width:2px,color:#000
    classDef web fill:#a4373a,stroke:#7d2b2d,stroke-width:2px,color:#fff

    class BlobStorage,TableStorage,Storage storage
    class ImageIngest,DashboardAPI,ImageProcessor,MLModel compute
    class Gmail external
    class KeyVault,Security security
    class AppInsights monitor
    class Dashboard,WebApp web
```

## Storage Account Details

```mermaid
flowchart TB
    subgraph StorageAccount["Azure Storage Account: wildboarmodel9792119636"]
        subgraph BlobService["Blob Service"]
            ImagesContainer[Container: images<br/><br/>Access: Private<br/>Protocol: HTTPS]

            subgraph BlobContent["Blob Content"]
                Blob1[Blob: messageId_image1.jpg]
                Blob2[Blob: messageId_image2.png]
                Blob3[Blob: messageId_imageN.jpg]
            end

            ImagesContainer --> BlobContent
        end

        subgraph TableService["Table Service"]
            ImagesTable[Table: images<br/><br/>Partition Strategy: Date-based<br/>yyyy-MM-dd]

            subgraph TableSchema["Entity Schema"]
                Schema["ImageRecord Entity<br/>─────────────<br/>PartitionKey: yyyy-MM-dd<br/>RowKey: GUID<br/>─────────────<br/>CapturedAt: DateTime<br/>BlobStorageUrl: string<br/>IsProcessed: bool<br/>ContainsWildboar: bool?<br/>ConfidenceScore: double?<br/>─────────────<br/>Timestamp: DateTime<br/>ETag: string"]
            end

            ImagesTable --> TableSchema
        end

        ConnectionString[Connection String<br/>─────────────<br/>DefaultEndpointsProtocol=https<br/>AccountName=wildboarmodel9792119636<br/>AccountKey=stored in Key Vault<br/>EndpointSuffix=core.windows.net]
    end

    KeyVault2[Azure Key Vault] -.->|Provides<br/>Connection String| ConnectionString

    classDef storage fill:#ff8c00,stroke:#cc6600,stroke-width:2px,color:#fff
    classDef security fill:#d13438,stroke:#a02428,stroke-width:2px,color:#fff
    classDef schema fill:#e8f4f8,stroke:#0078d4,stroke-width:1px,color:#000

    class StorageAccount,ImagesContainer,ImagesTable storage
    class KeyVault2 security
    class Schema,ConnectionString schema
```

## Key Vault Secret Management

```mermaid
flowchart LR
    subgraph KeyVault["Azure Key Vault<br/>wildboarmodel2463753574.vault.azure.net"]
        subgraph Secrets["Stored Secrets"]
            StorageConn[AzureStorage--ConnectionString<br/><br/>Value: DefaultEndpoints...]
            GmailClient[Gmail--ClientId<br/><br/>Value: Google OAuth Client ID]
            GmailSecret[Gmail--ClientSecret<br/><br/>Value: Google OAuth Secret]
            GmailRefresh[Gmail--RefreshToken<br/><br/>Value: OAuth Refresh Token]
            AppInsKey[ApplicationInsights--ConnectionString<br/><br/>Value: InstrumentationKey=...]
        end
    end

    subgraph Apps["Applications"]
        Ingest[ImageIngest Function<br/><br/>Needs:<br/>- Storage Connection<br/>- Gmail Credentials<br/>- App Insights]
        Processor[Image Processor<br/><br/>Needs:<br/>- Storage Connection<br/>- App Insights]
        DashAPI[Dashboard API<br/><br/>Needs:<br/>- Storage Connection<br/>- App Insights]
    end

    StorageConn -.->|Read at Startup| Ingest
    StorageConn -.->|Read at Startup| Processor
    StorageConn -.->|Read at Startup| DashAPI

    GmailClient -.->|OAuth| Ingest
    GmailSecret -.->|OAuth| Ingest
    GmailRefresh -.->|Token Refresh| Ingest

    AppInsKey -.->|Telemetry| Ingest
    AppInsKey -.->|Telemetry| Processor
    AppInsKey -.->|Telemetry| DashAPI

    classDef vault fill:#d13438,stroke:#a02428,stroke-width:3px,color:#fff
    classDef secret fill:#ffd6d8,stroke:#d13438,stroke-width:1px,color:#000
    classDef app fill:#50e6ff,stroke:#00b4d8,stroke-width:2px,color:#000

    class KeyVault vault
    class StorageConn,GmailClient,GmailSecret,GmailRefresh,AppInsKey secret
    class Ingest,Processor,DashAPI app
```

## Detailed Component Interactions

```mermaid
sequenceDiagram
    participant KV as Azure Key Vault
    participant Gmail as Gmail API
    participant Ingest as ImageIngest Function
    participant Blob as Blob Storage<br/>(images container)
    participant Table as Table Storage<br/>(images table)
    participant Processor as Image Processor
    participant ML as ML.NET Model
    participant User as User Browser
    participant React as React Dashboard
    participant API as Dashboard API
    participant AI as App Insights

    Note over Ingest: Daily @ 6 AM CET - Timer Trigger Fires

    Ingest->>KV: Request secrets<br/>(Storage, Gmail, AppInsights)
    KV-->>Ingest: Return secrets

    Ingest->>Gmail: Authenticate (OAuth2)<br/>with ClientId, Secret, RefreshToken
    Gmail-->>Ingest: Access Token

    Ingest->>Gmail: Fetch emails after last timestamp
    Gmail-->>Ingest: Email messages + attachments

    loop For each image attachment
        Ingest->>Blob: Upload blob to 'images' container<br/>Name: messageId_filename
        Blob-->>Ingest: Blob URL

        Ingest->>Table: Insert ImageRecord<br/>PartitionKey: yyyy-MM-dd<br/>RowKey: new GUID()<br/>IsProcessed: false<br/>CapturedAt: timestamp<br/>BlobStorageUrl: url
        Table-->>Ingest: Success

        Ingest->>AI: Log telemetry<br/>(Image ingested)
    end

    Note over Processor: Batch Execution - Runs on-demand

    Processor->>KV: Request secrets<br/>(Storage, AppInsights)
    KV-->>Processor: Return secrets

    Processor->>Table: Query WHERE IsProcessed = false<br/>ORDER BY CapturedAt
    Table-->>Processor: List of unprocessed ImageRecords

    loop For each unprocessed image
        Processor->>Blob: Download blob by URL
        Blob-->>Processor: Image bytes

        Processor->>ML: Predict(imageBytes)
        ML-->>Processor: Label + Confidence (0.0-1.0)

        alt Confidence >= 0.60 threshold
            Processor->>Table: Update ImageRecord<br/>IsProcessed: true<br/>ContainsWildboar: true/false<br/>ConfidenceScore: value
        else Low confidence
            Processor->>Table: Update ImageRecord<br/>IsProcessed: true<br/>ContainsWildboar: false<br/>ConfidenceScore: value
        end

        Table-->>Processor: Update success
        Processor->>AI: Log metric<br/>(ImagesProcessed++,<br/>WildboarDetected if true)
    end

    Note over User,API: User Accesses Dashboard

    User->>React: Navigate to Dashboard

    React->>API: GET /api/detections?<br/>startDate=2025-01-01&<br/>endDate=2025-01-31&<br/>minConfidence=0.6

    API->>KV: Request Storage secret
    KV-->>API: Connection string

    API->>Table: Query images<br/>WHERE PartitionKey >= startDate<br/>AND PartitionKey <= endDate<br/>AND ConfidenceScore >= 0.6<br/>GROUP BY hour
    Table-->>API: Aggregated detection data

    API->>AI: Log request trace
    API-->>React: JSON time-series data
    React-->>User: Display charts

    User->>React: Filter image gallery<br/>(date, wildboar only)

    React->>API: GET /api/images?<br/>date=2025-01-15&<br/>containsWildboar=true

    API->>Table: Query images<br/>WHERE PartitionKey = 2025-01-15<br/>AND ContainsWildboar = true<br/>AND IsProcessed = true
    Table-->>API: List of matching ImageRecords

    API-->>React: JSON gallery data<br/>(metadata + URLs)
    React-->>User: Display image thumbnails

    User->>React: Click image to enlarge

    React->>API: GET /api/images/sas?<br/>blobUrl=https://...&<br/>expiryMinutes=60

    API->>Blob: Generate SAS token<br/>(read permission, 60 min expiry)
    Blob-->>API: Signed URL with token

    API-->>React: SAS URL
    React->>Blob: Fetch image with SAS token
    Blob-->>React: Image bytes
    React-->>User: Display full-size image
```

## Data Lifecycle

```mermaid
stateDiagram-v2
    [*] --> EmailReceived: Gmail sends email<br/>with image attachment

    EmailReceived --> StoredInBlob: ImageIngest Function<br/>uploads to Blob Storage

    StoredInBlob --> MetadataCreated: ImageIngest Function<br/>creates Table entity<br/>IsProcessed - false

    MetadataCreated --> PendingProcessing: Waiting for<br/>Image Processor

    PendingProcessing --> Processing: Image Processor<br/>downloads blob

    Processing --> MLInference: ML.NET model<br/>analyzes image

    MLInference --> ResultStored: Update Table entity<br/>IsProcessed - true<br/>ContainsWildboar - bool<br/>ConfidenceScore - double

    ResultStored --> AvailableInDashboard: Dashboard API<br/>can query results

    AvailableInDashboard --> DisplayedToUser: React Dashboard<br/>shows in gallery/charts

    DisplayedToUser --> [*]

    note right of MetadataCreated
        Table Storage Entity:
        PartitionKey: yyyy-MM-dd
        RowKey: GUID
        BlobStorageUrl: URL
        IsProcessed: false
    end note

    note right of ResultStored
        Updated Entity:
        IsProcessed: true
        ContainsWildboar: true/false
        ConfidenceScore: 0.0-1.0
    end note
```

## Deployment Pipeline

```mermaid
flowchart TB
    subgraph Dev["Development"]
        Code[Source Code<br/>Local Development]
    end

    subgraph GH["GitHub Actions CI/CD"]
        Trigger{Push to main?}

        BuildFunc[Workflow: deploy-functions.yml<br/><br/>Build .NET 10<br/>Functions/ImageIngest]

        BuildDocker[Workflow: build-image-processor.yml<br/><br/>Build Docker Image<br/>ImageProcessing/]

        BuildSWA[Workflow: deploy-dashboard-swa.yml<br/><br/>Build React + API<br/>Web/Dashboard + Functions]
    end

    subgraph Registry["Container Registry"]
        DockerHub[Docker Hub<br/>ml-wildboar-processor<br/><br/>Tags:<br/>- latest<br/>- main<br/>- commit SHA]
    end

    subgraph AzureDeploy["Azure Deployment Targets"]
        FuncApp[Azure Functions<br/>imageingest<br/><br/>OIDC Auth:<br/>- Client ID<br/>- Tenant ID<br/>- Subscription ID]

        SWA[Static Web Apps<br/>delightful-meadow-07e919603<br/><br/>Token Auth:<br/>- SWA Deploy Token]

        ContainerHost[Container Runtime<br/><br/>Options:<br/>- Azure Container Instances<br/>- Azure Kubernetes Service<br/>- Docker CLI]
    end

    Code -->|git push| Trigger

    Trigger -->|Functions/** changed| BuildFunc
    Trigger -->|ImageProcessing/** changed| BuildDocker
    Trigger -->|Web/** changed| BuildSWA

    BuildFunc -->|OIDC Deploy| FuncApp
    BuildDocker -->|Push Image| DockerHub
    BuildSWA -->|Deploy| SWA

    DockerHub -->|docker pull & run| ContainerHost

    FuncApp -.->|Uses| StorageAcct[Azure Storage Account]
    FuncApp -.->|Uses| KV[Azure Key Vault]
    ContainerHost -.->|Uses| StorageAcct
    ContainerHost -.->|Uses| KV
    SWA -.->|Uses| StorageAcct
    SWA -.->|Uses| KV

    classDef cicd fill:#2088ff,stroke:#0366d6,stroke-width:2px,color:#fff
    classDef azure fill:#0078d4,stroke:#004578,stroke-width:2px,color:#fff
    classDef storage fill:#ff8c00,stroke:#cc6600,stroke-width:2px,color:#fff
    classDef security fill:#d13438,stroke:#a02428,stroke-width:2px,color:#fff

    class BuildFunc,BuildDocker,BuildSWA,Trigger cicd
    class FuncApp,SWA,ContainerHost azure
    class StorageAcct storage
    class KV security
```

## Security Architecture

```mermaid
flowchart TB
    subgraph Internet["Internet / Public"]
        UserBrowser[User Browser<br/>HTTPS Only]
        GmailAPI[Gmail API<br/>OAuth2]
    end

    subgraph AzureSecurity["Azure Security Layer"]
        FW[Azure Firewall /<br/>Network Security]

        subgraph KeyVault["Azure Key Vault<br/>wildboarmodel2463753574"]
            RBAC[Role-Based Access Control<br/><br/>Principals:<br/>- ImageIngest Function MI<br/>- Image Processor MI<br/>- Dashboard API MI]

            Secrets2[Secrets Store<br/><br/>All connection strings<br/>and credentials]
        end

        subgraph Storage2["Azure Storage<br/>wildboarmodel9792119636"]
            StorageAuth[Authentication<br/><br/>Options:<br/>- Account Key via Key Vault<br/>- Managed Identity<br/>- SAS Tokens]

            BlobAccess[Blob Access<br/><br/>- Private Container<br/>- SAS tokens for read<br/>- 60 min expiry]

            TableAccess[Table Access<br/><br/>- Connection string auth<br/>- HTTPS only<br/>- Partition isolation]
        end
    end

    subgraph Apps2["Application Layer"]
        IngestFunc[ImageIngest Function<br/><br/>Managed Identity<br/>Key Vault access]

        ProcWorker[Image Processor<br/><br/>Managed Identity<br/>Key Vault access]

        DAPI2[Dashboard API<br/><br/>Managed Identity<br/>Key Vault access]
    end

    UserBrowser <-->|HTTPS<br/>TLS 1.2+| FW
    FW <-->|Secure| DAPI2

    GmailAPI <-->|OAuth2<br/>JWT tokens| IngestFunc

    IngestFunc -->|Get secrets| KeyVault
    ProcWorker -->|Get secrets| KeyVault
    DAPI2 -->|Get secrets| KeyVault

    KeyVault -->|Provide credentials| IngestFunc
    KeyVault -->|Provide credentials| ProcWorker
    KeyVault -->|Provide credentials| DAPI2

    IngestFunc -->|Authenticated| Storage2
    ProcWorker -->|Authenticated| Storage2
    DAPI2 -->|Generate SAS| BlobAccess
    DAPI2 -->|Query| TableAccess

    classDef security fill:#d13438,stroke:#a02428,stroke-width:3px,color:#fff
    classDef storage fill:#ff8c00,stroke:#cc6600,stroke-width:2px,color:#fff
    classDef app fill:#50e6ff,stroke:#00b4d8,stroke-width:2px,color:#000
    classDef public fill:#7fba00,stroke:#5c8a00,stroke-width:2px,color:#fff

    class KeyVault,RBAC,Secrets2,FW security
    class Storage2,BlobAccess,TableAccess,StorageAuth storage
    class IngestFunc,ProcWorker,DAPI2 app
    class UserBrowser,GmailAPI public
```

## Technology Stack

```mermaid
mindmap
  root((ML-Wildboar<br/>Wildboar Detection System))
    Azure Infrastructure
      Storage Account
        Blob Storage
          Container images
          SAS Token Access
        Table Storage
          Table images
          Partition Strategy
      Key Vault
        Connection Strings
        API Credentials
        OAuth Tokens
      Functions
        Timer Triggers
        HTTP Triggers
      Static Web Apps
        CDN Hosting
        API Integration
      Application Insights
        Telemetry
        Metrics
    Backend Services
      .NET 10.0
        ImageIngest Function
          Gmail API Client
          Image Extraction
        Image Processor
          Docker Container
          Worker Service
      .NET 8.0
        Dashboard API
          GetDetections
          GetImages
          GetImageSasToken
      ML/AI
        ML.NET
        ONNX Runtime
        TensorFlow
    Frontend
      React 19.2.3
      TypeScript
      TanStack Query
      Recharts Charts
      React Lightbox
    External APIs
      Gmail API v1
      Google OAuth2
      Azure SDK
    DevOps
      GitHub Actions
        OIDC Auth
        Multi-workflow
      Docker
        Multi-stage Build
        Docker Hub
      Monitoring
        App Insights
        Custom Metrics
```
