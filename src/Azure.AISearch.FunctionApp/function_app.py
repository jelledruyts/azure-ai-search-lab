import azure.functions as func
import os
import logging
import json
import jsonschema
from chunker.text_chunker import TextChunker
from chunker.chunk_metadata_helper import ChunkEmbeddingHelper

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

TEXT_CHUNKER = TextChunker()
CHUNK_METADATA_HELPER = ChunkEmbeddingHelper()

"""
Required environment variables:
"AZURE_OPENAI_API_KEY"
"AZURE_OPENAI_API_VERSION"
"AZURE_OPENAI_EMBEDDING_DEPLOYMENT"
"AZURE_OPENAI_SERVICE_NAME"

Optional environment variables:
"AZURE_OPENAI_EMBEDDING_SLEEP_INTERVAL_SECONDS" (default: 1)
"MIN_CHUNK_SIZE" (default: 10)
"NUM_TOKENS" (default: 2048)
"TOKEN_OVERLAP" (default: 0)
"API_KEY" (default: no API key required)
"""

@app.function_name(name="TextEmbedder")
@app.route(route="chunk-embed")
def text_chunking(req: func.HttpRequest) -> func.HttpResponse:

    logging.info('Python HTTP trigger function processed a request.')

    # Use basic API key authentication for demo purposes to avoid a dependency on the Function App keys
    api_key = os.getenv("API_KEY")
    if api_key and api_key != req.headers.get("authorization"):
        return func.HttpResponse(status_code=403)

    sleep_interval_seconds = int(os.getenv("AZURE_OPENAI_EMBEDDING_SLEEP_INTERVAL_SECONDS", "1"))
    num_tokens = int(os.getenv("NUM_TOKENS", "2048"))
    min_chunk_size = int(os.getenv("MIN_CHUNK_SIZE", "10"))
    token_overlap = int(os.getenv("TOKEN_OVERLAP", "0"))

    request = req.get_json()

    try:
        jsonschema.validate(request, schema=get_request_schema())
    except jsonschema.exceptions.ValidationError as e:
        return func.HttpResponse("Invalid request: {0}".format(e), status_code=400)

    values = []
    for value in request['values']:
        recordId = value['recordId']
        document_id = value['data']['document_id']
        text = value['data']['text']
        filepath = value['data']['filepath']
        fieldname = value['data']['fieldname']
        logging.info(f'Processing record "{recordId}":')
        logging.info(f'- document_id: "{document_id}"')
        logging.info(f'- filepath: "{filepath}"')
    
        # chunk documents into chunks of (by default) 2048 tokens, and for each chunk, generate the vector embedding
        logging.info(f'Chunking to {num_tokens} tokens (min chunk size is {min_chunk_size}, token overlap is {token_overlap}).')
        chunking_result = TEXT_CHUNKER.chunk_content(text, file_path=filepath, num_tokens=num_tokens, min_chunk_size=min_chunk_size, token_overlap=token_overlap)
        logging.info(f'Generating embeddings for {len(chunking_result.chunks)} chunks.')
        content_chunk_metadata = CHUNK_METADATA_HELPER.generate_chunks_with_embedding(document_id, [c.content for c in chunking_result.chunks], fieldname, sleep_interval_seconds)

        for document_chunk, embedding_metadata in zip(chunking_result.chunks, content_chunk_metadata):
            document_chunk.embedding_metadata = embedding_metadata

        values.append({
            "recordId": recordId,
            "data": chunking_result,
            "errors": None,
            "warnings": None
        })


    response_body = { "values": values }

    logging.info(f'Python HTTP trigger function created {len(chunking_result.chunks)} chunks.')

    response = func.HttpResponse(json.dumps(response_body, default=lambda obj: obj.__dict__))
    response.headers['Content-Type'] = 'application/json'    
    return response

def get_request_schema():
    return {
        "$schema": "http://json-schema.org/draft-04/schema#",
        "type": "object",
        "properties": {
            "values": {
                "type": "array",
                "minItems": 1,
                "items": {
                    "type": "object",
                    "properties": {
                        "recordId": {"type": "string"},
                        "data": {
                            "type": "object",
                            "properties": {
                                "text": {"type": "string", "minLength": 1},
                                "document_id": {"type": "string", "minLength": 1},
                                "filepath": {"type": "string", "minLength": 1},
                                "fieldname": {"type": "string", "minLength": 1}
                            },
                            "required": ["text", "document_id", "filepath", "fieldname"],
                        },
                    },
                    "required": ["recordId", "data"],
                },
            }
        },
        "required": ["values"],
    }