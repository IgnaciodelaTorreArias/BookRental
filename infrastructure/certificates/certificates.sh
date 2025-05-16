#!/bin/bash

cd $(dirname "$(realpath "$0")")

openssl genrsa -out BookRentalCA.key 4096
openssl req -x509 -new -nodes -key BookRentalCA.key -sha256 -days 1825 -out BookRentalCA.crt -config development.cnf
openssl genrsa -out services/APIGateway/APIGateway.key 4096
openssl req -new -key services/APIGateway/APIGateway.key -out services/APIGateway/APIGateway.csr -config services/APIGateway/development.cnf
openssl genrsa -out services/Inventory/Inventory.key 4096
openssl req -new -key services/Inventory/Inventory.key -out services/Inventory/Inventory.csr -config services/Inventory/development.cnf
openssl genrsa -out services/Rental/Rental.key 4096
openssl req -new -key services/Rental/Rental.key -out services/Rental/Rental.csr -config services/Rental/development.cnf

openssl x509 -req -in services/APIGateway/APIGateway.csr -CA BookRentalCA.crt -CAkey BookRentalCA.key -CAcreateserial -out services/APIGateway/APIGateway.crt -days 730 -sha256 -extfile services/APIGateway/endpoint.ext
openssl x509 -req -in services/Inventory/Inventory.csr -CA BookRentalCA.crt -CAkey BookRentalCA.key -CAserial BookRentalCA.srl -out services/Inventory/Inventory.crt -days 730 -sha256 -extfile services/Inventory/endpoint.ext
openssl x509 -req -in services/Rental/Rental.csr -CA BookRentalCA.crt -CAkey BookRentalCA.key -CAserial BookRentalCA.srl -out services/Rental/Rental.crt -days 730 -sha256 -extfile services/Rental/endpoint.ext

rm BookRentalCA.srl
rm services/APIGateway/APIGateway.csr
rm services/Inventory/Inventory.csr
rm services/Rental/Rental.csr

source ../../testing.env
openssl pkcs12 -export -out services/APIGateway/APIGateway.pfx -inkey services/APIGateway/APIGateway.key -in services/APIGateway/APIGateway.crt -certfile BookRentalCA.crt -password pass:$APIGATEWAY_PASS
openssl pkcs12 -export -out services/Inventory/Inventory.pfx -inkey services/Inventory/Inventory.key -in services/Inventory/Inventory.crt -certfile BookRentalCA.crt -password pass:$INVENTORY_PASS
openssl pkcs12 -export -out services/Rental/Rental.pfx -inkey services/Rental/Rental.key -in services/Rental/Rental.crt -certfile BookRentalCA.crt -password pass:$RENTAL_PASS

cp services/APIGateway/APIGateway.pfx ../../src/APIGateway
cp services/Inventory/Inventory.pfx ../../src/Inventory/Public
cp services/Inventory/Inventory.pfx ../../src/Inventory/Internal
cp BookRentalCA.crt ../../src/Inventory/Internal
cp services/Rental/Rental.pfx ../../src/RentalService
cp BookRentalCA.crt ../../src/RentalService