winget install ConfluentInc.Confluent-CLI

confluent local kafka start --plaintext-ports 5550
confluent local kafka topic create book_management
confluent local kafka topic create book_available
confluent local kafka topic create book_returned

confluent local kafka topic create pending_confirmation
confluent local kafka topic create confirmed_rental