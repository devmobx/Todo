locals {
  common_tags = {
    Environment = "${var.env}"
    OrganizationName : "Devmobx"
    Project = "${var.project}"
    ProjectOwner : "Craig Chick"
    ProjectOwnerEmail : "craigchick@outlook.com"
  }
}