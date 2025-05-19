#!/bin/bash
set -e

src=$(dirname "$(realpath "$0")")
cd $src
source ../../testing.env

# =====     Root      =====
openssl genrsa -des3 -passout pass:$BOOKRENTALCA_PASS -out BookRentalCA.key 4096
openssl req -x509 -new -nodes -key BookRentalCA.key -passin pass:$BOOKRENTALCA_PASS -sha256 -days 1825 -out BookRentalCA.crt -config development.cnf

#
# Services
#

# =====     APIGATEWAY      =====
cd $src
cd services/APIGateway
openssl genrsa\
    -out    APIGateway.key 4096
openssl req -new\
    -key    APIGateway.key\
    -out    APIGateway.csr\
    -config development.cnf
openssl x509 -req\
    -in     APIGateway.csr\
    -out    APIGateway.crt\
    -CA $src/BookRentalCA.crt -CAkey $src/BookRentalCA.key -passin pass:$BOOKRENTALCA_PASS -CAcreateserial\
    -days 730 -sha256 -extfile endpoint.ext
rm APIGateway.csr
openssl pkcs12 -export\
    -out    APIGateway.pfx\
    -inkey  APIGateway.key\
    -in     APIGateway.crt\
    -certfile $src/BookRentalCA.crt -password pass:$APIGATEWAY_PASS
cp APIGateway.pfx $src/../../src/APIGateway

# =====     Inventory       =====
cd $src
cd services/Inventory
openssl genrsa\
    -out    Inventory.key 4096
openssl req -new\
    -key    Inventory.key\
    -out    Inventory.csr\
    -config development.cnf
openssl x509 -req\
    -in     Inventory.csr\
    -out    Inventory.crt\
    -CA $src/BookRentalCA.crt -CAkey $src/BookRentalCA.key -passin pass:$BOOKRENTALCA_PASS -CAcreateserial\
    -days 730 -sha256 -extfile endpoint.ext
rm Inventory.csr
openssl pkcs12 -export\
    -out    Inventory.pfx\
    -inkey  Inventory.key\
    -in     Inventory.crt\
    -certfile $src/BookRentalCA.crt -password pass:$INVENTORY_PASS
cp Inventory.pfx $src/../../src/Inventory/Public
cp Inventory.pfx $src/../../src/Inventory/Internal
cp $src/BookRentalCA.crt $src/../../src/Inventory/Public
cp $src/BookRentalCA.crt $src/../../src/Inventory/Internal

# =====     Rental          =====
cd $src
cd services/Rental
openssl genrsa\
    -out    Rental.key 4096
openssl req -new\
    -key    Rental.key\
    -out    Rental.csr\
    -config development.cnf
openssl x509 -req\
    -in     Rental.csr\
    -out    Rental.crt\
    -CA $src/BookRentalCA.crt -CAkey $src/BookRentalCA.key -passin pass:$BOOKRENTALCA_PASS -CAcreateserial\
    -days 730 -sha256 -extfile endpoint.ext
rm Rental.csr
openssl pkcs12 -export\
    -out    Rental.pfx\
    -inkey  Rental.key\
    -in     Rental.crt\
    -certfile $src/BookRentalCA.crt -password pass:$RENTAL_PASS
cp Rental.pfx $src/../../src/RentalService
cp $src/BookRentalCA.crt $src/../../src/RentalService

#
# Databases
#

# =====     Embeddings      =====
cd $src
cd databases/embeddings
openssl genrsa\
    -out    embeddings.key 4096
openssl req -new\
    -key    embeddings.key\
    -out    embeddings.csr\
    -config development.cnf
openssl x509 -req\
    -in     embeddings.csr\
    -out    embeddings.crt\
    -CA $src/BookRentalCA.crt -CAkey $src/BookRentalCA.key -passin pass:$BOOKRENTALCA_PASS -CAcreateserial\
    -days 730 -sha256 -extfile endpoint.ext
rm embeddings.csr
cp embeddings.crt $src/../databases/embeddings
cp embeddings.key $src/../databases/embeddings
cp $src/BookRentalCA.crt $src/../databases/embeddings
cp embeddings.crt $src/../databases/embeddings/init
cp embeddings.key $src/../databases/embeddings/init
cp $src/BookRentalCA.crt $src/../databases/embeddings/init

cd $src
rm BookRentalCA.srl