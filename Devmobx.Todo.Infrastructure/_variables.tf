variable "project" {
  default     = "Todo"
  type        = string
  description = "Project Name"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
}

variable "default_location" {
  default     = "westeurope"
  type        = string
  description = "Default location for deployment"
}

variable "env" {
  type        = string
  description = "environment"
}