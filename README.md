# Azure AI Search Playground

https://github.com/Azure-Samples/azure-search-power-skills/
https://github.com/Azure-Samples/azure-search-sample-data

https://learn.microsoft.com/azure/ai-services/openai/concepts/models

At the time of writing (August 2023), the following regions allow the following GPT and embeddings models:

East US: gpt-35-turbo (0301), gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)
France Central: gpt-35-turbo (0301), gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)

Canada East*: gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)
Japan East: gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)
North Central US: gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)

South Central US: gpt-35-turbo (0301), text-embedding-ada-002 (version 2)
West Europe: gpt-35-turbo (0301), text-embedding-ada-002 (version 2)

*Note: AI Enrichment in Azure Cognitive Search (to use skillsets) isn't available in this region, see
https://azure.microsoft.com/explore/global-infrastructure/products-by-region/?products=search&regions=all

Also note that when using gpt-35-turbo, only model version 0301 is supported for Azure OpenAI "On Your Data".