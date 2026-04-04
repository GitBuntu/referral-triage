az functionapp deployment source config-zip \
  --resource-group <GROUP_NAME> \
  --name <APP_NAME> \
  --src-path publish.zip

  OR

  1. Install required extensions:

Azure Functions extension (ms-azuretools.vscode-azurefunctions)
Azure Account extension (ms-vscode.azure-account)
2. Sign in to Azure:

Open the Command Palette (Ctrl+Shift+P)
Search for "Azure: Sign In" and follow the prompts
3. Publish the function app:

Open Command Palette (Ctrl+Shift+P)
Search for "Azure Functions: Deploy to Function App"
Select or create a Function App in Azure:
Choose your subscription
Create a new Function App (or select existing)
Provide a globally unique name
Choose runtime (Node.js, Python, etc.)
Choose location/region
Choose storage account (or create new)
