# Azure AI Search Playground

https://github.com/Azure-Samples/azure-search-power-skills/

https://learn.microsoft.com/azure/ai-services/openai/concepts/models

At the time of writing (August 2023), the following regions allow the following GPT and embeddings models:

East US: gpt-35-turbo (0301), gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)
France Central: gpt-35-turbo (0301), gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)

Canada East: gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)
Japan East: gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)
North Central US: gpt-35-turbo (0613), gpt-35-turbo-16k (0613), text-embedding-ada-002 (version 2)

South Central US: gpt-35-turbo (0301), text-embedding-ada-002 (version 2)
West Europe: gpt-35-turbo (0301), text-embedding-ada-002 (version 2)

Note: if deployment fails with an error "Encountered an error (InternalServerError) from host runtime",
this means the keys of the Function App could not yet be retrieved as it is still being configured and deployed.
Simply redeploying the same template should work, as by then everything should have been finalized.
