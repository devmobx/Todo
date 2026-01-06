variable "env" {
  type        = string
  description = "environment"
}

variable "project" {
  type        = string
  description = "Project Name"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
}

variable "location" {
  default     = "westeurope"
  type        = string
  description = "Deployment location"
}