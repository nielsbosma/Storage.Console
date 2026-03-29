# Storage.Console

CLI tool for uploading files and folders to Azure Blob Storage with time-limited read-only SAS URLs.

## Install

```bash
dotnet tool install --global Storage.Console
```

## Usage

```bash
# Upload a file (SAS URL expires in 30 days)
storage upload ivy-tendril ./report.pdf

# Upload a folder (auto-zipped, excludes .git/bin/obj/node_modules)
storage upload ivy-tendril ./my-project

# Upload with custom expiry
storage upload ivy-tendril ./data.csv --expires 2026-06-01

# Force zip a single file
storage upload ivy-tendril ./large-file.log --zipped
```

## Configuration

Copy `src/example.config.yaml` to `src/config.yaml` and fill in your storage profiles:

```yaml
storage:
  ivy-tendril:
    provider: azure
    account_name: stivytelemetry
    container_name: ivy-tendril
```

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed and logged in (`az login`)
- Access to the target Azure Storage account

## How it works

1. Uses `az storage container generate-sas` to create a write SAS token for upload
2. Uploads the file/zip via the Azure Blob SDK
3. Generates a read-only SAS token with the specified expiry
4. Returns the full SAS URL

The URL stops working after the expiry date. To auto-delete blobs at expiry, configure an [Azure lifecycle management policy](https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview) on the storage account.
