resource "azurerm_resource_group" "this" {
  name     = "${var.project}-${var.environment}-rg"
  location = var.location
}

resource "azurerm_container_registry" "this" {
  name                = replace("${var.project}${var.environment}acr", "-", "")
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku                 = "Basic"
  admin_enabled       = false # pull via AKS managed identity, not admin creds
}

# smallest viable node pool for a trial subscription — check quota in your region first (ADR-0014)
resource "azurerm_kubernetes_cluster" "this" {
  name                = "${var.project}-${var.environment}-aks"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  dns_prefix          = "${var.project}${var.environment}"
  sku_tier            = "Free"

  default_node_pool {
    name       = "system"
    node_count = 1
    vm_size    = "Standard_D4s_v5" # 4 vCPU, 16GB RAM — check current pricing/quota
  }

  identity { type = "SystemAssigned" }
}

resource "azurerm_role_assignment" "aks_pull" {
  scope                = azurerm_container_registry.this.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.this.kubelet_identity[0].object_id
}

resource "azurerm_postgresql_flexible_server" "this" {
  name                   = "${var.project}-${var.environment}-pg"
  resource_group_name    = azurerm_resource_group.this.name
  location               = azurerm_resource_group.this.location
  version                = "16"
  sku_name               = "B_Standard_B1ms" # smallest burstable tier — check current pricing/quota
  storage_mb              = 32768
  administrator_login    = "todoadmin"
  administrator_password = var.db_admin_password
  zone                   = "1"
}

resource "azurerm_postgresql_flexible_server_database" "this" {
  name      = "tododb"
  server_id = azurerm_postgresql_flexible_server.this.id
}

resource "azurerm_key_vault" "this" {
  name                = "${var.project}-${var.environment}-kv"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku_name            = "standard"
  tenant_id           = data.azurerm_client_config.current.tenant_id
}

data "azurerm_client_config" "current" {}
