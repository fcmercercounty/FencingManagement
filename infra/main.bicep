// Azure resources for the Fencing Club API: App Service (Linux, .NET 8), Azure SQL,
// Notification Hubs and Key Vault. Deploy with:
//   az deployment group create -g <resource-group> -f infra/main.bicep -p infra/main.parameters.json
// The web app signs in to SQL and Key Vault with its managed identity; no passwords in app settings.

@description('Short name used to build resource names, e.g. "fencingclub".')
@minLength(3)
@maxLength(12)
param appName string

param location string = resourceGroup().location

@description('App Service plan SKU. B1 is the cheapest tier that supports always-on.')
param appServiceSku string = 'B1'

@description('Azure SQL database SKU, e.g. Basic, S0, GP_S_Gen5_1 (serverless).')
param sqlDatabaseSku string = 'Basic'

@description('Entra ID user or group that administers the SQL server (display name / UPN).')
param sqlAdminLogin string

@description('Object id of that Entra ID user or group.')
param sqlAdminObjectId string

@description('HMAC key for signing JWTs, at least 32 characters.')
@secure()
@minLength(32)
param jwtSigningKey string

var suffix = uniqueString(resourceGroup().id)
var webAppName = '${appName}-api-${suffix}'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// --- Hosting ---
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan-${suffix}'
  location: location
  kind: 'linux'
  sku: { name: appServiceSku }
  properties: { reserved: true }
}

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: '/health'
    }
  }
}

resource webSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: web
  name: 'appsettings'
  properties: {
    ConnectionStrings__DefaultConnection: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=${db.name};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
    Database__Initialize: 'Migrate'
    Jwt__SigningKey: '@Microsoft.KeyVault(SecretUri=${jwtSecret.properties.secretUri})'
    NotificationHub__ConnectionString: '@Microsoft.KeyVault(SecretUri=${hubSecret.properties.secretUri})'
    NotificationHub__HubName: hub.name
  }
  dependsOn: [ kvAccess ]
}

// --- Database ---
resource sql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${appName}-sql-${suffix}'
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: sqlAdminLogin
      sid: sqlAdminObjectId
      tenantId: subscription().tenantId
      principalType: 'User'
    }
  }
}

resource db 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sql
  name: '${appName}-db'
  location: location
  sku: { name: sqlDatabaseSku }
}

// Lets App Service (and other Azure services) reach the server. Tighten with VNet integration later.
resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sql
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// --- Push notifications ---
resource hubNamespace 'Microsoft.NotificationHubs/namespaces@2023-09-01' = {
  name: '${appName}-push-${suffix}'
  location: location
  sku: { name: 'Free' }
  properties: { namespaceType: 'NotificationHub' }
}

resource hub 'Microsoft.NotificationHubs/namespaces/notificationHubs@2023-09-01' = {
  parent: hubNamespace
  name: '${appName}-hub'
  location: location
  properties: {}
}

resource hubFullAccess 'Microsoft.NotificationHubs/namespaces/notificationHubs/authorizationRules@2023-09-01' = {
  parent: hub
  name: 'ApiFullAccess'
  properties: { rights: [ 'Listen', 'Send', 'Manage' ] }
}

// --- Secrets ---
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${appName}-kv-${take(suffix, 8)}'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
  }
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'JwtSigningKey'
  properties: { value: jwtSigningKey }
}

resource hubSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'NotificationHubConnectionString'
  properties: { value: hubFullAccess.listKeys().primaryConnectionString }
}

resource kvAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, web.id, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: web.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output webAppName string = web.name
output webAppUrl string = 'https://${web.properties.defaultHostName}'
output sqlServer string = sql.properties.fullyQualifiedDomainName
output sqlDatabase string = db.name
