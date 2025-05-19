#!/bin/sh
set -e

echo 'Waiting for Embeddings database...'
until curl -s https://Embeddings:6333/collections\
    --cert ./embeddings.crt\
    --key ./embeddings.key\
    --cacert ./BookRentalCA.crt; do 
    sleep 1
done
echo 'Creating collection.'
curl -s -X PUT 'https://Embeddings:6333/collections/test_collection'\
    --cert ./embeddings.crt\
    --key ./embeddings.key\
    --cacert ./BookRentalCA.crt\
    --header 'Content-Type: application/json'\
    --data-raw '{"vectors":{"size": 384,"distance":"Cosine"}}'
curl -s https://Embeddings:6333/collections --cert ./embeddings.crt --key ./embeddings.key --cacert ./BookRentalCA.crt