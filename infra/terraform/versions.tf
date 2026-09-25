terraform {
  required_version = ">= 1.9"
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 4.0" }
  }
  # backend "azurerm" { ... }  # configure remote state before first apply — do not use local state
  backend "azurerm" {
    resource_group_name  = "rush-todo-tfstate-rg"
    storage_account_name = "rushtodotfstate8791"
    container_name        = "tfstate"
    key                    = "rush-todo.tfstate"
  }
}

provider "azurerm" {
  features {}
}
