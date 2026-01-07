variable "keyvault_name" {
  type        = string
  description = "Name of the Key Vault (must be globally unique, 3-24 chars)"
}

resource "azurerm_key_vault" "kv" {
  name                       = var.keyvault_name
  location                   = var.location
  resource_group_name        = azurerm_resource_group.rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = var.env == "prod" ? true : false
}

# Grant access to whoever is running Terraform
resource "azurerm_key_vault_access_policy" "kv_access_policy" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id


  certificate_permissions = [
    "Get",
    "List",
    "Create",
    "Delete",
    "Recover",
    "Backup",
    "Restore"
  ]

  key_permissions = [
    "Get",
    "List",
    "Create",
    "Delete",
    "Recover",
    "Backup",
    "Restore"
  ]

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
    "Recover",
    "Backup",
    "Restore",
    "Purge"
  ]
}

# Cosmos Tenant secrets
resource "azurerm_key_vault_secret" "tenant_id" {
  name         = "TenantId"
  value        = data.azurerm_client_config.current.tenant_id
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_key_vault_access_policy.kv_access_policy]
}

resource "azurerm_key_vault_secret" "client_id" {
  name         = "ClientId"
  value        = data.azurerm_client_config.current.client_id
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_key_vault_access_policy.kv_access_policy]
}

# Cosmos DB secrets
resource "azurerm_key_vault_secret" "cosmosdb_endpoint" {
  name         = "CosmosDbEndpoint"
  value        = azurerm_cosmosdb_account.cosmosdb_account.endpoint
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_key_vault_access_policy.kv_access_policy]
}

resource "azurerm_key_vault_secret" "cosmosdb_key" {
  name         = "CosmosDbPrimaryKey"
  value        = azurerm_cosmosdb_account.cosmosdb_account.primary_key
  key_vault_id = azurerm_key_vault.kv.id
  depends_on   = [azurerm_key_vault_access_policy.kv_access_policy]
}

# Output the Key Vault URI
output "key_vault_uri" {
  value = azurerm_key_vault.kv.vault_uri
}
