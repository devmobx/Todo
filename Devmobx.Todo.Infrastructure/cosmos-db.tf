variable "cosmosdb_account_name" {
  type        = string
  description = "Name of the Cosmos DB account"
}

variable "cosmosdb_database_name" {
  type        = string
  description = "Name of the Cosmos DB database"
}

# Cosmos DB Account - SERVERLESS
resource "azurerm_cosmosdb_account" "cosmosdb_account" {
  name                = var.cosmosdb_account_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"
  capabilities {
    name = "EnableServerless"
  }
  consistency_policy {
    consistency_level = "Session"
  }
  geo_location {
    location          = var.location
    failover_priority = 0
  }
  depends_on = [
    azurerm_resource_group.rg,
  ]
}

# Database - NO throughput setting for serverless
resource "azurerm_cosmosdb_sql_database" "cosmosdb_sql_database" {
  name                = var.cosmosdb_database_name
  resource_group_name = azurerm_cosmosdb_account.cosmosdb_account.resource_group_name
  account_name        = azurerm_cosmosdb_account.cosmosdb_account.name
}

# Container - NO throughput/autoscale settings for serverless
resource "azurerm_cosmosdb_sql_container" "cosmosdb_sql_container_todo_items" {
  name                  = "TodoItems"
  resource_group_name   = azurerm_cosmosdb_account.cosmosdb_account.resource_group_name
  account_name          = azurerm_cosmosdb_account.cosmosdb_account.name
  database_name         = azurerm_cosmosdb_sql_database.cosmosdb_sql_database.name
  partition_key_paths   = ["/userId"]
  partition_key_version = 2
}